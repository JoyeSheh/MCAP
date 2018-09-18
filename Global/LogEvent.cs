using System;
using System.IO;

namespace Observer
{
    internal class LogEventArgs : EventArgs
    {
        public DateTime Time { get; }
        public Exception Ex { get; }

        public LogEventArgs(DateTime time, Exception ex)
        {
            Time = time;
            Ex = ex;
        }
    }

    internal class LogEvent
    {
        private string _path;

        public LogEvent(string path) => _path = path + "\\log";

        public void Log(object sender, LogEventArgs e)
        {
            if (!Directory.Exists(_path)) {
                Directory.CreateDirectory(_path);
            }

            DateTime time = e.Time;
            Exception ex = e.Ex;
            try {
                using (FileStream fs = new FileStream(Path.Combine(_path, string.Format("{0}.txt", time.ToString("yyyyMMdd"))), FileMode.Append, FileAccess.Write, FileShare.Read))
                using (StreamWriter sw = new StreamWriter(fs)) {
                    sw.WriteLine(time.ToString("HH:mm:ss"));
                    sw.WriteLine(ex.GetType().Name);
                    sw.WriteLine(ex.Message);
                    sw.WriteLine(ex.StackTrace);
                    sw.WriteLine();
                }
            } catch { }
        }
    }
}
