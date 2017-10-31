using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

        public delegate void ReportProgressDelegate(string message);

        public delegate void FinishDelegate();

        private ReportProgressDelegate _report;
        private FinishDelegate _finish;

        private ObservableCollection<string> _log;

        private Process _updateProcess;
        private BackgroundWorker worker;


        private string[] args;


        string deleteFile = "Del /F /Q \"{0}\"";
        string makeDirectory = "mkdir \"{0}\"";
        string removeDirectory = "rmdir \"{0}\"";
        string moveCommand = "Move /Y \"{1}\" \"{2}\"";
        string pause4SecCommand = "Choice /C Y /N /D Y /T 1";
        string pause2SecCommand = "Choice /C Y /N /D Y /T 1";
        string startCommand = " Start \"\" /D \"{0}\" \"{1}\" {2}";


        [Serializable]
        private struct config
        {
            public string updatesPath;
            public string backupPath;
            public string appPath;
            public bool downloaded;
        };

        private config _config;

        public MainWindow()
        {
            InitializeComponent();
            _log = new ObservableCollection<string>();
            Log.ItemsSource = _log;
            Topmost = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //_updateProcess = Process.Start(@"\update progress.exe");

            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);
            worker.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
            worker.RunWorkerAsync();
            
            _report = new ReportProgressDelegate(Report);
            _finish = new FinishDelegate(FinishUpdate);

        }

        private void MainWindow_OnContentRendered(object sender, EventArgs e)
        {
            Topmost = false;
            //Thread.Sleep(10000);
            //worker.RunWorkerAsync();
        }
        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            args = Environment.GetCommandLineArgs();
            worker.ReportProgress(100, "Starting update.");
            new Thread(new ThreadStart(delegate
            {
                Update();
            })).Start();
        }
        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            _log.Add(e.UserState.ToString());
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Dispatcher.Invoke(delegate
            {
                Abort.Visibility = Visibility.Collapsed;
                Finish.Visibility = Visibility.Visible;
            });
        }

        private void Abort_OnClick(object sender, RoutedEventArgs e)
        {
            //Abort.IsEnabled = false;
        }

        private void Finish_OnClick(object sender, RoutedEventArgs e)
        {
            //return to previous app
            string exe = args[4];
            string argv = "";
            for (int i = 5; i < args.Length; i++)
            {
                argv += $"\"{args[i]}\" ";
            }
            if (!string.IsNullOrEmpty(exe))
            {
                ProcessStartInfo info = new ProcessStartInfo(exe);
                info.Arguments = argv;
                Process.Start(info);
            }
            Process.GetCurrentProcess().Kill();
        }

        #region Delegates' functions

        private void Report(string message)
        {
            Dispatcher.Invoke(() =>
            {
                _log.Add(message);
                Log.SelectedIndex = Log.Items.Count - 1;
            });
        }

        private void FinishUpdate()
        {
            Dispatcher.Invoke(() =>
            {
                Abort.Visibility = Visibility.Collapsed;
                Finish.Visibility = Visibility.Visible;
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = 100;
                ProgressBar.Maximum = 100;
            });
        }


        #endregion

        private void Update()
        {
            //path of the root folder of installation
            string rootPath = args[1];
            //path of the root folder of updates
            string rootTempPath = args[2];
            //save to config file if in order to continue next time if canceled so not to download again
            bool backupCreated = CreateBackUp(rootPath, rootTempPath);

            if (!backupCreated)
            {
                //delete backup
                //
                //
                //
                _report("There was a problem creating backup.");
                return;
            }

            _report("Backup successfully created.");
            
            //path of the root folder of backup
            string backupPath = Path.Combine(rootTempPath, "backup");
            _config = new config
            {
                downloaded = true,
                appPath = rootPath,
                backupPath = backupPath,
                updatesPath = rootTempPath
            };

            _report("Updating application files.");
            
            bool filesUpdated = UpdateAppFiles();

            if (!filesUpdated)
            {
                //rollback
                //
                //
                //
                string rollbackExe = args[3];
                return;
            }

            _report("Application successfully updated.");
            
            //clear backup files and downloaded updates
            Directory.Delete(backupPath, true);
            Directory.Delete(rootTempPath, true);
            _finish();
        }

        private bool CreateBackUp(string appBaseDir, string tempPath)
        {
            string backupPath = System.IO.Path.Combine(tempPath, "backup");
            System.IO.Directory.CreateDirectory(backupPath);
            _report("Creating application backup.");

            bool result = DuplicateFileSystem(appBaseDir, backupPath);

            return result;
        }

        private bool DuplicateFileSystem(string appBaseDir, string tempDir)
        {
            try
            {
                bool result = true;
                DirectoryInfo info = new DirectoryInfo(appBaseDir);
                FileInfo[] files = info.GetFiles();
                foreach (FileInfo file in files)
                {
                    if (File.Exists(System.IO.Path.Combine(tempDir, file.Name)))
                        File.Delete(System.IO.Path.Combine(tempDir, file.Name));
                    File.Copy(System.IO.Path.Combine(appBaseDir, file.Name), System.IO.Path.Combine(tempDir, file.Name));
                    _report($"Copying file: {file.Name}");
                    if (!File.Exists(System.IO.Path.Combine(tempDir, file.Name)))
                    {
                        _report($"ERROR - Failed to copy file: {file.Name}");
                        return false;
                    }
                }
                string[] directories = System.IO.Directory.GetDirectories(appBaseDir);
                foreach (string directory in directories)
                {
                    string nextTempDir = System.IO.Path.Combine(tempDir, directory.Split('\\').Last());
                    System.IO.Directory.CreateDirectory(nextTempDir);
                    _report($"Creating directory: {directory.Split('\\').Last()}");
                    result &= DuplicateFileSystem(directory, nextTempDir);
                }
                return result;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool UpdateAppFiles()
        {
            string tempDir = _config.updatesPath;
            string appBaseDir = _config.appPath;

            bool result = ReplaceFiles(appBaseDir, tempDir);
            result = DeleteLeftoverFile(appBaseDir, tempDir);

            return result;
        }

        private bool ReplaceFiles(string appBaseDir, string tempDir)
        {
            bool result = true;
            DirectoryInfo info = new DirectoryInfo(tempDir);
            FileInfo[] files = info.GetFiles();
            foreach (FileInfo file in files)
            {
                if (File.Exists(System.IO.Path.Combine(appBaseDir, file.Name)))
                {
                    File.Replace(System.IO.Path.Combine(tempDir, file.Name),
                        System.IO.Path.Combine(appBaseDir, file.Name),
                        System.IO.Path.Combine(appBaseDir, $"{file.Name}.bac"));
                    _report($"Replacing file: {file.Name}");
                }
                else
                {
                    File.Copy(System.IO.Path.Combine(appBaseDir, file.Name), System.IO.Path.Combine(tempDir, file.Name));
                    _report($"Copying file: {file.Name}");
                }
                if (File.Exists(System.IO.Path.Combine(appBaseDir, file.Name)))
                    continue;

                _report($"ERROR - Failed to update file: {file.Name}");
                return false;
            }

            string[] directories = System.IO.Directory.GetDirectories(tempDir);
            foreach (string directory in directories)
            {
                string nextDir = System.IO.Path.Combine(appBaseDir, directory.Split('\\').Last());
                if (!Directory.Exists(nextDir))
                {
                    _report($"Creating directory: {directory.Split('\\').Last()}");
                    System.IO.Directory.CreateDirectory(nextDir);
                }
                result &= DuplicateFileSystem(directory, nextDir);
            }

            return result;
        }

        private bool DeleteLeftoverFiles(string appBaseDir, string tempDir)
        {
            bool result = true;
            string[] directories = Directory.GetDirectories(appBaseDir);
            foreach (string directory in directories)
            {
                string nextDir = System.IO.Path.Combine(appBaseDir, directory.Split('\\').Last());
                string nexTempDir = Path.Combine(tempDir, directory.Split('\\').Last());
                if (Directory.Exists(nextTempDir))
                {
                    DeleteLeftoverFiles(nextDir, nexTempDir);
                    continue;
                }

                Directory.Delete(nextDir, true);
                if (Directory.Exists(nextDir))
                    return false;
            }

            DirectoryInfo info = new DirectoryInfo(tempDir);
            FileInfo[] files = info.GetFiles();
            foreach (FileInfo file in files)
            {
                if (!File.Exists(Path.Combine(tempDir, file.Name)))
                    File.Delete(file);
                if (File.Exists(file))
                    return false;
            }

            return result;
        }

        private bool Rollback(string appBaseDir, string tempDir)
        {
            string backupPath = System.IO.Path.Combine(tempPath, "backup");

            bool result = ReplaceFiles(backupPath, appBaseDir);

            result = DeleteLeftoverFiles(appBaseDir, backupPath);

            return true;
        }

        /*
        /// <summary>
        /// Updates application.
        /// </summary>
        /// <param name="tempRootPath">Path of the temp directory where files are downloaded</param>
        /// <param name="rootPath">Path of the app install directory.</param>
        /// <param name="directory"></param>
        /// <param name="updateLaunchArgs"></param>
        private ProcessStartInfo UpdateApplication(object tempRootPath, string rootPath, Directory directory, string updateLaunchArgs, string exe)
        {
            string echo =
                "echo Updating application...Please wait until it is done. Application will start once it is finished.";
            string commands = $"/C Choice /C Y /N /D Y /T 5 &  Choice /C Y /N /D Y /T 15";

            //string commands = $"\\C ";
            //generate commands to execute
            //move content to app's directory
            commands += GenerateScript(tempRootPath, rootPath, directory, updateLaunchArgs);
            //delete directories and files from the temp directory
            commands += GenerateDeleteScript(tempRootPath, directory);
            //commands exe
            commands += String.Format(startCommand, rootPath, exe, updateLaunchArgs);
            //close cmd
            commands += " & exit";

            //File.WriteAllText("D:\\test.bat", script);


            ProcessStartInfo info = new ProcessStartInfo();
            info.Arguments = commands;
            //info.WindowStyle = ProcessWindowStyle.Hidden;
            //info.CreateNoWindow = true;
            info.FileName = "cmd.exe";
            return info;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="tempRootPath"></param>
        /// <param name="rootPath"></param>
        /// <param name="directory"></param>
        /// <param name="updateLaunchArgs"></param>
        /// <returns></returns>
        private string GenerateScript(object tempRootPath, string rootPath, Directory directory, string updateLaunchArgs)
        {
            string retCommand = "";
            foreach (Directory childDirectory in directory.Directories)
            {
                string command = pause4SecCommand + " & " + makeDirectory;
                command = String.Format(command, $"{rootPath}\\{childDirectory.Name}");
                retCommand += $"{command} & ";
                retCommand += GenerateScript(tempRootPath + "\\" + childDirectory.Name, rootPath + "\\" + childDirectory.Name, childDirectory, updateLaunchArgs);
            }
            foreach (KeyValuePair<string, string> file in directory.Files)
            {
                string command = pause4SecCommand + " & " + deleteFile + " & " + pause2SecCommand + " & " + moveCommand;
                command = String.Format(command, rootPath + "\\" + file.Key, tempRootPath + "\\" + file.Key, rootPath + "\\" + file.Key);
                retCommand += $"{command} &";
            }
            return retCommand;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tempRootPath"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        private string GenerateDeleteScript(object tempRootPath, Directory directory)
        {
            string retCommand = "";
            foreach (KeyValuePair<string, string> file in directory.Files)
            {
                string command = deleteFile;
                command = String.Format(command, tempRootPath + "\\" + file.Key);
                retCommand += $"{command} &";
            }
            foreach (Directory childDirectory in directory.Directories)
            {
                retCommand += GenerateDeleteScript(tempRootPath + "\\" + childDirectory.Name, childDirectory);
                string command = removeDirectory;
                command = String.Format(command, $"{tempRootPath}\\{childDirectory.Name}");
                retCommand += $"{command} & ";
            }
            return retCommand;
        }
        */
    }
}
