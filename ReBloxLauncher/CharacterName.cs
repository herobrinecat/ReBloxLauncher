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
            if (Properties.Settings.Default.CharactersList != null && Properties.Settings.Default.CharactersList.Count > 0)
            {
                try
                {
                    for (int i = 0; i < Properties.Settings.Default.CharactersList.Count; i++)
                    {
                        SaveAvatarType avatarType = JsonConvert.DeserializeObject<SaveAvatarType>(Properties.Settings.Default.CharactersList[i]);
                        if (avatarType.name == textBox1.Text.Trim())
                        {
                            MessageBox.Show("This character name is taken! Please choose a different name.", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            textBox1.Text = "";
                            return;
                        }
                    }
                } catch (Exception e1)
                {
                    Console.WriteLine("<ERROR> Something went wrong while checking if the character name is taken! Look at the error below:\r\n" + e1);
                    return;
                }
            }
            frm1.characterName = textBox1.Text.Trim();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (Properties.Settings.Default.CharactersList != null && Properties.Settings.Default.CharactersList.Count > 0)
                {
                    try
                    {
                        for (int i = 0; i < Properties.Settings.Default.CharactersList.Count; i++)
                        {
                            SaveAvatarType avatarType = JsonConvert.DeserializeObject<SaveAvatarType>(Properties.Settings.Default.CharactersList[i]);
                            if (avatarType.name == textBox1.Text.Trim())
                            {
                                MessageBox.Show("This character name is taken! Please choose a different name.", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                textBox1.Text = "";
                                return;
                            }
                        }
                    }
                    catch (Exception e1)
                    {
                        Console.WriteLine("<ERROR> Something went wrong while checking if the character name is taken! Look at the error below:\r\n" + e1);
                        return;
                    }
                }
                frm1.characterName = textBox1.Text.Trim();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}
