using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Asst;
using Infrastructure;
using Observer;

namespace Protocol
{
    public enum FunCode
    {
        CoilStatus = 1,
        InputStatus,
        HoldingRegister,
        InputRegister
    }

    public enum DataType
    {
        Bit,
        Ushort,
        Short,
        NUint,
        NInt,
        NFloat,
        NUlong,
        NLong,
        NDouble,
        HUint,
        HInt,
        HFloat,
        HUlong,
        HLong,
        HDouble
    }

    struct RequestInfo
    {
        private const int BitInByte = 8;
        private const int ByteInRegister = 2;

        private static readonly ushort[] CRCtab = new ushort[]
        {
                0x0000,0xC0C1,0xC181,0x0140,0xC301,0x03C0,0x0280,0xC241,0xC601,0x06C0,0x0780,0xC741,0x0500,0xC5C1,0xC481,0x0440,
                0xCC01,0x0CC0,0x0D80,0xCD41,0x0F00,0xCFC1,0xCE81,0x0E40,0x0A00,0xCAC1,0xCB81,0x0B40,0xC901,0x09C0,0x0880,0xC841,
                0xD801,0x18C0,0x1980,0xD941,0x1B00,0xDBC1,0xDA81,0x1A40,0x1E00,0xDEC1,0xDF81,0x1F40,0xDD01,0x1DC0,0x1C80,0xDC41,
                0x1400,0xD4C1,0xD581,0x1540,0xD701,0x17C0,0x1680,0xD641,0xD201,0x12C0,0x1380,0xD341,0x1100,0xD1C1,0xD081,0x1040,
                0xF001,0x30C0,0x3180,0xF141,0x3300,0xF3C1,0xF281,0x3240,0x3600,0xF6C1,0xF781,0x3740,0xF501,0x35C0,0x3480,0xF441,
                0x3C00,0xFCC1,0xFD81,0x3D40,0xFF01,0x3FC0,0x3E80,0xFE41,0xFA01,0x3AC0,0x3B80,0xFB41,0x3900,0xF9C1,0xF881,0x3840,
                0x2800,0xE8C1,0xE981,0x2940,0xEB01,0x2BC0,0x2A80,0xEA41,0xEE01,0x2EC0,0x2F80,0xEF41,0x2D00,0xEDC1,0xEC81,0x2C40,
                0xE401,0x24C0,0x2580,0xE541,0x2700,0xE7C1,0xE681,0x2640,0x2200,0xE2C1,0xE381,0x2340,0xE101,0x21C0,0x2080,0xE041,
                0xA001,0x60C0,0x6180,0xA141,0x6300,0xA3C1,0xA281,0x6240,0x6600,0xA6C1,0xA781,0x6740,0xA501,0x65C0,0x6480,0xA441,
                0x6C00,0xACC1,0xAD81,0x6D40,0xAF01,0x6FC0,0x6E80,0xAE41,0xAA01,0x6AC0,0x6B80,0xAB41,0x6900,0xA9C1,0xA881,0x6840,
                0x7800,0xB8C1,0xB981,0x7940,0xBB01,0x7BC0,0x7A80,0xBA41,0xBE01,0x7EC0,0x7F80,0xBF41,0x7D00,0xBDC1,0xBC81,0x7C40,
                0xB401,0x74C0,0x7580,0xB541,0x7700,0xB7C1,0xB681,0x7640,0x7200,0xB2C1,0xB381,0x7340,0xB101,0x71C0,0x7080,0xB041,
                0x5000,0x90C1,0x9181,0x5140,0x9301,0x53C0,0x5280,0x9241,0x9601,0x56C0,0x5780,0x9741,0x5500,0x95C1,0x9481,0x5440,
                0x9C01,0x5CC0,0x5D80,0x9D41,0x5F00,0x9FC1,0x9E81,0x5E40,0x5A00,0x9AC1,0x9B81,0x5B40,0x9901,0x59C0,0x5880,0x9841,
                0x8801,0x48C0,0x4980,0x8941,0x4B00,0x8BC1,0x8A81,0x4A40,0x4E00,0x8EC1,0x8F81,0x4F40,0x8D01,0x4DC0,0x4C80,0x8C41,
                0x4400,0x84C1,0x8581,0x4540,0x8701,0x47C0,0x4680,0x8641,0x8201,0x42C0,0x4380,0x8341,0x4100,0x81C1,0x8081,0x4040
        };

