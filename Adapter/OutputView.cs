using Observer;
using System;

namespace Adapter
{
    internal class OutputView : IOutputAdapter
    {
        public event EventHandler<LogEventArgs> Log;

        public OutputView(string[][] para)
        {
        }

        public bool Connect()
        {
            return true;
        }

        public void DisConnect()
        {
        }

        public void SetData(int inNo, int[] update, DateTime time, float[] real)
        {
        }

        public bool IsConnect()
        {
            return true;
        }

        public void SetState(string tag, DateTime time, int value)
        {

        }
    }
}
