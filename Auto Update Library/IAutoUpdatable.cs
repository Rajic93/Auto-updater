using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Auto_updater
{

    public delegate void WpfWindowCloseMethod(ProcessStartInfo info);

    /// <summary>
    /// An interface that all applications have to implement in order to use AutoUpdater
    /// </summary>
    public interface IAutoUpdatable
    {
        /// <summary>
        /// The name of your application as wanted to display on the update form.
        /// </summary>
        string ApplicationName { get; }
        /// <summary>
        /// An identifier string used to identify your application in the update.xml.
        /// Should be the same as your appID in the update.xml.
        /// </summary>
        string ApplicationId { get; }
        /// <summary>
        /// The current assembly.
        /// </summary>
        Assembly ApplicationAssembly { get; }
        /// <summary>
        /// The application's icon to be displayed in the top left.
        /// </summary>
        Icon ApplicationIcon { get; }
        /// <summary>
        /// The location of the update.xml on a server.
        /// </summary>
        Uri UpdateXmlLocation { get; }
        /// <summary>
        /// The context of the program.
        /// For Windows Forms applications, use "this".
        /// Console applications, reference System.Windows.Forms and return null.
        /// </summary>
        Form Context { get; }
        
    }
}