        public int Device { get; set; }
        public FunCode Funcode { get; }
        public int Start { get; }
        public int Count { get; }
        public int RspnsLen { get; }
        public RequestInfo(FunCode funcode, int start, int count) : this()
        {
            Funcode = funcode;
            Start = start;
            Count = count;
            switch (funcode)
            {
                case FunCode.CoilStatus:
                case FunCode.InputStatus:
                {
                    RspnsLen = Count / BitInByte + (0 == Count % BitInByte ? 0 : 1);
                    break;
                }
                case FunCode.HoldingRegister:
                case FunCode.InputRegister:
                {
                    RspnsLen = Count * 2;
                    break;
                }
            }
        }

        public byte[] ToArray()
        {
            byte[] request = new byte[]
            {
                    (byte)Device,
                    (byte)Funcode,
                    (byte)(Start >> 8),
                    (byte)(Start & 0xff),
                    (byte)(Count >> 8),
                    (byte)(Count & 0xff)
            };
            byte[] result = new byte[8];
            Buffer.BlockCopy(request, 0, result, 0, 6);
            Buffer.BlockCopy(BitConverter.GetBytes(CRC16(request)), 0, result, 6, 2);
            return result;
        }

        private ushort CRC16(byte[] datas)
        {
            ushort crc = 0xffff;
            foreach (byte data in datas)
            {
                crc = (ushort)((crc >> 8) ^ CRCtab[(crc & 0xff ^ data)]);
            }
            return crc;
        }
    }

    struct PrivInfo
    {
        public float Ratio { get; }
        public int Offset { get; }
        public DataType Type { get; }

        public PrivInfo(float ratio, int offset, DataType type)
        {
            Ratio = ratio;
            Offset = offset;
            Type = type;
        }
    }

    struct AddrIdx
    {
        public int Addr { get; }
        public int Idx { get; }
        public AddrIdx(int addr, int idx)
        {
            Addr = addr;
            Idx = idx;
        }
    }

    internal class InputModbus : IInputProtocol
    {
        private const int LenOfHead = 3;
        private const int LenOfRear = 2;
        private const int IndexOfDevice = 0;
        private const int IndexOfFunCode = 1;
        private const int IndexOfLength = 2;


        private TCP tcp;
        private RequestInfo[] rqstInfo;
        private PrivInfo[] privInfo;
        private Dictionary<int, int> dicAnalog;
        private Lookup<int, int> lookDigital;
        private int currentIdx = -1;
        private int currentLen;
        private byte[] ResponseGram;
        private List<int> lRefresh;
        private float[] Value;
        private AutoResetEvent ev;
        private int timeOut;
        private int retry;
        private InfoCenter ic;


