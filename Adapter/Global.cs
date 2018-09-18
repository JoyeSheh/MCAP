using System;
using System.Collections.Concurrent;

namespace Protocol
{

    //public struct stPtInf
    //{
    //    public string inID;
    //    public string outID;
    //    public string desc;
    //    public string[] reserve;
    //}



    public class Global
    {
        public static int CalcDueTime(int interval)
        {
            DateTime now = DateTime.Now;
            int secondsbynow = now.Hour * 3600 + now.Minute * 60 + now.Second;
            return (interval - secondsbynow % interval) * 1000 - now.Millisecond;
        }
    }

}
