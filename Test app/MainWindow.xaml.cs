using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Auto_updater;
using Update_progress;

namespace Test_app
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            AutoUpdater update = new AutoUpdater(new ApplicationInfo
            {
                ApplicationAssembly = Assembly.GetExecutingAssembly(),
                ApplicationIcon = null,
                ApplicationId = "app",
                ApplicationName = "App",
                UpdateXmlLocation = new Uri("http://127.0.0.1:8080/manifest/app/update.xml")
            });
            update.DoUpdate();
        }


    }
}