        public InputModbus(string path)
        {
            IniFile ini = new IniFile(path + "\\Modbus.ini");

            string mode = ini.GetStr("TCP", "Mode");
            IPAddress ip = IPAddress.Parse(ini.GetStr("TCP", "IP"));
            int port = ini.GetInt("TCP", "Port", 9000);
            tcp = TCPFactory.CreateInstance(mode, ip, port, OnReceiveData);

            string[] dvcTyp = ini.GetStr("REQUEST", "Device").Split(';');
            string[] rgstrTyp = ini.GetStr("REQUEST", "Register").Split('|');
            int typCount = dvcTyp.Length < rgstrTyp.Length ? dvcTyp.Length : rgstrTyp.Length;
            RequestInfo[][] requestInfo = new RequestInfo[typCount][];
            int maxRegCount = ini.GetInt("REQUEST", "Count", 120);
            int rqstCount = 0;

            for (int i = 0; i < typCount; ++i)
            {
                //处理寄存器地址
                string[] szRgstr = rgstrTyp[i].Split(';');
                List<RequestInfo> lstRqst = new List<RequestInfo>();
                foreach (string req in szRgstr)
                {
                    string[] prop = req.Split(',');
                    FunCode funcode = (FunCode)Enum.Parse(typeof(FunCode), prop[0]);
                    int begin = int.Parse(prop[1]);
                    int end = int.Parse(prop[2]);
                    while (maxRegCount < end - begin + 1)
                    {
                        lstRqst.Add(new RequestInfo(funcode, begin, maxRegCount));
                        begin += maxRegCount;
                    }
                    lstRqst.Add(new RequestInfo(funcode, begin, end - begin + 1));
                }
                RequestInfo[] rqstAry = lstRqst.ToArray();

                //处理设备地址,融合寄存器地址,生成存放请求报文的临时锯齿数组
                string[] szDvc = dvcTyp[i].Split(',');
                int dvcStart = int.Parse(szDvc[0]);
                int dvcCount = int.Parse(szDvc[1]);
                requestInfo[i] = new RequestInfo[rqstAry.Length * dvcCount];
                for (int j = 0; j < dvcCount; ++j)
                {
                    int dvcAddr = dvcStart + j;
                    for (int k = 0; k < rqstAry.Length; ++k)
                    {
                        rqstAry[k].Device = dvcAddr;
                    }
                    Array.ConstrainedCopy(rqstAry, 0, requestInfo[i], rqstAry.Length * j, rqstAry.Length);
                }
                rqstCount += requestInfo[i].Length;
            }

            //将临时数组的报文参数拷贝至最终的一维数组
            rqstInfo = new RequestInfo[rqstCount];
            int offset = 0;
            foreach (RequestInfo[] rqst in requestInfo)
            {
                Array.ConstrainedCopy(rqst, 0, rqstInfo, offset, rqst.Length);
                offset += rqst.Length;
            }

            lRefresh = new List<int>();
            ev = new AutoResetEvent(false);
            timeOut = ini.GetInt("REQUEST", "Timeout", 1000);
            retry = ini.GetInt("REQUEST", "Retry", 4);
            ic = new InfoCenter(path);
        }

        public bool Connect()
        {
            return tcp.Connect();
        }

        private void OnReceiveData(object sender, ReceiveEventArgs e)
        {
            byte[] receive = e.Receive;
            ic.Gram(DateTime.Now, "RX", receive);

            if (-1 == currentIdx || 6 >= receive.Length)
                return;
            
            if (0 == currentLen)
            {
                if (rqstInfo[currentIdx].Funcode == (FunCode)receive[IndexOfFunCode] && rqstInfo[currentIdx].RspnsLen == receive[IndexOfLength])
                    ResponseGram = new byte[LenOfHead + receive[IndexOfLength] + LenOfRear];                
                else
                    return;                
            }

            if (ResponseGram.Length <= currentLen + receive.Length)
            {
                Array.ConstrainedCopy(receive, 0, ResponseGram, currentLen, ResponseGram.Length - currentLen);
                currentLen = ResponseGram.Length;
            }
            else
            {
                Array.ConstrainedCopy(receive, 0, ResponseGram, currentLen, receive.Length);
                currentLen += receive.Length;
                return;
            }

            int dev = ResponseGram[IndexOfDevice];
            FunCode fun = (FunCode)ResponseGram[IndexOfFunCode];
            int addrHi = (dev << 8 | (int)fun) << 16;
            switch (fun)
            {
                case FunCode.CoilStatus:
                case FunCode.InputStatus:
                {
                    GetStatusValue(ResponseGram, addrHi | rqstInfo[currentIdx].Start, rqstInfo[currentIdx].Count);
                    break;
                }
                case FunCode.HoldingRegister:
                case FunCode.InputRegister:
                {
                    GetRegisterValue(ResponseGram, addrHi | rqstInfo[currentIdx].Start, rqstInfo[currentIdx].Count);
                    break;
                }
            }
            ev.Set();
        }

        private void GetStatusValue(byte[] gram, int start, int count)
        {
            int boundbyte = count / 8;

            for (int i = 0; i < boundbyte; ++i)
            {
                for (int j = 0; j < 8; ++j)
                {
                    if (dicAnalog.TryGetValue(start++, out int idx))
                    {
                        lRefresh.Add(idx);
                        Value[idx] = gram[LenOfHead + i] >> j & 1;
                    }
                }
            }

            int boundbit = count % 8;
            if (0 == boundbit)
                return;

            for (int i = 0; i < boundbit; i++)
            {
                if (dicAnalog.TryGetValue(start++, out int idx))
                {
                    lRefresh.Add(idx);
                    Value[idx] = gram[LenOfHead + boundbyte] >> i & 1;
                }
            }
        }

