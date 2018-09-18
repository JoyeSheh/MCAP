using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Asst
{
    internal class InfoCenter
    {
        private string logPath;
        private string gramPath;

        public InfoCenter(string path)
        {
            logPath = path + "\\log";
            gramPath = path + "\\gram";
        }

        public void Log(DateTime dt, Exception ex)
        {
            if(!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            try
            {
                using(FileStream fs = new FileStream(Path.Combine(logPath, string.Format("{0}.txt", dt.ToString("yyyyMMdd"))), FileMode.Append, FileAccess.Write, FileShare.Read))
                using(StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(dt.ToString("HH:mm:ss"));
                    sw.WriteLine(ex.GetType().Name);
                    sw.WriteLine(ex.Message);
                    sw.WriteLine(ex.StackTrace);
                    sw.WriteLine();
                }
            }
            catch
            { }
        }

        public void Gram(DateTime dt, string direct, byte[] data)
        {
            if(!Directory.Exists(gramPath))
            {
                Directory.CreateDirectory(gramPath);
            }

            try
            {
                using(FileStream fs = new FileStream(Path.Combine(gramPath, string.Format("{0}.txt", dt.ToString("yyyyMMdd"))), FileMode.Append, FileAccess.Write, FileShare.Read))
                using(StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(string.Format("{0}\t{1}:", dt.ToString("HH:mm:ss"), direct));
                    sw.WriteLine(BitConverter.ToString(data).Replace('-', ' '));
                    sw.WriteLine();
                }
            }
            catch(Exception ex)
            {
                Log(DateTime.Now, ex);
            }
        }

        public void Gram(DateTime dt, string message)
        {
            if (!Directory.Exists(gramPath))
            {
                Directory.CreateDirectory(gramPath);
            }

            try
            {
                using (FileStream fs = new FileStream(Path.Combine(gramPath, string.Format("{0}.txt", dt.ToString("yyyyMMdd"))), FileMode.Append, FileAccess.Write, FileShare.Read))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(dt.ToString("HH:mm:ss"));
                    sw.WriteLine(message);
                    sw.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Log(DateTime.Now, ex);
            }
        }
    }
}
