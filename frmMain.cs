using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Asst;
using Protocol;
using Observer;
using System.Collections.Generic;
using Adapter;
using System.Collections.Concurrent;

namespace MCAP
{
    internal struct ShowInfo
    {
        public string Input { get; }
        public string Output { get; }
        public string Desc { get; }

        public ShowInfo(string input, string output, string desc)
        {
            Input = input;
            Output = output;
            Desc = desc;
        }
    }

    internal struct ChannelData
    {
        public int ChannelNo { get; }
        public int[] Update { get; }
        public DateTime Time { get; }
        public float[] Value { get; }

        public ChannelData(DataEventArgs data, float[] value)
        {
            ChannelNo = data.ChannelNo;
            Update = new int[data.Update.Length];
            Buffer.BlockCopy(data.Update, 0, Update, 0, sizeof(int) * data.Update.Length);
            Time = data.Time;
            Value = new float[value.Length];
            Buffer.BlockCopy(value, 0, Value, 0, sizeof(float) * value.Length);
        }
    }

    internal struct ChannelState
    {
        public DateTime Time { get; }
        public string ConnTag { get; }
        public string CommTag { get; }
        public int ConnValue { get; }
        public int CommValue { get; }

        public ChannelState(StateEventArgs state)
        {
            Time = state.Time;
            ConnTag = state.ConnTag;
            CommTag = state.CommTag;
            ConnValue = state.ConnValue;
            CommValue = state.CommValue;
        }
    }

    public partial class frmMain : Form
    {
        private string outType;
        private int inputNum;
        private InputChannel[] inChnl;
        private ShowInfo[][] ptShow;
        private int lastSelected;
        private IOutputAdapter outChnl;
        private ConcurrentQueue<ChannelData> queueData;
        private ConcurrentQueue<ChannelState> queueState;
        private int[] isOnNewData;
        private System.Threading.Timer tmSetOutput = null;
        private int isSetOutput = 0;
        private System.Threading.Timer tmSetUI = null;
        private int isSetUI = 0;
        private Action<Color[]> ColorInput;
        private Action<int[], DateTime[], float[]> UpdatePoint;
        private InfoCenter ic;
        private LogEvent logEvent;

        public frmMain()
        {
            IniFile cfgMain = new IniFile("main.ini");
            ic = new InfoCenter("Main");

            string[] basePath = cfgMain.GetStr("INPUT", "BasePath").Split(',');

            inputNum = basePath.Length;
            inChnl = new InputChannel[inputNum];
            ptShow = new ShowInfo[inputNum][];
            isOnNewData = new int[inputNum];
            string[][] output = new string[inputNum][];

            for (int i = 0; i < basePath.Length; ++i)
            {
                inChnl[i] = new InputChannel(i, basePath[i], ref ptShow[i], OnNewData, OnNewState);
                output[i] = new string[ptShow[i].Length];
                for (int j = 0; j < ptShow[i].Length; ++j)
                {
                    output[i][j] = ptShow[i][j].Output;
                }
            }

            logEvent = new LogEvent("Main");
            //创建输出接口
            outType = cfgMain.GetStr("IO", "Output");
            outChnl = AdapterFactory.CreateOutput(outType, output, logEvent.Log);
            //outChnl.InitPt(output);
            //outChnl.Connect();

            queueData = new ConcurrentQueue<ChannelData>();
            queueState = new ConcurrentQueue<ChannelState>();

            ColorInput = new Action<Color[]>(SetInputColor);
            UpdatePoint = new Action<int[], DateTime[], float[]>(SetPointUpdate);

            tmSetOutput = new System.Threading.Timer(new TimerCallback(SetOutput), null, Global.CalcDueTime(1), 1000);
            tmSetUI = new System.Threading.Timer(new TimerCallback(SetUI), null, Global.CalcDueTime(1), 1000);
            InitializeComponent();
        }

        private void SetOutput(object state)
        {
            if ((0 == queueData.Count && 0 == queueState.Count) || !outChnl.IsConnect)
            {
                return;
            }

            if (1 == Interlocked.Exchange(ref isSetOutput, 1))
            {
                return;
            }

            if (0 < queueData.Count && queueData.TryDequeue(out ChannelData data))
            {
                outChnl.SetData(data.ChannelNo, data.Update, data.Time, data.Value);
            }

            if (0 < queueState.Count && queueState.TryDequeue(out ChannelState stat))
            {
                outChnl.SetState(stat.ConnTag, stat.Time, stat.ConnValue);
                outChnl.SetState(stat.CommTag, stat.Time, stat.CommValue);
            }

            Interlocked.Exchange(ref isSetOutput, 0);
        }

        private void OnNewData(object sender, DataEventArgs e)
        {
            InputChannel channel = inChnl[e.ChannelNo];
            queueData.Enqueue(new ChannelData(e, channel.DataValue));

            if (e.ChannelNo == lastSelected)
            {
                //刷新测点列表
                lvwPoint.BeginInvoke(UpdatePoint, e.Update, channel.DataTime, channel.DataValue);
            }
        }

        private void OnNewState(object sender, StateEventArgs e)
        {
            queueState.Enqueue(new ChannelState(e));
        }