        private void GetRegisterValue(byte[] gram, int start, int count)
        {
            int i = 0;
            while (i < count)
            {
                int id = LenOfHead + i * 2;
                if (dicAnalog.TryGetValue(start + i, out int aid))
                {
                    lRefresh.Add(aid);
                    switch (privInfo[aid].Type)
                    {
                        case DataType.Ushort:
                        {
                            byte[] value = new byte[] { gram[id + 1], gram[id] };
                            Value[aid] = BitConverter.ToUInt16(value, 0) * privInfo[aid].Ratio + privInfo[aid].Offset;
                            i++;
                            break;
                        }
                        case DataType.Short:
                        {
                            byte[] value = new byte[] { gram[id + 1], gram[id] };
                            Value[aid] = BitConverter.ToInt16(value, 0) * privInfo[aid].Ratio + privInfo[aid].Offset;
                            i++;
                            break;
                        }
                        case DataType.NUint:
                        {
                            byte[] value = new byte[] { gram[id + 3], gram[id + 2], gram[id + 1], gram[id] };
                            Value[aid] = BitConverter.ToUInt32(value, 0) * privInfo[aid].Ratio + privInfo[aid].Offset;
                            i += 2;
                            break;
                        }
                        case DataType.NInt:
                        {
                            byte[] value = new byte[] { gram[id + 3], gram[id + 2], gram[id + 1], gram[id] };
                            Value[aid] = BitConverter.ToInt32(value, 0) * privInfo[aid].Ratio + privInfo[aid].Offset;
                            i += 2;
                            break;
                        }
                        case DataType.NFloat:
                        {
                            byte[] value = new byte[] { gram[id + 3], gram[id + 2], gram[id + 1], gram[id] };
                            Value[aid] = BitConverter.ToSingle(value, 0) * privInfo[aid].Ratio + privInfo[aid].Offset;
                            i += 2;
                            break;
                        }
                        case DataType.NUlong:
                        {
                            byte[] value = new byte[] { gram[id + 7], gram[id + 6], gram[id + 5], gram[id + 4], gram[id + 3], gram[id + 2], gram[id + 1], gram[id] };
                            Value[aid] = BitConverter.ToUInt64(value, 0) * privInfo[aid].Ratio + privInfo[aid].Offset;
                            i += 4;
                            break;
                        }
                        case DataType.NLong:
                        {
                            byte[] value = new byte[] { gram[id + 7], gram[id + 6], gram[id + 5], gram[id + 4], gram[id + 3], gram[id + 2], gram[id + 1], gram[id] };
                            Value[aid] = BitConverter.ToInt64(value, 0) * privInfo[aid].Ratio + privInfo[aid].Offset;
                            i += 4;
                            break;
                        }
                        case DataType.NDouble:
                        {
                            byte[] value = new byte[] { gram[id + 7], gram[id + 6], gram[id + 5], gram[id + 4], gram[id + 3], gram[id + 2], gram[id + 1], gram[id] };
                            Value[aid] = (float)BitConverter.ToDouble(value, 0) * privInfo[aid].Ratio + privInfo[aid].Offset;
                            i += 4;
                            break;
                        }
                        case DataType.HUint:
                        {
                            byte[] value = new byte[] { gram[id + 1], gram[id], gram[id + 3], gram[id + 2] };
                            Value[aid] = BitConverter.ToUInt32(value, 0) * privInfo[aid].Ratio + privInfo[aid].Offset;
                            i += 2;
                            break;
                        }
                        case DataType.HInt:
                        {
                            byte[] value = new byte[] { gram[id + 1], gram[id], gram[id + 3], gram[id + 2] };
                            Value[aid] = BitConverter.ToInt32(value, 0) * privInfo[aid].Ratio + privInfo[aid].Offset;
                            i += 2;
                            break;
                        }
                        case DataType.HFloat:
                        {
                            byte[] value = new byte[] { gram[id + 1], gram[id], gram[id + 3], gram[id + 2] };
                            Value[aid] = BitConverter.ToSingle(value, 0) * privInfo[aid].Ratio + privInfo[aid].Offset;
                            i += 2;
                            break;
                        }
                        case DataType.HUlong:
                        {
                            byte[] value = new byte[] { gram[id + 1], gram[id], gram[id + 3], gram[id + 2], gram[id + 5], gram[id + 4], gram[id + 7], gram[id + 6] };
                            Value[aid] = BitConverter.ToUInt64(value, 0) * privInfo[aid].Ratio + privInfo[aid].Offset;
                            i += 4;
                            break;
                        }
                        case DataType.HLong:
                        {
                            byte[] value = new byte[] { gram[id + 1], gram[id], gram[id + 3], gram[id + 2], gram[id + 5], gram[id + 4], gram[id + 7], gram[id + 6] };
                            Value[aid] = BitConverter.ToInt64(value, 0) * privInfo[aid].Ratio + privInfo[aid].Offset;
                            i += 4;
                            break;
                        }
                        case DataType.HDouble:
                        {
                            byte[] value = new byte[] { gram[id + 1], gram[id], gram[id + 3], gram[id + 2], gram[id + 5], gram[id + 4], gram[id + 7], gram[id + 6] };
                            Value[aid] = (float)BitConverter.ToDouble(value, 0) * privInfo[aid].Ratio + privInfo[aid].Offset;
                            i += 4;
                            break;
                        }
                    }
                }
                else
                {
                    byte[] value = new byte[] { gram[id + 1], gram[id] };
                    short val = BitConverter.ToInt16(value, 0);
                    foreach (int did in lookDigital[start + i])
                    {
                        lRefresh.Add(did);
                        Value[did] = val >> privInfo[did].Offset & 1;
                    }
                    ++i;
                }
            }
        }

