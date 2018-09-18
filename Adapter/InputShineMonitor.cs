using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Applctn.Http;
using Asst;
using Newtonsoft.Json.Linq;
using Observer;

namespace Adapter
{
    internal struct PlantPara
    {
        public string Query { get; }
        public string Key { get; }

        public PlantPara(string query, string key)
        {
            Query = query;
            Key = key;
        }
    }

    internal struct SMDeviceInfo
    {
        public ParaIndex[] paraIndices;

        public string Collector { get; }
        public int Code { get; }
        public int Address { get; }
        public string Serial { get; }

        public SMDeviceInfo(string collector, int code, int address, string serial) : this()
        {
            Collector = collector;
            Code = code;
            Address = address;
            Serial = serial;
        }
    }

    internal class InputShineMonitor : IInputAdapter
    {
        private const string OPEN_API_URI = @"http://api.shinemonitor.com/public/";
        private readonly string salt;
        private readonly string queryAuth;
        private readonly int plantID;
        private readonly PlantPara[] plantParas;
        private readonly SMDeviceInfo[] deviceInfos;
        private readonly Dictionary<string, int> WorkStatus;
        private string secret;
        private string token;
        private bool isConnect;

        private readonly InfoCenter ic;

        public event EventHandler<LogEventArgs> Log;

        public InputShineMonitor(string path,string[][] para)
        {
            salt = DateTime.Now.ToString("yyyyMMdd");

            IniFile ini = new IniFile(path + "\\ShineMonitor.ini");

            string pwd = ini.GetStr("ACCOUNT", "Password");
            string sha1Pwd = Sha1ToLower(pwd);
            string usr = ini.GetStr("ACCOUNT", "User");
            string encodeUsr = HttpUtility.UrlEncode(usr, Encoding.UTF8).ToUpper();
            string companyKey = ini.GetStr("ACCOUNT", "CompanyKey");
            string action = $"&action=auth&usr={encodeUsr}&company-key={companyKey}";
            string sign = Sha1ToLower(salt + sha1Pwd + action);
            queryAuth = $"{OPEN_API_URI}?sign={sign}&salt={salt}{action}";

            plantID = ini.GetInt("PLANT", "ID", 0);
            string[] queries = ini.GetStr("PLANT", "Query").Split(',');
            string[] keys = ini.GetStr("PLANT", "Key").Split(',');
            plantParas = new PlantPara[3];
            for (int i = 0; i < plantParas.Length; ++i) plantParas[i] = new PlantPara(queries[i], keys[i]);

            string[] collectorIDs = ini.GetStr("DEVICE", "CollectorID").Split(',');
            string[] deviceGroups = ini.GetStr("DEVICE", "Detail").Split('|');
            int collectorCount = collectorIDs.Length <= deviceGroups.Length ? collectorIDs.Length : deviceGroups.Length;
            List<SMDeviceInfo> lstDevices = new List<SMDeviceInfo>();
            for (int i = 0; i < collectorCount; ++i)
            {
                string[] devices = deviceGroups[i].Split(';');
                foreach (string device in devices)
                {
                    string[] details = device.Split(',');
                    lstDevices.Add(new SMDeviceInfo(collectorIDs[i], int.Parse(details[0]), int.Parse(details[1]), details[2]));
                }
            }
            deviceInfos = lstDevices.ToArray();

            string[] works = ini.GetStr("DEVICE", "Status").Split(',');
            WorkStatus = new Dictionary<string, int>();
            for (int i = 0; i < works.Length; ++i) WorkStatus.Add(works[i], i);

            List<ParaIndex>[] lstPara = new List<ParaIndex>[deviceInfos.Length];
            for (int i = 0; i < lstPara.Length; ++i) lstPara[i] = new List<ParaIndex>();

            for (int i = 0; i < para.Length; ++i)
            {
                int jIndex = int.Parse(para[i][0]);
                if (0 < jIndex) lstPara[int.Parse(para[i][2])].Add(new ParaIndex(i, jIndex, float.Parse(para[i][1])));
            }

            for (int i = 0; i < lstPara.Length; ++i) deviceInfos[i].paraIndices = lstPara[i].ToArray();

            ic = new InfoCenter(path);
        }

        private string Sha1ToLower(string message)
        {
            byte[] input = Encoding.UTF8.GetBytes(message);
            SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider();
            byte[] output = sha.ComputeHash(input);
            sha.Dispose();
            return BitConverter.ToString(output).Replace("-", string.Empty).ToLower();
        }

        private string MakeUrl(string action)
        {
            string sign = Sha1ToLower(salt + secret + token + action);
            string query = $"{OPEN_API_URI}?sign={sign}&salt={salt}&token={token}{action}";
            return query;
        }

        public bool Connect()
        {
            string authRet = HttpPub.HttpGet(queryAuth);
            ic.Gram(DateTime.Now, authRet);
            if (!HttpPub.IsValid(authRet, "err", 0))
            {
                isConnect = false;
                return false;
            }

            JToken jToken = JToken.Parse(authRet)["dat"];
            secret = jToken["secret"].ToString();
            token = jToken["token"].ToString();
            isConnect = true;
            return true;
        }

        public void DisConnect()
        {
            secret = null;
            token = null;
            isConnect = false;
        }

        public bool GetData(ref int[] update, float[] value)
        {
            List<int> lUpdate = new List<int>();

            for (int i = 0; i < plantParas.Length; ++i)
            {
                string action = $"&action={plantParas[i].Query}&plantid={plantID.ToString()}";
                string ret = HttpPub.HttpGet(MakeUrl(action));
                ic.Gram(DateTime.Now, ret);
                if (!HttpPub.IsValid(ret, "err", 0))
                {
                    return false;
                }

                try
                {
                    value[i] = Convert.ToSingle(JToken.Parse(ret)["dat"][plantParas[i].Key]);
                    lUpdate.Add(i);
                }
                catch (Exception ex)
                {
                    ic.Log(DateTime.Now, ex);
                }
            }

            foreach (SMDeviceInfo device in deviceInfos)
            {
                string action = $"&action=queryDeviceLastData&i18n=zh_CN&pn={device.Collector}&devcode={device.Code}&devaddr={device.Address}&sn={device.Serial}";
                string ret = HttpPub.HttpGet(MakeUrl(action));
                ic.Gram(DateTime.Now, ret);
                if (!HttpPub.IsValid(ret, "err", 0))
                {
                    return false;
                }

                try
                {
                    JArray jArray = JToken.Parse(ret)["dat"] as JArray;
                    foreach (ParaIndex para in device.paraIndices)
                    {
                        if (0 < para.Ratio)
                        {
                            value[para.Index] = Convert.ToSingle(jArray[para.JIndex]["val"]) * para.Ratio;
                        }
                        else
                        {
                            value[para.Index] = WorkStatus[jArray[para.JIndex]["val"].ToString()];
                        }
                        lUpdate.Add(para.Index);
                    }
                }
                catch (Exception ex)
                {
                    ic.Log(DateTime.Now, ex);
                }
            }
            if (0 == lUpdate.Count) return false;

            update = lUpdate.ToArray();
            return true;
        }

        public void InitPt(string[][] para)
        {

        }

        public bool IsConnect()
        {
            return isConnect;
        }
    }
}
