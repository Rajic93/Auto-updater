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
                string rootTempPath = $"{Path.GetTempPath()}AutoUpdate\\updates";

                string argv = PrepareArgs(update.LaunchArgs);
                string args =
                    $"\"{rootPath}\" \"{rootTempPath}\" \"{Process.GetCurrentProcess().MainModule.FileName}\" \"{update.Exe}\" {argv}";
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

        private string PrepareArgs(string updateLaunchArgs)
        {
            string args = "";
            string[] argv = updateLaunchArgs.Split(' ');
            foreach (string arg in argv)
            {
                args += $"\"{arg}\" ";
            }
            return args;
        }
    }
}
