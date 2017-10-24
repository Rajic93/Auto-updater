using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;

namespace Auto_updater
{
    /// <summary>
    /// Contains update information.
    /// </summary>
    internal class AutoUpdateXml
    {
        private Version version;
        private Uri uri;
        private Directory directory;
        private string description;
        private string launchArgs;
        private string exe;

        /// <summary>
        /// The update version
        /// </summary>
        internal Version Version => version;
        /// <summary>
        /// The location of the binary.
        /// </summary>
        internal Uri Uri => uri;
        /// <summary>
        /// Root directory's with all files and directories to update.
        /// </summary>
        internal Directory Directory => directory;
        /// <summary>
        /// Description of the update.
        /// </summary>
        internal string Description => description;
        /// <summary>
        /// The arguments to pass to the update application on startup.
        /// </summary>
        internal string LaunchArgs => launchArgs;

        internal string Exe => exe;

        /// <summary>
        /// Creates new AutoUpdateXml object.
        /// </summary>
        /// <param name="version"></param>
        /// <param name="uri"></param>
        /// <param name="directory"></param>
        /// <param name="description"></param>
        /// <param name="launchArgs"></param>
        internal AutoUpdateXml(Version version, Uri uri, Directory directory, string description,
            string launchArgs, string exe)
        {
            this.version = version;
            this.uri = uri;
            this.directory = directory;
            this.description = description;
            this.launchArgs = launchArgs;
            this.exe = exe;
        }

        /// <summary>
        /// Checks if update's version is greater than the old version.
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        internal bool IsNewerThan(Version version) => this.version > version;

        /// <summary>
        /// Checks if the update.xml file exists on the server.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        internal static bool ExistsOnServer(Uri location)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest) WebRequest.Create(location.AbsoluteUri);
                HttpWebResponse res = (HttpWebResponse) req.GetResponse();
                res.Close();
                return res.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary>
        /// Parses the update.xml file retrieved from the server.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="appId"></param>
        /// <returns></returns>
        internal static AutoUpdateXml Parse(Uri location, string appId)
        {
            Version version = null;
            string uri = "", filename = "", md5 = "", description = "", launchArgs = "", exe = "";
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(location.AbsoluteUri);

                XmlNode node = doc.DocumentElement["update"];

                if (node == null)
                    return null;

                version = Version.Parse(node["version"].GetAttribute("value") == "" ? "1.0.1": node["version"].GetAttribute("value"));
                uri = node["url"].GetAttribute("value");
                //filename = node["filename"].InnerText;
                //md5 = node["md5"].InnerText;
                description = node["description"].GetAttribute("value");
                launchArgs = node["launchArgs"].GetAttribute("value");
                exe = node["exe"].GetAttribute("value");

                XmlNode rootDirectory = node["directory"];
                Directory root = new Directory("root");
                ParseDirectory(rootDirectory, root);
                

                return new AutoUpdateXml(version, new Uri(uri), root, description, launchArgs, exe);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Parses the directories and files nodes from the update.xml file.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="directory"></param>
        private static void ParseDirectory(XmlNode node, Directory directory)
        {
            foreach (XmlNode childeNode in node.ChildNodes)
            {
                if (childeNode.Name == "directory")
                {
                    Directory newDirectory = new Directory(childeNode.Attributes["name"].Value);
                    directory.Directories.Add(newDirectory);
                    ParseDirectory(childeNode, newDirectory);
                }
                else if (childeNode.Name == "file")
                {
                    directory.Files.Add(new KeyValuePair<string, string>(childeNode.Attributes["name"].Value, childeNode.Attributes["md5"].Value));
                }
            }
        }
    }

    /// <summary>
    /// Contains information of the directories and files to update.
    /// </summary>
    public struct Directory
    {
        public string Name;
        public List<Directory> Directories;
        public List<KeyValuePair<string, string>> Files;

        internal Directory(string name)
        {
            Name = name;
            Directories = new List<Directory>();
            Files = new List<KeyValuePair<string, string>>();
        } 
    }
}
