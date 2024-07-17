using System;
using System.Windows;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Client
{
    public class Function
    {
        // 下发命令
        public static ScriptOutputInfo IssueCommand(string scriptName, string scriptPara, Dictionary<string, string> postParameter)
        {
            try
            {
                // 调用命令脚本
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "\"config\\script\\" + scriptName + "\\" + scriptName + ".py\" " + scriptPara,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                Process scriptProcess = new Process();
                scriptProcess.StartInfo = processInfo;
                scriptProcess.Start();
                ScriptOutputInfo scriptOutput = JsonConvert.DeserializeObject<ScriptOutputInfo>(scriptProcess.StandardOutput.ReadToEnd());
                scriptProcess.WaitForExit();
                if (scriptOutput == null)
                {
                    throw new Exception();
                }
                // 添加命令数据
                if (scriptOutput.selfAsmHex != null)
                {
                    postParameter.Add("selfAsmHex", scriptOutput.selfAsmHex);
                    postParameter.Add("selfAsmHash", scriptOutput.selfAsmHash);
                    postParameter.Add("paraHex", scriptOutput.paraHex);
                    new HttpRequest("?packageName=SessionController&structName=CommandController&funcName=AddCommandData", postParameter).GeneralRequest();
                }
                return scriptOutput;
            }
            catch
            {
                MessageBox.Show("命令脚本调用失败 / Python 未添加至环境变量 / 数据过长", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        // 调用工具
        public static void InvokeTool(string toolName, string toolPara)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = toolName,
                Arguments = toolPara,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            Process scriptProcess = new Process();
            scriptProcess.StartInfo = processInfo;
            scriptProcess.Start();
            Console.WriteLine(scriptProcess.StandardOutput.ReadToEnd());
            scriptProcess.WaitForExit();
        }

        // 格式化文件路径
        public static string FormatFilePath(string filePath)
        {
            filePath = Regex.Replace(filePath, "/+", "\\");
            filePath = Regex.Replace(filePath, @"\\+", "\\");
            return filePath.TrimEnd('\\');
        }

        // 格式化文件大小
        public static string FormatFileSize(string fileSize)
        {
            ulong fileSizeNum = ulong.Parse(fileSize);
            int unitIndex = 0;
            string[] unit = { " B", " KB", " MB", " GB", " TB", " PB" };
            while (fileSizeNum > 1024)
            {
                fileSizeNum = fileSizeNum / 1024;
                unitIndex++;
            }
            return fileSizeNum.ToString() + unit[unitIndex];
        }

        // 设置单选选中
        public static void SetRadioResult(WrapPanel wrapPanel, string result)
        {
            foreach (RadioButton radioButton in wrapPanel.Children)
            {
                if ((string)radioButton.Content == result)
                {
                    radioButton.IsChecked = true;
                    break;
                }
            }
        }

        // 获取单选结果
        public static string GetRadioResult(WrapPanel wrapPanel)
        {
            string result = null;
            foreach (RadioButton radioButton in wrapPanel.Children)
            {
                if (radioButton.IsChecked == true)
                {
                    result = (string)radioButton.Content;
                    break;
                }
            }
            return result;
        }
    }
}