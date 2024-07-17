using System;
using System.Windows;
using System.Threading;
using System.Data.SQLite;

// 正式上线会话
namespace Client
{
    public class Session
    {
        // 获取正式上线会话信息列表
        public static void GetSessionInfoList(SessionController sessionController)
        {
            while (true)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    sessionController.sessionInfoList.Clear();
                    using (SQLiteConnection conn = new SQLiteConnection(@"Data Source = config\client.db"))
                    {
                        conn.Open();
                        string sql = "select * from SessionInfo order by heartbeat desc";
                        using (SQLiteCommand sqlCommand = new SQLiteCommand(sql, conn))
                        {
                            using (SQLiteDataReader dataReader = sqlCommand.ExecuteReader())
                            {
                                while (dataReader.Read())
                                {
                                    SessionInfo sessionInfo = new SessionInfo()
                                    {
                                        fid = (string)dataReader["fid"],
                                        sid = (string)dataReader["sid"],
                                        publicIP = (string)dataReader["publicIP"],
                                        privateIP = (string)dataReader["privateIP"],
                                        username = (string)dataReader["username"],
                                        processName = (string)dataReader["processName"],
                                        pid = (string)dataReader["pid"],
                                        bit = (string)dataReader["bit"],
                                        listenerName = (string)dataReader["listenerName"],
                                        connectTime = (string)dataReader["connectTime"],
                                        heartbeat = Convert.ToInt32(dataReader["heartbeat"]),
                                        currentHeartbeat = CalculateCurrentHeartbeat(Convert.ToInt32(dataReader["heartbeat"]))
                                    };
                                    sessionController.sessionInfoList.Add(sessionInfo);
                                }
                            }
                        }
                    }
                    sessionController.sessionInfoList_DataGrid.ItemsSource = sessionController.sessionInfoList;
                }));
                Thread.Sleep(500);
                if (Update.threadClose)
                {
                    break;
                }
            }
        }

        // 计算当前心跳
        public static string CalculateCurrentHeartbeat(int heartbeat)
        {
            heartbeat = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() - heartbeat;
            if (heartbeat < 60)
            {
                return heartbeat.ToString() + 's';
            }
            else if (heartbeat < 3600)
            {
                heartbeat /= 60;
                return heartbeat.ToString() + "min";
            }
            else
            {
                heartbeat /= 3600;
                return heartbeat.ToString() + "h";
            }
        }

        // 添加正式上线会话信息
        public static void AddSessionInfo(SessionInfo sessionInfo)
        {
            using (SQLiteConnection conn = new SQLiteConnection(@"Data Source = config\client.db"))
            {
                conn.Open();
                string sql = "replace into SessionInfo (fid, sid, publicIP, privateIP, username, processName, pid, bit, listenerName, connectTime, heartbeat) values (@fid, @sid, @publicIP, @privateIP, @username, @processName, @pid, @bit, @listenerName, @connectTime, @heartbeat)";
                using (SQLiteCommand sqlCommand = new SQLiteCommand(sql, conn))
                {
                    sqlCommand.Parameters.AddWithValue("@fid", sessionInfo.fid);
                    sqlCommand.Parameters.AddWithValue("@sid", sessionInfo.sid);
                    sqlCommand.Parameters.AddWithValue("@publicIP", sessionInfo.publicIP);
                    sqlCommand.Parameters.AddWithValue("@privateIP", sessionInfo.privateIP);
                    sqlCommand.Parameters.AddWithValue("@username", sessionInfo.username);
                    sqlCommand.Parameters.AddWithValue("@processName", sessionInfo.processName);
                    sqlCommand.Parameters.AddWithValue("@pid", sessionInfo.pid);
                    sqlCommand.Parameters.AddWithValue("@bit", sessionInfo.bit);
                    sqlCommand.Parameters.AddWithValue("@listenerName", sessionInfo.listenerName);
                    sqlCommand.Parameters.AddWithValue("@connectTime", sessionInfo.connectTime);
                    sqlCommand.Parameters.AddWithValue("@heartbeat", sessionInfo.heartbeat);
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }
    }
}