using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Asst;
using Trnsprt.TCP;
using Observer;

namespace Adapter
{
    internal class Input13761 : IInputAdapter
    {
        private static readonly int IndexOfOuterHead = 0;
        private static readonly int IndexOfUserDataLen1 = 1;
        private static readonly int IndexOfUserDataLen2 = 3;
        private static readonly int LenOfOuterHead = 6;
        private static readonly int IndexOfUserData = 6;
        private static readonly int IndexOfInnerHead = 28;
        private static readonly int IndexOfInnerData = 38;
        private static readonly int IndexOfInnerCheck = 42;
        private static readonly int IndexOfOuterCheck = 60;
        private static readonly int LenOfRear = 2;
        private static readonly byte HeadByte = 0x68;
        private static readonly int IndexOfBCD = 35;
        private static readonly int LenOfBCD = 4;
        private static readonly int BaseOfBCD = 0x33333333;
        private static readonly int UnitOfBCD = 0x10000;

        private struct ByteAddr
        {
            public ByteAddr(ulong addr)
            {
                Inner = new byte[6];
                for (int i = 0; i < Inner.Length && 0 != addr; ++i)
                {
                    int bcd = (int)(addr % 100);
                    Inner[i] = (byte)(bcd / 10 * 16 + bcd % 10);
                    addr /= 100;
                }

                Outer = new byte[5]
                {
                    Inner[2],
                    Inner[3],
                    Inner[1],
                    Inner[0],
                    0x02
                };
            }
            public byte[] Inner { get; }
            public byte[] Outer { get; }
        }




        private TCPBase tcp;
        private Dictionary<ulong, ByteAddr> dicAddr;
        private byte[][] RequestGram;
        private int currentIdx = -1;
        private int currentLen;
        private byte[] ResponseGram;
        private List<int> lRefresh;
        private float[] Value;
        private AutoResetEvent ev;
        private int timeOut;
        private int retry;
        private InfoCenter ic;


        public Input13761(string path)
        {
            IniFile ini = new IniFile(path + "\\Modbus.ini");

            string mode = ini.GetStr("TCP", "Mode");
            IPAddress ip = IPAddress.Parse(ini.GetStr("TCP", "IP"));
            int port = ini.GetInt("TCP", "Port", 9000);
            tcp = TCPFactory.Instance.CreateTCP(mode, ip, port, OnReceiveData);

            string[] address = ini.GetStr("DEVICE", "Address").Split(';');
            dicAddr = new Dictionary<ulong, ByteAddr>();
            foreach (string addr in address)
            {
                ulong.TryParse(addr, out ulong key);
                dicAddr.Add(key, new ByteAddr(key));
            }

            lRefresh = new List<int>();
            ev = new AutoResetEvent(false);
            timeOut = ini.GetInt("REQUEST", "Timeout", 1000);
            retry = ini.GetInt("REQUEST", "Retry", 4);
            ic = new InfoCenter(path);
        }


        public event EventHandler<LogEventArgs> Log;

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
            int i = 0;
            foreach (byte[] request in RequestGram)
            {
                currentIdx = i++;
                currentLen = 0;
                try
                {
                    tcp.Send(request);
                }
                catch (SocketException se)
                {
                    LogEventArgs eLog = new LogEventArgs(DateTime.Now, se as Exception);
                    Interlocked.CompareExchange(ref Log, null, null)?.Invoke(this, eLog);
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
                }
                ic.Gram(DateTime.Now, "TX", request);
                int counter = 0;
                while (!ev.WaitOne(timeOut) && retry > counter++)
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

        public void InitPt(string[][] para)
        {
            RequestGram = new byte[para.Length][];
            for (int i = 0; i < para.Length; ++i)
            {
                ByteAddr addr = dicAddr[ulong.Parse(para[i][0])];
                RequestGram[i] = MakeRequestGram(addr, int.Parse(para[i][1]));
            }

            Value = new float[para.Length];
        }

        private byte[] MakeRequestGram(ByteAddr addr, int idntfr)
        {
            byte[] gram = new byte[(0xDA >> 2) + 8]
            {
                0x68,0xDA,0x00,0xDA,0x00,0x68,
                0x4B,0x00,0x00,0x00,0x00,0x00,
                0x10,0x60,0x00,0x00,0x01,0x00,
                0x01,0xC3,0x01,0x01,0x14,0x00,
                0xFE,0xFE,0xFE,0xFE,
                0x68,0x00,0x00,0x00,0x00,0x00,0x00,0x68,
                0x11,0x04,0x00,0x00,0x00,0x00,
                0x00,0x16,
                0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
                0x00,0x16
            };

            Buffer.BlockCopy(gram, IndexOfUserData + 1, addr.Outer, 0, addr.Outer.Length);
            Buffer.BlockCopy(gram, IndexOfInnerHead + 1, addr.Inner, 0, addr.Inner.Length);
            Buffer.BlockCopy(gram, IndexOfInnerData, BitConverter.GetBytes(BaseOfBCD + UnitOfBCD * idntfr), 0, sizeof(int));

            for (int i = IndexOfUserData; i < IndexOfInnerHead; ++i)
                gram[IndexOfOuterCheck] += gram[i];

            for (int i = IndexOfInnerHead; i < IndexOfInnerCheck; ++i)
            {
                gram[IndexOfOuterCheck] += gram[i];
                gram[IndexOfInnerCheck] += gram[i];
            }

            gram[IndexOfOuterCheck] += (byte)(gram[IndexOfInnerCheck] + 0x16);

            return gram;
        }

        public bool IsConnect()
        {
            return tcp.Connected();
        }

        private void OnReceiveData(object sender, ReceiveEventArgs e)
        {
            byte[] receive = e.Receive;
            ic.Gram(DateTime.Now, "RX", receive);

            if (LenOfOuterHead + LenOfRear >= receive.Length)
            {
                return;
            }

            if (0 == currentLen)
            {
                int userDataLen = receive[IndexOfUserDataLen1 + 1] << 6 | receive[IndexOfUserDataLen1] >> 2;
                if (HeadByte == receive[IndexOfOuterHead] && HeadByte == receive[IndexOfOuterHead + LenOfOuterHead]
                    && userDataLen == (receive[IndexOfUserDataLen2 + 1] << 6 | receive[IndexOfUserDataLen2] >> 2))
                {

                    ResponseGram = new byte[LenOfOuterHead + userDataLen + LenOfRear];
                }
                else
                {
                    return;
                }
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
            }

            if (ResponseGram.Length != currentLen)
            {
                return;
            }

            lRefresh.Add(currentIdx);
            for (int i = 0; i < LenOfBCD; ++i)
            {                
                Value[currentIdx] += ReadBCD(ResponseGram[IndexOfBCD + i]-0x33, i - 1);
            }
            ev.Set();
        }

        private float ReadBCD(int bcd, int exp)
        {
            return (bcd / 16 * 10 + bcd % 16) * (float)Math.Pow(100, exp);
        }
    }
}
