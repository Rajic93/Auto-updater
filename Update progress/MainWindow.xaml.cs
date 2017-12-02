using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Directory = System.IO.Directory;

namespace Update_progress
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        public delegate void FinishDelegate();

        public delegate void DisableDelegate(Control ctrl);

        private FileSystemActions.ReportProgressDelegate _report;
        private FinishDelegate _finish;
        private DisableDelegate _disable;

        private ObservableCollection<string> _log;

        private Process _updateProcess;
        private BackgroundWorker worker;


        private string[] _args;
        private string _appPath;
        private string _updatesPath;
        private string _backupPath;
        private string _argv = "";
        private string _rollbackExe;
        private string _exe;

        private FileSystemActions.config _config;
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
            _disable = Disable;

        }

        private void MainWindow_OnContentRendered(object sender, EventArgs e)
        {
            Topmost = false;
        }

        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            _args = Environment.GetCommandLineArgs();
            _appPath = _args[1];
            //path of the root folder of updates
            _updatesPath = _args[2];
            //save to config file if in order to continue next time if canceled so not to download again
            _backupPath = Path.Combine(_updatesPath.Substring(0, _updatesPath.Length - "\\updates".Length), "backup");
            //return to previous app
            _rollbackExe = _args[3];
            //launch updated app
            _exe = _args[4];
            //launch args
            for (int i = 5; i < _args.Length; i++)
            {
                _argv += $"\"{_args[i]}\" ";
            }
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
            if (!string.IsNullOrEmpty(_exe))
            {
                _info = new ProcessStartInfo(_exe);
                _info.Arguments = _argv;
            }
            if (_info == null)
                _report("There is no exe file specified for the new app.");
            else
                Process.Start(_info);

            ProcessStartInfo info = new ProcessStartInfo();
            info.WindowStyle = ProcessWindowStyle.Hidden;
            info.CreateNoWindow = true;

            if (_config.replaceUpdateProgressExe)
            {
                string del = _backupPath.Substring(0, _backupPath.Length - _backupPath.Split('\\').Last().Length);
                string script = CreateScript(_config.updateProgressSrc.Substring(0, _config.updateProgressSrc.Length - "Update progress.exe".Length - 1),
                    _config.appPath, "Update progress.exe", del);
                File.WriteAllText($"{Path.GetTempPath()}script.bat", script);
                info.FileName = $"{Path.GetTempPath()}script.bat";
                info.UseShellExecute = true;
            }

            if (_config.deleteUpdateProgressExe)
            {
                info.Arguments = string.Format($"{FileSystemActions.Commands.Pause2SecCommand} & {FileSystemActions.Commands.DeleteFile}",
                    $"{Directory.GetCurrentDirectory()}\\Update progress.exe");
                info.FileName = "cmd.exe";
            }

            _disable(Finish);
            Process.Start(info);
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

        private void Disable(Control ctrl)
        {
            Dispatcher.Invoke(() =>
            {
                ctrl.IsEnabled = false;
            });
        }


        #endregion

        private void Update()
        {
            _config = new FileSystemActions.config
            {
                downloaded = true,
                appPath = _appPath,
                backupPath = _backupPath,
                updatesPath = _updatesPath,
                deleteUpdateProgressExe = false,
                replaceUpdateProgressExe = false
            };

#if DEBUG
            Report(_config.ToString()); 
#endif

            FileSystemActions action = new FileSystemActions
            {
                Config = _config,
                Report = _report
            };

            if (!action.BackUp())
            {
                Directory.Delete(_backupPath, true);
                _report("There was a problem creating backup.");
                return;
            }

            _report("Backup successfully created.\nUpdating application files.");
            _disable(Abort);
            if (!action.Update())
            {
                if (!action.Rollback())
                {
                    _report("Fatal error: Application not backed up correctly. It may need reinstallation.");
                    _rollback = false;
                    _fatal = true;
                }
                if (!string.IsNullOrEmpty(_rollbackExe))
                {
                    _info = new ProcessStartInfo(_rollbackExe);
                    _info.Arguments = _argv;
                    _rollback = true;
                }
                _finish();
                return;
            }
            //clear backup files and downloaded updates
            _report("Deleting leftover files");
            bool filesUpdated = action.Clean();
            _finish();
            _report("Application successfully updated.");
        }

        private string CreateScript(string src, string dest, string file, string del)
        {
            //https://superuser.com/questions/1015584/compare-and-replace-files-using-a-batch-file
            return "@ECHO ON\r\n" +
                   "SLEEP 5" +
                   "SETLOCAL\r\n" +
                   $"SET SourceDir={src}\r\n" +
                   $"SET TargetDir={dest}\r\n" +
                   //$"SET LogFile=C:\\LogPath\\Logfile.txt\r\n" +
                   $"ROBOCOPY \"%SourceDir%\" \"%TargetDir%\" \"{file}\" /NP /R:5 /TS /FP\r\n" +
                   $"@RD /S /Q \"{del}\"";
        }
    }
}
