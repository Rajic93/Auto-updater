using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Update_progress
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<string> _log;

        private Process _updateProcess;

        [Serializable]
        private struct config
        {
            public string updatesPath;
            public string backupPath;
            public string appPath;
            public bool downloaded;
        };


        public MainWindow()
        {
            InitializeComponent();
            _log = new ObservableCollection<string>();
            Log.ItemsSource = _log;
            Topmost = true;
            //_updateProcess = Process.Start(@"\update progress.exe");

        }

        private void Abort_OnClick(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private void Finish_OnClick(object sender, RoutedEventArgs e)
        {

        }

        private void Update()
        {
            string[] args = Environment.GetCommandLineArgs();
            //path of the root folder of installation
            string rootPath = args[0];
            //path of the root folder of updates
            string rootTempPath = args[1];
            //save to config file if in order to continue next time if canceled so not to download again
            //bool backupCreated = CreateBackUp(rootPath, rootTempPath);
            //if (!backupCreated)
            //{
               
            //    //System.Windows.Forms.MessageBox.Show("There was a problem creating backup.",
            //    //"Backup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return;
            //}

            ////path of the root folder of backup
            string backupPath = Path.Combine(rootTempPath, "backup");
            //config configStruct = new config
            //{
            //    downloaded = true,
            //    appPath = rootPath,
            //    backupPath = backupPath,
            //    updatesPath = rootTempPath
            //};
            _log.Add(rootPath);
            _log.Add(rootTempPath);
            _log.Add(backupPath);
        }

        private bool CreateBackUp(string appBaseDir, string tempPath)
        {
            string backupPath = System.IO.Path.Combine(tempPath, "backup");
            System.IO.Directory.CreateDirectory(backupPath);

            bool result = DuplicateFileSystem(appBaseDir, backupPath);

            return result;
        }

        public bool DuplicateFileSystem(string appBaseDir, string tempDir)
        {
            bool result = true;
            DirectoryInfo info = new DirectoryInfo(appBaseDir);
            FileInfo[] files = info.GetFiles();
            foreach (FileInfo file in files)
            {
                if (File.Exists(System.IO.Path.Combine(tempDir, file.Name)))
                    File.Delete(System.IO.Path.Combine(tempDir, file.Name));
                File.Copy(System.IO.Path.Combine(appBaseDir, file.Name), System.IO.Path.Combine(tempDir, file.Name));
                if (!File.Exists(System.IO.Path.Combine(tempDir, file.Name)))
                    return false;
            }
            string[] directories = System.IO.Directory.GetDirectories(appBaseDir);
            foreach (string directory in directories)
            {
                string nextTempDir = System.IO.Path.Combine(tempDir, directory.Split('\\').Last());
                System.IO.Directory.CreateDirectory(nextTempDir);
                result &= DuplicateFileSystem(directory, nextTempDir);
            }
            return result;
        }
    }
}
