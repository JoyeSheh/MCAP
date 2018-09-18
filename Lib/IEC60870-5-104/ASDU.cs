using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Infrastructure
{
    public class ASDU
    {
        public enum Type
        {
            M_SP_NA_1 = 0x01,   //单点信息
            M_ME_NA_1 = 0x09,   //测量值, 规一化值
            M_ME_NB_1 = 0x0b,   //测量值, 标度化值
            M_ME_NC_1 = 0x0d,   //测量值, 短浮点数
            M_IT_NA_1 = 0x0f,   //累计量
            C_IC_NA_1 = 0x64,   //总召唤命令
            C_CI_NA_1 = 0x65    //电能脉冲召唤命令
            /*
            //在监视方向的过程信息
            Single_point = 1,//单点信息
            Double_point = 3,//双点信息
            BuSit = 5,//步位置信息
            BackMValue = 9,//规一化测量值
            BDMValue = 11,//标度化测量值
            FloatMValue = 13,//短浮点型测量值
            AddValue = 15,//累积值
            Time_Single_point = 30,//带时标单点信息
            Time_Double_point = 31,//带时标双点信息
            Time_BuSit = 32,//带时标步位置信息
            Time_Bu32Bits = 33,//带时标步位置信息
            Time_BackMValue = 34,//带时标规一化测量值
            Time_BDMValue = 35,//带时标标度化测量值
            Time_FloatMValue = 36,//带时标短浮点型测量值
            Time_AddValue = 37,//带时标累积值
            Time_RelayEvent = 38,//带时标继电器保护装置事件
            Time_RelayStartEvents = 39,//带时标继电器保护装置成组启动事件
            Time_RelayOutputInfos = 40,//带时标继电器保护装置成组出口信息           
             * */
        }

        public enum Reason
        {
            UnDef = 0x00,
            PerCyc = 0x01,  //周期循环
            Back = 0x02,    //背景扫描
            Spont = 0x03,   //突发
            Init = 0x04,    //初始化
            Req = 0x05,     //请求或被请求
            Act = 0x06,     //激活
            ActCon = 0x07,  //激活确认
            Deact = 0x08,   //停止激活
            DeactCon = 0x09,//停止激活确认
            ActTerm = 0x0a, //激活结束
            IntroGen = 0x14,//响应总召唤
            ReqcoGen = 0x25 //响应计数量总召唤
        }

        private const int Type_Len = 1;
        private const int VSQ_Len = 1;
        private const int TransRes_Len = 2;
        private const int PubAddr_Len = 2;
        private const int InfObjAddr_Len = 3;
        private Type type;
        private byte vsq;
        private Reason transRes;
        public Reason TransRes
        {
            get { return transRes; }
            set { transRes = value; }
        }

        private int pubAddr = 1;

        //数据集
        private List<DataStruct> data = new List<DataStruct>();
        //数据集
        public List<DataStruct> Data
        {
            get { return data; }
        }

        public void Pack(Type typeID)
        {
            type = typeID;
            vsq = 1;
            transRes = Reason.Act;
            pubAddr = 1;
            if (Type.C_IC_NA_1 == typeID)
            {
                data.Add(new DataStruct() { Addr = 0, Data = 0x14 });
            }
            else
            {
                data.Add(new DataStruct() { Addr = 0, Data = 0x45 });
            }
        }
        /// <summary>
        /// 解包协议
        /// </summary>
        /// <param name="buffer">待解包的数组</param>
        /// <param name="startSit">ASDU起始位置</param>
        /// <param name="lenth">长度</param>
        /// <returns></returns>
        public Reason UnPack(byte[] buffer, int startSit, int lenth)
        {
            if ((lenth + startSit > buffer.Length) || (lenth - startSit < 4))
            {
                return Reason.UnDef;
            }
            this.type = (Type)buffer[startSit];
            this.vsq = buffer[startSit + 1];
            this.transRes = (Reason)buffer[startSit + 2];
            this.pubAddr = buffer[startSit + 4];
            Reason res = transRes;
            switch (res)
            {
                case Reason.IntroGen:
                GetDataUnpack(buffer, startSit + 6, lenth - 6);
                break;
                case Reason.Spont:
                GetDataUnpack(buffer, startSit + 6, lenth - 6);
                break;
                case Reason.ActTerm:
                break;
                case Reason.Act:
                GetDataUnpack(buffer, startSit + 6, lenth - 6);
                break;
                case Reason.ActCon:
                GetDataUnpack(buffer, startSit + 6, lenth - 6);
                break;
                default:
                return Reason.UnDef;
            }
            return res;
        }
        /// <summary>
        /// 解包数据
        /// </summary>
        /// <param name="buffer">待解包的数组</param>
        /// <param name="startSit">起始位置</param>
        /// <param name="lenth">长度</param>
        private void GetDataUnpack(byte[] buffer, int startSit, int lenth)
        {
            data.Clear();
            switch (this.type)
            {
                case Type.M_SP_NA_1:
                if ((this.vsq & 0x80) == 0x80)
                {
                    QueGetSinglePoint(buffer, startSit + 3, vsq & 0x7f, GetAddr(buffer, startSit));
                }
                else
                {
                    GetSinglePoint(buffer, startSit, vsq & 0x7f);
                }
                break;
                case Type.M_ME_NC_1:
                if ((this.vsq & 0x80) == 0x80)
                {
                    QueGetFloatMValue(buffer, startSit + 3, vsq & 0x7f, GetAddr(buffer, startSit));
                }
                else
                {
                    GetFloatMValue(buffer, startSit, vsq & 0x7f);
                }
                break;



                default:
                break;
            }
        }
        /// <summary>
        /// 顺序解包单点数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startSit"></param>
        /// <param name="lenth"></param>
        /// <param name="startAddr"></param>
        private void QueGetSinglePoint(byte[] buffer, int startSit, int lenth, int startAddr)
        {
            for (int i = 0; i < lenth; i++)
            {
                data.Add(new DataStruct() { Addr = startAddr + i, Data = buffer[startSit + i] });
            }
        }
        /// <summary>
        /// 顺序解包浮点测量值
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startSit"></param>
        /// <param name="lenth"></param>
        /// <param name="startAddr"></param>
        private void QueGetFloatMValue(byte[] buffer, int startSit, int lenth, int startAddr)
        {
            for (int i = 0; i < lenth; i++)
            {
                data.Add(new DataStruct() { Addr = startAddr + i, Data = BitConverter.ToSingle(buffer, startSit + 5 * i), Quality = (DataStruct.QualityType)buffer[startSit + 5 * i + 4] });
            }
        }
        /// <summary>
        /// 顺序解包含时标的单点测量值
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startSit"></param>
        /// <param name="lenth"></param>
        /// <param name="startAddr"></param>
        private void QueGetTimeSinglePoint(byte[] buffer, int startSit, int lenth, int startAddr)
        {
            for (int i = 0; i < lenth; i++)
            {
                data.Add(new DataStruct() { Addr = startAddr + i, Data = buffer[startSit + 8 * i], Time = GetDateTime(buffer, startSit + 8 * i + 1) });
            }
        }
        /// <summary>
        /// 顺序解包含时标的浮点测量值
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startSit"></param>
        /// <param name="lenth"></param>
        /// <param name="startAddr"></param>
        private void QueGetTimeFloatMValue(byte[] buffer, int startSit, int lenth, int startAddr)
        {
            for (int i = 0; i < lenth; i++)
            {
                data.Add(new DataStruct() { Addr = startAddr + i, Data = BitConverter.ToSingle(buffer, startSit + 12 * i), Time = GetDateTime(buffer, startSit + 12 * i + 4), Quality = (DataStruct.QualityType)buffer[startSit + 12 * i + 11] });
            }
        }
        /// <summary>
        /// 非顺序解包单点数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startSit"></param>
        /// <param name="lenth"></param>
        private void GetSinglePoint(byte[] buffer, int startSit, int lenth)
        {
            for (int i = 0; i < lenth; i++)
            {
                data.Add(new DataStruct() { Addr = GetAddr(buffer, startSit + 4 * i), Data = buffer[startSit + 4 * i + 3] });
            }
        }
        /// <summary>
        /// 非顺序解包浮点测量值
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startSit"></param>
        /// <param name="lenth"></param>
        private void GetFloatMValue(byte[] buffer, int startSit, int lenth)
        {
            for (int i = 0; i < lenth; i++)
            {
                data.Add(new DataStruct() { Addr = GetAddr(buffer, startSit + 8 * i), Data = BitConverter.ToSingle(buffer, startSit + 8 * i + 3), Quality = (DataStruct.QualityType)buffer[startSit + 8 * i + 7] });
            }
        }
        /// <summary>
        /// 非顺序解包含时标的单点测量值
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startSit"></param>
        /// <param name="lenth"></param>
        private void GetTimeSinglePoint(byte[] buffer, int startSit, int lenth)
        {
            for (int i = 0; i < lenth; i++)
            {
                data.Add(new DataStruct() { Addr = GetAddr(buffer, startSit + 11 * i), Data = buffer[startSit + 11 * i + 3], Time = GetDateTime(buffer, startSit + 11 * i + 4) });
            }
        }
        /// <summary>
        /// 非顺序解包含时标的浮点测量值
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startSit"></param>
        /// <param name="lenth"></param>
        private void GetTimeFloatMValue(byte[] buffer, int startSit, int lenth)
        {
            for (int i = 0; i < lenth; i++)
            {
                data.Add(new DataStruct() { Addr = GetAddr(buffer, startSit + 14 * i), Data = BitConverter.ToSingle(buffer, startSit + 15 * i + 3), Time = GetDateTime(buffer, startSit + 15 * i + 7), Quality = (DataStruct.QualityType)buffer[startSit + 15 * i + 14] });
            }
        }
        /// <summary>
        /// 解析时间标志
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startSit"></param>
        /// <returns></returns>
        private DateTime GetDateTime(byte[] buffer, int startSit)
        {
            try
            {
                int ms = buffer[startSit + 1] * 256 + buffer[startSit + 0];
                DateTime datetime = new DateTime
                    (2000 + buffer[startSit + 6], buffer[startSit + 5], buffer[startSit + 4] & 0x1f, buffer[startSit + 3],
                    buffer[startSit + 2], ms / 1000, ms % 1000);
                return datetime;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return new DateTime();
            }
        }
        /// <summary>
        /// 解析数据地址
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startSit"></param>
        /// <returns></returns>
        private int GetAddr(byte[] buffer, int startSit)
        {
            return (buffer[startSit + 2] << 16) + (buffer[startSit + 1] << 8) + buffer[startSit];
        }
        /// <summary>
        /// 转换成字符串
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            List<byte> temp = new List<byte>();
            temp.Add((byte)this.type);
            temp.Add(this.vsq);
            temp.Add((byte)transRes);
            temp.Add(0);
            temp.Add((byte)this.pubAddr);
            temp.Add(0);
            foreach (var member in data)
            {

                    temp.Add((byte)(member.Addr & 0x000000ff));
                    temp.Add((byte)(member.Addr & 0x0000ff00));
                    temp.Add((byte)(member.Addr & 0x00ff0000));
                
                if (member.Data != null)
                {
                    if (member.DataLenth == 1)
                    {
                        temp.Add((byte)member.Data);
                    }
                }
                if (member.Time != null)
                {
                    temp.Add((byte)((member.Time.Value.Millisecond + member.Time.Value.Second * 1000) & 0x000000ff));
                    temp.Add((byte)((member.Time.Value.Millisecond + member.Time.Value.Second * 1000) & 0x0000ff00));
                    temp.Add((byte)(member.Time.Value.Minute));
                    temp.Add((byte)(member.Time.Value.Hour));
                    temp.Add((byte)(member.Time.Value.Day + (((int)member.Time.Value.DayOfWeek << 6) & 0xe0)));
                    temp.Add((byte)(member.Time.Value.Month));
                    temp.Add((byte)(member.Time.Value.Year % 2000));
                }
                if (member.Quality != null)
                {
                    temp.Add((byte)member.Quality);
                }
            }
            return temp.ToArray();
        }
        /// <summary>
        /// 重写ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("\r\nASDU:");
            byte[] temp = this.ToArray();
            for (int i = 0; i < temp.Length; i++)
            {
                sb.Append("0x" + temp[i].ToString("X2") + " ");
            }
            return sb.ToString();
        }
    }

    public class DataStruct
    {
        //长度
        private int _dataLenth = 1;
        public int DataLenth
        {
            get { return _dataLenth; }
            set { _dataLenth = value; }
        }
        //地址
        private int _addr;
        public int Addr
        {
            get { return _addr; }
            set { _addr = value; }
        }
        //数据
        private double? _data;
        public double? Data
        {
            get { return _data; }
            set { _data = value; }
        }
        //时间
        private DateTime? _time;
        public DateTime? Time
        {
            get { return _time; }
            set { _time = value; }
        }
        //数据质量
        private QualityType? _quality;
        public QualityType? Quality
        {
            get { return _quality; }
            set { _quality = value; }
        }
        public enum QualityType
        {
            OK = 0,
            UK1 = 1,
            UK2 = 2,
            UK3 = 3,
            UK4 = 4,
            UK5 = 5,
            UK6 = 6,
        }
    }
}
