using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
namespace ReBloxLauncher
{
    public partial class CharacterName : Form
    {
        Form1 frm1;
        public CharacterName(Form1 form1)
        {
            InitializeComponent();
            frm1 = form1;
        }

        public class AssetData
        {
            public ulong id { get; set; } = 0;
        }

        public class BodyColors
        {
            public uint headColor { get; set; } = 194;
            public uint leftArmColor { get; set; } = 194;
            public uint leftLegColor { get; set; } = 194;
            public uint rightArmColor { get; set; } = 194;
            public uint rightLegColor { get; set; } = 194;
            public uint torsoColor { get; set; } = 194;
        }

        public class SaveAvatarType
        {
            public string name { get; set; } = "Untitled Character 1";
            public string bodyType { get; set; } = "R6";
            public IList<AssetData> asset { get; set; }
            public BodyColors colors { get; set; }
            public string base64FullBody { get; set; } = "";
            public string base64HeadShot { get; set; } = "";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void CharacterName_Load(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            frm1.characterName = textBox1.Text.Trim();
            frm1.allowImages = checkBox1.Checked;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                frm1.characterName = textBox1.Text.Trim();
                frm1.allowImages = checkBox1.Checked;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}