        public void DisConnect()
        {
            tcp.DisConnect();
        }

        public bool IsConnect()
        {
            return tcp.Connected();
        }

        public event EventHandler<LogEventArgs> Log;

        public void InitPt(stInput[] ptInput)
        {
            privInfo = new PrivInfo[ptInput.Length];
            dicAnalog = new Dictionary<int, int>();
            List<AddrIdx> lstAdId = new List<AddrIdx>();

            for (int i = 0; i < ptInput.Length; ++i)
            {
                string[] scndry = ptInput[i].secondary;
                int reg = int.Parse(ptInput[i].primary);
                int dev = int.Parse(scndry[2]);
                FunCode fun = (FunCode)Enum.Parse(typeof(FunCode), scndry[3]);
                privInfo[i] = new PrivInfo(float.Parse(scndry[0]), int.Parse(scndry[1]), (DataType)Enum.Parse(typeof(DataType), scndry[4]));
                int addr = (dev << 24) | ((int)fun << 16) | reg;
                if (FunCode.CoilStatus == fun || FunCode.InputStatus == fun || DataType.Bit != privInfo[i].Type)
                {
                    dicAnalog[addr] = i;
                }
                else
                {
                    lstAdId.Add(new AddrIdx(addr, i));
                }
            }

            lookDigital = (Lookup<int, int>)lstAdId.ToLookup(p => p.Addr, p => p.Idx);
            Value = new float[ptInput.Length];
        }

        public bool GetData(ref int[] update, float[] value)
        {
            int i = 0;
            foreach (RequestInfo req in rqstInfo)
            {
                byte[] send = req.ToArray();
                currentIdx = i++;
                currentLen = 0;
                try
                {
                    tcp.Send(send);
                }
                catch (SocketException se)
                {
                    LogEventArgs eLog = new LogEventArgs(DateTime.Now, se as Exception);
                    Interlocked.CompareExchange(ref Log, null, null)?.Invoke(this, eLog);
                    //ic.Log(DateTime.Now, se as Exception);
                    if (10054 == se.ErrorCode)
                    {
                        tcp.DisConnect();
                        break;
                    }
                }
                catch (Exception ex)
                {
                    LogEventArgs eLog = new LogEventArgs(DateTime.Now, ex);
                    Interlocked.CompareExchange(ref Log, null, null)?.Invoke(this, eLog);
                    //ic.Log(DateTime.Now, ex);
                }
                ic.Gram(DateTime.Now, "TX", send);
                int counter = 0;
                while (!ev.WaitOne(timeOut) && retry > counter++)
                //while (!ev.WaitOne(timeOut))
                {
                    ;
                }
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
    }
}