using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Applctn.Http
{
    internal struct ParaIndex
    {
        public int Index { get; }
        public int JIndex { get; }
        public float Ratio { get; }
        public ParaIndex(int index,int jindex,float ratio)
        {
            Index=index;
            JIndex=jindex;
            Ratio=ratio;
        }
    }

    class HttpPub
    {
        public static string HttpGet(string url)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method="GET";
            request.ContentType="text/html;charset=UTF-8";
            request.KeepAlive=false;

            try
            {
                using(HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                using(StreamReader reader = new StreamReader(response.GetResponseStream(),Encoding.UTF8))
                {
                    return reader.ReadToEnd().Trim();
                }
            }
            catch(Exception)
            {
                return string.Empty;
            }
            finally
            {
                request.Abort();
            }
        }

        public static bool IsValid(string result,string key,int value)
        {
            if(string.Empty.Equals(result))
            {
                return false;
            }

            try
            {
                return (value==Convert.ToInt32(JToken.Parse(result)[key]));
            }
            catch
            {
                return false;
            }
        }
    }
}
