using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Auto_updater
{
    /// <summary>
    /// Provides application update support in C#
    /// </summary>
    public class AutoUpdater
    {
        /// <summary>
        /// Holds program-to-update's info.
        /// </summary>
        private IAutoUpdatable applicationInfo;
        /// <summary>
        /// Thread to find the update.
        /// </summary>
        private BackgroundWorker bgWorker;
        

        string deleteFile = "Del /F /Q \"{0}\"";
        string makeDirectory = "mkdir \"{0}\"";
        string removeDirectory = "rmdir \"{0}\"";
        string moveCommand = "Move /Y \"{1}\" \"{2}\"";
        string pause4SecCommand = "Choice /C Y /N /D Y /T 1";
        string pause2SecCommand = "Choice /C Y /N /D Y /T 1";
        string startCommand = " Start \"\" /D \"{0}\" \"{1}\" {2}";


        #region Public functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appInfo"></param>
        public AutoUpdater(IAutoUpdatable appInfo)
        {
            applicationInfo = appInfo;
            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool CheckForUpdate()
        {
            if (AutoUpdateXml.ExistsOnServer(applicationInfo.UpdateXmlLocation))
                return AutoUpdateXml.Parse(applicationInfo.UpdateXmlLocation, applicationInfo.ApplicationId)
                    .IsNewerThan(applicationInfo.ApplicationAssembly.GetName().Version);
            return false;
        }

        #endregion

        /// <summary>
        /// Checks for an update for the program passed.
        /// If there is an update, a dialog asking to download will appear.
        /// </summary>
        public void DoUpdate()
        {
            if (!bgWorker.IsBusy)
                bgWorker.RunWorkerAsync(applicationInfo);
        }

        /// <summary>
        /// Checks for/parses update.xml on the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            IAutoUpdatable appInfo = (IAutoUpdatable) e.Argument;

            if (!AutoUpdateXml.ExistsOnServer(appInfo.UpdateXmlLocation))
                e.Cancel = true;
            else
                e.Result = AutoUpdateXml.Parse(appInfo.UpdateXmlLocation, appInfo.ApplicationId);
        }

        /// <summary>
        /// Initiates download after update.xml is downloaded and parsed if it is needed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
                return;

            AutoUpdateXml update = (AutoUpdateXml) e.Result;

            if (update == null || !update.IsNewerThan(applicationInfo.ApplicationAssembly.GetName().Version))
                return;

            if (new AutoUpdateAcceptForm(applicationInfo, update).ShowDialog() == DialogResult.Yes)
                DownloadUpdate(update);
        }

        /// <summary>
        /// Creates download update form and updates application.
        /// </summary>
        /// <param name="update"></param>
        private void DownloadUpdate(AutoUpdateXml update)
        {
            lblDownloading form = new lblDownloading(update.Uri.ToString(), update.Directory, applicationInfo.ApplicationIcon);
            DialogResult result = form.ShowDialog();

            if (result == DialogResult.OK)
            {
                string currentPath = applicationInfo.ApplicationAssembly.Location;
                //path of the root folder of installation
                string rootPath = Path.GetDirectoryName(currentPath);
                //path of the root folder of updates
                string rootTempPath = $"{Path.GetTempPath()}AutoUpdate";

                string args = $"\"{rootPath}\" \"{rootTempPath}\"";
                ProcessStartInfo info = new ProcessStartInfo("Update progress.exe");
                info.Arguments = args;
                info.Verb = "runas";
                
                //ProcessStartInfo info = UpdateApplication(rootTempPath, rootPath, update.Directory, update.LaunchArgs, update.Exe);
                //info.UseShellExecute = false;

                Process.Start(info);
                Process.GetCurrentProcess().Kill();
            }
            else if (result == DialogResult.Abort)
            {
                MessageBox.Show("The update download was canceled.\nThis program has not been modified.",
                    "Update Download Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("There was a problem downloading update.\nPlease try again later.",
                    "Update Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

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
    }
}
