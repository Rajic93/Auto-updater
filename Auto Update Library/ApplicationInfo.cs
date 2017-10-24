using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Auto_updater
{
    public class ApplicationInfo : IAutoUpdatable
    {
        public string ApplicationName { get; set; }
        public string ApplicationId { get; set; }
        public Assembly ApplicationAssembly { get; set; }
        public Icon ApplicationIcon { get; set; }
        public Uri UpdateXmlLocation { get; set; }
        public Form Context { get; set; }
    }
}
