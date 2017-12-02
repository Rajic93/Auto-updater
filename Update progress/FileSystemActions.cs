using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

namespace Update_progress
{
    public class FileSystemActions
    {
        public delegate void ReportProgressDelegate(string message);

        [Serializable]
        public class config
        {
            public string updatesPath;
            public string backupPath;
            public string appPath;
            public bool downloaded;
            public bool deleteUpdateProgressExe;
            public bool replaceUpdateProgressExe;
            public string updateProgressSrc;
            public string updateProgressDest;

            public override string ToString() =>
                $"config:\n\tappPath: {appPath}\n\tbackupPath: {backupPath}\n\tupdatesPath: {updatesPath}";
        };

        public static class Commands
        {
            public static string DeleteFile = "Del /F /Q \"{0}\"";
            public static string MakeDirectory = "mkdir \"{0}\"";
            public static string RemoveDirectory = "rmdir \"{0}\"";
            public static string MoveCommand = "Move /Y \"{1}\" \"{2}\"";
            public static string Pause4SecCommand = "Choice /C Y /N /D Y /T 1";
            public static string Pause2SecCommand = "Choice /C Y /N /D Y /T 1";
            public static string StartCommand = " Start \"\" /D \"{0}\" \"{1}\" {2}";
            public static string CopyCommand = "Copy /V /Y \"{0}\" \"{1}\"";
        }

        private ReportProgressDelegate _report;
        private config _config;

        public ReportProgressDelegate Report
        {
            private get { return _report; }
            set { _report = value; }
        }
        public config Config
        {
            private get { return _config; }
            set { _config = value; }
        }

        public bool BackUp()
        {
            System.IO.Directory.CreateDirectory(_config.backupPath);
            Report("Creating application backup.");
            bool result = DuplicateFileSystem(_config.appPath, _config.backupPath);
            return result;
        }

        public bool Update()
        {
            string tempDir = System.IO.Directory.GetDirectories(_config.updatesPath)[0];
            Report("Replacing files");
            bool result = ReplaceFiles(_config.appPath, tempDir);
            return result;
        }

        public bool Rollback()
        {
            return ReplaceFiles(_config.backupPath, _config.appPath);
        }

        public bool Clean()
        {
            string updates = Directory.GetDirectories(_config.updatesPath)[0];
            //Report($"\n\nApp dir: {_config.appPath}\nUpdates dir: {updates}\n\n");
            return RecursiveDelete(_config.appPath, updates);
        }

