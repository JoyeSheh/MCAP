using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Asst;
using Observer;

namespace Adapter
{
    internal class OutputSQLServer : IOutputAdapter
    {
        private readonly SqlConnection conn;
        private readonly SqlDataAdapter sdaReal;
        private readonly int histPeriod;

        private readonly string[][] tag;
        private readonly List<int>[] histPool;
        private readonly float[][] Value;
        private readonly InfoCenter ic;

        public event EventHandler<LogEventArgs> Log;

        public OutputSQLServer(string[][] para)
        {
            IniFile iniPrcs = new IniFile("SQLServer.ini");

            SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder
            {
                DataSource = iniPrcs.GetStr("CONNECT", "Server"),
                InitialCatalog = iniPrcs.GetStr("CONNECT", "Database"),
                UserID = iniPrcs.GetStr("CONNECT", "User"),
                Password = iniPrcs.GetStr("CONNECT", "Password")
            };

            conn = new SqlConnection(scsb.ConnectionString);

            sdaReal = new SqlDataAdapter("SELECT T_TAG,T_TIME,D_VALUE FROM T_INFO_REALDATA", conn)
            {
                UpdateCommand = PrepareCommand("UPDATE T_INFO_REALDATA SET T_TIME=@T_TIME, D_VALUE=@D_VALUE WHERE T_TAG=@T_TAG"),
            };

            histPeriod = iniPrcs.GetInt("ADVANCE", "HistPeriod", 300);

            int inputNum = para.Length;
            tag = new string[inputNum][];
            histPool = new List<int>[inputNum];
            Value = new float[inputNum][];

            Array.ConstrainedCopy(para, 0, tag, 0, para.Length);
            for (int i = 0; i < para.Length; ++i)
            {
                histPool[i] = new List<int>();
                Value[i] = new float[para[i].Length];
            }

            ic = new InfoCenter("Main");
        }

        public bool Connect()
        {
            try
            {
                conn.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool IsConnect => ConnectionState.Open==conn.State;



        public void DisConnect()
        {
            conn.Close();
        }

        public void SetData(int inNo, int[] update, DateTime time, float[] value)
        {
#if DEBUG
            try
            {
#endif
                UpdateRealTable(inNo, update, time, value);
                UpdateHistTable(inNo, update, time, value);
#if DEBUG
            }
            catch (Exception ex)
            {
                ic.Log(DateTime.Now, ex);
            }
#endif
        }

        private SqlCommand PrepareCommand(string cmd)
        {
            SqlParameter sParaTag = new SqlParameter("@T_TAG", SqlDbType.VarChar, 50, "T_TAG");
            SqlParameter sParaTime = new SqlParameter("@T_TIME", SqlDbType.DateTime, 23, "T_TIME");
            SqlParameter sParaValue = new SqlParameter("@D_VALUE", SqlDbType.Float, 4, "D_VALUE");
            SqlCommand sqlCommand = new SqlCommand(cmd, conn);
            sqlCommand.Parameters.AddRange(new SqlParameter[] { sParaTag, sParaTime, sParaValue });
            sqlCommand.UpdatedRowSource = UpdateRowSource.None;

            return sqlCommand;
        }

        private void UpdateRealTable(int inNo, int[] update, DateTime time, float[] value)
        {
            List<int> lUpdate = new List<int>(update);
            using (DataTable dtbl = new DataTable())
            {
                try
                {
                    sdaReal.Fill(dtbl);
                }
                catch (SqlException ex)
                {
                    ic.Log(DateTime.Now, ex);
                    return;
                }

                if (0 < dtbl.Rows.Count)
                {
                    try
                    {
                        dtbl.PrimaryKey = new DataColumn[] { dtbl.Columns["T_TAG"] };
                    }
                    catch (Exception ex)
                    {
                        ic.Log(DateTime.Now, ex);
                        return;
                    }
                    foreach (int idx in update)
                    {
                        DataRow dr = dtbl.Rows.Find(tag[inNo][idx]);
                        if (null != dr)
                        {
                            dr[1] = time;
                            dr[2] = value[idx];
                            lUpdate.Remove(idx);
                        }
                    }
                    if (update.Length > lUpdate.Count)
                    {
                        try
                        {
                            int tt = sdaReal.Update(dtbl);
                            dtbl.AcceptChanges();
                        }
                        catch (SqlException ex)
                        {
                            ic.Log(DateTime.Now, ex);
                        }
                    }
                }
            }
            if (0 < lUpdate.Count)
            {
                InsertTable(inNo, lUpdate.ToArray(), time, value, "T_INFO_REALDATA");
            }
        }

        private void UpdateHistTable(int inNo, int[] update, DateTime time, float[] value)
        {
            foreach (int idx in update)
            {
                histPool[inNo].Add(idx);
                Value[inNo][idx] = value[idx];
            }

            if (0 == (time.Minute * 60 + time.Second) % histPeriod && 0 < histPool[inNo].Count)
            {
                InsertTable(inNo, histPool[inNo].Distinct().ToArray(), time, Value[inNo], "T_INFO_HISTDATA");
                histPool[inNo].Clear();
            }
        }

        private void InsertTable(int inNo, int[] update, DateTime time, float[] value, string table)
        {
            using (DataTable dataTable = new DataTable())
            {
                dataTable.Columns.Add("T_TAG", typeof(string));
                dataTable.Columns.Add("T_TIME", typeof(DateTime));
                dataTable.Columns.Add("D_VALUE", typeof(float));

                foreach (int idx in update)
                {
                    dataTable.Rows.Add(tag[inNo][idx], time, value[idx]);
                }

                using (SqlBulkCopy bcp = new SqlBulkCopy(conn)
                {
                    BatchSize = 800,
                    BulkCopyTimeout = 150,
                    DestinationTableName = table
                })
                {
                    bcp.ColumnMappings.Add("T_TAG", "T_TAG");
                    bcp.ColumnMappings.Add("T_TIME", "T_TIME");
                    bcp.ColumnMappings.Add("D_VALUE", "D_VALUE");
                    try
                    {
                        bcp.WriteToServer(dataTable);
                    }
                    catch (InvalidOperationException ex)
                    {
                        ic.Log(DateTime.Now, ex);
                    }
                }
            }
        }

        public void SetState(string tag, DateTime time, int value)
        {
            try
            {
                string cmd;
                using (SqlDataAdapter sdaComm = new SqlDataAdapter(string.Format("SELECT * FROM T_INFO_REALDATA WHERE T_TAG = '{0}'", tag), conn))
                using (DataTable dtbl = new DataTable())
                {
                    sdaComm.Fill(dtbl);
                    if (0 < dtbl.Rows.Count)
                    {
                        cmd = string.Format("UPDATE T_INFO_REALDATA SET T_TIME = '{0}', D_VALUE = {1} WHERE T_TAG = '{2}'", time.ToString(), value, tag);
                    }
                    else
                    {
                        cmd = string.Format("INSERT INTO T_INFO_REALDATA(T_TAG, T_TIME, D_VALUE) VALUES('{0}', '{1}', {2})", tag, time.ToString(), value);
                    }
                    using (SqlCommand sqlcmd = new SqlCommand(cmd, conn))
                    {
                        sqlcmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                ic.Log(DateTime.Now, ex);
            }
        }
    }
}
