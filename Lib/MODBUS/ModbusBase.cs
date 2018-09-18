using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Applctn.Modbus
{
    public enum FunCode : byte
    {
        CoilStatus = 1,
        InputStatus,
        HoldingRegister,
        InputRegister
    }

    public enum RegisterType : byte
    {
        [Description("ToUInt16")]
        Bit = 0x1f,
        [Description("ToInt16")]
        I2 = 0x10,
        [Description("ToUInt16")]
        U2 = 0x11,
        [Description("ToInt32")]
        NI4 = 0x20,
        [Description("ToUInt32")]
        NU4 = 0x21,
        [Description("ToSingle")]
        NR4 = 0x22,
        [Description("ToInt64")]
        NI8 = 0x40,
        [Description("ToUInt64")]
        NU8 = 0x41,
        [Description("ToDouble")]
        NR8 = 0x42,
        [Description("ToInt32")]
        HI4 = 0xa0,
        [Description("ToUInt32")]
        HU4 = 0xa1,
        [Description("ToSingle")]
        HR4 = 0xa2,
        [Description("ToInt64")]
        HI8 = 0xc0,
        [Description("ToUInt64")]
        HU8 = 0xc1,
        [Description("ToDouble")]
        HR8 = 0xc2
    }

    internal struct MiniFrame
    {
        public FunCode Funcode { get; }
        public ushort Start { get; }
        public ushort Count { get; }
        public MiniFrame(FunCode funcode, ushort start, ushort count)
        {
            Funcode = funcode;
            Start = start;
            Count = count;
        }
    }

    internal struct FrameInfo
    {
        public byte Device { get; }
        public FunCode Funcode { get; }
        public ushort Start { get; }
        public ushort Count { get; }
        public byte[] RequestArray { get; }
        public int FullStart { get; }
        public int RspnsLen { get; }

        public FrameInfo(RequestBase request, byte device, MiniFrame miniFrame)
        {
            Device = device;
            Funcode = miniFrame.Funcode;
            Start = miniFrame.Start;
            Count = miniFrame.Count;
            RequestArray = request.CreateRequestArray(device, miniFrame);
            FullStart = ModbusPub.CalcFullAddr(device, miniFrame.Funcode, miniFrame.Start);
            RspnsLen = request.CalcResponseLen(miniFrame);
        }
    }

    internal static class ModbusPub
    {
        public static int CalcFullAddr(byte device, FunCode funcode, ushort addr)
        {
            return (device << 24) | ((byte)funcode << 16) | addr;
        }
    }

    internal sealed class ModbusFactory
    {
        private static readonly Lazy<ModbusFactory> instance = new Lazy<ModbusFactory>(() => new ModbusFactory());

        public static ModbusFactory Instance { get { return instance.Value; } }

        private ModbusFactory() { }

        public RequestBase CreateRequest(string mode)
        {
            Type type = Type.GetType(typeof(RequestBase).FullName.Replace("Base", mode), true);
            return Activator.CreateInstance(type) as RequestBase;
        }

        public ResponseBase CreateResponse(string mode, Dictionary<int, RegisterType> dictype)
        {
            Type type = Type.GetType(typeof(ResponseBase).FullName.Replace("Base", mode), true);
            return Activator.CreateInstance(type, new object[] { dictype }) as ResponseBase;
        }
    }

    internal class RequestBase
    {
        private const int BitInByte = 8;
        protected const int LenOfRequest = 6;
        protected int LenOfHead;
        protected int LenOfRear;

        public virtual byte[] CreateRequestArray(byte device, MiniFrame miniFrame)
        {
            byte[] result = new byte[LenOfRequest]
            {
                device,
                (byte)miniFrame.Funcode,
                (byte)(miniFrame.Start >> 8),
                (byte)(miniFrame.Start & 0xff),
                (byte)(miniFrame.Count >> 8),
                (byte)(miniFrame.Count & 0xff)
            };
            return result;
        }

        public int CalcResponseLen(MiniFrame miniFrame)
        {
            int dataLen = 0;
            switch (miniFrame.Funcode) {
                case FunCode.CoilStatus:
                case FunCode.InputStatus: {
                    dataLen = (miniFrame.Count / BitInByte + (0 == miniFrame.Count % BitInByte ? 0 : 1));
                    break;
                }
                case FunCode.HoldingRegister:
                case FunCode.InputRegister: {
                    dataLen = (miniFrame.Count * 2);
                    break;
                }
            }
            return LenOfHead + dataLen + LenOfRear;
        }
    }

    internal struct AddrVal
    {
        public int FullAddr { get; }
        public float Value { get; }

        public AddrVal(int fulladdr, float value)
        {
            FullAddr = fulladdr;
            Value = value;
        }
    }

    internal class ResponseBase
    {
        private const int ByteInRegister = 2;
        protected int IndexOfFunCode;
        protected int IndexOfLength;
        protected int LenOfHead;
        protected int LenOfRear;
        protected Dictionary<int, RegisterType> dicType;

        public ResponseBase(Dictionary<int, RegisterType> dictype)
        {
            dicType = new Dictionary<int, RegisterType>(dictype);
        }

        private float ParseByReflection(RegisterType registertype, byte[] data)
        {
            Type type = typeof(RegisterType);
            MethodInfo methodInfo = typeof(BitConverter).GetMethod(type.GetField(Enum.GetName(type, registertype)).GetCustomAttribute<DescriptionAttribute>(false).Description);
            return Convert.ToSingle(methodInfo.Invoke(null, new object[] { data, 0 }));
        }

        private List<AddrVal> GetRegisterValue(byte[] data, FrameInfo fram)
        {
            List<AddrVal> rslt = new List<AddrVal>();
            int regPosition = 0;
            while (regPosition < fram.Count) {
                if (dicType.TryGetValue(fram.FullStart + regPosition, out RegisterType registerType)) {
                    int regType = (int)registerType;
                    int regCount = regType >> 4 & 0x07;
                    byte[] unit = new byte[ByteInRegister * regCount];
                    int bytePosition = ByteInRegister * regPosition;
                    if (0 == regType >> 7) {
                        Buffer.BlockCopy(data, bytePosition, unit, 0, unit.Length);
                        Array.Reverse(unit);
                    } else {
                        for (int i = 0; i < unit.Length; i += ByteInRegister) {
                            unit[i] = data[bytePosition + i + 1];
                            unit[i + 1] = data[bytePosition + i];
                        }
                    }
                    rslt.Add(new AddrVal(fram.FullStart + regPosition, ParseByReflection(registerType, unit)));
                    regPosition += regCount;
                } else {
                    ++regPosition;
                }
            }
            return rslt;
        }

        private List<AddrVal> GetStatusValue(byte[] data, FrameInfo fram)
        {
            List<AddrVal> rslt = new List<AddrVal>();
            BitArray bitArray = new BitArray(data);
            for (int i = 0; i < fram.Count; ++i) {
                rslt.Add(new AddrVal(fram.FullStart + i, bitArray[i] ? 1 : 0));
            }
            return rslt;
        }

        public bool IsValid(byte[] gram, FrameInfo frame)
        {
            return (frame.Funcode == (FunCode)gram[IndexOfFunCode] && frame.RspnsLen == LenOfHead + gram[IndexOfLength] + LenOfRear);
        }

        public List<AddrVal> ParseGram(byte[] gram, FrameInfo fram)
        {
            FunCode funcode = (FunCode)gram[IndexOfFunCode];
            byte[] data = new byte[fram.RspnsLen - LenOfHead - LenOfRear];
            Buffer.BlockCopy(gram, LenOfHead, data, 0, data.Length);
            List<AddrVal> rslt = new List<AddrVal>();
            switch (funcode) {
                case FunCode.CoilStatus:
                case FunCode.InputStatus: {
                    rslt = GetStatusValue(data, fram);
                    break;
                }
                case FunCode.HoldingRegister:
                case FunCode.InputRegister: {
                    rslt = GetRegisterValue(data, fram);
                    break;
                }
            }
            return rslt;
        }
    }
}