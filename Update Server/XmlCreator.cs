using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Server
{
    internal class XmlCreator
    {
        public static XmlDocument Create()
        {
            XmlDocument doc  = new XmlDocument();
            XmlDeclaration declaration = doc.CreateXmlDeclaration("1.0", "UTF-8", "");
            doc.AppendChild(declaration);
            XmlNode autoUpdateNode = doc.CreateNode(XmlNodeType.Element, "AutoUpdate", String.Empty);
            //update node
            XmlNode updateNode = doc.CreateNode(XmlNodeType.Element, "update", String.Empty);
            XmlAttribute attr = doc.CreateAttribute("appId");
            attr.Value = "test";
            updateNode.Attributes.SetNamedItem(attr);
            autoUpdateNode.AppendChild(updateNode);
            doc.AppendChild(autoUpdateNode);
            //version, url, exe, desc, args
            XmlNode versionNode = doc.CreateNode(XmlNodeType.Element, "version", String.Empty);
            XmlNode urlNode = doc.CreateNode(XmlNodeType.Element, "url", String.Empty);
            XmlNode exeNode = doc.CreateNode(XmlNodeType.Element, "exe", String.Empty);
            XmlNode descNode = doc.CreateNode(XmlNodeType.Element, "description", String.Empty);
            XmlNode argsNode = doc.CreateNode(XmlNodeType.Element, "launchArgs", String.Empty);
            updateNode.AppendChild(versionNode);
            updateNode.AppendChild(urlNode);
            updateNode.AppendChild(exeNode);
            updateNode.AppendChild(descNode);
            updateNode.AppendChild(argsNode);
            //directory
            XmlNode directoryNode = doc.CreateNode(XmlNodeType.Element, "directory", String.Empty);
            directoryNode = GenerateXml(directoryNode, $"{Directory.GetCurrentDirectory()}\\updates", doc, "root");
            updateNode.AppendChild(directoryNode);
            Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}\\app");

            return doc;
            //doc.Save($"{Directory.GetCurrentDirectory()}\\app\\update.xml");

            //Directory.GetDirectories("");
        }

        private static XmlNode GenerateXml(XmlNode directoryNode, string currentDirectory, XmlDocument doc, string directoryName)
        {
            XmlAttribute attribute = doc.CreateAttribute("name");
            attribute.Value = directoryName;
            directoryNode.Attributes.SetNamedItem(attribute);
            string[] subDirectories = Directory.GetDirectories(currentDirectory);
            foreach (string subDirectory in subDirectories)
            {
                XmlNode subNode = doc.CreateNode(XmlNodeType.Element, "directory", String.Empty);
                string name = subDirectory.Split('\\').Last();
                directoryNode.AppendChild(subNode);
                GenerateXml(subNode, subDirectory, doc, name);
            }
            string[] files = Directory.GetFiles(currentDirectory);
            foreach (string file in files)
            {
                XmlNode subNode = doc.CreateNode(XmlNodeType.Element, "file", String.Empty);
                string name = file.Split('\\').Last();
                XmlAttribute nameAttribute = doc.CreateAttribute("name");
                nameAttribute.Value = name;
                XmlAttribute md5Attribute = doc.CreateAttribute("md5");
                using (var md5 = MD5.Create())
                    using (var stream = File.OpenRead(file))
                        md5Attribute.Value = ToHex(md5.ComputeHash(stream), false);
                subNode.Attributes.SetNamedItem(nameAttribute);
                subNode.Attributes.SetNamedItem(md5Attribute);
                directoryNode.AppendChild(subNode);
            }
            return directoryNode;
        }

        public static string ToString(XmlDocument doc)
        {
            using (var stringWriter = new StringWriter())
            using (var xmlTextWriter = XmlWriter.Create(stringWriter))
            {
                doc.WriteTo(xmlTextWriter);
                xmlTextWriter.Flush();
                return stringWriter.GetStringBuilder().ToString();
            }
        }

        public static string GetPrettyXml(XmlDocument doc)
        {
            // Configure how XML is to be formatted
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true
                ,
                IndentChars = "  "
                ,
                NewLineChars = System.Environment.NewLine
                ,
                NewLineHandling = NewLineHandling.Replace
                //,NewLineOnAttributes = true
                //,OmitXmlDeclaration = false
            };

            // Use wrapper class that supports UTF-8 encoding
            StringWriterWithEncoding sw = new StringWriterWithEncoding(Encoding.UTF8);

            // Output formatted XML to StringWriter
            using (XmlWriter writer = XmlWriter.Create(sw, settings))
            {
                doc.Save(writer);
            }

            // Get formatted text from writer
            return sw.ToString();
        }

        private sealed class StringWriterWithEncoding : StringWriter
        {
            private readonly Encoding encoding;

            /// <summary>
            /// Creates a new <see cref="PrettyXmlFormatter"/> with the specified encoding
            /// </summary>
            /// <param name="encoding"></param>
            public StringWriterWithEncoding(Encoding encoding)
            {
                this.encoding = encoding;
            }

            /// <summary>
            /// Encoding to use when dealing with text
            /// </summary>
            public override Encoding Encoding
            {
                get { return encoding; }
            }
        }


        public static string ToHex(byte[] bytes, bool upperCase)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
                result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));

            return result.ToString();
        }
    }
}
