using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Watcher watcher;

        private XmlDocument _xmlDocument;

        public MainWindow()
        {
            InitializeComponent();
            watcher = new Watcher(_xmlDocument, UpdateXmlView);
            watcher.Initialize();
        }

        private void UpdateXmlView(XmlDocument doc)
        {
            

            FlowDocument ObjFdoc = new FlowDocument();

            //Add paragraphs to flowdocument Blocks property

            Paragraph ObjPara1 = new Paragraph();

            ObjPara1.Inlines.Add(new Run(XmlCreator.GetPrettyXml(doc)));
            

            ObjFdoc.Blocks.Add(ObjPara1);

            // Finally Assign the FlowDocuemnt object to Document Property fo RichTextBox Control.
            XmlFile.Document = ObjFdoc;
            //Update(ObjFdoc);

        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            _xmlDocument = XmlCreator.Create();
            UpdateXmlView(_xmlDocument);
        }

        private void ButtonBase2_OnClick(object sender, RoutedEventArgs e)
        {

            XmlNodeList list = _xmlDocument.GetElementsByTagName("update");

            (list[0] as XmlElement).SetAttribute("appId", AppId.Text);
            list = list[0].ChildNodes;
            foreach (XmlElement xmlElement in list)
            {
                string value = "";
                if (xmlElement.Name == "version")
                    value = Version.Text;
                if (xmlElement.Name == "url")
                    value = Url.Text;
                if (xmlElement.Name == "exe")
                    value = Exe.Text;
                if (xmlElement.Name == "description")
                    value = new TextRange(Description.Document.ContentStart, Description.Document.ContentEnd).Text;
                if (xmlElement.Name == "launchArgs")
                    value = new TextRange(Args.Document.ContentStart, Args.Document.ContentEnd).Text;
                xmlElement.SetAttribute("value", value);
            }

            _xmlDocument.Save($"{Directory.GetCurrentDirectory()}\\app\\update.xml");
            UpdateXmlView(_xmlDocument);
        }
    }
}
