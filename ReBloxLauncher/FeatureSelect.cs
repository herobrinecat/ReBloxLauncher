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
    public partial class FeatureSelect : Form
    {
        string[] allowedFeatures;
        string clientVersion;

        string[] numberFlags = new string[] { "ChangeMaxPlayers" };
        (int, int, int)[] maxMinDefaultNumberFlags = new (int, int, int)[] { (1000, 1, 15) };
        public FeatureSelect()
        {
            InitializeComponent();
        }

        public FeatureSelect(string[] features, string version)
        {
            InitializeComponent();
            allowedFeatures = features;
            clientVersion = version;
        }
        
        int lasty = 55;
        int lastx = -249;

        private bool hasDecimal(double num, int totalColumn)
        {
            double result = num / totalColumn;
            return Math.Floor(result) != result;
        }

        private class VersionFlag
        {
            public string version { get; set; }
            public string[] flags { get; set; }
        }
        
        private class VersionFlags
        {
            public VersionFlag[] data { get; set; }
        }

        private bool SaveFlag(string version, string flag, object value = null)
        {
            if (version != "" && flag != "")
            {
                try
                {
                    if (Properties.Settings.Default.FeaturesEnabled != "")
                    {
                        if (Properties.Settings.Default.FeaturesEnabled.EndsWith("}") && Properties.Settings.Default.FeaturesEnabled.StartsWith("{"))
                        {
                            VersionFlags flags = JsonConvert.DeserializeObject<VersionFlags>(Properties.Settings.Default.FeaturesEnabled);
                            if (flags.data.Any(e => e.version == version))
                            {
                                for (int i = 0; i < flags.data.Length; i++)
                                {
                                    if (flags.data[i].version == version)
                                    {
                                        if (flags.data[i].flags != null && flags.data[i].flags.Length > 0)
                                        {
                                            List<string> flagsEdit = flags.data[i].flags.ToList();

                                            if (flag.StartsWith("-"))
                                            {
                                                flagsEdit.Remove(flag.Remove(0, 1));
                                            }
                                            else
                                            {
                                                flagsEdit.Add(flag);
                                            }

                                            flags.data[i].flags = flagsEdit.ToArray();

                                            string parsedJSON = JsonConvert.SerializeObject(flags);

                                            Properties.Settings.Default.FeaturesEnabled = parsedJSON;
                                            Properties.Settings.Default.Save();
                                            break;
                                        }
                                        else
                                        {
                                            List<string> flagsEdit = new List<string>();

                                            if (flag.StartsWith("-") == false)
                                            {
                                                flagsEdit.Add(flag);
                                            }

                                            flags.data[i].flags = flagsEdit.ToArray();

                                            string parsedJSON = JsonConvert.SerializeObject(flags);

                                            Properties.Settings.Default.FeaturesEnabled = parsedJSON;
                                            Properties.Settings.Default.Save();
                                            break;
                                        }
                                    }
                                }
                                return true;
                            }
                            else
                            {
                                List<VersionFlag> flagsList = flags.data.ToList();
                                flagsList.Add(new VersionFlag { version = version, flags = flag.Contains(",") ? flag.Split(',') : new string[] { flag } });

                                flags.data = flagsList.ToArray();

                                string parsedJSON = JsonConvert.SerializeObject(flags);

                                Properties.Settings.Default.FeaturesEnabled = parsedJSON;
                                Properties.Settings.Default.Save();

                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        VersionFlags flags = new VersionFlags { data = new VersionFlag[] { new VersionFlag { version = version, flags = flag.Contains(",") ? flag.Split(',') : new string[] { flag } } } };

                        string parsedJSON = JsonConvert.SerializeObject(flags);

                        Properties.Settings.Default.FeaturesEnabled = parsedJSON;
                        Properties.Settings.Default.Save();
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private string getDescription(string flag)
        {
            if (flag == "DisableRCCAuth") return "Sets the \"SetIsPlayerAuthenticationRequired\" to false, allowing older launcher versions to join.";
            if (flag == "ChangeMaxPlayers") return "Change the maximum number of players that can join the server.";
            return flag;
        }

        private bool CheckFlag(string version, string flagName)
        {
            if (Properties.Settings.Default.FeaturesEnabled != "")
            {
                VersionFlags versions = JsonConvert.DeserializeObject<VersionFlags>(Properties.Settings.Default.FeaturesEnabled);
                for (int i = 0; i < versions.data.Length; i++)
                {
                    if (versions.data[i].version == version)
                    {
                        if (versions.data[i].flags.Contains(flagName))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        private void FeatureSelect_Load(object sender, EventArgs e)
        {
            int loopOffset = 0;
            if (allowedFeatures != null && allowedFeatures.Length > 0) 
            {
                label2.Visible = false;
            }

            for (int i = 0; i < allowedFeatures.Length; i++)
            {
                if (allowedFeatures[i].StartsWith("-"))
                {
                    if (i + 1 >= allowedFeatures.Length) break;
                    loopOffset++;
                    i++;
                }
                if (hasDecimal(i - loopOffset + 1, 4) == false)
                {
                    lastx = 12;
                    lasty += 164;
                }
                else
                {
                    lastx += 261;
                }
                Panel panel = new Panel();
                panel.Size = new Size(255, 147);
                panel.BackColor = Color.FromArgb(20, 20, 20);
                panel.Location = new Point(lastx, lasty);
                panel.Name = "feature" + i.ToString();
                Label titleLabel = new Label();
                titleLabel.Font = new Font("Arial", 12f, FontStyle.Bold, GraphicsUnit.Point);
                titleLabel.Location = new Point(5, 11);
                titleLabel.Text = allowedFeatures[i];
                titleLabel.AutoSize = true;
                titleLabel.ForeColor = Color.White;
                titleLabel.Name = "title" + i.ToString();
                Label descriptionLabel = new Label();
                descriptionLabel.Font = new Font("Arial", 8f, FontStyle.Regular, GraphicsUnit.Point);
                descriptionLabel.Location = new Point(6, 48);
                descriptionLabel.Text = getDescription(allowedFeatures[i]);
                descriptionLabel.AutoSize = false;
                descriptionLabel.ForeColor = Color.White;
                descriptionLabel.Name = "description" + i.ToString();
                descriptionLabel.Size = new Size(243, 69);
                if (numberFlags.Contains(allowedFeatures[i]))
                {
                    NumericUpDown numberBox = new NumericUpDown();
                    numberBox.Font = new Font("Arial", 8.25f, FontStyle.Regular, GraphicsUnit.Point);
                    numberBox.Location = new Point(9, 120);
                    numberBox.AutoSize = true;
                    numberBox.ForeColor = Color.White;
                    numberBox.Tag = allowedFeatures[i];
                    numberBox.Name = "checkBox" + i.ToString();
                    numberBox.Value = maxMinDefaultNumberFlags[Array.IndexOf(numberFlags, allowedFeatures[i])].Item3;
                    numberBox.Maximum = maxMinDefaultNumberFlags[Array.IndexOf(numberFlags, allowedFeatures[i])].Item1;
                    numberBox.Minimum = maxMinDefaultNumberFlags[Array.IndexOf(numberFlags, allowedFeatures[i])].Item2;
                    numberBox.ValueChanged += NumberBox_ValueChanged;
                    panel.Controls.Add(numberBox);
                }
                else
                {
                    CheckBox enableBox = new CheckBox();
                    enableBox.Text = "Enable";
                    enableBox.Font = new Font("Arial", 8.25f, FontStyle.Regular, GraphicsUnit.Point);
                    enableBox.Location = new Point(9, 120);
                    enableBox.AutoSize = true;
                    enableBox.ForeColor = Color.White;
                    enableBox.Tag = allowedFeatures[i];
                    enableBox.Name = "checkBox" + i.ToString();
                    enableBox.Checked = CheckFlag(clientVersion, allowedFeatures[i]);
                    enableBox.CheckedChanged += EnableBox_CheckedChanged;
                    panel.Controls.Add(enableBox);
                }
                panel.Controls.Add(titleLabel);
                panel.Controls.Add(descriptionLabel);
                
                this.Controls.Add(panel);
            }    
        }

        private void NumberBox_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown numberbox = sender as NumericUpDown;
            SaveFlag(clientVersion, (string)numberbox.Tag, numberbox.Value);
        }

        private void EnableBox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;
            SaveFlag(clientVersion, checkbox.Checked ? (string)checkbox.Tag : "-" + (string)checkbox.Tag);
        }
    }
}
