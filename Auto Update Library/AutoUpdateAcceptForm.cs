using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Auto_updater
{
    /// <summary>
    /// The form to prompt user to accept update.
    /// </summary>
    public partial class AutoUpdateAcceptForm : Form
    {
        /// <summary>
        /// The program to update's info.
        /// </summary>
        private IAutoUpdatable applicationInfo;
        /// <summary>
        /// The update info from the update.xml.
        /// </summary>
        private AutoUpdateXml update;

        private InfoForm updateInfoForm;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationInfo"></param>
        /// <param name="update"></param>
        internal AutoUpdateAcceptForm(IAutoUpdatable applicationInfo, AutoUpdateXml update)
        {
            InitializeComponent();
            this.applicationInfo = applicationInfo;
            this.update = update;

            Text = applicationInfo.ApplicationName + " Update Available";
            if (applicationInfo.ApplicationIcon != null)
                Icon = applicationInfo.ApplicationIcon;
            lblNewVersion.Text = String.Format("New Version: {0}", update.Version.ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Yes;
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;
            Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if(updateInfoForm == null)
                updateInfoForm = new InfoForm(applicationInfo, update);
            updateInfoForm.ShowDialog(this);
        }
    }
}
