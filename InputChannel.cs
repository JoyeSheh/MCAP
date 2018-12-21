using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Adapter;
using Asst;
using Observer;
using Protocol;

namespace MCAP
{
    internal class DataEventArgs:EventArgs
    {
        public int ChannelNo { get; }
        public int[] Update { get; }
        public DateTime Time { get; }

        public DataEventArgs(int channelNo,int[] update,DateTime time)
        {
            ChannelNo=channelNo;
            Update=update;
            Time=time;
        }
    }

    internal class StateEventArgs:EventArgs
    {
        public DateTime Time { get; }
        public string ConnTag { get; }
        public string CommTag { get; }
        public int ConnValue { get; }
        public int CommValue { get; }

        public StateEventArgs(DateTime time,string conntag,string commtag,int connvalue,int commvalue)
        {
            Time=time;
            ConnTag=conntag;
            CommTag=commtag;
            ConnValue=connvalue;
            CommValue=commvalue;
        }
    }

    internal class InputChannel
    {
        private const int TSPD = 24*60*60;
        private readonly int period;
        private readonly int[] deltaSecond;
        private readonly int ptNum;
        private readonly IInputAdapter adapter;
        private readonly LogEvent logEvent;
        private readonly Timer tmGetInput = null;
        private int isGetInput = 0;
        private readonly Timer tmCheckConn = null;
        private int isCheckConn = 0;
        private readonly string connTag;
        private readonly string commTag;
        private readonly InfoCenter ic;

        public int No { get; }
        public string Desc { get; }
        public DateTime[] DataTime { get; }
        public float[] DataValue { get; }
        public int[] TotalUpdate { get; private set; }
        public bool IsConnect { get { return adapter.IsConnect; } }

        private event EventHandler<DataEventArgs> NewData;
        private event EventHandler<StateEventArgs> NewState;

        public InputChannel(int idx,string path,ref ShowInfo[] showInfo,EventHandler<DataEventArgs> onNewData,EventHandler<StateEventArgs> onNewState)
        {
            No=idx;

            IniFile cfg = new IniFile($"{path}\\channel.ini");
            ic=new InfoCenter(path);

            //读测点清单
            string[] slines = File.ReadAllLines($"{path}\\tag.csv",Encoding.Default);
            if(null==slines||0==slines.Length)
            {
                throw new Exception("测点信息异常。");
            }

            ptNum=slines.Length;
            string[][] para = new string[ptNum][];
            showInfo=new ShowInfo[ptNum];
            DataTime=new DateTime[ptNum];
            DataValue=new float[ptNum];
            TotalUpdate=new int[0];

            for(int i = 0;i<slines.Length;++i)
            {
                string[] sfield = slines[i].Split(',');
                //保存测点属性
                showInfo[i]=new ShowInfo(sfield[1],sfield[2],sfield[3]);
                para[i]=new string[sfield.Length-3];
                para[i][0]=sfield[1];
                Array.ConstrainedCopy(sfield,4,para[i],1,para[i].Length-1);
            }

            logEvent=new LogEvent(path);
            //创建输入接口
            adapter=AdapterFactory.CreateInput(cfg.GetStr("Input","Protocol"),path,para,logEvent.Log);

            period=cfg.GetInt("Input","Period",1);
            deltaSecond=new int[TSPD/period];
            for(int i = 0;i<deltaSecond.Length;++i)
            {
                deltaSecond[i]=period*i;
            }

            connTag=cfg.GetStr("State","Connection");
            commTag=cfg.GetStr("State","Communication");

            Desc=cfg.GetStr("Other","Desc");

            tmGetInput=new Timer(new TimerCallback(GetInput),null,Global.CalcDueTime(period),1000*period);
            tmCheckConn=new Timer(new TimerCallback(CheckConn),null,Global.CalcDueTime(5),5000);

            NewData+=new EventHandler<DataEventArgs>(onNewData);
            if(0<connTag.Length||0<commTag.Length)
            {
                NewState+=new EventHandler<StateEventArgs>(onNewState);
            }
        }

        private void GetInput(object state)
        {
            if(1==Interlocked.Exchange(ref isGetInput,1))
            {
                return;
            }

            try
            {
                DateTime now = DateTime.Now;
                int tm_idx = (now.Hour*3600+now.Minute*60+now.Second)/period;
                DateTime time = now.Date.AddSeconds(deltaSecond[tm_idx]);

                int connValue = 0;
                int commValue = 0;
                if(adapter.IsConnect)
                {
                    connValue=1;
                    int[] update = null;
                    if(adapter.GetData(ref update,DataValue))
                    {
                        commValue=1;
                        foreach(int idx in update)
                        {
                            DataTime[idx]=time;
                        }
                        TotalUpdate=TotalUpdate.Union(update).ToArray();
                        DataEventArgs dataEvent = new DataEventArgs(No,update,time);
                        Interlocked.CompareExchange(ref NewData,null,null)?.Invoke(this,dataEvent);
                    }
                }
                StateEventArgs stateEvent = new StateEventArgs(time,connTag,commTag,connValue,commValue);
                Interlocked.CompareExchange(ref NewState,null,null)?.Invoke(this,stateEvent);
            }
            catch(Exception ex)
            {
                ic.Log(DateTime.Now,ex);
            }

            Interlocked.Exchange(ref isGetInput,0);
        }

        private void CheckConn(object state)
        {
            if(1==Interlocked.Exchange(ref isCheckConn,1))
            {
                return;
            }

            if(!adapter.IsConnect)
            {
                adapter.Connect();
            }

            Interlocked.Exchange(ref isCheckConn,0);
        }

        public void Dispose()
        {
            if(null!=adapter)
            {
                adapter.DisConnect();
            }
            tmGetInput.Dispose();
            tmCheckConn.Dispose();
        }
    }
}