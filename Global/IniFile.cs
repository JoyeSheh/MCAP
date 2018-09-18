using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Asst
{
    internal class IniFile
    {
        #region
        //读取整型键值
        [DllImport("kernel32", EntryPoint = "GetPrivateProfileIntW", CharSet = CharSet.Unicode)]
        private static extern int getKeyIntValue(string section, string Key, int nDefault, string filename);

        //读取字符串键值
        [DllImport("kernel32", EntryPoint = "GetPrivateProfileStringW", CharSet = CharSet.Unicode)]
        private static extern int getKeyStrValue(string section, string key, int lpDefault, [MarshalAs(UnmanagedType.LPWStr)] string szValue, int nlen, string filename);

        //写字符串键值
        [DllImport("kernel32", EntryPoint = "WritePrivateProfileStringW", CharSet = CharSet.Unicode)]
        private static extern bool setKeyValue(string section, string key, string szValue, string filename);
        #endregion

        private const int BUFFER_SIZE = 1024;
        private string _path;		//ini文件路径

        public IniFile(string path)
        {
            _path = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + path;
        }

        /// 读整型键值
        public int GetInt(string section, string key, int nDefault)
        {
            return getKeyIntValue(section, key, nDefault, _path);
        }

        /// 读字符串键值
        public string GetStr(string section, string key)
        {
            string szBuffer = new string('0', BUFFER_SIZE);
            int nlen = getKeyStrValue(section, key, 0, szBuffer, BUFFER_SIZE, _path);
            return szBuffer.Substring(0, nlen);
        }

        /// 写整型键值
        public bool SetInt(string section, string key, int dwValue)
        {
            return setKeyValue(section, key, dwValue.ToString(), _path);
        }

        /// 写字符串键值
        public bool SetStr(string section, string key, string szValue)
        {
            return setKeyValue(section, key, szValue, _path);
        }
    }
}
