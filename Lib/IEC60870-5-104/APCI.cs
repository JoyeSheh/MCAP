using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Infrastructure
{
    public abstract class APCI
    {
        public enum UISFormat
        {
            I,
            S,
            U
        }

        protected readonly byte StartByte = 0x68;
        protected byte apdu_Lenth;
        protected byte[] ControlByte = new byte[4] { 0, 0, 0, 0 };

        private short ns;
        public short Ns
        {
            get { return ns; }
            set { ns = value; }
        }

        private short nr;
        public short Nr
        {
            get { return nr; }
            set { nr = value; }
        }

        private UISFormat uisType;
        public UISFormat UisType
        {
            get { return uisType; }
            set { uisType = value; }
        }

        //public override string ToString()
        //{
        //    StringBuilder res = new StringBuilder();
        //    res.Append("0x" + StartByte.ToString("X2") + " ");
        //    res.Append("0x" + apdu_Lenth.ToString("X2") + " ");
        //    for (int i = 0; i < 4; i++)
        //    {
        //        res.Append("0x" + ControlByte[i].ToString("X2") + " ");
        //    }
        //    return res.ToString();
        //}

        //public byte[] ToArray()
        //{
        //    return new byte[6]
        //    {
        //        this.StartByte,
        //        this.apdu_Lenth,
        //        this.ControlByte[0],
        //        this.ControlByte[1],
        //        this.ControlByte[2],
        //        this.ControlByte[3]
        //    };
        //}

        public static APCI GetApci(byte[] apcibuffer)
        {
            switch (apcibuffer[2] & 0x03)
            {
                case 3:
                {
                    if (apcibuffer[1] != 4)
                    {
                        throw new Exception("U格式数据长度异常");
                    }
                    APCIClassUFormat.UFormatType utype = (APCIClassUFormat.UFormatType)apcibuffer[2];
                    APCIClassUFormat apci = new APCIClassUFormat(utype)
                    {
                        uisType = UISFormat.U
                    };
                    return apci;
                }
                case 1:
                {
                    if (apcibuffer[1] != 4)
                    {
                        throw new Exception("S格式数据长度异常");
                    }
                    short nr = (short)((short)(apcibuffer[4] >> 1) + (short)((short)apcibuffer[5] << 7));
                    APCIClassSFormat apci = new APCIClassSFormat(nr);
                    apci.uisType = UISFormat.S;
                    return apci;
                }
                case 2:
                case 0:
                {
                    if (apcibuffer[1] < 4)
                    {
                        throw new Exception("I格式数据长度异常");
                    }
                    short ns = (short)((short)(apcibuffer[2] >> 1) + (short)((short)apcibuffer[3] << 7));
                    short nr = (short)((short)(apcibuffer[4] >> 1) + (short)((short)apcibuffer[5] << 7));
                    APCIClassIFormat apci = new APCIClassIFormat(ns, nr);
                    apci.APDULenth = apcibuffer[1];
                    apci.uisType = UISFormat.I;
                    return apci;
                }
                default:
                throw new Exception("格式解析异常");
            }

        }
    }
    /// <summary>
    /// U格式APCI
    /// </summary>
    public class APCIClassUFormat:APCI
    {
        public enum UFormatType
        {
            StartSet = 3 + (1 << 2),
            StartConfirm = 3 + (1 << 3),
            StopSet = 3 + (1 << 4),
            StopConfirm = 3 + (1 << 5),
            TestSet = 3 + (1 << 6),
            TestConfirm = 3 + (1 << 7),
        }
        public APCIClassUFormat(UFormatType type)
        {
            this.apdu_Lenth = 4;
            this.ControlByte[0] = Convert.ToByte(type);
            this.UisType = UISFormat.U;
        }
    }
    /// <summary>
    /// I格式APCI
    /// </summary>
    public class APCIClassIFormat:APCI
    {
        public APCIClassIFormat(short ns, short nr)
        {
            this.Ns = ns;
            this.Nr = nr;
            this.ControlByte[0] = Convert.ToByte((ns << 1) & 0x00fe);
            this.ControlByte[1] = Convert.ToByte((ns >> 7) & 0x00ff);
            this.ControlByte[2] = Convert.ToByte((nr << 1) & 0x00fe);
            this.ControlByte[3] = Convert.ToByte((nr >> 7) & 0x00ff);
            this.UisType = UISFormat.I;
        }

        public byte APDULenth
        {
            set
            {
                this.apdu_Lenth = value;
            }
        }
    }
    /// <summary>
    /// S格式APCI
    /// </summary>
    public class APCIClassSFormat:APCI
    {
        public APCIClassSFormat(short nr)
        {
            this.Nr = nr;
            this.apdu_Lenth = 4;
            this.ControlByte[0] = 1;
            this.ControlByte[2] = Convert.ToByte((nr << 1) & 0x00fe);
            this.ControlByte[3] = Convert.ToByte((nr >> 7) & 0x00ff);
            this.UisType = UISFormat.S;
        }
    }
}
