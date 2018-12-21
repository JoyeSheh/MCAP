using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Applctn.Http;
using Asst;
using Newtonsoft.Json.Linq;
using Observer;

namespace Adapter
{
    internal struct ALDeviceInfo
    {
        public string DeviceID { get; }
        public ParaIndex[] ParaIndices { get; }

        public ALDeviceInfo(string deviceID,ParaIndex[] paraIndices)
        {
            DeviceID = deviceID;
            ParaIndices = paraIndices;
        }
    }

    class InputAnyLink : IInputAdapter
    {
        private const string BASE_URL = @"http://47.98.131.56:8600/";
        private const string LOGIN_URL = @"user/login";
        private readonly string Hash;
        private readonly string LoginJson;
        private readonly ALDeviceInfo[] deviceInfos;
        private string Token;
        private bool isConnect;

        private readonly InfoCenter ic;

        public event EventHandler<LogEventArgs> Log;

        public InputAnyLink(string path,string[][] para)
        {
            IniFile ini = new IniFile(path + "\\AnyLink.ini");

            string name = ini.GetStr("ACCOUNT", "Name");
            string password = ini.GetStr("ACCOUNT", "Password");
            Hash = ini.GetStr("ACCOUNT", "Hash");
            LoginJson = $"{{name:\"{name}\",password:\"{password}\",hash:\"{Hash}\"}}";

            string[] deviceIDs = ini.GetStr("DEVICE", "ID").Split(',');            
            List<ParaIndex>[] lstPara = new List<ParaIndex>[deviceIDs.Length];
            for (int i = 0; i < lstPara.Length; ++i)
            {
                lstPara[i] = new List<ParaIndex>();
            }
            for (int i = 0; i < para.Length; ++i)
            {
                int dIndex = int.Parse(para[i][2]);
                if (!para[i][0].Contains("+"))
                {
                    lstPara[dIndex].Add(new ParaIndex(i, int.Parse(para[i][0]), float.Parse(para[i][1])));
                }
                else
                {
                    string[] hilo = para[i][0].Split('+');
                    int jIndex = int.Parse(hilo[0]) << 16 | int.Parse(hilo[1]);
                    lstPara[dIndex].Add(new ParaIndex(i, jIndex, float.Parse(para[i][1])));
                }
            }

            deviceInfos = new ALDeviceInfo[deviceIDs.Length];
            for (int i = 0; i < deviceIDs.Length; ++i)
            {
                deviceInfos[i] = new ALDeviceInfo(deviceIDs[i], lstPara[i].ToArray());
            }

            ic = new InfoCenter(path);
        }

        private string HttpPost(string url, string json)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/json;charset=UTF-8";
            request.ContentLength = Encoding.UTF8.GetByteCount(json);

            try
            {
                using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write(json);
                }
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd().Trim();
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
            finally
            {
                request.Abort();
            }
        }

        public bool Connect()
        {
            string loginResponse = HttpPost(BASE_URL + LOGIN_URL, LoginJson);
            if (!HttpPub.IsValid(loginResponse, "status", 100))
            {
                isConnect = false;
                return false;
            }

            Token = JToken.Parse(loginResponse)["data"].ToString();
            isConnect = true;
            return true;
        }

        public void DisConnect()
        {
            Token = null;
            isConnect = false;
        }

        public bool GetData(ref int[] update, float[] value)
        {
            List<int> lUpdate = new List<int>();
            foreach (ALDeviceInfo device in deviceInfos)
            {
                string url = $"{BASE_URL}currentdata?token={Token}&hash={Hash}&deviceid={device.DeviceID}";
                string response = HttpPub.HttpGet(url);
                if (!HttpPub.IsValid(response, "status", 100))
                {
                    isConnect = false;
                    return false;
                }

                try
                {
                    JArray jArray = JToken.Parse(response)["data"] as JArray;
                    if (0 == jArray.Count) continue;
                    foreach (ParaIndex para in device.ParaIndices)
                    {
                        if (0 == para.JIndex >> 16)
                        {
                            value[para.Index] = Convert.ToSingle(jArray[para.JIndex]["val"]) * para.Ratio;
                        }
                        else
                        {
                            int high = para.JIndex >> 16;
                            int low = para.JIndex & 0xffff;
                            value[para.Index] = (Convert.ToUInt16(jArray[high]["val"]) << 16 | Convert.ToUInt16(jArray[low]["val"])) * para.Ratio;
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

        public bool IsConnect => isConnect;
    }
}
