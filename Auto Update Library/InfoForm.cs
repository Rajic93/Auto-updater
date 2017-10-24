using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Auto_updater
{
    /// <summary>
    /// Form to show the information about the update.
    /// </summary>
    public partial class InfoForm : Form
    {
        /// <summary>
        /// Creates a new InfoForm.
        /// </summary>
        /// <param name="applicationInfo"></param>
        /// <param name="updateInfo"></param>
        internal InfoForm(IAutoUpdatable applicationInfo, AutoUpdateXml updateInfo)
        {
            InitializeComponent();
            //Sets the icon if it's not null
            if (applicationInfo.ApplicationIcon != null)
                Icon = applicationInfo.ApplicationIcon;

            //Fill the UI
            Text = applicationInfo.ApplicationName + " Update Info";
            lblVersions.Text = String.Format("Current Version: {0}\nUpdate Version: {1}",
                applicationInfo.ApplicationAssembly.GetName().Version.ToString(), updateInfo.Version.ToString());
            txtDescription.Text = updateInfo.Description;
        }
        
        private void InfoForm_KeyDown(object sender, KeyEventArgs e)
        {
            //only allow Ctrl - C to copy text
            if (!e.Control && e.KeyCode == Keys.C)
                e.SuppressKeyPress = true;
        }
    }
}
