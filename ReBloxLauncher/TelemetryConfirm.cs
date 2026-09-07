using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReBloxLauncher
{
    public partial class TelemetryConfirm : Form
    {
        public TelemetryConfirm()
        {
            InitializeComponent();
        }

        private void TelemetryConfirm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.firstTime = false;
            Properties.Settings.Default.Save();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.TelemetryEnabled = checkBox1.Checked;
            if (checkBox1.Checked && Properties.Settings.Default.uuid == Guid.Empty)
            {
                Properties.Settings.Default.uuid = Guid.NewGuid();
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
