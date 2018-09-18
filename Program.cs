using System;
using System.Windows.Forms;
using Applctn;

namespace MCAP
{
    internal static class Program
    {
        public static frmMain frmmain;
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        /// 

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            frmmain = new frmMain();
            Application.Run(frmmain);
        }
    }
}