        private bool DuplicateFileSystem(string sourceDir, string destinationDir)
        {
            try
            {
                Report($"{destinationDir}");
                bool result = true;
                System.IO.DirectoryInfo info = new DirectoryInfo(sourceDir);
                FileInfo[] files = info.GetFiles();
                foreach (FileInfo file in files)
                {
                    if (File.Exists(System.IO.Path.Combine(destinationDir, file.Name)))
                        File.Delete(System.IO.Path.Combine(destinationDir, file.Name));
                    File.Copy(System.IO.Path.Combine(sourceDir, file.Name), System.IO.Path.Combine(destinationDir, file.Name));
                    Report($"\tCopying file: {file.Name}");
                    if (!File.Exists(System.IO.Path.Combine(destinationDir, file.Name)))
                    {
                        Report($"\tERROR - Failed to copy file: {file.Name}");
                        return false;
                    }
                }
                string[] directories = System.IO.Directory.GetDirectories(sourceDir);
                foreach (string directory in directories)
                {
                    if (directory.Split('\\').Last().Equals("backup"))
                        continue;
                    string nextDestDir = System.IO.Path.Combine(destinationDir, directory.Split('\\').Last());
                    System.IO.Directory.CreateDirectory(nextDestDir);
                    Report($"\tCreating directory: {directory.Split('\\').Last()}");
                    result &= DuplicateFileSystem(directory, nextDestDir);
                }
                return result;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool ReplaceFiles(string appBaseDir, string tempDir)
        {
            Report($"Destination: {appBaseDir}\nSource: {tempDir}");
            bool result = true;
            DirectoryInfo info = new DirectoryInfo(tempDir);
            FileInfo[] files = info.GetFiles();
            foreach (FileInfo file in files)
            {
                try
                {
                    if (File.Exists(System.IO.Path.Combine(appBaseDir, file.Name)))
                    {
                        //compute hash
                        MD5 cypher = MD5.Create();
                        byte[] hash = cypher.ComputeHash(File.ReadAllBytes(System.IO.Path.Combine(appBaseDir, file.Name)));
                        byte[] hash2 = cypher.ComputeHash(File.ReadAllBytes(System.IO.Path.Combine(tempDir, file.Name)));
                        if (hash2.Equals(hash))
                            continue;
                        Report($"\tDeleting old file: {file.FullName}");
                        File.Delete(System.IO.Path.Combine(appBaseDir, file.Name));
                        //File.Replace(System.IO.Path.Combine(tempDir, file.Name),
                        //    System.IO.Path.Combine(appBaseDir, file.Name),
                        //    System.IO.Path.Combine(appBaseDir, $"{file.Name}.bac"));
                    }
                    Report($"\tCopying new file: {file.FullName} to \"{System.IO.Path.Combine(appBaseDir, file.Name)}\"");
                    File.Copy(System.IO.Path.Combine(tempDir, file.Name),
                        System.IO.Path.Combine(appBaseDir, file.Name));
                    Report($"\tFile succesfully replaced");
                }
                catch (IOException ioException)
                {
                    Report($"\tError at file: {file.Name}\n{ioException.Message}");
                    return false;
                }
                catch (Exception exception)
                {
                    if (file.Name != "Update progress.exe")
                        return false;
                    Report("\tUpdate progress.exe not replaced. To be done later (at the end of the update.)");
                    _config.replaceUpdateProgressExe = true;
                    _config.updateProgressSrc = System.IO.Path.Combine(tempDir, file.Name);
                    _config.updateProgressDest = System.IO.Path.Combine(appBaseDir, file.Name);
                    continue;
                }
                if (File.Exists(System.IO.Path.Combine(appBaseDir, file.Name)))
                    continue;

                Report($"\tERROR - Failed to update file: {file.Name}");
                return false;
            }

            string[] directories = System.IO.Directory.GetDirectories(tempDir);
            foreach (string directory in directories)
            {
                if (directory.Split('\\').Last().Equals("backup"))
                    continue;
                string nextAppBaseDir = System.IO.Path.Combine(appBaseDir, directory.Split('\\').Last());
                Report($"\tNext appBaseDir: {nextAppBaseDir}");
                if (!System.IO.Directory.Exists(nextAppBaseDir))
                {
                    Report($"\tCreating directory: {directory.Split('\\').Last()}");
                    System.IO.Directory.CreateDirectory(nextAppBaseDir);
                }
                result &= ReplaceFiles(nextAppBaseDir, directory);
            }

            return result;
        }

        private bool RecursiveDelete(string appBaseDir, string tempDir)
        {
#if DEBUG
            Report($"\tApplication directory: {appBaseDir}\n\tDownloads directory: {tempDir}");
#endif
            bool result = true;
            string[] directories = System.IO.Directory.GetDirectories(appBaseDir);
            foreach (string directory in directories)
            {
                string nextDir = System.IO.Path.Combine(appBaseDir, directory.Split('\\').Last());
                string nextTempDir = Path.Combine(tempDir, directory.Split('\\').Last());
#if DEBUG
                Report($"\tChecking directory: {nextTempDir}"); 
#endif
                if (!System.IO.Directory.Exists(nextTempDir))
                {
                    Report($"\tDeleting directory: {nextTempDir}");
                    System.IO.Directory.Delete(nextDir, true);
                    if (System.IO.Directory.Exists(nextDir))
                        return false;
                    continue;
                }
                result = RecursiveDelete(nextDir, nextTempDir);
            }

            DeleteLeftOverFiles(appBaseDir, tempDir);

            return result;
        }

        private void DeleteLeftOverFiles(string appDir, string updatesDir)
        {
            DirectoryInfo info = new DirectoryInfo(appDir);
            FileInfo[] files = info.GetFiles();
            foreach (FileInfo file in files)
            {
                Report($"\tChecking file: {Path.Combine(updatesDir, file.Name)}");
                if (File.Exists(Path.Combine(updatesDir, file.Name)))
                    continue;
                try
                {
                    File.Delete(Path.Combine(appDir, file.Name));
                }
                catch (Exception exception)
                {
                    Report($"\tFile not deleted: {Path.Combine(appDir, file.Name)}");
                    if (file.Name == "Update progress.exe" && !_config.replaceUpdateProgressExe)
                    {
                        Report(file.Name);
                        _config.deleteUpdateProgressExe = true;
                    }
                }
                //if (File.Exists(Path.Combine(appBaseDir, file.Name)))
                //    return false;
            }
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

        public void Test(string app, string updates)
        {
            RecursiveDelete(app, updates);
        }
    }
}