        private void SetInputColor(Color[] color)
        {
            lvwChnl.BeginUpdate();
            for (int i = 0; i < lvwChnl.Items.Count; ++i)
            {
                lvwChnl.Items[i].BackColor = color[i];
            }
            lvwChnl.EndUpdate();
        }

        private void SetPointUpdate(int[] update, DateTime[] time, float[] value)
        {
            lvwPoint.BeginUpdate();
            foreach (int idx in update)
            {
                lvwPoint.Items[idx].SubItems[4].Text = value[idx].ToString();
                lvwPoint.Items[idx].SubItems[5].Text = time[idx].ToString();
            }
            lvwPoint.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            lvwPoint.EndUpdate();
        }

        private void SetUI(object state)
        {
            if (1 == Interlocked.Exchange(ref isSetUI, 1))
            {
                return;
            }

#if DEBUG
            try
            {
#endif
                //刷新通道列表
                Color[] colors = new Color[inChnl.Length];
                foreach (InputChannel inchnl in inChnl)
                {
                    colors[inchnl.No] = inchnl.IsConnect ? Color.Green : Color.Red;
                }
                lvwChnl.BeginInvoke(ColorInput, colors);

                //刷新状态栏
                Color color;
                if (outChnl.IsConnect)
                {
                    color = Color.Green;
                }
                else
                {
                    color = Color.Red;
                    outChnl.Connect();
                }
                string update = string.Empty;
                UI.SetStatusStrip(ssOutChnl, color, update, DateTime.Now);
#if DEBUG
            }
            catch (Exception ex)
            {
                ic.Log(DateTime.Now, ex);
            }
#endif

            Interlocked.Exchange(ref isSetUI, 0);
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            IniFile cfgMain = new IniFile("main.ini");

            Color[] color = new Color[inChnl.Length];
            lvwChnl.BeginUpdate();
            foreach (InputChannel inchnl in inChnl)
            {
                lvwChnl.Items.Add(new ListViewItem(new string[]
                {
                    inchnl.No.ToString("D2"),
                    inchnl.Desc,
                    string.Empty
                }));

                color[inchnl.No] = inchnl.IsConnect ? Color.Green : Color.Red;
            }
            lvwChnl.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            lvwChnl.EndUpdate();

            ColorInput(color);
            lvwChnl.Items[0].Selected = true;
            lastSelected = 0;

            RedrawList(0);
            tsslblPrtc.Text = outType;
        }

        private void RedrawList(int idx)
        {
            lvwPoint.BeginUpdate();
            for (int i = 0; i < ptShow[idx].Length; ++i)
            {
                lvwPoint.Items.Add(new ListViewItem(new string[]
                {
                    i.ToString("D4"),
                    ptShow[idx][i].Input,
                    ptShow[idx][i].Output,
                    ptShow[idx][i].Desc,
                    string.Empty,
                    string.Empty
                }));
            }
            lvwPoint.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            lvwPoint.EndUpdate();
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = (DialogResult.Cancel == MessageBox.Show("是否关闭程序？", "关闭程序", MessageBoxButtons.OKCancel, MessageBoxIcon.Question));
        }

        private void FrmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                foreach (InputChannel inchnl in inChnl)
                {
                    inchnl.Dispose();
                }
                outChnl.DisConnect();
            }
            catch (Exception ex)
            {
                ic.Log(DateTime.Now, ex);
            }
            Environment.Exit(Environment.ExitCode);
        }

        private class UI
        {
            private delegate void SetStatusStripCallback(StatusStrip ss, Color color, string update, DateTime dtm);

            private static SetStatusStripCallback sss = new SetStatusStripCallback(SetStatusStrip);

            public static void SetStatusStrip(StatusStrip ss, Color color, string update, DateTime dtm)
            {
                if (ss.InvokeRequired)
                {
                    ss.BeginInvoke(sss, new object[] { ss, color, update, dtm });
                }
                else
                {
                    ss.Items[0].BackColor = color;
                    ss.Items[1].Text = update;
                    ss.Items[2].Text = dtm.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
        }

        private void LvwChnl_MouseUp(object sender, MouseEventArgs e)
        {
            if (0 < lvwChnl.SelectedItems.Count)
            {
                int inNo = lvwChnl.Items.IndexOf(lvwChnl.FocusedItem);
                if (lastSelected == inNo)
                {
                    return;
                }
                lastSelected = inNo;

                lvwPoint.BeginUpdate();
                foreach (ListViewItem item in lvwPoint.Items)
                {
                    item.Remove();
                }
                lvwPoint.EndUpdate();
                RedrawList(inNo);
                if (0 < inChnl[inNo].TotalUpdate.Length)
                {
                    SetPointUpdate(inChnl[inNo].TotalUpdate, inChnl[inNo].DataTime, inChnl[inNo].DataValue);
                }
            }
            else
            {
                if (null != lvwChnl.FocusedItem && null == lvwChnl.GetItemAt(e.X, e.Y))
                {
                    lvwChnl.FocusedItem.Selected = true;
                }
            }
        }
    }
}
