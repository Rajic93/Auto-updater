using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
        private bool _rollback;
        private ProcessStartInfo _info;
        private bool _fatal;

        public MainWindow()
        {
            InitializeComponent();
            _log = new ObservableCollection<string>();
            Log.ItemsSource = _log;
            Topmost = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //_updateProcess = Process.Start(@"\update progress.exe");

            worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            worker.DoWork += bgWorker_DoWork;
            worker.ProgressChanged += bgWorker_ProgressChanged;
            worker.RunWorkerCompleted += bgWorker_RunWorkerCompleted;
            worker.RunWorkerAsync();
            
            _report = Report;
            _finish = FinishUpdate;

        }

        private void MainWindow_OnContentRendered(object sender, EventArgs e)
        {
            Topmost = false;
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
            
        }

        private void Abort_OnClick(object sender, RoutedEventArgs e)
        {
            //Abort.IsEnabled = false;
        }

        private void Finish_OnClick(object sender, RoutedEventArgs e)
        {
            if(_rollback && !_fatal)
            {
                Process.Start(_info);
                Process.GetCurrentProcess().Kill();
                return;
            }

            if (_fatal)
            {
                Process.GetCurrentProcess().Kill();
                return;
            }

            //return to previous app
            string exe = args[4];
            string argv = "";
            for (int i = 5; i < args.Length; i++)
            {
                argv += $"\"{args[i]}\" ";
            }
            if (!string.IsNullOrEmpty(exe))
            {
                _info = new ProcessStartInfo(exe);
                _info.Arguments = argv;
            }
            if (_info == null)
                _report("There is no exe file specified for the new app.");
            if (_info != null)
            {
                Process.Start(_info); 
            }
            Process.GetCurrentProcess().Kill();
        }

        #region Delegates' functions

        private void Report(string message)
        {
            Dispatcher.Invoke(() =>
            {
                
                if (!File.Exists("D:\\log.txt"))
                    File.Create("D:\\log.txt").Close();
                File.AppendAllText("D:\\log.txt", $"{message}\n");
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
                if (!Rollback(rootPath, rootTempPath))
                {
                    _report("Fatal error: Application not backed up correctly. It may need reinstallation.");
                    _rollback = false;
                    _fatal = true;
                }
                //
                string rollbackExe = args[3];
                string argv = "";
                for (int i = 5; i < args.Length; i++)
                {
                    argv += $"\"{args[i]}\" ";
                }
                if (!string.IsNullOrEmpty(rollbackExe))
                {
                    _info = new ProcessStartInfo(rollbackExe);
                    _info.Arguments = argv;
                    _rollback = true;
                }
                _finish();
                return;
            }

            _report("Application successfully updated.");

            //clear backup files and downloaded updates
            _report("Deleting leftover files");
            string tempDir = _config.updatesPath;
            tempDir = Directory.GetDirectories(tempDir)[1];
            filesUpdated &= DeleteLeftOverFiles(_config.appPath, tempDir);
            _report("Deleting backup files");
            Directory.Delete(backupPath, true);
            _report("Deleting downloaded files");
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

        public bool DuplicateFileSystem(string appBaseDir, string tempDir)
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
                    if (directory.Split('\\').Last().Equals("backup"))
                        continue;
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
            tempDir = Directory.GetDirectories(tempDir)[1];
            string appBaseDir = _config.appPath;

            _report("Replacing files");
            bool result = ReplaceFiles(appBaseDir, tempDir);

            return result;
        }

        private bool ReplaceFiles(string appBaseDir, string tempDir)
        {
            _report($"Destination: {appBaseDir}");
            _report($"Source: {tempDir}");
            bool result = true;
            DirectoryInfo info = new DirectoryInfo(tempDir);
            FileInfo[] files = info.GetFiles();
            foreach (FileInfo file in files)
            {
                if (File.Exists(System.IO.Path.Combine(appBaseDir, file.Name)))
                {
                    //compute hash
                    MD5 cypher = MD5.Create();
                    byte[] hash = cypher.ComputeHash(File.ReadAllBytes(System.IO.Path.Combine(appBaseDir, file.Name)));
                    byte[] hash2 = cypher.ComputeHash(File.ReadAllBytes(System.IO.Path.Combine(tempDir, file.Name)));
                    if (hash2.Equals(hash))
                        continue;
                    try
                    {
                        _report($"Replacing file: {file.FullName}");
                        File.Replace(System.IO.Path.Combine(tempDir, file.Name),
                                        System.IO.Path.Combine(appBaseDir, file.Name),
                                        System.IO.Path.Combine(appBaseDir, $"{file.Name}.bac"));
                    }
                    catch (IOException ioException)
                    {
                        _report($"{ioException.Message}");
                        _report($"Error at file: {file.Name}");
                        return false;
                    }
                }
                else
                {
                    _report($"Copying file: {file.FullName} to \"{System.IO.Path.Combine(appBaseDir, file.Name)}\"");
                    File.Copy(System.IO.Path.Combine(tempDir, file.Name), System.IO.Path.Combine(appBaseDir, file.Name));
                    _report($"File succesfully copied");
                }
                if (File.Exists(System.IO.Path.Combine(appBaseDir, file.Name)))
                    continue;

                _report($"ERROR - Failed to update file: {file.Name}");
                return false;
            }

            string[] directories = System.IO.Directory.GetDirectories(tempDir);
            foreach (string directory in directories)
            {
                if (directory.Split('\\').Last().Equals("backup"))
                    continue;
                string nextAppBaseDir = System.IO.Path.Combine(appBaseDir, directory.Split('\\').Last());
                _report($"Next appBaseDir: {nextAppBaseDir}");
                if (!Directory.Exists(nextAppBaseDir))
                {
                    _report($"Creating directory: {directory.Split('\\').Last()}");
                    System.IO.Directory.CreateDirectory(nextAppBaseDir);
                }
                result &= ReplaceFiles(nextAppBaseDir, directory);
            }

            return result;
        }

        private bool DeleteLeftOverFiles(string appBaseDir, string tempDir)
        {
            _report($"Application directory: {appBaseDir}");
            _report($"Downloads directory: {tempDir}");
            bool result = true;
            string[] directories = Directory.GetDirectories(appBaseDir);
            foreach (string directory in directories)
            {
                string nextDir = System.IO.Path.Combine(appBaseDir, directory.Split('\\').Last());
                string nextTempDir = Path.Combine(tempDir, directory.Split('\\').Last());
                _report($"Checking directory: {nextTempDir}");
                if (!Directory.Exists(nextTempDir))
                {
                    _report($"Deleting directory: {nextTempDir}");
                    Directory.Delete(nextDir, true);
                    if (Directory.Exists(nextDir))
                        return false;
                    continue;
                }
                result = DeleteLeftOverFiles(nextDir, nextTempDir);
            }

            DirectoryInfo info = new DirectoryInfo(appBaseDir);
            FileInfo[] files = info.GetFiles();
            foreach (FileInfo file in files)
            {
                _report($"Checking file: {Path.Combine(tempDir, file.Name)}");
                if (!File.Exists(Path.Combine(tempDir, file.Name)))
                {
                    _report($"Deleting file: {Path.Combine(tempDir, file.Name)}");
                    try
                    {
                        File.Delete(Path.Combine(appBaseDir, file.Name));
                    }
                    catch (Exception exception)
                    {
                        _report($"File not deleted: {Path.Combine(appBaseDir, file.Name)}");
                    }
                    //if (File.Exists(Path.Combine(appBaseDir, file.Name)))
                    //    return false;
                }
            }

            return result;
        }

        private bool Rollback(string appBaseDir, string tempDir)
        {
            string backupPath = System.IO.Path.Combine(tempDir, "backup");

            bool result = ReplaceFiles(backupPath, appBaseDir);

            //result &= DeleteLeftOverFiles(appBaseDir, backupPath);

            return result;
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
