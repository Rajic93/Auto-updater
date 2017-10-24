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

        private XmlDocument _xmlDocument;

        private string _manifestLocation = $"{Directory.GetCurrentDirectory()}\\app\\update.xml";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void UpdateXmlView(XmlDocument doc)
        {
            FlowDocument ObjFdoc = new FlowDocument();
            Paragraph ObjPara1 = new Paragraph();
            ObjPara1.Inlines.Add(new Run(XmlCreator.GetPrettyXml(doc)));
            ObjFdoc.Blocks.Add(ObjPara1);
            XmlFile.Document = ObjFdoc;
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
                else if (xmlElement.Name == "url")
                    value = Url.Text;
                else if (xmlElement.Name == "exe")
                    value = Exe.Text;
                else if (xmlElement.Name == "description")
                    value = new TextRange(Description.Document.ContentStart, Description.Document.ContentEnd).Text;
                else if (xmlElement.Name == "launchArgs")
                    value = new TextRange(Args.Document.ContentStart, Args.Document.ContentEnd).Text;
                else
                    continue;
                xmlElement.SetAttribute("value", value);
            }

            _xmlDocument.Save(_manifestLocation);
            UpdateXmlView(_xmlDocument);
        }

        private void ButtonBase3_OnClick(object sender, RoutedEventArgs e)
        {
            _xmlDocument = new XmlDocument();
            _xmlDocument.Load(_manifestLocation);
            XmlNodeList list = _xmlDocument.GetElementsByTagName("update");
            AppId.Text = (list[0] as XmlElement).GetAttribute("appId");
            list = list[0].ChildNodes;
            foreach (XmlElement xmlElement in list)
            {
                string value = xmlElement.GetAttribute("value");
                if (xmlElement.Name == "version")
                    Version.Text = value;
                else if (xmlElement.Name == "url")
                    Url.Text = value;
                else if (xmlElement.Name == "exe")
                    Exe.Text = value;
                else if (xmlElement.Name == "description")
                {
                    FlowDocument ObjFdoc = new FlowDocument();
                    Paragraph ObjPara1 = new Paragraph();
                    ObjPara1.Inlines.Add(value);
                    ObjFdoc.Blocks.Add(ObjPara1);
                    Description.Document = ObjFdoc;
                }
                else if (xmlElement.Name == "launchArgs")
                {
                    FlowDocument ObjFdoc = new FlowDocument();
                    Paragraph ObjPara1 = new Paragraph();
                    ObjPara1.Inlines.Add(value);
                    ObjFdoc.Blocks.Add(ObjPara1);
                    Args.Document = ObjFdoc;
                }
            }
            FlowDocument ObjFdoc2 = new FlowDocument();
            Paragraph ObjPara12 = new Paragraph();
            ObjPara12.Inlines.Add(XmlCreator.GetPrettyXml(_xmlDocument));
            ObjFdoc2.Blocks.Add(ObjPara12);
            XmlFile.Document = ObjFdoc2;
        }
    }
}
