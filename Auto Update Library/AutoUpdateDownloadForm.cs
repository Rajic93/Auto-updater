using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Threading;

namespace Auto_updater
{
    /// <summary>
    /// Form that downloads the update and shows progress to the user.
    /// </summary>
    public partial class lblDownloading : Form
    {

        private delegate void UpdateUIDelegate();

        /// <summary>
        /// The Web client to download the update.
        /// </summary>
        private WebClient webClient;
        /// <summary>
        /// The thread to hash the file on.
        /// </summary>
        private BackgroundWorker bgWorker;
        /// <summary>
        /// 
        /// </summary>
        private Directory directory;

        private bool ready;

        private string baseUrl;

        internal Directory Directory => directory;

        public lblDownloading()
        {
        }

        /// <summary>
        /// Creates new AutoUpdateDownload form.
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="directory"></param>
        /// <param name="applicationInfoApplicationIcon"></param>
        public lblDownloading(string baseUrl, Directory directory, Icon applicationInfoApplicationIcon)
        {
            InitializeComponent();
            if (applicationInfoApplicationIcon != null)
                Icon = applicationInfoApplicationIcon;
            //tempFile = Path.GetTempPath();
            this.directory = directory;
            this.baseUrl = baseUrl;

            webClient = new WebClient();
            webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
            webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;

            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += BgWorker_DoWork;
            bgWorker.RunWorkerCompleted += BgWorker_RunWorkerCompleted;
            bgWorker.WorkerSupportsCancellation = true;


        }

        private void lblDownloading_Shown(object sender, EventArgs e)
        {
            try
            {
                //download files
                string tempDir = Path.GetTempPath() + "AutoUpdate";
                System.IO.Directory.CreateDirectory(tempDir); 
                new Thread(() =>
                {
                    DownloadFiles(baseUrl, directory, tempDir);
                    DialogResult = DialogResult.OK;
                    try
                    {
                        if (!IsDisposed)
                            Invoke(new MethodInvoker(() =>
                            {
                                if (!IsDisposed)
                                    Close();
                            }));
                    }
                    catch (InvalidOperationException ie)
                    {
                        
                    }
                }).Start();
                //webClient.DownloadFileAsync(updateUri, tempFile);
            }
            catch (Exception ex)
            {
                DialogResult = DialogResult.No;
                Close();
            }
        }

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                try
                {
                    DialogResult = DialogResult.No;
                    UpdateUIDelegate updateUiDelegate = new UpdateUIDelegate(() =>
                    {
                        this.Close();
                    });
                    this.BeginInvoke(updateUiDelegate);
                }
                catch (InvalidOperationException ie)
                {
                    
                }
            }
            else if (e.Cancelled)
            {
                try
                {
                    DialogResult = DialogResult.Abort;
                    UpdateUIDelegate updateUiDelegate = new UpdateUIDelegate(() =>
                    {
                        this.Close();
                    });
                    this.BeginInvoke(updateUiDelegate);
                }
                catch (InvalidOperationException ie)
                {
                    
                }
            }
            else
            {
                try
                {
                    if (!this.IsDisposed)
                        this.Invoke(new MethodInvoker(() =>
                        {
                            if (!IsDisposed)
                            {
                                lblProgress.Text = "Verifying download...";
                                progressBar.Style = ProgressBarStyle.Marquee;
                            }
                        }));
                }
                catch (InvalidOperationException ie)
                {
                    
                }
            }
            ready = true;
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            try
            {
                if (!IsDisposed)
                    Invoke(new MethodInvoker(() =>
                    {
                        if (!IsDisposed)
                        {
                            progressBar.Value = e.ProgressPercentage;
                            lblProgress.Text = String.Format("Downloaded {0} of {1}", e.BytesReceived, e.TotalBytesToReceive);
                        }
                    }));
            }
            catch (InvalidOperationException ie)
            {
                
            }
        }

        private void BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //DialogResult = (DialogResult) e.Result;
            //Close();
        }

        private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            WebClient client = ((Tuple<WebClient, string, string>) e.Argument).Item1;
            while (client.IsBusy)
            { }
            string file = ((Tuple<WebClient, string, string>) e.Argument).Item2;
            string updateMD5 = ((Tuple<WebClient, string, string>) e.Argument).Item3;
            if (Hasher.HashFile(file, HashType.MD5) != updateMD5)
                e.Result = DialogResult.No;
            else
                e.Result = DialogResult.OK;
        }

        private void lblDownloading_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (webClient.IsBusy)
            {
                webClient.CancelAsync();
                DialogResult = DialogResult.Abort;
            }
            if (bgWorker.IsBusy)
            {
                bgWorker.CancelAsync();
                DialogResult = DialogResult.Abort;
            }
        }

        private string FormatBytes(long bytes, int decimalPlaces, bool showByteType)
        {
            double newBytes = bytes;
            string formatString = "{0";
            string byteType = "0";
            if (newBytes > 1024 && newBytes < 1048576)
            {
                newBytes /= 1024;
                byteType = "KB";
            }
            else if (newBytes > 1048576 && newBytes < 1073741824)
            {
                newBytes /= 1048576;
                byteType = "MB";
            }
            else
            {
                newBytes /= 1073741824;
                byteType = "GB";
            }
            if (decimalPlaces > 0)
                formatString += ":0.";
            for (int i = 0; i < decimalPlaces; i++)
                formatString += "0";
            formatString += "}";
            if (showByteType)
                formatString += byteType;
            return String.Format(formatString, newBytes);
        }

        private void DownloadFiles(string path, Directory directoryToDownload, string tempDir)
        {
            foreach (KeyValuePair<string, string> file in directoryToDownload.Files)
            {
                ready = false;
                string uri = path + "/" + file.Key;
                webClient.DownloadFileAsync(new Uri(uri), tempDir + "\\" + file.Key);
                Tuple<WebClient, string, string> tuple = new Tuple<WebClient, string, string>(webClient, tempDir + "\\" + file.Key, file.Value);
                bgWorker.RunWorkerAsync(tuple);
                while (webClient.IsBusy || bgWorker.IsBusy)
                { }
            }
            foreach (Directory childDirectory in directoryToDownload.Directories)
            {
                System.IO.Directory.CreateDirectory($"{tempDir}\\{childDirectory.Name}");
                if (path.EndsWith("/"))
                    path = path.TrimEnd('/');
                DownloadFiles(path + "/" + childDirectory.Name, childDirectory, $"{tempDir}\\{childDirectory.Name}");
            }
        }
    }
}
