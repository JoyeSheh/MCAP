using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Asst;
using Observer;
using Trnsprt.TCP;


namespace Adapter
{
    internal class InputZTE : IInputAdapter
    {
        private struct PrivInfo
        {
            public float ratio;
            public int offset;
            public int reg;
        }

        private struct DeviceInfo
        {
            public int dev;
            public Dictionary<int, int> dicAnalog;
            public Lookup<int, int> lookDigital;
        }

        private struct AddrIdx
        {
            public int Addr;
            public int Idx;
        }

        private TCPBase tcp;
        private int devBase;
        private int devCount;
        private DeviceInfo[] devInfo;
        private PrivInfo[] privInfo;
        private int currentIdx = -1;
        private int currentLen;
        private byte[] ResponseGram;
        private List<int> lRefresh;
        private float[] Value;
        private AutoResetEvent ev = new AutoResetEvent(false);
        private InfoCenter ic;

        public InputZTE(string path)
        {
            IniFile ini = new IniFile(path + "\\ZTE.ini");

            string mode = ini.GetStr("TCP", "Mode");
            IPAddress ip = IPAddress.Parse(ini.GetStr("TCP", "IP"));
            int port = ini.GetInt("TCP", "Port", 9000);
            tcp = TCPFactory.Instance.CreateTCP(mode, ip, port, OnReceiveData);

            devBase = ini.GetInt("DEVICE", "Base", 1);
            devCount = ini.GetInt("DEVICE", "Count", 1);
            devInfo = new DeviceInfo[devCount];
            for (int i = 0; i < devInfo.Length; ++i)
            {
                devInfo[i].dicAnalog = new Dictionary<int, int>();
            }

            lRefresh = new List<int>();
            ic = new InfoCenter(path);
        }

        public bool IsConnect()
        {
            return tcp.Connected();
        }

        public event EventHandler<LogEventArgs> Log;

        public bool Connect()
        {
            return tcp.Connect();
        }

        private void OnReceiveData(object sender, ReceiveEventArgs e)
        {
            ic.Gram(DateTime.Now, "RX", e.Receive);

            if (-1 == currentIdx)
            {
                return;
            }

            if (0 == currentLen)
            {
                if (0xAA == e.Receive[0] && 1 == e.Receive[1])
                {
                    ResponseGram = new byte[7 + e.Receive[5]];
                }
                else
                {
                    return;
                }
            }

            if (ResponseGram.Length <= currentLen + e.Receive.Length)
            {
                Array.ConstrainedCopy(e.Receive, 0, ResponseGram, currentLen, ResponseGram.Length - currentLen);
                currentLen = ResponseGram.Length;
            }
            else
            {
                Array.ConstrainedCopy(e.Receive, 0, ResponseGram, currentLen, e.Receive.Length);
                currentLen += e.Receive.Length;
            }

            if (ResponseGram.Length != currentLen)
            {
                return;
            }

            ic.Gram(DateTime.Now, "RX", ResponseGram);

            int addr = BitConverter.ToUInt16(ResponseGram, 6);
            int offset = 8;
            while (offset < ResponseGram.Length - 1)
            {
                if (devInfo[currentIdx].dicAnalog.TryGetValue(addr, out int aid))
                {
                    if (1 == privInfo[aid].reg)
                    {
                        Value[aid] = BitConverter.ToInt16(ResponseGram, offset) * privInfo[aid].ratio + privInfo[aid].offset;
                        addr += 1;
                        offset += 2;
                    }
                    else
                    {
                        Value[aid] = BitConverter.ToInt32(ResponseGram, offset) * privInfo[aid].ratio + privInfo[aid].offset;
                        addr += 2;
                        offset += 4;
                    }
                    lRefresh.Add(aid);
                }
                else
                {
                    ushort val = BitConverter.ToUInt16(ResponseGram, offset);
                    foreach (int did in devInfo[currentIdx].lookDigital[addr])
                    {
                        Value[did] = (val & (1 << privInfo[did].offset)) == 0 ? 0 : 1;
                        lRefresh.Add(did);
                    }
                    addr += 1;
                    offset += 2;
                }
            }

            ev.Set();
        }

        public void DisConnect()
        {
            tcp.DisConnect();
        }

        public bool GetData(ref int[] update, float[] value)
        {
            foreach (DeviceInfo inf in devInfo)
            {
                byte[] send = new byte[10] { 0xAA, 1, (byte)inf.dev, 1, 2, 3, 8, 0, 164, 0 };
                int sum = 0;
                foreach (byte b in send)
                {
                    sum += b;
                }
                send[9] = (byte)(~(sum & 255) + 1);
                currentIdx = inf.dev - devBase;
                currentLen = 0;
                try
                {
                    tcp.Send(send);
                }
                catch (SocketException se)
                {
                    if (10054 == se.ErrorCode)
                    {
                        DisConnect();
                        break;
                    }
                }
                ic.Gram(DateTime.Now, "TX", send);
                ev.WaitOne(4096);
                Thread.Sleep(1024);
            }
            currentIdx = -1;
            if (0 == lRefresh.Count)
            {
                return false;
            }

            update = lRefresh.ToArray();
            lRefresh.Clear();
            foreach (int idx in update)
            {
                value[idx] = Value[idx];
            }
            return true;
        }

        public void InitPt(string[][] para)
        {
            privInfo = new PrivInfo[para.Length];
            List<AddrIdx>[] lstAdId = new List<AddrIdx>[devCount];
            for (int i = 0; i < lstAdId.Length; ++i)
            {
                lstAdId[i] = new List<AddrIdx>();
            }

            for (int i = 0; i < para.Length; ++i)
            {
                float.TryParse(para[i][1], out privInfo[i].ratio);
                int.TryParse(para[i][2], out privInfo[i].offset);
                int.TryParse(para[i][4], out privInfo[i].reg);
                int dev = int.Parse(para[i][3]) - devBase;
                int addr = int.Parse(para[i][0]);
                if (0 != privInfo[i].reg)
                {
                    devInfo[dev].dicAnalog[addr] = i;
                }
                else
                {
                    lstAdId[dev].Add(new AddrIdx
                    {
                        Addr = addr,
                        Idx = i
                    });
                }
            }
            for (int i = 0; i < devCount; ++i)
            {
                devInfo[i].dev = devBase + i;
                devInfo[i].lookDigital = (Lookup<int, int>)lstAdId[i].ToLookup(p => p.Addr, p => p.Idx);
            }
            Value = new float[para.Length];
        }
    }
}
