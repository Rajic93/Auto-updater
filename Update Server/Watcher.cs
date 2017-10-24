using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Server
{
    public delegate void UpdateCallback(XmlDocument doc);

    internal class Watcher
    {
        FileSystemWatcher watcher;
        private XmlDocument _xmlDocument;

        private UpdateCallback _callback;

        internal Watcher(XmlDocument xmlDocument, UpdateCallback callback)
        {
            string path = $"{Directory.GetCurrentDirectory()}\\updates";
            Directory.CreateDirectory(path);
            watcher = new FileSystemWatcher(path);
            _xmlDocument = xmlDocument;
            _callback = callback;
        }

        internal void Initialize()
        {
            watcher.Changed += Watcher_Changed;
            watcher.EnableRaisingEvents = true;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            _xmlDocument = XmlCreator.Create();
            //_callback(_xmlDocument);
        }
    }
}
