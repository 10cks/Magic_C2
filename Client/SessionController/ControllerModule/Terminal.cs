using System;
using System.Windows;
using System.Data.SQLite;
using System.Windows.Media;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// 命令终端
namespace Client
{
    public class Terminal
    {
        // 获取终端历史记录
        public static void GetTerminalHistory(SessionController sessionController, string selectedTerminalId = null)
        {
            int selectedTerminalIndex = -1;
            List<TabItem> terminalList = new List<TabItem>();
            using (SQLiteConnection conn = new SQLiteConnection(@"Data Source = config\client.db"))
            {
                conn.Open();
                string sql = "select * from TerminalHistory where sid=@sid";
                using (SQLiteCommand sqlCommand = new SQLiteCommand(sql, conn))
                {
                    sqlCommand.Parameters.AddWithValue("@sid", sessionController.selectedSessionInfo.sid);
                    using (SQLiteDataReader dataReader = sqlCommand.ExecuteReader())
                    {
                        for (int i = 0; dataReader.Read(); i++)
                        {
                            if (Convert.ToInt32(dataReader["id"]).ToString() == selectedTerminalId)
                            {
                                selectedTerminalIndex = i;
                            }

                            Grid grid = new Grid
                            {
                                RowDefinitions =
                                {
                                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                                    new RowDefinition { Height = GridLength.Auto }
                                }
                            };
                            TextBox commandHistory_TextBox = new TextBox()
                            {
                                Text = (string)dataReader["command"],
                                IsReadOnly = true,
                                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                Background = Brushes.Black,
                                Foreground = Brushes.Lime,
                                Padding = new Thickness(3)
                            };
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                commandHistory_TextBox.ScrollToEnd();
                            }), System.Windows.Threading.DispatcherPriority.Background);
                            TextBox commandInput_TextBox = new TextBox()
                            {
                                Tag = Convert.ToInt32(dataReader["id"]).ToString(),
                                Background = Brushes.Black,
                                Foreground = Brushes.Lime,
                                Padding = new Thickness(3)
                            };
                            commandInput_TextBox.KeyDown += sessionController.RunTerminalCommand_KeyDown;
                            Grid.SetRow(commandHistory_TextBox, 0);
                            Grid.SetRow(commandInput_TextBox, 1);
                            grid.Children.Add(commandHistory_TextBox);
                            grid.Children.Add(commandInput_TextBox);
                            TabItem terminal_TabItem = new TabItem()
                            {
                                Tag = commandHistory_TextBox,
                                Header = sessionController.selectedSessionInfo.sid,
                                Content = grid
                            };
                            terminal_TabItem.PreviewMouseLeftButtonDown += sessionController.ScrollTerminalWindowToEnd_PreviewMouseLeftButtonDown;
                            terminalList.Add(terminal_TabItem);
                        }
                        TabItem addTerminal_TabItem = new TabItem()
                        {
                            Header = "+"
                        };
                        addTerminal_TabItem.PreviewMouseLeftButtonDown += sessionController.AddTerminalWindow_PreviewMouseLeftButtonDown;
                        terminalList.Add(addTerminal_TabItem);
                    }
                }
            }

            // 设置默认选中
            if (terminalList.Count > 1)
            {
                sessionController.terminal_TabControl.ItemsSource = terminalList;
                if (selectedTerminalIndex > -1)
                {
                    sessionController.terminal_TabControl.SelectedIndex = selectedTerminalIndex;
                }
                else
                {
                    sessionController.terminal_TabControl.SelectedIndex = sessionController.terminal_TabControl.Items.Count - 2;
                }
            }
            // 创建该会话首个窗口
            else
            {
                UpdateTerminalHistory(null, sessionController.selectedSessionInfo.sid, "########################## > help ##########################", "insert into TerminalHistory (sid, command) values (@sid, @command)", sessionController);
            }
        }

        // 执行终端命令
        public static async void RunTerminalCommand(string id, string commandInput, SessionController sessionController)
        {
            string scriptName;
            string scriptPara;
            Regex regex = new Regex(@"^\s*(\S+)\s*(.*)$");
            commandInput = commandInput.Replace("\n", "");
            Match match = regex.Match(commandInput);
            if (match.Success)
            {
                scriptName = match.Groups[1].Value;
                scriptPara = match.Groups[2].Value;
            }
            else
            {
                MessageBox.Show("命令格式错误", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 关闭当前窗口
            if (scriptName == "close")
            {
                UpdateTerminalHistory(id, sessionController.selectedSessionInfo.sid, null, "delete from TerminalHistory where id=@id", sessionController);
                return;
            }

            // 下发命令
            Dictionary<string, string> postParameter = new Dictionary<string, string>
            {
                { "id", id },
                { "sid", sessionController.selectedSessionInfo.sid },
                { "scriptType", "Terminal" }
            };
            ScriptOutputInfo scriptOutput = Function.IssueCommand(scriptName, scriptPara, postParameter);

            // 显示命令信息
            if (scriptOutput != null && scriptOutput.display != null)
            {
                UpdateTerminalHistory(id, sessionController.selectedSessionInfo.sid, "\n> " + commandInput + "\n" + scriptOutput.display + "\n", "update TerminalHistory set command=@command where id=@id", sessionController);
            }
        }

        // 更新终端历史记录
        public static void UpdateTerminalHistory(string id, string sid, string newCommand, string sql, SessionController sessionController)
        {
            // 终端历史 + 新命令
            string command = newCommand;
            if (id != null && newCommand != null)
            {
                using (SQLiteConnection conn = new SQLiteConnection(@"Data Source = config\client.db"))
                {
                    conn.Open();
                    using (SQLiteCommand sqlCommand = new SQLiteCommand("select command from TerminalHistory where id=@id", conn))
                    {
                        sqlCommand.Parameters.AddWithValue("@id", id);
                        using (SQLiteDataReader dataReader = sqlCommand.ExecuteReader())
                        {
                            while (dataReader.Read())
                            {
                                command = (string)dataReader["command"] + newCommand;
                            }
                        }
                    }
                }
            }

            // 更新终端历史
            using (SQLiteConnection conn = new SQLiteConnection(@"Data Source = config\client.db"))
            {
                conn.Open();
                using (SQLiteCommand sqlCommand = new SQLiteCommand(sql, conn))
                {
                    sqlCommand.Parameters.AddWithValue("@id", id);
                    sqlCommand.Parameters.AddWithValue("@sid", sid);
                    if (newCommand != null)
                    {
                        sqlCommand.Parameters.AddWithValue("@command", command);
                    }
                    sqlCommand.ExecuteNonQuery();
                }
            }

            if (sessionController.selectedSessionInfo != null && sessionController.selectedSessionInfo.sid == sid)
            {
                GetTerminalHistory(sessionController, id);
            }
        }
    }
}