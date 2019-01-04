using System;
using Observer;

namespace Adapter
{
    internal interface IAdapter
    {
        bool IsConnect { get; }

        event EventHandler<LogEventArgs> Log;

        bool Connect();
        void DisConnect();
    }

    internal interface IInputAdapter : IAdapter
    {
        //void InitPt(string[][] para);
        bool GetData(ref int[] update, float[] value);
    }

    internal interface IOutputAdapter : IAdapter
    {
        //void InitPt(string[][] para);
        void SetData(int inNo, int[] update, DateTime time, float[] value);
        void SetState(string tag, DateTime time, int value);
    }

    internal class AdapterFactory
    {
        private static readonly Lazy<AdapterFactory> instance = new Lazy<AdapterFactory>(() => new AdapterFactory());

        public static AdapterFactory Instance { get { return instance.Value; } }

        private AdapterFactory() { }

        public static IInputAdapter CreateInput(string className, string path, string[][] para, EventHandler<LogEventArgs> log)
        {
            Type type = Type.GetType(typeof(IInputAdapter).FullName.Replace("IInputAdapter", $"Input{className}"), true);
            IInputAdapter input = (IInputAdapter)Activator.CreateInstance(type, new object[] { path, para });
            input.Log += new EventHandler<LogEventArgs>(log);
            return input;
        }

        public static IOutputAdapter CreateOutput(string className, string[][] para, EventHandler<LogEventArgs> log)
        {
            Type type = Type.GetType(typeof(IOutputAdapter).FullName.Replace("IOutputAdapter", $"Output{className}"), true);
            IOutputAdapter output = (IOutputAdapter)Activator.CreateInstance(type, new object[] { para });
            output.Log += new EventHandler<LogEventArgs>(log);
            return output;
        }
    }
}
