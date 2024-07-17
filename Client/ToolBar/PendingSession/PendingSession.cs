using System;
using System.Windows;
using System.Threading;
using System.Data.SQLite;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// 待定会话
namespace Client
{
    public partial class PendingSession
    {
        private Thread thread;
        private PendingSessionInfo selectedPendingSessionInfo;
        private ObservableCollection<PendingSessionInfo> pendingSessionInfoList = new ObservableCollection<PendingSessionInfo>();

        public PendingSession()
        {
            InitializeComponent();

            // 设置窗口关闭函数
            Closing += WindowClosing;

            // 创建获取待定会话信息列表线程
            thread = new Thread(() => GetPendingSessionInfoList());
            thread.Start();
        }

        // 获取待定会话信息列表
        private void GetPendingSessionInfoList()
        {
            while (true)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    pendingSessionInfoList.Clear();
                    using (SQLiteConnection conn = new SQLiteConnection(@"Data Source = config\client.db"))
                    {
                        conn.Open();
                        string sql = "select * from PendingSessionInfo order by heartbeat desc";
                        using (SQLiteCommand sqlCommand = new SQLiteCommand(sql, conn))
                        {
                            using (SQLiteDataReader dataReader = sqlCommand.ExecuteReader())
                            {
                                while (dataReader.Read())
                                {
                                    PendingSessionInfo pendingSessionInfo = new PendingSessionInfo()
                                    {
                                        fid = (string)dataReader["fid"],
                                        sid = (string)dataReader["sid"],
                                        publicIP = (string)dataReader["publicIP"],
                                        privateIP = (string)dataReader["privateIP"],
                                        listenerName = (string)dataReader["listenerName"],
                                        connectTime = (string)dataReader["connectTime"],
                                        heartbeat = Convert.ToInt32(dataReader["heartbeat"]),
                                        currentHeartbeat = Session.CalculateCurrentHeartbeat(Convert.ToInt32(dataReader["heartbeat"])),
                                        determineData = (string)dataReader["determineData"],
                                        pending = (string)dataReader["pending"]
                                    };
                                    pendingSessionInfoList.Add(pendingSessionInfo);
                                }
                            }
                        }
                    }
                    pendingSessionInfoList_DataGrid.ItemsSource = pendingSessionInfoList;
                }));
                Thread.Sleep(500);
            }
        }

        // 选中待定会话信息
        private void SelectPendingSessionInfo_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            selectedPendingSessionInfo = (sender as DataGridRow)?.Item as PendingSessionInfo;
        }

        // 显示判定数据
        private void DisplayDetermineData_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPendingSessionInfo == null)
            {
                return;
            }
            MessageBox.Show(selectedPendingSessionInfo.determineData, "判定数据", MessageBoxButton.YesNo, MessageBoxImage.Information);
        }

        // 进入正式上线阶段
        private void StartNextStage_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPendingSessionInfo == null)
            {
                return;
            }
            if (MessageBox.Show("是否启动？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Dictionary<string, string> postParameter = new Dictionary<string, string>
                {
                    { "sid", selectedPendingSessionInfo.sid },
                    { "command", "StartNextStage" }
                };
                new HttpRequest("?packageName=ToolBar&structName=PendingSession&funcName=SetPendingSessionCommand", postParameter).GeneralRequest();
            }
        }

        // 关闭进程
        private void CloseProcess_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPendingSessionInfo == null)
            {
                return;
            }
            if (MessageBox.Show("是否关闭进程？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Dictionary<string, string> postParameter = new Dictionary<string, string>
                {
                    { "sid", selectedPendingSessionInfo.sid },
                    { "command", "CloseProcess" }
                };
                new HttpRequest("?packageName=ToolBar&structName=PendingSession&funcName=SetPendingSessionCommand", postParameter).GeneralRequest();
            }
        }

        // 删除待定会话信息
        private void DeletePendingSessionInfo_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPendingSessionInfo == null)
            {
                return;
            }
            using (SQLiteConnection conn = new SQLiteConnection(@"Data Source = config\client.db"))
            {
                conn.Open();
                string sql = "delete from PendingSessionInfo where sid=@sid";
                using (SQLiteCommand sqlCommand = new SQLiteCommand(sql, conn))
                {
                    sqlCommand.Parameters.AddWithValue("@sid", selectedPendingSessionInfo.sid);
                    sqlCommand.ExecuteNonQuery();
                }
            }
            selectedPendingSessionInfo = null;
        }

        // 添加待定会话信息
        public static void AddPendingSessionInfo(PendingSessionInfo pendingSessionInfo)
        {
            using (SQLiteConnection conn = new SQLiteConnection(@"Data Source = config\client.db"))
            {
                conn.Open();
                string sql = "replace into PendingSessionInfo (fid, sid, publicIP, privateIP, listenerName, connectTime, heartbeat, determineData, pending) values (@fid, @sid, @publicIP, @privateIP, @listenerName, @connectTime, @heartbeat, @determineData, @pending)";
                using (SQLiteCommand sqlCommand = new SQLiteCommand(sql, conn))
                {
                    sqlCommand.Parameters.AddWithValue("@fid", pendingSessionInfo.fid);
                    sqlCommand.Parameters.AddWithValue("@sid", pendingSessionInfo.sid);
                    sqlCommand.Parameters.AddWithValue("@publicIP", pendingSessionInfo.publicIP);
                    sqlCommand.Parameters.AddWithValue("@privateIP", pendingSessionInfo.privateIP);
                    sqlCommand.Parameters.AddWithValue("@listenerName", pendingSessionInfo.listenerName);
                    sqlCommand.Parameters.AddWithValue("@connectTime", pendingSessionInfo.connectTime);
                    sqlCommand.Parameters.AddWithValue("@heartbeat", pendingSessionInfo.heartbeat);
                    sqlCommand.Parameters.AddWithValue("@determineData", pendingSessionInfo.determineData);
                    sqlCommand.Parameters.AddWithValue("@pending", pendingSessionInfo.pending);
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        // 终止获取待定会话信息列表线程
        private void WindowClosing(object sender, CancelEventArgs e)
        {
            thread.Abort();
        }
    }
}