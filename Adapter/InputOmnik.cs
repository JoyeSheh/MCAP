using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Asst;
using Observer;
using Trnsprt.TCP;

namespace Adapter
{
    internal struct ParaInfo
    {
        public int Addr { get; }
        public float Ratio { get; }
        public ParaInfo(int addr, float ratio)
        {
            Addr = addr;
            Ratio = ratio;
        }
    }

    internal class InputOmnik : IInputAdapter
    {
        private const byte AP_Address = 1;
        private readonly byte Inverter_Address;
        private readonly byte[] Serial;
        private readonly TCPBase tcp;
        private bool isRegisted;
        private readonly ManualResetEventSlim eventSlim;
        private readonly int timeOut;
        private readonly int retry;

        private readonly ParaInfo[] paraInfos;
        private readonly List<int> lstUpdate;
        private readonly float[] Value;

        private readonly InfoCenter ic;

        public event EventHandler<LogEventArgs> Log;

        public InputOmnik(string path,string[][] para)
        {
            IniFile ini = new IniFile(path + "\\Omnik.ini");

            Inverter_Address = (byte)ini.GetInt("DEVICE", "Address", 1);
            Serial = Encoding.Default.GetBytes(ini.GetStr("DEVICE", "Serial"));

            paraInfos = new ParaInfo[para.Length];
            for (int i = 0; i < para.Length; ++i)
            {
                if (!para[i][0].Contains("+"))
                {
                    paraInfos[i] = new ParaInfo(int.Parse(para[i][0]), float.Parse(para[i][1]));
                }
                else
                {
                    string[] hilo = para[i][0].Split('+');
                    int addr = int.Parse(hilo[0]) << 16 | int.Parse(hilo[1]);
                    paraInfos[i] = new ParaInfo(addr, float.Parse(para[i][1]));
                }
            }
            lstUpdate = new List<int>();
            Value = new float[para.Length];

            int port = ini.GetInt("TCP", "Port", 9000);
            tcp = TCPFactory.Instance.CreateTCP("Server", IPAddress.Any, port, OnReceiveData);

            eventSlim = new ManualResetEventSlim(false, 100);
            timeOut = ini.GetInt("REQUEST", "Timeout", 1000);
            retry = ini.GetInt("REQUEST", "Retry", 4);

            ic = new InfoCenter(path);
        }

        private byte[] CreateRequestArray(byte control, byte function, byte[] data)
        {
            byte[] head = new byte[9] { 0x3a, 0x3a, AP_Address, 0, 0, Inverter_Address, control, function, (byte)data.Length };
            int len = head.Length + data.Length + 2;
            byte[] result = new byte[len];
            Buffer.BlockCopy(head, 0, result, 0, head.Length);
            if (0 < data.Length) Buffer.BlockCopy(data, 0, result, head.Length, data.Length);
            ushort checksum = 0;
            for (int i = 0; i < head.Length + data.Length; ++i) checksum += result[i];
            result[len - 2] = (byte)(checksum >> 8);
            result[len - 1] = (byte)(checksum & 0xff);
            return result;
        }

        private byte[] CreateRegisterArray()
        {
            byte[] data = new byte[Serial.Length + 1];
            Buffer.BlockCopy(Serial, 0, data, 0, Serial.Length);
            data[data.Length - 1] = Inverter_Address;
            return CreateRequestArray(0x10, 0x01, data);
        }

        private bool TrySend(byte[] message)
        {
            eventSlim.Reset();
            try {
                tcp.Send(message);
                ic.Gram(DateTime.Now, "TX", message);
            } catch (Exception ex) {
                LogEventArgs eLog = new LogEventArgs(DateTime.Now, ex);
                Interlocked.CompareExchange(ref Log, null, null)?.Invoke(this, eLog);
                return false;
            }
            return true;
        }

        private bool IsValid(byte[] gram)
        {
            if (11 > gram.Length) return false;
            if (0x3a != gram[0] || 0x3a != gram[1] || Inverter_Address != gram[3] || AP_Address != gram[4]) return false;
            return (gram.Length == 9 + gram[8] + 2);
        }

        private void OnReceiveData(object sender, ReceiveEventArgs e)
        {
            byte[] receive = e.Receive;
            ic.Gram(DateTime.Now, "RX", receive);
            if (!IsValid(receive)) return;

            if (0x11 == receive[6] && 0x90 == receive[7]) {
                byte[] data = new byte[receive[8]];
                Buffer.BlockCopy(receive, 9, data, 0, data.Length);
                ParseGram(data);
            } else if (0x10 == receive[6] && 0x81 == receive[7]) {
                isRegisted = (0x06 == receive[9]);
            }

            eventSlim.Set();
        }

        private void ParseGram(byte[] gram)
        {
            for (int i = 0; i < paraInfos.Length; ++i) {                
                if (0 == paraInfos[i].Addr >> 16) {
                    int addr = paraInfos[i].Addr * 2;
                    Value[i] = (gram[addr] << 8 | gram[addr + 1]) * paraInfos[i].Ratio;
                } else {
                    int high = (paraInfos[i].Addr >> 16) * 2;
                    int low = (paraInfos[i].Addr & 0xffff) * 2;
                    Value[i] = (gram[high] << 24 | gram[high + 1] << 16 | gram[low] << 8 | gram[low + 1]) * paraInfos[i].Ratio;
                }
                lstUpdate.Add(i);
            }
        }

        public bool Connect()
        {
            return tcp.Connect();
        }

        public void DisConnect()
        {
            tcp.DisConnect();
        }

        public bool GetData(ref int[] update, float[] value)
        {
            if (!(isRegisted || TrySend(CreateRegisterArray()))) return false;

            if (!(eventSlim.Wait(timeOut) && isRegisted)) return false;

            if (!TrySend(CreateRequestArray(0x11, 0x10, new byte[0]))) return false;

            int counter = 0;
            //eventSlim.Wait();
            while (!eventSlim.Wait(timeOut) && retry > counter) {
                ++counter;
            }

            if (0 == lstUpdate.Count) {
                isRegisted = false;
                return false;
            }

            Array.ConstrainedCopy(Value, 0, value, 0, value.Length);
            update = lstUpdate.ToArray();
            lstUpdate.Clear();
            return true;
        }

        public void InitPt(string[][] para)
        {

        }

        public bool IsConnect()
        {
            return tcp.Connected();
        }
    }
}
