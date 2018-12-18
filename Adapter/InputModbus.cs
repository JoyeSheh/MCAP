using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Applctn.Modbus;
using Asst;
using Observer;
using Trnsprt.TCP;

namespace Adapter
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct RegisterInfo
    {
        [FieldOffset(0)]
        private readonly ushort _hasBit;
        [FieldOffset(2)]
        private readonly float _ratio;
        [FieldOffset(6)]
        private readonly int _offset;
        [FieldOffset(2)]
        private readonly uint _bitLow;
        [FieldOffset(6)]
        private readonly uint _bitHigh;

        public ushort HasBit => _hasBit;
        public float Ratio => _ratio;
        public int Offset => _offset;
        public uint BitLow => _bitLow;
        public uint BitHigh => _bitHigh;

        public RegisterInfo(float ratio, int offset) : this()
        {
            _ratio = ratio;
            _offset = offset;
        }

        public RegisterInfo(ushort hasBit, byte[] bitOffset) : this()
        {
            _hasBit = hasBit;
            _bitLow = BitConverter.ToUInt32(bitOffset, 0);
            _bitHigh = BitConverter.ToUInt32(bitOffset, 4);
        }
    }

    internal struct AddrIdx
    {
        public int FullAddr { get; }
        public int Idx { get; }
        public AddrIdx(int fulladdr, int idx)
        {
            FullAddr = fulladdr;
            Idx = idx;
        }
    }

    internal class InputModbus : IInputAdapter
    {
        private readonly FrameInfo[] frameInfo;
        private readonly Dictionary<int, RegisterInfo> dicInfo;
        private readonly Dictionary<int, int> dicIndex;
        private readonly ResponseBase response;
        private readonly List<AddrVal> addrVals;

        private readonly TCPBase tcp;       
        private readonly int timeOut;
        private readonly int retry;

        private int currentIdx;
        private int currentLen;
        private byte[] ResponseGram;
        private ManualResetEventSlim eventSlim;

        private readonly InfoCenter ic;

        public event EventHandler<LogEventArgs> Log;

        public InputModbus(string path, string[][] para)
        {
            IniFile ini = new IniFile(path + "\\Modbus.ini");

            string modbusMode = ini.GetStr("PROTOCOL", "Mode");
            RequestBase request = ModbusFactory.Instance.CreateRequest(modbusMode);

            string[] dvcTyp = ini.GetStr("REQUEST", "Device").Split(';');
            string[] rgstrTyp = ini.GetStr("REQUEST", "Register").Split('|');
            int typCount = dvcTyp.Length < rgstrTyp.Length ? dvcTyp.Length : rgstrTyp.Length;

            ushort maxRegCount = (ushort)ini.GetInt("REQUEST", "Count", 120);
                        
            List<FrameInfo> lstFrmInfo = new List<FrameInfo>();

            for (int i = 0; i < typCount; ++i)
            {
                string[] szRgstr = rgstrTyp[i].Split(';');
                List<MiniFrame> lstMiniFrm = new List<MiniFrame>();
                foreach (string req in szRgstr)
                {
                    string[] prop = req.Split(',');
                    FunCode funcode = (FunCode)Enum.Parse(typeof(FunCode), prop[0]);
                    ushort begin = ushort.Parse(prop[1]);
                    int end = int.Parse(prop[2]);
                    while (maxRegCount < end - begin + 1)
                    {
                        lstMiniFrm.Add(new MiniFrame(funcode, begin, maxRegCount));
                        begin += maxRegCount;
                    }
                    lstMiniFrm.Add(new MiniFrame(funcode, begin, (ushort)(end - begin + 1)));
                }

                string[] szDvc = dvcTyp[i].Split(',');
                int dvcBegin = int.Parse(szDvc[0]);
                int dvcCount = int.Parse(szDvc[1]);
                while(dvcCount-- > 0)
                {
                    for (int j = 0; j < lstMiniFrm.Count; ++j)
                    {
                        lstFrmInfo.Add(new FrameInfo(request, (byte)dvcBegin, lstMiniFrm[j]));
                    }
                    ++dvcBegin;
                }
            }
            frameInfo = lstFrmInfo.ToArray();

            dicIndex = new Dictionary<int, int>();
            dicInfo = new Dictionary<int, RegisterInfo>();
            Dictionary<int, RegisterType> dicType = new Dictionary<int, RegisterType>();
            List<AddrIdx> lstAdId = new List<AddrIdx>();

            for (int i = 0; i < para.Length; ++i)
            {
                byte device = byte.Parse(para[i][3]);
                Enum.TryParse(para[i][4], out FunCode funcode);
                ushort register = ushort.Parse(para[i][0]);
                int fullAddr = ModbusPub.CalcFullAddr(device, funcode, register);

                if (FunCode.HoldingRegister == funcode || FunCode.InputRegister == funcode)
                {
                    Enum.TryParse(para[i][5], out RegisterType registerType);

                    if (RegisterType.Bit != registerType)
                    {
                        dicType[fullAddr] = registerType;
                        dicInfo[fullAddr] = new RegisterInfo(float.Parse(para[i][1]), int.Parse(para[i][2]));
                        dicIndex[fullAddr] = i;
                    }
                    else
                    {
                        lstAdId.Add(new AddrIdx(fullAddr, ushort.Parse(para[i][2]) << 16 | i));
                    }

                }
                else
                {
                    dicIndex[fullAddr] = i;
                }
            }

            if (0 < lstAdId.Count)
            {
                Lookup<int, int> bitGroup = (Lookup<int, int>)lstAdId.ToLookup(p => p.FullAddr, p => p.Idx);
                foreach (IGrouping<int, int> group in bitGroup)
                {
                    dicType[group.Key] = RegisterType.Bit;
                    int baseIndex = group.Min() & 0xffff;
                    ushort hasBit = 0;
                    byte[] bitOffset = new byte[8];
                    foreach (int index in group)
                    {
                        int offset = index >> 16;
                        hasBit |= (ushort)(1 << offset);
                        bitOffset[offset / 2] |= (byte)((index & 0xffff) - baseIndex << offset % 2 * 4);
                    }
                    dicInfo[group.Key] = new RegisterInfo(hasBit, bitOffset);
                    dicIndex[group.Key] = baseIndex;
                }
            }
            response = ModbusFactory.Instance.CreateResponse(modbusMode, dicType);
            addrVals = new List<AddrVal>();

            string tcpMode = ini.GetStr("TCP", "Mode");
            IPAddress ip = IPAddress.Parse(ini.GetStr("TCP", "IP"));
            int port = ini.GetInt("TCP", "Port", 9000);
            tcp = TCPFactory.Instance.CreateTCP(tcpMode, ip, port, OnReceiveData);

            eventSlim = new ManualResetEventSlim(false, 100);
            timeOut = ini.GetInt("REQUEST", "Timeout", 1000);
            retry = ini.GetInt("REQUEST", "Retry", 4);

            
            ic = new InfoCenter(path);
        }

        private void OnReceiveData(object sender, ReceiveEventArgs e)
        {
            byte[] receive = e.Receive;
            ic.Gram(DateTime.Now, "RX", receive);

            if (frameInfo.Length == currentIdx || 6 >= receive.Length)
                return;

            if (0 == currentLen)
            {
                if (response.IsValid(receive, frameInfo[currentIdx]))
                    ResponseGram = new byte[frameInfo[currentIdx].RspnsLen];
                else
                    return;
            }

            Array.ConstrainedCopy(receive, 0, ResponseGram, currentLen, receive.Length);
            currentLen += receive.Length;
            if (ResponseGram.Length == currentLen)
            {
                currentLen = 0;
            }
            else
            {
                return;
            }

            addrVals.AddRange(response.ParseGram(receive, frameInfo[currentIdx]));
            eventSlim.Set();
        }

        public bool Connect()
        {
            return tcp.Connect();
        }

        public void DisConnect()
        {
            tcp.DisConnect();
        }

        public bool IsConnect()
        {
            return tcp.Connected();
        }

        public void InitPt(string[][] para)
        {
            //dicIndex = new Dictionary<int, int>();
            //dicInfo = new Dictionary<int, RegisterInfo>();
            //Dictionary<int, RegisterType> dicType = new Dictionary<int, RegisterType>();
            //List<AddrIdx> lstAdId = new List<AddrIdx>();

            //for (int i = 0; i < para.Length; ++i) {
            //    byte device = byte.Parse(para[i][3]);
            //    Enum.TryParse(para[i][4], out FunCode funcode);
            //    ushort register = ushort.Parse(para[i][0]);
            //    int fullAddr = ModbusPub.CalcFullAddr(device, funcode, register);

            //    if (FunCode.HoldingRegister == funcode || FunCode.InputRegister == funcode) {
            //        Enum.TryParse(para[i][5], out RegisterType registerType);

            //        if (RegisterType.Bit != registerType) {
            //            dicType[fullAddr] = registerType;
            //            dicInfo[fullAddr] = new RegisterInfo(float.Parse(para[i][1]), int.Parse(para[i][2]));
            //            dicIndex[fullAddr] = i;
            //        } else {
            //            ushort offset = ushort.Parse(para[i][2]);
            //            lstAdId.Add(new AddrIdx(fullAddr, offset << 16 | i));
            //        }

            //    } else {
            //        dicIndex[fullAddr] = i;
            //    }
            //}

            //if (0 < lstAdId.Count) {
            //    Lookup<int, int> bitGroup = (Lookup<int, int>)lstAdId.ToLookup(p => p.FullAddr, p => p.Idx);
            //    foreach (IGrouping<int, int> group in bitGroup) {
            //        dicType[group.Key] = RegisterType.Bit;
            //        int baseIndex = group.Min() & 0xffff;
            //        ushort hasBit = 0;
            //        byte[] bitOffset = new byte[8];
            //        foreach (int index in group) {
            //            int offset = index >> 16;
            //            hasBit |= (ushort)(1 << offset);
            //            bitOffset[offset / 2] |= (byte)((index & 0xffff) - baseIndex << offset % 2 * 4);
            //        }
            //        dicInfo[group.Key] = new RegisterInfo(hasBit, bitOffset);
            //        dicIndex[group.Key] = baseIndex;
            //    }
            //}
            //response = ModbusFactory.Instance.CreateResponse(modbusMode, dicType);
        }

        public bool GetData(ref int[] update, float[] value)
        {
            for (currentIdx = 0; currentIdx < frameInfo.Length; ++currentIdx)
            {
                byte[] request = frameInfo[currentIdx].RequestArray;
                eventSlim.Reset();
                try
                {
                    tcp.Send(request);
                }
                catch (Exception ex)
                {
                    LogEventArgs eLog = new LogEventArgs(DateTime.Now, ex);
                    Interlocked.CompareExchange(ref Log, null, null)?.Invoke(this, eLog);
                    continue;
                }
                ic.Gram(DateTime.Now, "TX", request);
                int counter = 0;
                
                //eventSlim.Wait();
                while (!eventSlim.Wait(timeOut) && retry > counter)
                {
                    ++counter;
                }
            }

            if (0 == addrVals.Count) return false;

            List<int> lRefresh = new List<int>();
            foreach (AddrVal addrVal in addrVals)
            {
                int idx = dicIndex[addrVal.FullAddr];
                if (dicInfo.TryGetValue(addrVal.FullAddr, out RegisterInfo register))
                {
                    if (0 == register.HasBit)
                    {
                        value[idx] = addrVal.Value * register.Ratio + register.Offset;
                        lRefresh.Add(idx);
                    }
                    else
                    {
                        BitArray bitHas = new BitArray(BitConverter.GetBytes(register.HasBit));
                        byte[] bitOffset = BitConverter.GetBytes((ulong)register.BitHigh << 32 | register.BitLow);
                        BitArray bitVal = new BitArray(BitConverter.GetBytes((ushort)addrVal.Value));
                        for (int i = 0; i < bitHas.Length; ++i)
                        {
                            if (!bitHas[i]) continue;

                            int bitIdx = idx + (bitOffset[i / 2] >> i % 2 * 4 & 0xf);
                            value[bitIdx] = bitVal[i] ? 1 : 0;
                            lRefresh.Add(bitIdx);
                        }
                    }
                }
                else
                {
                    value[idx] = addrVal.Value;
                    lRefresh.Add(idx);
                }
            }
            update = lRefresh.ToArray();

            return true;
        }
    }
}