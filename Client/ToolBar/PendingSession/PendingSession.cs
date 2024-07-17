using System;
using System.Windows;
using System.Threading;
using System.Data.SQLite;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// �����Ự
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

            // ���ô��ڹرպ���
            Closing += WindowClosing;

            // ������ȡ�����Ự��Ϣ�б��߳�
            thread = new Thread(() => GetPendingSessionInfoList());
            thread.Start();
        }

        // ��ȡ�����Ự��Ϣ�б�
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

        // ѡ�д����Ự��Ϣ
        private void SelectPendingSessionInfo_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            selectedPendingSessionInfo = (sender as DataGridRow)?.Item as PendingSessionInfo;
        }

        // ��ʾ�ж�����
        private void DisplayDetermineData_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPendingSessionInfo == null)
            {
                return;
            }
            MessageBox.Show(selectedPendingSessionInfo.determineData, "�ж�����", MessageBoxButton.YesNo, MessageBoxImage.Information);
        }

        // ������ʽ���߽׶�
        private void StartNextStage_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPendingSessionInfo == null)
            {
                return;
            }
            if (MessageBox.Show("�Ƿ�������", "����", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Dictionary<string, string> postParameter = new Dictionary<string, string>
                {
                    { "sid", selectedPendingSessionInfo.sid },
                    { "command", "StartNextStage" }
                };
                new HttpRequest("?packageName=ToolBar&structName=PendingSession&funcName=SetPendingSessionCommand", postParameter).GeneralRequest();
            }
        }

        // �رս���
        private void CloseProcess_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPendingSessionInfo == null)
            {
                return;
            }
            if (MessageBox.Show("�Ƿ�رս��̣�", "����", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Dictionary<string, string> postParameter = new Dictionary<string, string>
                {
                    { "sid", selectedPendingSessionInfo.sid },
                    { "command", "CloseProcess" }
                };
                new HttpRequest("?packageName=ToolBar&structName=PendingSession&funcName=SetPendingSessionCommand", postParameter).GeneralRequest();
            }
        }

        // ɾ�������Ự��Ϣ
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

        // ��Ӵ����Ự��Ϣ
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

        // ��ֹ��ȡ�����Ự��Ϣ�б��߳�
        private void WindowClosing(object sender, CancelEventArgs e)
        {
            thread.Abort();
        }
    }
}