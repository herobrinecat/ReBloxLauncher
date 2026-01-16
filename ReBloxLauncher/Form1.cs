using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Threading;
using System.Text.RegularExpressions;
using DiscordRPC;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using System.Globalization;
using System.Net.Sockets;

namespace ReBloxLauncher
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            InitializeComponent();
        }
        //Variables
        bool starting = true;
        bool onetime = false;
        int previousIndexAsset = -1;
        int previousIndexMap = -1;
        int previousIndexClient = -1;
        int previousIndexAvatar = -1;
        string datafolder = Path.GetDirectoryName(Application.ExecutablePath) + @"\data";
        string hostargument = "";
        string joinargument = "";
        bool UseJoinJSONLink = false;
        bool useIPForwarder = false;
        bool ReserveAssetIdForMap = false;
        int placeid = 1;
        bool caInstalled = false;
        readonly object syncLock = new object();
        Random random = new Random();
        readonly List<string> directoryasset = new List<string>();
        FileStream ostrm;
        StreamWriter writer;
        TextWriter oldOut = Console.Out;
        DiscordRpcClient client;
        readonly System.Timers.Timer timer = new System.Timers.Timer(150);
        public int resultBrickColor = 0;
        bool launchershortcut = false;
        bool useNewRoblox = false;
        bool isJoiningOrStudio = false;
        string updateurl = Properties.Settings.Default.UpdateURL;
        bool searchingNetwork = false;
        bool internetConnected = false;
        bool useOldSignature = false;
        bool useOldAssetFormat = false;
        bool dontLoadMapFromArgument = false;
        //Functions

        System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs e)
        {
            string dllName = e.Name.Contains(',') ? e.Name.Substring(0, e.Name.IndexOf(',')) : e.Name.Replace(".dll", "");

            dllName = dllName.Replace(".", "_");

            if (dllName.EndsWith("_resources")) return null;

            System.Resources.ResourceManager rm = new System.Resources.ResourceManager(GetType().Namespace + ".Properties.Resources", System.Reflection.Assembly.GetExecutingAssembly());

            byte[] bytes = (byte[])rm.GetObject(dllName);

            return System.Reflection.Assembly.Load(bytes);
        }

        public int RandomNumber(int min, int max)
        {
            lock (syncLock)
            {
                return random.Next(min, max);
            }
        }
        public bool IsAdministrator()
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }
        private int convertDateRangeToInt(string dateRange)
        {
            switch (dateRange) 
            {
                case "E":
                    return 1;
                case "M":
                    return 2;
                case "L":
                    return 3;
                default:
                    return 0;
            }
        }
        private void LoadAssets()
        {
            statusText.Invoke(new Action(() => { statusText.Text = "Loading assets..."; }));
            string[] splited = Properties.Settings.Default.AssetPackEnabled.Split('|');
            foreach (string s in splited)
            {
                if (Directory.Exists(s))
                {
                    bool compatible = true;
                    string[] config = File.ReadAllLines(s + @"\ReBlox.ini");
                    for (int i = 0; i < config.Length; i++)
                    {
                        if (config[i].Trim().StartsWith("Clients="))
                        {
                            string[] splited1 = config[i].Trim().Split(new char[] { '=' }, 2);
                            if (splited1[1] == "*")
                            {
                                compatible = true;
                                break;
                            }
                            else if (splited1[1].Contains(","))
                            {
                                bool valuefound = false;
                                string[] splitedtwo = splited1[1].Split(',');
                                foreach (string version in splitedtwo)
                                {
                                    if (version == Properties.Settings.Default.lastselectedversion)
                                    {
                                        valuefound = true;
                                        compatible = true;
                                        break;
                                    }
                                }
                                if (valuefound == false) compatible = false; break;
                            }
                            else if (splited1[1] == Properties.Settings.Default.lastselectedversion)
                            {
                                compatible = true; break;
                            }
                            else if (splited1[1].EndsWith("+"))
                            {
                                try 
                                {
                                    string year = splited1[1].Substring(0, 4);
                                    string daterange = splited1[1].Substring(4, 1);

                                    if (int.TryParse(year + convertDateRangeToInt(daterange).ToString(), out _) == true)
                                    {
                                        int total = int.Parse(year + convertDateRangeToInt(daterange).ToString());
                                        string year1 = Properties.Settings.Default.lastselectedversion.Substring(0, 4);
                                        string daterange1 = Properties.Settings.Default.lastselectedversion.Substring(4, 1);
                                        if (int.TryParse(year1 + convertDateRangeToInt(daterange1).ToString(), out _) == true)
                                        {
                                            int total1 = int.Parse(year1 + convertDateRangeToInt(daterange1).ToString());

                                            if (total1 >= total)
                                            {
                                                compatible = true; break;
                                            }
                                            else
                                            {
                                                compatible = false; break;
                                            }
                                        }
                                        else
                                        {
                                            compatible = false; break;
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("<WARN> Skipping asset pack \"" + Path.GetFileName(s) + "\" due to invalid clients");
                                        compatible = false; break;
                                    }
                                } catch
                                {
                                    Console.WriteLine("<WARN> Skipping asset pack \"" + Path.GetFileName(s) + "\" due to invalid clients");
                                    compatible = false; break;
                                }
                            }
                            else if (splited1[1].EndsWith("-"))
                            {
                                try
                                {
                                    string year = splited1[1].Substring(0, 4);
                                    string daterange = splited1[1].Substring(4, 1);

                                    if (int.TryParse(year + convertDateRangeToInt(daterange).ToString(), out _) == true)
                                    {
                                        int total = int.Parse(year + convertDateRangeToInt(daterange).ToString());
                                        string year1 = Properties.Settings.Default.lastselectedversion.Substring(0, 4);
                                        string daterange1 = Properties.Settings.Default.lastselectedversion.Substring(4, 1);
                                        if (int.TryParse(year1 + convertDateRangeToInt(daterange1).ToString(), out _) == true)
                                        {
                                            int total1 = int.Parse(year1 + convertDateRangeToInt(daterange1).ToString());

                                            if (total1 <= total)
                                            {
                                                compatible = true; break;
                                            }
                                            else
                                            {
                                                compatible = false; break;
                                            }
                                        }
                                        else
                                        {
                                            compatible = false; break;
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("<WARN> Skipping asset pack \"" + Path.GetFileName(s) + "\" due to invalid clients");
                                        compatible = false; break;
                                    }
                                } catch
                                {
                                    Console.WriteLine("<WARN> Skipping asset pack \"" + Path.GetFileName(s) + "\" due to invalid clients");
                                    compatible = false; break;
                                }
                            }
                            else
                            {
                                compatible = false; break;
                            }
                        }
                    }
                    if (compatible)
                    {
                        string[] files = Directory.GetFiles(s);
                        foreach (string file in files)
                        {
                            if (file.EndsWith(".ini") == false)
                            {
                                if (ReserveAssetIdForMap && Path.GetFileNameWithoutExtension(file) != placeid.ToString() || ReserveAssetIdForMap == false || isJoiningOrStudio == true)
                                {
                                    File.Copy(file, datafolder + @"\tools\RobloxAssetFixer\assets\" + Path.GetFileName(file), true);
                                }
                                else
                                {
                                    File.Copy(datafolder + @"\maps\" + listBox2.GetItemText(listBox2.SelectedItem), datafolder + @"\tools\RobloxAssetFixer\assets\" + Path.GetFileName(file), true);
                                }
                            }
                        }
                    }

                }
                else
                {
                    Console.WriteLine("<WARN> Unable to load asset in \"" + s + "\", does it even exist?");
                }
            }
                if (Directory.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\assets"))
                {
                    string[] files = Directory.GetFiles(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\assets");
                    if (files.Length > 0 )
                    {
                        foreach (string file in files)
                        {
                            if (ReserveAssetIdForMap && Path.GetFileNameWithoutExtension(file) != placeid.ToString() || ReserveAssetIdForMap == false || isJoiningOrStudio == true)
                            {
                                File.Copy(file, datafolder + @"\tools\RobloxAssetFixer\assets\" + Path.GetFileName(file), true);
                            }
                            else
                            {
                                File.Copy(datafolder + @"\maps\" + listBox2.GetItemText(listBox2.SelectedItem), datafolder + @"\tools\RobloxAssetFixer\assets\" + Path.GetFileName(file), true);
                            }
                        }
                        
                    }
                }
            if (ReserveAssetIdForMap && isJoiningOrStudio == false)
            {
                File.Copy(datafolder + @"\maps\" + listBox2.GetItemText(listBox2.SelectedItem), datafolder + @"\tools\RobloxAssetFixer\assets\" + placeid + ".rbxl", true);
            }
        }
        private void ClearAssets()
        {
            statusText.Invoke(new Action(() => { statusText.Text = "Cleaning up assets..."; }));
            string[] files = Directory.GetFiles(datafolder + @"\tools\RobloxAssetFixer\assets");
            foreach (string file in files)
            {
                if (file.EndsWith("avatar.png") == false && file.EndsWith("gameicon.png") == false && file.EndsWith("headshot.png") == false)
                {
                    try
                    {
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                        }
                    }
                    catch 
                    { 
                        //do nothing
                    }
                }
            }
        }
        private void Initialize()
        {
           try
            {
               if (launchershortcut == false)
                {
                    if (Directory.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\logs"))
                    {
                        bool success = false;
                        try
                        {
                            if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\logs\log.log"))
                            {
                                File.Move(Path.GetDirectoryName(Application.ExecutablePath) + @"\logs\log.log", Path.GetDirectoryName(Application.ExecutablePath) + @"\logs\log" + RandomNumber(10000, 99999) + ".log");
                            }
                            ostrm = new FileStream("./logs/log.log", FileMode.OpenOrCreate, FileAccess.Write);
                            writer = new StreamWriter(ostrm);
                            success = true;
                        }
                        catch (UnauthorizedAccessException _)
                        {
                            MessageBox.Show("Failed to open log.log for logging! Another launcher may be using the log file, please check your processes and make sure that the folder is not read-only and has proper permission.", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        catch (Exception _)
                        {
                            MessageBox.Show("Failed to open log.log for logging, make sure the folder is not read-only!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        if (success)
                        {
                            Console.SetOut(writer);
                            Console.WriteLine("<INFO> Logging has started!");
                        }
                    }
                    if (WineDetector.IsRunningOnWine())
                    {
                        Console.WriteLine("<WARN> Wine detected, Wine version: " + WineDetector.getWineVersion());
                        button7.Visible = false;
                        MessageBox.Show("It appears you're running on Wine, we recommend running the RobloxAssetFixer natively on Linux! Please do not send bug reports of Studio crashing if you're on Wine, we will ignore or close your issue. To prevent using sudo in node.js, please run \"sudo setcap CAP_NET_BIND_SERVICE=+eip /path/to/nodejs\" in your terminal. RobloxAssetFixer will not run in favor of running it natively instead of Wine! You also need to set the hosts file manually in /etc/hosts!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    string[] directories2 = null;
                    if (Directory.Exists(datafolder + @"\maps")) directories2 = Directory.GetFiles(datafolder + @"\maps");
                    string[] directories = null;
                    if (Directory.Exists(datafolder + @"\clients")) directories = Directory.GetDirectories(datafolder + @"\clients");
                    Console.WriteLine("<INFO> Adding and sorting clients to the list");
                    if (Directory.Exists(datafolder + @"\clients") && directories != null && directories.Length > 0)
                    {
                        for (int i = 0; i < directories.Length; i++)
                        {
                            if (directories[i].EndsWith("L") && directories[i + 1] != null && directories[i + 1].EndsWith("M"))
                            {
                                listBox1.Items.Add(Path.GetFileName(directories[i + 1]));
                                listBox1.Items.Add(Path.GetFileName(directories[i]));
                                i++;
                            }
                            else
                            {
                                listBox1.Items.Add(Path.GetFileName(directories[i]));
                            }
                        }
                        if (listBox1.Items.IndexOf(Properties.Settings.Default.lastselectedversion) > -1)
                        {
                            listBox1.SetSelected(listBox1.Items.IndexOf(Properties.Settings.Default.lastselectedversion), true);
                        }
                    }
                    else
                    {
                        listBox1.Visible = false;
                        panel1.Visible = false;
                        label30.Visible = true;
                    }
                    Console.WriteLine("<INFO> Adding maps to the list");
                    if (Directory.Exists(datafolder + @"\maps") && directories2 != null && directories2.Length > 0) 
                    {
                        foreach (string directory in directories2)
                        {
                            if (directory.EndsWith(".rbxl") || directory.EndsWith(".rbxlx"))
                            {
                                listBox2.Items.Add(Path.GetFileName(directory));
                            }
                        }
                        if (listBox2.Items.IndexOf(Properties.Settings.Default.lastselectedmap) > -1)
                        {
                            listBox2.SetSelected(listBox2.Items.IndexOf(Properties.Settings.Default.lastselectedmap), true);
                        }
                    }
                    Console.WriteLine("<INFO> Adding asset packs to the list");
                    RefreshAssetPacks();
                    Console.WriteLine("<INFO> Setting up UI...");
                    if (Properties.Settings.Default.ClothesArray.Split('|').Length > 0) listBox4.Items.AddRange(Properties.Settings.Default.ClothesArray.Split('|'));
                    checkBox1.Checked = WineDetector.IsRunningOnWine() == false ? Properties.Settings.Default.useAuth : false;
                    checkBox1.Enabled = !WineDetector.IsRunningOnWine();
                    textBox3.ReadOnly = !Properties.Settings.Default.useAuth && WineDetector.IsRunningOnWine();
                    checkBox2.Checked = Properties.Settings.Default.UsePatchInStudio;
                    checkBox4.Checked = Properties.Settings.Default.ShowConsole;
                    checkBox3.Checked = Properties.Settings.Default.ClearTemp;
                    checkBox5.Checked = Properties.Settings.Default.AccountOver13;
                    checkBox6.Checked = Properties.Settings.Default.EnableDataStore;
                    checkBox7.Checked = WineDetector.IsRunningOnWine() == false ? Properties.Settings.Default.DiscordRPC : false;
                    checkBox7.Enabled = !WineDetector.IsRunningOnWine();
                    checkBox8.Checked = Properties.Settings.Default.EnableBadges;
                    checkBox9.Checked = Properties.Settings.Default.EnableFollowing;
                    checkBox10.Checked = WineDetector.IsRunningOnWine() == false ? Properties.Settings.Default.assetFromServer : false;
                    checkBox10.Enabled = !WineDetector.IsRunningOnWine();
                    comboBox1.SelectedIndex = Properties.Settings.Default.avatarR15 ? 1 : 0;
                    textBox4.Text = Properties.Settings.Default.UserId.ToString();
                    textBox5.Text = Properties.Settings.Default.username;
                    button5.Visible = (Properties.Settings.Default.CADontShow && caInstalled);
                    numericUpDown1.Value = Properties.Settings.Default.HeadColor;
                    numericUpDown2.Value = Properties.Settings.Default.LeftArmColor;
                    numericUpDown3.Value = Properties.Settings.Default.LeftLegColor;
                    numericUpDown4.Value = Properties.Settings.Default.RightArmColor;
                    numericUpDown5.Value = Properties.Settings.Default.RightLegColor;
                    numericUpDown6.Value = Properties.Settings.Default.TorsoColor;
                    comboBox2.Text = Properties.Settings.Default.ChatStyle;
                    comboBox3.Text = Properties.Settings.Default.Membership;
                    TorsoPanel.BackColor = convertBrickColortoColor(Properties.Settings.Default.TorsoColor);
                    HeadPanel.BackColor = convertBrickColortoColor(Properties.Settings.Default.HeadColor);
                    RightArmPanel.BackColor = convertBrickColortoColor(Properties.Settings.Default.RightArmColor);
                    RightLegPanel.BackColor = convertBrickColortoColor(Properties.Settings.Default.RightLegColor);
                    LeftArmPanel.BackColor = convertBrickColortoColor(Properties.Settings.Default.LeftArmColor);
                    LeftLegPanel.BackColor = convertBrickColortoColor(Properties.Settings.Default.LeftLegColor);
                    directories = null;
                    directories2 = null;
                    if (Properties.Settings.Default.DiscordRPC && WineDetector.IsRunningOnWine() == false)
                    {
                        try
                        {
                            Console.WriteLine("<INFO> Initializing Discord RPC");
                            client = new DiscordRpcClient(Encoding.UTF8.GetString(Convert.FromBase64String("MTQ0MjMwODk2MDk5NTUwODI1NA==")));
                            client.Initialize();

                            client.SetPresence(new RichPresence()
                            {
                                Details = "v" + Properties.Settings.Default.version,
                                State = "Viewing clients",
                                Assets = new Assets()
                                {
                                    LargeImageKey = "rebloxicon"
                                },
                                Timestamps = Timestamps.Now
                            });
                            timer.Elapsed += (sender, args) => { client.Invoke(); };
                            timer.Start();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("<ERROR> " + e.Message + "\nStack Trace: " + e.StackTrace);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("<ERROR> " + e.Message + "\nStack Trace: " + e.StackTrace);
                MessageBox.Show("Something when wrong while trying to initialize the launcher! Please look into log.log in the logs folder for more details!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
            starting = false;
        }

        private Color convertBrickColortoColor(int brickcolor)
        {
            switch (brickcolor)
            {
                case 1:
                    return Color.FromArgb(242, 243, 243);
                case 2:
                    return Color.FromArgb(161, 165, 162);
                case 3:
                    return Color.FromArgb(249, 233, 153);
                case 5:
                    return Color.FromArgb(215, 197, 154);
                case 6:
                    return Color.FromArgb(194, 218, 184);
                case 9:
                    return Color.FromArgb(232, 186, 200);
                case 11:
                    return Color.FromArgb(128, 187, 219);
                case 12:
                    return Color.FromArgb(203, 132, 66);
                case 18:
                    return Color.FromArgb(204, 142, 105);
                case 21:
                    return Color.FromArgb(196, 40, 28);
                case 22:
                    return Color.FromArgb(196, 112, 160);
                case 23:
                    return Color.FromArgb(13, 105, 172);
                case 24:
                    return Color.FromArgb(245, 205, 48);
                case 25:
                    return Color.FromArgb(98, 71, 50);
                case 26:
                    return Color.FromArgb(27, 42, 53);
                case 27:
                    return Color.FromArgb(109, 110, 108);
                case 28:
                    return Color.FromArgb(40, 127, 71);
                case 29:
                    return Color.FromArgb(161, 196, 140);
                case 36:
                    return Color.FromArgb(243, 207, 155);
                case 37:
                    return Color.FromArgb(75, 151, 75);
                case 38:
                    return Color.FromArgb(160, 95, 53);
                case 39:
                    return Color.FromArgb(193, 202, 222);
                case 40:
                    return Color.FromArgb(236, 236, 236);
                case 41:
                    return Color.FromArgb(205, 84, 75);
                case 42:
                    return Color.FromArgb(193,223,240);
                case 43:
                    return Color.FromArgb(123,182,232);
                case 44:
                    return Color.FromArgb(247, 241, 141);
                case 45:
                    return Color.FromArgb(180, 210, 228);
                case 47:
                    return Color.FromArgb(217, 133, 108);
                case 48:
                    return Color.FromArgb(132, 182, 141);
                case 49:
                    return Color.FromArgb(248, 241, 132);
                case 50:
                    return Color.FromArgb(236, 232, 222);
                case 100:
                    return Color.FromArgb(238, 196, 182);
                case 101:
                    return Color.FromArgb(218, 134, 122);
                case 102:
                    return Color.FromArgb(110, 153, 202);
                case 103:
                    return Color.FromArgb(199, 193, 183);
                case 104:
                    return Color.FromArgb(107, 50, 124);
                case 105:
                    return Color.FromArgb(226, 155, 64);
                case 106:
                    return Color.FromArgb(218, 133, 65);
                case 107:
                    return Color.FromArgb(0, 143, 156);
                case 108:
                    return Color.FromArgb(104, 92, 67);
                case 110:
                    return Color.FromArgb(67, 84, 147);
                case 111:
                    return Color.FromArgb(191, 183, 177);
                case 112:
                    return Color.FromArgb(104, 116, 172);
                case 113:
                    return Color.FromArgb(229, 173, 200);
                case 115:
                    return Color.FromArgb(199, 210, 60);
                case 116:
                    return Color.FromArgb(85, 165, 175);
                case 118:
                    return Color.FromArgb(183, 215, 213);
                case 119:
                    return Color.FromArgb(164, 179, 71);
                case 120:
                    return Color.FromArgb(217, 228, 167);
                case 121:
                    return Color.FromArgb(231, 172, 88);
                case 123:
                    return Color.FromArgb(211, 111, 76);
                case 124:
                    return Color.FromArgb(146, 57, 120);
                case 125:
                    return Color.FromArgb(234, 184, 146);
                case 126:
                    return Color.FromArgb(165, 165, 203);
                case 127:
                    return Color.FromArgb(220, 188, 129);
                case 128:
                    return Color.FromArgb(174, 122, 89);
                case 131:
                    return Color.FromArgb(156, 163, 168);
                case 133:
                    return Color.FromArgb(213, 115, 61);
                case 134:
                    return Color.FromArgb(216, 221, 86);
                case 135:
                    return Color.FromArgb(116, 134, 157);
                case 136:
                    return Color.FromArgb(135, 124, 144);
                case 137:
                    return Color.FromArgb(224, 152, 100);
                case 138:
                    return Color.FromArgb(149, 138, 115);
                case 140:
                    return Color.FromArgb(32, 58, 86);
                case 141:
                    return Color.FromArgb(39, 70, 45);
                case 143:
                    return Color.FromArgb(207, 226, 247);
                case 145:
                    return Color.FromArgb(121, 136, 161);
                case 146:
                    return Color.FromArgb(149, 142, 163);
                case 147:
                    return Color.FromArgb(147, 135, 104);
                case 148:
                    return Color.FromArgb(87, 88, 87);
                case 149:
                    return Color.FromArgb(22, 29, 50);
                case 150:
                    return Color.FromArgb(171, 173, 172);
                case 151:
                    return Color.FromArgb(120, 144, 130);
                case 153:
                    return Color.FromArgb(149, 121, 119);
                case 154:
                    return Color.FromArgb(123, 46, 47);
                case 157:
                    return Color.FromArgb(255, 246, 123);
                case 158:
                    return Color.FromArgb(225, 164, 194);
                case 168:
                    return Color.FromArgb(117, 108, 98);
                case 176:
                    return Color.FromArgb(151, 105, 91);
                case 178:
                    return Color.FromArgb(180, 132, 85);
                case 179:
                    return Color.FromArgb(137, 135, 136);
                case 180:
                    return Color.FromArgb(215, 169, 75);
                case 190:
                    return Color.FromArgb(249, 214, 46);
                case 191:
                    return Color.FromArgb(232, 171, 45);
                case 192:
                    return Color.FromArgb(105, 64, 40);
                case 193:
                    return Color.FromArgb(207, 96, 36);
                case 194:
                    return Color.FromArgb(163, 162, 165);
                case 195:
                    return Color.FromArgb(70, 103, 164);
                case 196:
                    return Color.FromArgb(35, 71, 139);
                case 198:
                    return Color.FromArgb(142, 66, 133);
                case 199:
                    return Color.FromArgb(99, 95, 98);
                case 200:
                    return Color.FromArgb(130, 138, 93);
                case 208:
                    return Color.FromArgb(229, 228, 223);
                case 209:
                    return Color.FromArgb(176, 142, 68);
                case 210:
                    return Color.FromArgb(112, 149, 120);
                case 211:
                    return Color.FromArgb(121, 181, 181);
                case 212:
                    return Color.FromArgb(159, 195, 233);
                case 213:
                    return Color.FromArgb(108, 129, 183);
                case 216:
                    return Color.FromArgb(144, 76, 42);
                case 217:
                    return Color.FromArgb(124, 92, 70);
                case 218:
                    return Color.FromArgb(150, 112, 159);
                case 219:
                    return Color.FromArgb(107, 96, 155);
                case 220:
                    return Color.FromArgb(167, 169, 206);
                case 221:
                    return Color.FromArgb(205, 98, 152);
                case 222:
                    return Color.FromArgb(228, 173, 200);
                case 223:
                    return Color.FromArgb(220, 144, 149);
                case 224:
                    return Color.FromArgb(240, 213, 160);
                case 225:
                    return Color.FromArgb(235, 184, 127);
                case 226:
                    return Color.FromArgb(253, 234, 141);
                case 232:
                    return Color.FromArgb(125, 187, 221);
                case 268:
                    return Color.FromArgb(52, 43, 117);
                case 301:
                    return Color.FromArgb(80, 109, 84);
                case 302:
                    return Color.FromArgb(91, 93, 105);
                case 303:
                    return Color.FromArgb(0, 16, 176);
                case 304:
                    return Color.FromArgb(44, 101, 29);
                case 305:
                    return Color.FromArgb(82, 124, 174);
                case 306:
                    return Color.FromArgb(51, 88, 130);
                case 307:
                    return Color.FromArgb(16, 42, 220);
                case 308:
                    return Color.FromArgb(61, 21, 133);
                case 309:
                    return Color.FromArgb(52, 142, 64);
                case 310:
                    return Color.FromArgb(91, 154, 76);
                case 311:
                    return Color.FromArgb(159, 161, 172);
                case 312:
                    return Color.FromArgb(89, 34, 89);
                case 313:
                    return Color.FromArgb(31, 128, 29);
                case 314:
                    return Color.FromArgb(159, 173, 192);
                case 315:
                    return Color.FromArgb(9, 137, 207);
                case 316:
                    return Color.FromArgb(123, 0, 123);
                case 317:
                    return Color.FromArgb(124, 156, 107);
                case 318:
                    return Color.FromArgb(138, 171, 133);
                case 319:
                    return Color.FromArgb(185, 196, 177);
                case 320:
                    return Color.FromArgb(202, 203, 209);
                case 321:
                    return Color.FromArgb(167, 94, 155);
                case 322:
                    return Color.FromArgb(123, 47, 123);
                case 323:
                    return Color.FromArgb(148, 190, 129);
                case 324:
                    return Color.FromArgb(168, 189, 153);
                case 325:
                    return Color.FromArgb(223, 223, 222);
                case 327:
                    return Color.FromArgb(151, 0, 0);
                case 328:
                    return Color.FromArgb(177, 229, 166);
                case 329:
                    return Color.FromArgb(152, 194, 219);
                case 330:
                    return Color.FromArgb(255, 152, 220);
                case 331:
                    return Color.FromArgb(255, 89, 89);
                case 332:
                    return Color.FromArgb(117, 0, 0);
                case 333:
                    return Color.FromArgb(239, 184, 56);
                case 334:
                    return Color.FromArgb(248, 217, 190);
                case 335:
                    return Color.FromArgb(231, 231, 236);
                case 336:
                    return Color.FromArgb(199, 212, 228);
                case 337:
                    return Color.FromArgb(255, 148, 148);
                case 338:
                    return Color.FromArgb(190, 104, 98);
                case 339:
                    return Color.FromArgb(86, 36, 36);
                case 340:
                    return Color.FromArgb(241, 231, 199);
                case 341:
                    return Color.FromArgb(254, 243, 187);
                case 342:
                    return Color.FromArgb(224, 178, 208);
                case 343:
                    return Color.FromArgb(212, 144, 189);
                case 344:
                    return Color.FromArgb(150, 85, 85);
                case 345:
                    return Color.FromArgb(143, 76, 42);
                case 346:
                    return Color.FromArgb(211, 190, 150);
                case 347:
                    return Color.FromArgb(226, 220, 188);
                case 348:
                    return Color.FromArgb(237, 234, 234);
                case 349:
                    return Color.FromArgb(233, 218, 218);
                case 350:
                    return Color.FromArgb(136, 62, 62);
                case 351:
                    return Color.FromArgb(188, 155, 93);
                case 352:
                    return Color.FromArgb(199, 172, 120);
                case 353:
                    return Color.FromArgb(202, 191, 163);
                case 354:
                    return Color.FromArgb(187, 179, 178);
                case 355:
                    return Color.FromArgb(108, 88, 75);
                case 356:
                    return Color.FromArgb(160, 132, 79);
                case 357:
                    return Color.FromArgb(149, 137, 136);
                case 358:
                    return Color.FromArgb(171, 168, 158);
                case 359:
                    return Color.FromArgb(175, 148, 131);
                case 360:
                    return Color.FromArgb(150, 103, 102);
                case 361:
                    return Color.FromArgb(86, 66, 54);
                case 362:
                    return Color.FromArgb(126, 103, 63);
                case 363:
                    return Color.FromArgb(105, 102, 92);
                case 364:
                    return Color.FromArgb(90, 76, 66);
                case 365:
                    return Color.FromArgb(106, 57, 9);
                case 1001:
                    return Color.FromArgb(248, 248, 248);
                case 1002:
                    return Color.FromArgb(205, 205, 205);
                case 1003:
                    return Color.FromArgb(17, 17, 17);
                case 1004:
                    return Color.FromArgb(255, 0, 0);
                case 1005:
                    return Color.FromArgb(255, 176, 0);
                case 1006:
                    return Color.FromArgb(180, 128, 255);
                case 1007:
                    return Color.FromArgb(163, 75, 75);
                case 1008:
                    return Color.FromArgb(193, 190, 66);
                case 1009:
                    return Color.FromArgb(255, 255, 0);
                case 1010:
                    return Color.FromArgb(0, 0, 255);
                case 1011:
                    return Color.FromArgb(0, 32, 96);
                case 1012:
                    return Color.FromArgb(33, 84, 185);
                case 1013:
                    return Color.FromArgb(4, 175, 236);
                case 1014:
                    return Color.FromArgb(170, 85, 0);
                case 1015:
                    return Color.FromArgb(170, 0, 170);
                case 1016:
                    return Color.FromArgb(255, 102, 204);
                case 1017:
                    return Color.FromArgb(255, 175, 0);
                case 1018:
                    return Color.FromArgb(18, 238, 212);
                case 1019:
                    return Color.FromArgb(0, 255, 255);
                case 1020:
                    return Color.FromArgb(0, 255, 0);
                case 1021:
                    return Color.FromArgb(50, 125, 21);
                case 1022:
                    return Color.FromArgb(127, 142, 100);
                case 1023:
                    return Color.FromArgb(140, 91, 159);
                case 1024:
                    return Color.FromArgb(175, 221, 255);
                case 1025:
                    return Color.FromArgb(255, 201, 201);
                case 1026:
                    return Color.FromArgb(177, 167, 255);
                case 1027:
                    return Color.FromArgb(159, 243, 233);
                case 1028:
                    return Color.FromArgb(204, 255, 204);
                case 1029:
                    return Color.FromArgb(255, 255, 204);
                case 1030:
                    return Color.FromArgb(255, 204, 153);
                case 1031:
                    return Color.FromArgb(98, 37, 209);
                case 1032:
                    return Color.FromArgb(255, 0, 191);
                default:
                    return Color.Empty;
            }
        }
        private string GenerateUUID()
        {
            lock (syncLock)
            {
                return Guid.NewGuid().ToString();
            }
        }

        public bool CheckForInternetConnection(int timeoutMs = 10000, string url = null)
        {
            try 
            {
                url ??= CultureInfo.InstalledUICulture switch
                {
                    { Name: var n } when n.StartsWith("fa") => //Iran
                        "http://www.aparat.com",
                    { Name: var n } when n.StartsWith("zh") => //China (is there even china users???)
                        "http://www.baidu.com",
                    _ =>
                        "http://www.gstatic.com/generate_204"
                };

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.KeepAlive = false;
                request.Timeout = timeoutMs;
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    Console.WriteLine("<INFO> Internet test successful!");
                    internetConnected = true;
                    return true;
                }
            }
            catch
            {
                internetConnected = false;
                Console.WriteLine("<INFO> Failed to check the internet, not checking updates...");
                return false;
            }
        }
        private void SetupJoinScript(string ipaddr, int port)
        {
            Console.WriteLine("<INFO> Setting up join script for " + Properties.Settings.Default.lastselectedversion);
            statusText.Invoke(new Action(() => { statusText.Text = "Setting up join script..."; }));
            string waitingForCharacterGuid = GenerateUUID().ToLower();
            string sessionId = GenerateUUID().ToLower();
            if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt")) File.Delete(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt");
            File.WriteAllText(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt", @"{""ClientPort"":0,""MachineAddress"":""" + ipaddr + @""",""ServerPort"":" + port.ToString() + @",""PingUrl"":"""",""PingInterval"":120,""UserName"":""" + Properties.Settings.Default.username + @""",""SeleniumTestMode"":false,""UserId"":" + Properties.Settings.Default.UserId + @",""SuperSafeChat"":false,""CharacterAppearance"":""http://assetgame.reblox.zip/Asset/CharacterFetch.ashx?userId=" + Properties.Settings.Default.UserId + @"&placeId=1"",""ClientTicket"":""" + DateTime.UtcNow.ToString("G") + @";h0eeFX/hZrNHXjP01PeaXT8dA8yVZbGKSMR6omd818fXJwuc/RceXUA8EJwdlfn7IWDfqjF2e22EhFyPXhucHqxQjY3GQd+zPAfS7KfQzItRVIFnjXbfWEGPKKFFEP4QcTs9Q141sd3G83ye9ZdGbOXPjy9VwpdvEnFToarYX7Q=;TCtJG0d2d0pFaHYnHDzJQttKfZlZyHZmcRtUNcy9vyivgiwQtB/illTbHvaUc/9w+oy8XRi+giLEvwuRmRttGKKnpA5Qt7dwCyXz2UIzt5/8TSJYqIKT99iPjBg0/PQFmguI7LoSk1KfElEDwzCWGT3tryAiT7S7a1SjInteSAU="",""GameId"":""00000000-0000-0000-0000-000000000000"",""PlaceId"":" + (ReserveAssetIdForMap ? placeid : 1) + @",""MeasurementUrl"":"""",""WaitingForCharacterGuid"":""" + waitingForCharacterGuid + @""",""BaseUrl"":""http://www.reblox.zip/"",""ChatStyle"":""" + Properties.Settings.Default.ChatStyle + @""",""VendorId"":0,""ScreenShotInfo"":"""",""VideoInfo"":""<?xml version=\""1.0\""?><entry xmlns=\""http://www.w3.org/2005/Atom\"" xmlns:media=\""http://search.yahoo.com/mrss/\"" xmlns:yt=\""http://gdata.youtube.com/schemas/2007\""><media:group><media:title type=\""plain\""><![CDATA[ROBLOX Place]]></media:title><media:description type=\""plain\""><![CDATA[ For more games visit http://www.roblox.com]]></media:description><media:category scheme=\""http://gdata.youtube.com/schemas/2007/categories.cat\"">Games</media:category><media:keywords>ROBLOX, video, free game, online virtual world</media:keywords></media:group></entry>"",""CreatorId"":1,""CreatorTypeEnum"":""User"",""MembershipType"":""" + Properties.Settings.Default.Membership.Replace(" ", "") + @""",""AccountAge"":365,""CookieStoreFirstTimePlayKey"":""rbx_evt_ftp"",""CookieStoreFiveMinutePlayKey"":""rbx_evt_fmp"",""CookieStoreEnabled"":true,""IsRobloxPlace"":true,""GenerateTeleportJoin"":false,""IsUnknownOrUnder13"":" + (!Properties.Settings.Default.AccountOver13).ToString().ToLower() + @",""SessionId"":""" + sessionId + @"|00000000-0000-0000-0000-000000000000|0|204.236.226.210|8|" + DateTime.UtcNow.ToString("")+ @"Z|0|null|null|null|null"",""DataCenterId"":0,""UniverseId"":2,""BrowserTrackerId"":0,""UsePortraitMode"":false,""FollowUserId"":0,""characterAppearanceId"":0}");
        }

        private void SetupGameFiles(bool studio = false)
        {
            statusText.Invoke(new Action(() => { statusText.Text = "Setting up game files..."; }));
            Console.WriteLine("<INFO> Checking for FFlags for " + Properties.Settings.Default.lastselectedversion);
            if (studio == true) 
            {
                if (Directory.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings"))
                {
                    Console.WriteLine("<INFO> Copying the ClientAppSettings.json to the RobloxAssetFixer");
                    if (File.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json"))
                    {
                        if (File.ReadAllText(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json").Contains("{id}")) 
                        {
                            File.Move(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json", datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json.bak");
                            File.WriteAllText(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json", File.ReadAllText(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json.bak").Replace("{id}", Properties.Settings.Default.UserId.ToString()));
                        }
                        if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\ClientAppSettings.json")) File.Delete(datafolder + @"\tools\RobloxAssetFixer\ClientAppSettings.json");
                        File.WriteAllText(datafolder + @"\tools\RobloxAssetFixer\ClientAppSettings.json", File.ReadAllText(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json").Replace("{id}", Properties.Settings.Default.UserId.ToString()));
                    }
                }
            }
            else 
            {
                if (Directory.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Player\ClientSettings"))
                {
                    Console.WriteLine("<INFO> Copying the ClientAppSettings.json to the RobloxAssetFixer");

                    if (File.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Player\ClientSettings\ClientAppSettings.json"))
                    {
                        if (File.ReadAllText(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Player\ClientSettings\ClientAppSettings.json").Contains("{id}"))
                        {
                            File.Move(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Player\ClientSettings\ClientAppSettings.json", datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json.bak");
                            File.WriteAllText(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Player\ClientSettings\ClientAppSettings.json", File.ReadAllText(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json.bak").Replace("{id}", Properties.Settings.Default.UserId.ToString()));
                        }
                        if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\ClientAppSettings.json")) File.Delete(datafolder + @"\tools\RobloxAssetFixer\ClientAppSettings.json");
                        File.WriteAllText(datafolder + @"\tools\RobloxAssetFixer\ClientAppSettings.json", File.ReadAllText(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Player\ClientSettings\ClientAppSettings.json").Replace("{id}", Properties.Settings.Default.UserId.ToString()));
                    }
                }
                else if (Directory.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings"))
                {
                    Console.WriteLine("<INFO> Copying the ClientAppSettings.json to the RobloxAssetFixer");
                    if (File.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json"))
                    {
                        if (File.ReadAllText(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json").Contains("{id}"))
                        {
                            File.Move(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json", datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json.bak");
                            File.WriteAllText(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json", File.ReadAllText(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json.bak").Replace("{id}", Properties.Settings.Default.UserId.ToString()));
                        }
                        if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\ClientAppSettings.json")) File.Delete(datafolder + @"\tools\RobloxAssetFixer\ClientAppSettings.json");
                        File.WriteAllText(datafolder + @"\tools\RobloxAssetFixer\ClientAppSettings.json", File.ReadAllText(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json").Replace("{id}", Properties.Settings.Default.UserId.ToString()));
                    }
                }
            } 
            if (UseJoinJSONLink == true)
            {
                Console.WriteLine("<INFO> Checking for the game folder for " + Properties.Settings.Default.lastselectedversion);
                if (Directory.Exists(datafolder + @"\tools\RobloxAssetFixer\game") == false && Directory.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\game"))
                {
                    Console.WriteLine("<INFO> Creating the game folder and copying its content to the folder for " + Properties.Settings.Default.lastselectedversion);
                    Directory.CreateDirectory(datafolder + @"\tools\RobloxAssetFixer\game");
                    foreach (string file in Directory.GetFiles(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\game"))
                    {
                        if (Path.GetFileName(file) == "join.ashx") 
                        {
                            if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file))) File.Delete(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file));
                           File.WriteAllText(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file), File.ReadAllText(file).Replace("{ip}", textBox1.Text).Replace("{port}", textBox2.Text).Replace("{username}", Properties.Settings.Default.username).Replace("{id}", Properties.Settings.Default.UserId.ToString()).Replace("{13}",(!Properties.Settings.Default.AccountOver13).ToString().ToLower()).Replace("{membership}",Properties.Settings.Default.Membership.Replace(" ", "")).Replace("{chatstyle}", Properties.Settings.Default.ChatStyle));
                        }
                        else if (Path.GetFileName(file) == "gameserver.ashx")
                        {
                            if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file))) File.Delete(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file));
                            File.WriteAllText(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file), File.ReadAllText(file).Replace("{ip}", textBox1.Text).Replace("{port}", textBox2.Text).Replace("{username}", Properties.Settings.Default.username).Replace("{id}", Properties.Settings.Default.UserId.ToString()).Replace("{13}", (!Properties.Settings.Default.AccountOver13).ToString().ToLower()));
                        }
                        else if (Path.GetFileName(file) == "placespecificscript.ashx")
                        {
                            if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file))) File.Delete(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file));
                            File.WriteAllText(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file), File.ReadAllText(file).Replace("{id}", (ReserveAssetIdForMap ? placeid : 1).ToString()));
                        }
                        else if (Path.GetFileName(file) == "visit.ashx")
                        {
                            if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file))) File.Delete(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file));
                            File.WriteAllText(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file), File.ReadAllText(file).Replace("{id}", Properties.Settings.Default.UserId.ToString()).Replace("{membership}",Properties.Settings.Default.Membership.Replace(" ", "")));

                        }
                        else
                        {
                            File.Copy(file, datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file));
                        }
                    }
                }
            }
        }

        private void RemoveGameFiles()
        {
           try
            {
                statusText.Invoke(new Action(() => { statusText.Text = "Cleaning up files..."; }));
                Console.WriteLine("<INFO> Cleaning up game files and removing the ClientAppSettings file");
                if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\ClientAppSettings.json")) File.Delete(datafolder + @"\tools\RobloxAssetFixer\ClientAppSettings.json");
                if (File.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json.bak"))
                {
                    File.Delete(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json");
                    File.Move(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json.bak", datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json");
                }
                if (File.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Player\ClientSettings\ClientAppSettings.json.bak"))
                {
                    File.Delete(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Player\ClientSettings\ClientAppSettings.json");
                    File.Move(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Player\ClientSettings\ClientAppSettings.json.bak", datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Player\ClientSettings\ClientAppSettings.json");
                }
                if (Directory.Exists(datafolder + @"\tools\RobloxAssetFixer\clothes") == true) Directory.Delete(datafolder + @"\tools\RobloxAssetFixer\clothes", true);

                if (UseJoinJSONLink == true)
                {
                    if (Directory.Exists(datafolder + @"\tools\RobloxAssetFixer\game") == true) Directory.Delete(datafolder + @"\tools\RobloxAssetFixer\game", true);
                    if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt")) File.Delete(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt");
                }
            }
            catch
            {
                //do nothing
            }
        }

        private static void ExportPrivateKey(RSACryptoServiceProvider csp, TextWriter outputStream)
        {
            if (csp.PublicOnly) throw new ArgumentException("CSP does not contain a private key", "csp");
            var parameters = csp.ExportParameters(true);
            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream);
                writer.Write((byte)0x30); // SEQUENCE
                using (var innerStream = new MemoryStream())
                {
                    var innerWriter = new BinaryWriter(innerStream);
                    EncodeIntegerBigEndian(innerWriter, new byte[] { 0x00 }); // Version
                    EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent);
                    EncodeIntegerBigEndian(innerWriter, parameters.D);
                    EncodeIntegerBigEndian(innerWriter, parameters.P);
                    EncodeIntegerBigEndian(innerWriter, parameters.Q);
                    EncodeIntegerBigEndian(innerWriter, parameters.DP);
                    EncodeIntegerBigEndian(innerWriter, parameters.DQ);
                    EncodeIntegerBigEndian(innerWriter, parameters.InverseQ);
                    var length = (int)innerStream.Length;
                    EncodeLength(writer, length);
                    writer.Write(innerStream.GetBuffer(), 0, length);
                }

                var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
                outputStream.WriteLine("-----BEGIN RSA PRIVATE KEY-----");
                // Output as Base64 with lines chopped at 64 characters
                for (var i = 0; i < base64.Length; i += 64)
                {
                    outputStream.WriteLine(base64, i, Math.Min(64, base64.Length - i));
                }
                outputStream.WriteLine("-----END RSA PRIVATE KEY-----");
            }
        }

        private static void EncodeLength(BinaryWriter stream, int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException("length", "Length must be non-negative");
            if (length < 0x80)
            {
                // Short form
                stream.Write((byte)length);
            }
            else
            {
                // Long form
                var temp = length;
                var bytesRequired = 0;
                while (temp > 0)
                {
                    temp >>= 8;
                    bytesRequired++;
                }
                stream.Write((byte)(bytesRequired | 0x80));
                for (var i = bytesRequired - 1; i >= 0; i--)
                {
                    stream.Write((byte)(length >> (8 * i) & 0xff));
                }
            }
        }

        private static void EncodeIntegerBigEndian(BinaryWriter stream, byte[] value, bool forceUnsigned = true)
        {
            stream.Write((byte)0x02); // INTEGER
            var prefixZeros = 0;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] != 0) break;
                prefixZeros++;
            }
            if (value.Length - prefixZeros == 0)
            {
                EncodeLength(stream, 1);
                stream.Write((byte)0);
            }
            else
            {
                if (forceUnsigned && value[prefixZeros] > 0x7f)
                {
                    // Add a prefix zero to force unsigned if the MSB is 1
                    EncodeLength(stream, value.Length - prefixZeros + 1);
                    stream.Write((byte)0);
                }
                else
                {
                    EncodeLength(stream, value.Length - prefixZeros);
                }
                for (var i = prefixZeros; i < value.Length; i++)
                {
                    stream.Write(value[i]);
                }
            }
        }

#nullable enable
        public static bool ValidateUrlWithUrlCreate(string url, out Uri? uri)
        {
            var success = Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri);

            return success;
        }
#nullable disable

        private void Form1_Load(object sender, EventArgs e)
        {
                label2.Text = "v" + Properties.Settings.Default.version;
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {

                if (args[i] == "-datafolder")
                {
                    if (Directory.Exists(args[i + 1]))
                    {
                        datafolder = args[i + 1];
                        i++;
                    }
                    else
                    {
                        MessageBox.Show("This is an invalid data folder! Please try a different data folder or rename it without spaces!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (args[i] == "--savekeys")
                {
                    using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                    {
                        byte[] publicdata = RSA.ExportCspBlob(false);
                        byte[] privatedata = RSA.ExportCspBlob(true);
                        File.WriteAllText(datafolder + @"\public.txt", Convert.ToBase64String(publicdata));
                        File.WriteAllText(datafolder + @"\private.txt", Convert.ToBase64String(privatedata));
                        using (TextWriter w = File.CreateText(datafolder + @"\private.pem"))
                        {
                            ExportPrivateKey(RSA, w);
                        }
                        MessageBox.Show("BLOB created", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Question);
                    }
                }
                else if (args[i] == "-launch")
                {
                    if (Directory.Exists(datafolder + @"\clients\" + args[i + 1]))
                    {
                        launchershortcut = true;
                        new LaunchScreen(args[i + 1], datafolder).Show();
                        i++;
                    }
                    else
                    {
                        MessageBox.Show("This is an invalid version!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (args[i] == "-updateurl") 
                {
                    if (ValidateUrlWithUrlCreate(args[i + 1], out _))
                    {
                        updateurl = args[i + 1];
                    }
                    else
                    {
                        Console.WriteLine("<WARN> The URL for the update server is not valid! Reverting to the default...");
                    }
                }
                }
                if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\ca.pem"))
                {
                    X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    X509Certificate2 x509 = new X509Certificate2(X509Certificate2.CreateFromCertFile(Path.GetDirectoryName(Application.ExecutablePath) + @"\ca.pem"));
                    var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, x509.Thumbprint, false);
                    if (certificates != null && certificates.Count > 0 || Properties.Settings.Default.CADontShow)
                    {
                        Console.WriteLine("<INFO> The CA cert that is checked is installed on the computer");
                        x509.Dispose();
                        caInstalled = true;
                    }
                    else
                    {
                        var result = MessageBox.Show("Wanna install the CA certificate of ReBlox? This is not required, but this can make connection secure and be able to download assets/decals from the browser via domain name. Pressing Cancel will not show this ever again, however you can still install it in settings at anytime.", "CA Certificate", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                        if (result == DialogResult.Yes)
                        {
                            if (IsAdministrator())
                            {
                                Console.WriteLine("<INFO> Installing the CA Certificate");
                                store.Close();
                                store.Open(OpenFlags.ReadWrite);
                                store.Add(x509);
                                store.Close();
                                x509.Dispose();
                                caInstalled = true;
                            }
                            else
                            {
                                ProcessStartInfo ps = new ProcessStartInfo();
                                ps.UseShellExecute = true;
                                ps.FileName = Application.ExecutablePath;
                                ps.Verb = "runas";
                                Process.Start(ps);
                                Application.Exit();
                            }
                        }
                        else if (result == DialogResult.Cancel)
                        {
                            Properties.Settings.Default.CADontShow = true;
                            Properties.Settings.Default.Save();
                        }
                    }
                }
            Thread thread = new Thread(() =>
            {
                if (CheckForInternetConnection())
                {
                    try
                    {
                        WebClient client = new WebClient();
                        Console.WriteLine("<INFO> Checking the version of the client and the server reports...");
                        if (client.DownloadString(updateurl + @"/version.txt") != Properties.Settings.Default.version)
                        {
                            label33.Invoke(new Action(() => { label33.Visible = true; }));
                            button6.Invoke(new Action(() => { button6.Visible = true; }));
                            Console.WriteLine("<INFO> Update is available for the ReBlox Launcher!");
                        }
                        else
                        {
                            Console.WriteLine("<INFO> No update is available.");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("<ERROR> Something when wrong while trying to check for updates! Look in the error for details!\r\n" + e);
                    }
                }
            });
            thread.TrySetApartmentState(ApartmentState.STA);
            thread.Start();
            Initialize();
        }

        public void SearchNetworks()
        {
            UdpClient clientUdpClient = ServerUtils.GetClient(0);
            IPEndPoint ServerEp = new IPEndPoint(IPAddress.Any, 0);
            byte[] requestData = Encoding.UTF8.GetBytes("ping");
            byte[] ServerResponseData = null;

            clientUdpClient.Client.ReceiveTimeout = 10000;
            clientUdpClient.Client.SendTimeout = 10000;

            clientUdpClient.Send(requestData, requestData.Length, new IPEndPoint(IPAddress.Parse("231.100.2.3"), 50358));
            label35.Invoke(new Action(() => { label35.Visible = true; }));
            try
            {
                while (searchingNetwork == true)
                {
                    ServerResponseData = clientUdpClient.Receive(ref ServerEp);
                    string ServerResponse;
                    if (ServerResponseData != null)
                    {
                        ServerResponse = Encoding.UTF8.GetString(ServerResponseData);

                        string[] responsesplit = ServerResponse.Split(new char[] { '|' }, 7);

                        Console.WriteLine("<INFO> Found server: " + ServerEp.Address.ToString());
                            ListViewItem item = null;
                        listView1.Invoke(new Action(() => { item = listView1.Items.Add(responsesplit[6]); }));
                            item.SubItems.Add(responsesplit[1]);
                            item.SubItems.Add(responsesplit[2]);
                            item.SubItems.Add(responsesplit[3]);
                            item.SubItems.Add(responsesplit[4]);
                            item.SubItems.Add(ServerEp.Address.ToString());

                    }

                }
            }
            catch (SocketException)
            {
                label35.Invoke(new Action(() => { label35.Visible = false; }));
                searchingNetwork = false;
            }
            catch (Exception e)
            {
                Console.WriteLine("<ERROR> Something went wrong while looking for servers! " + e);
            }
            

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1) 
            {
                isJoiningOrStudio = true;
                Thread thread = new Thread(async () =>
                {
                    try
                    {
                        if (File.ReadAllText(@"C:\Windows\System32\drivers\etc\hosts").Contains("\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip") == false && Properties.Settings.Default.UsePatchInStudio == true && WineDetector.IsRunningOnWine() == false)
                        {
                            if (MessageBox.Show("Wanna edit the hosts file to make sure that decals loads? This is recommended for best experience!\n\nIf you wanna load assets, provide your .ROBLOSECURITY in the settings section and enable Use auth for loading assets. (This will not conflict with your latest Roblox Studio)", "hosts File Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                if (IsAdministrator())
                                {
                                    button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                    button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                    button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                    
                                    File.AppendAllText(@"C:\Windows\System32\drivers\etc\hosts", "\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip");
                                    statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                    if (Properties.Settings.Default.UsePatchInStudio)
                                    {
                                        if (UseJoinJSONLink) SetupJoinScript("127.0.0.1", 53640);
                                        SetupGameFiles(true);
                                        LoadAssets();
                                    }
                                    ProcessStartInfo ps = new ProcessStartInfo();
                                    ps.UseShellExecute = false;
                                    ps.CreateNoWindow = false;
                                    listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Studio\RobloxStudioBeta.exe"; }));
                                    ProcessStartInfo ps1 = new ProcessStartInfo();
                                    ps1.UseShellExecute = false;
                                    ps1.FileName = datafolder + @"\tools\node\node.exe";
                                    ps1.RedirectStandardOutput = true;
                                    ps1.CreateNoWindow = true;
                                    if (Properties.Settings.Default.useAuth && internetConnected)
                                    {
                                        if (Properties.Settings.Default.avatarR15)
                                        {
                                            if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        }
                                        else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                    }
                                    else
                                    {
                                        if (Properties.Settings.Default.avatarR15)
                                        {
                                            if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        }
                                        else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                    }
                                    ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                    ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                    if (textBox5.Text.Length > 2 && textBox4.Text.Length > 0)
                                    {
                                        if (IsNodeFromAppRunning() == false)
                                        {
                                            Process test = Process.Start(ps1);
                                            statusText.Invoke(new Action(() => { statusText.Text = "Waiting for node server to start..."; }));
                                            button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                            button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                            button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                            test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                            {
                                                if (e1.Data != null)
                                                {
                                                    if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                    {
                                                        setupAvatarOnServer();
                                                        statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                        Process roblox = Process.Start(ps);
                                                        roblox.EnableRaisingEvents = true;
                                                        roblox.Exited += Roblox_Exited;
                                                        test.CancelOutputRead();
                                                        await Task.Delay(3000);
                                                        statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                        statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                    }
                                                }
                                            });
                                            test.BeginOutputReadLine();
                                        }
                                        else
                                        {
                                            setupAvatarOnServer();
                                            button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                            button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                            button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                            statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                            Process roblox = Process.Start(ps);
                                            roblox.EnableRaisingEvents = true;
                                            roblox.Exited += Roblox_Exited;
                                            await Task.Delay(3000);
                                            statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                            statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                        }

                                    }
                                    else
                                    {
                                        MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                                else
                                {
                                    ProcessStartInfo ps = new ProcessStartInfo();
                                    ps.UseShellExecute = true;
                                    ps.FileName = Application.ExecutablePath;
                                    ps.Verb = "runas";
                                    Process.Start(ps);
                                    Application.Exit();
                                }
                            }
                            else
                            {
                                statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                ProcessStartInfo ps = new ProcessStartInfo();
                                listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Studio\RobloxStudioBeta.exe"; }));
                                Process.Start(ps);
                                await Task.Delay(3000);
                                statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                            }
                        }
                        else
                        {
                            statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                            button1.Invoke(new Action(() => { button1.Enabled = false; }));
                            button2.Invoke(new Action(() => { button2.Enabled = false; }));
                            button3.Invoke(new Action(() => { button3.Enabled = false; }));
                            if (Properties.Settings.Default.UsePatchInStudio)
                            {
                                if (UseJoinJSONLink) SetupJoinScript("127.0.0.1", 53640);
                                SetupGameFiles(true);
                                LoadAssets();
                            }
                            ProcessStartInfo ps = new ProcessStartInfo();
                            ps.UseShellExecute = false;
                            ps.CreateNoWindow = false;
                            listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Studio\RobloxStudioBeta.exe"; }));
                            ProcessStartInfo ps1 = new ProcessStartInfo();
                            ps1.UseShellExecute = false;
                            ps1.FileName = datafolder + @"\tools\node\node.exe";
                            ps1.RedirectStandardOutput = true;
                            ps1.CreateNoWindow = true;

                            if (Properties.Settings.Default.useAuth && internetConnected)
                            {
                                if (Properties.Settings.Default.avatarR15)
                                {
                                    if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                }
                                else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                            }
                            else
                            {
                                if (Properties.Settings.Default.avatarR15)
                                {
                                    if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                }
                                else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                            }
                            ps1.WindowStyle = ProcessWindowStyle.Hidden;
                            ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";

                            if (textBox5.Text.Length > 2 && textBox4.Text.Length > 0)
                            {

                                if (Properties.Settings.Default.UsePatchInStudio)
                                {
                                    if (IsNodeFromAppRunning() == false)
                                    {
                                        Process test = Process.Start(ps1);
                                        button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                        button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                        button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                        statusText.Invoke(new Action(() => { statusText.Text = "Waiting for node server to start..."; }));
                                        test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                        {
                                            if (e1.Data != null)
                                            {
                                                if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                {
                                                    setupAvatarOnServer();
                                                    statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                    Process roblox = Process.Start(ps);
                                                    roblox.EnableRaisingEvents = true;
                                                    roblox.Exited += Roblox_Exited;
                                                    test.CancelOutputRead();
                                                    await Task.Delay(3000);
                                                    statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                    statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                }
                                            }
                                        });
                                        test.BeginOutputReadLine();
                                    }
                                    else
                                    {
                                        setupAvatarOnServer();
                                        statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                        button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                        button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                        button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                        Process roblox = Process.Start(ps);
                                        roblox.EnableRaisingEvents = true;
                                        roblox.Exited += Roblox_Exited;
                                        await Task.Delay(3000);
                                        statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                        statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                    }
                                }
                                else
                                {
                                    button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                    button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                    button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                    statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                    Process roblox = Process.Start(ps);
                                    roblox.EnableRaisingEvents = true;
                                    roblox.Exited += Roblox_Exited;
                                    await Task.Delay(3000);
                                    statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                    statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                }

                            }
                            else
                            {
                                MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }

                        }
                    }
                    catch (Exception e1)
                    {
                        MessageBox.Show("Something went wrong while trying to launch Studio, please look in the error message for details: " + e1.Message, "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Console.WriteLine("<ERROR> Something went wrong when attempting to launch Studio! Please look in the error below and report it to the developer if it's launcher-sided!\n" + e1.Message + "\nStack Trace:\n" + e1.StackTrace);
                        await Task.Delay(3000);
                        statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                        statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                    }
                });
                thread.TrySetApartmentState(ApartmentState.MTA);
                thread.Start();
            }
            else
            {
                MessageBox.Show("A client must be selected to be able to run!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void Roblox_Exited(object sender, EventArgs e)
        {
            Process[] processesr = Process.GetProcessesByName("RobloxStudioBeta");
            Process[] processesr1 = Process.GetProcessesByName("RobloxPlayerBeta");
            if (processesr.Length <= 0 && processesr1.Length <= 0) 
            {
                button1.Invoke(new Action(() => { button1.Enabled = false; }));
                button2.Invoke(new Action(() => { button2.Enabled = false; }));
                button3.Invoke(new Action(() => { button3.Enabled = false; }));
                statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                Process[] processes = Process.GetProcessesByName("node");
                if (processes.Length > 0)
                {
                    foreach (Process process in processes)
                    {
                        if (process.MainModule.FileName == datafolder + @"\tools\node\node.exe")
                        {
                            Console.WriteLine("<INFO> Shutting down the node server");
                            statusText.Invoke(new Action(() => { statusText.Text = "Closing node server..."; }));
                            process.Kill();
                        }
                    }
                }
                processes = Process.GetProcessesByName("IPForwarder");
                if (processes.Length > 0)
                {
                    foreach (Process process in processes)
                    {
                        Console.WriteLine("<INFO> Shutting down IPForwarder");
                        statusText.Invoke(new Action(() => { statusText.Text = "Closing IPForwarder..."; }));
                        process.Kill();
                    }
                }

                if (Properties.Settings.Default.ClearTemp)
                {
                    Console.WriteLine("<INFO> Clearing ROBLOX's temporary files");
                    statusText.Invoke(new Action(() => { statusText.Text = "Clearing temp files..."; }));
                    if (Directory.Exists(Path.GetTempPath() + "Roblox")) Directory.Delete(Path.GetTempPath() + "Roblox", true);
                }
                RemoveGameFiles();
                ClearAssets();
                ServerUtils.StopListServer();
                button1.Invoke(new Action(() => { button1.Enabled = true; }));
                button2.Invoke(new Action(() => { button2.Enabled = true; }));
                button3.Invoke(new Action(() => { button3.Enabled = true; }));
                statusText.Invoke(new Action(() => { statusText.Visible = false; }));
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (listBox1.SelectedIndex > -1)
                {
                    label5.Text = listBox1.GetItemText(listBox1.SelectedItem);
                    Properties.Settings.Default.lastselectedversion = listBox1.GetItemText(listBox1.SelectedItem);
                    Properties.Settings.Default.Save();
                    if (File.Exists(datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\ReBlox.ini"))
                    {
                        string[] config = File.ReadAllLines(datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\ReBlox.ini");
                        if (config[0] == "[Reblox]")
                        {
                            placeid = -1;
                            UseJoinJSONLink = false;
                            useIPForwarder = false;
                            ReserveAssetIdForMap = false;
                            label24.Visible = false;
                            useNewRoblox = false;
                            useOldSignature = false;
                            useOldAssetFormat = false;
                            dontLoadMapFromArgument = false;
                            for (int i = 0; i < config.Length; i++)
                            {
                                if (config[i].Trim().StartsWith("Description=\""))
                                {
                                    string[] splited = config[i].Trim().Split(new char[] { '=' }, 2);
                                    label6.Text = splited[1].Remove(0, 1).Remove(splited[1].Length - 2, 1);
                                }
                                else if (config[i].Trim().StartsWith("JoinArgument=\""))
                                {
                                    string[] splited = config[i].Trim().Split(new char[] { '=' }, 2);
                                    joinargument = splited[1].Remove(0, 1).Remove(splited[1].Length - 2, 1);
                                }
                                else if (config[i].Trim().StartsWith("HostArgument=\""))
                                {
                                    string[] splited = config[i].Trim().Split(new char[] { '=' }, 2);
                                    hostargument = splited[1].Remove(0, 1).Remove(splited[1].Length - 2, 1);
                                }
                                else if (config[i].Trim().StartsWith("Version="))
                                {
                                    string[] splited = config[i].Trim().Split(new char[] { '=' }, 2);
                                    if (splited[1].Trim() != "")
                                    {
                                        label24.Visible = true;
                                        label24.Text = splited[1];
                                    }
                                    else
                                    {
                                        label24.Visible = false;
                                    }
                                }
                                else if (config[i].Trim() == "UseJoinScript=true")
                                {
                                    UseJoinJSONLink = true;
                                    label27.Visible = true;
                                    comboBox2.Visible = true;
                                    label32.Visible = true;
                                    comboBox3.Visible = true;
                                }
                                else if (config[i].Trim() == "UseJoinScript=false")
                                {
                                    UseJoinJSONLink = false;
                                    label27.Visible = false;
                                    comboBox2.Visible = false;
                                    label32.Visible = false;
                                    comboBox3.Visible = false;
                                }
                                else if (config[i].Trim() == "2018OverMessage=true")
                                {
                                    if (Properties.Settings.Default.UsePatchInStudio == false)
                                    {
                                        MessageBox.Show("Note that RobloxAssetFixer is strongly recommended to be able to use this client property and login (which you just put your username in and your random letter in password.) You should turn on \"Use RobloxAssetFixer when opening in Studio mode\", located in Settings [If you're launching in Studio]", "You should turn this on...", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    }
                                }
                                else if (config[i].Trim() == "UseIPForwarder=true")
                                {
                                    useIPForwarder = true;
                                }
                                else if (config[i].Trim() == "UseIPForwarder=false")
                                {
                                    useIPForwarder = false;
                                }
                                else if (config[i].Trim().StartsWith("PlaceId="))
                                {
                                    if (int.TryParse(config[i].Trim().Replace("PlaceId=", ""), out _) == true)
                                    {
                                        placeid = int.Parse(config[i].Trim().Replace("PlaceId=", ""));
                                    }
                                }
                                else if (config[i].Trim() == "UsePlaceAsAsset=true")
                                {
                                    if (placeid > -1)
                                    {
                                        ReserveAssetIdForMap = true;
                                    }
                                    else
                                    {
                                        MessageBox.Show("PlaceId is required for UsePlaceAsAsset", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                                else if (config[i].Trim() == "UsePlaceAsAsset=false")
                                {
                                    ReserveAssetIdForMap = false;
                                    placeid = -1;
                                }
                                else if (config[i].Trim() == "UseNewRobloxName=true")
                                {
                                    useNewRoblox = true;
                                }
                                else if (config[i].Trim() == "UseOldSignature=true") 
                                {
                                    useOldSignature = true;
                                }
                                else if (config[i].Trim() == "UseOldAssetFormat=true")
                                {
                                    useOldAssetFormat = true;
                                }
                                else if (config[i].Trim() == "DontLoadMapFromArgument=true")
                                {
                                    dontLoadMapFromArgument = true;
                                }
                            }
                            string[] images = Directory.GetFiles(datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\images");
                            if (images.Length > 0)
                            {
                                int randomchoose = RandomNumber(0, images.Length);
                                pictureBox1.ImageLocation = images[randomchoose];
                                pictureBox1.Image = Image.FromFile(images[randomchoose]);
                                randomchoose = 0;
                            }
                            timer1.Stop();
                            timer1.Start();
                            images = null;
                        }
                        else
                        {
                            MessageBox.Show("Config file failed to load!", "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                    }
                }
            } catch
            {
                MessageBox.Show("Something went wrong while trying to get the client or the client you are trying to pick doesn't exist.", "Reblox Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (File.ReadAllText(@"C:\Windows\System32\drivers\etc\hosts").Contains("\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip"))
            {
                if (IsAdministrator())
                {
                    string content = File.ReadAllText(@"C:\Windows\System32\drivers\etc\hosts");
                    File.Delete(@"C:\Windows\System32\drivers\etc\hosts");
                    File.WriteAllText(@"C:\Windows\System32\drivers\etc\hosts", content.Replace("\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip",""));
                    content = null;
                }
                else
                {
                    ProcessStartInfo ps = new ProcessStartInfo();
                    ps.UseShellExecute = true;
                    ps.FileName = Application.ExecutablePath;
                    ps.Verb = "runas";
                    Process.Start(ps);
                    Application.Exit();
                }
            }
            else
            {
                MessageBox.Show("It looks like either you reverted it before or never edited it!", "hosts File", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            listBox1.Visible = false;
            panel1.Visible = false;
            panel2.Visible = true;
            listBox2.Visible = false;
            button9.Visible = false;
            button10.Visible = false;
            panel3.Visible = false;
            panel5.Visible = false;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            listBox1.Visible = true;
            panel1.Visible = true;
            panel2.Visible = false;
            listBox2.Visible = false;
            button9.Visible = false;
            button10.Visible = false;
            panel3.Visible = false;
            panel5.Visible = false;
        }

        public class AssetData
        {
            public int id { get; set; } = 0;
        }
        public class BodyColors
        {
            public int headColor { get; set; } = 194;
            public int leftArmColor { get; set; } = 194;
            public int leftLegColor { get; set; } = 194;
            public int rightArmColor { get; set; } = 194;
            public int rightLegColor { get; set; } = 194;
            public int torsoColor { get; set; } = 194;
        }
        public class AvatarType
        {
            public string bodyType { get; set; } = "R6";
            public IList<AssetData> asset { get; set; }
            public BodyColors colors { get; set; }
        }

        private bool IsNodeFromAppRunning()
        {
            bool isRunning = false;
            try
            {
                if (WineDetector.IsRunningOnWine() == false)
                {
                    Process[] processes = Process.GetProcessesByName("node");
                    foreach (Process process in processes)
                    {
                        if (process.MainModule.FileName == datafolder + @"\tools\node\node.exe")
                        {
                            isRunning = true; break;
                        }
                    }
                }
                else
                {
                    isRunning = true;
                }
            }
            catch
            {
                return false; 
            }
            return isRunning;
        }
        private async void setupAvatarOnServer()
        {
            if (IsNodeFromAppRunning() && starting == false)
            {
                var jsondata = new AvatarType { };
                if (Properties.Settings.Default.ClothesArray.Length > 0)
                {
                    List<AssetData> data = new List<AssetData>();
                    foreach (var assetid in Properties.Settings.Default.ClothesArray.Split('|'))
                    {
                        data.Add(new AssetData { id = int.Parse(assetid) });
                    }
                    jsondata = new AvatarType
                    {
                        bodyType = (Properties.Settings.Default.avatarR15 ? "R15" : "R6"),
                        asset = data,
                        colors = new BodyColors { headColor = Properties.Settings.Default.HeadColor, leftArmColor = Properties.Settings.Default.LeftArmColor, leftLegColor = Properties.Settings.Default.LeftLegColor, rightArmColor = Properties.Settings.Default.RightArmColor, rightLegColor = Properties.Settings.Default.RightLegColor, torsoColor = Properties.Settings.Default.TorsoColor }
                    };
                }
                else
                {
                    jsondata = new AvatarType
                    {
                        bodyType = (Properties.Settings.Default.avatarR15 ? "R15" : "R6"),
                        asset = { },
                        colors = new BodyColors { headColor = Properties.Settings.Default.HeadColor, leftArmColor = Properties.Settings.Default.LeftArmColor, leftLegColor = Properties.Settings.Default.LeftLegColor, rightArmColor = Properties.Settings.Default.RightArmColor, rightLegColor = Properties.Settings.Default.RightLegColor, torsoColor = Properties.Settings.Default.TorsoColor }
                    };
                }

                try
                {
                    HttpClient client = new HttpClient();

                    client.BaseAddress = new Uri("http://reblox.zip");

                    string result = JsonConvert.SerializeObject(jsondata);
                    var buffer = Encoding.UTF8.GetBytes(result);
                    var byteContent = new ByteArrayContent(buffer);
                    byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    HttpResponseMessage response = await client.PostAsync("/v1/avatar/set-avatar?userId=" + Properties.Settings.Default.UserId, byteContent);

                    response.Dispose();

                    client.Dispose();

                    result = "";
                } catch
                {
                    // do nothing
                }
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1) 
            {
                isJoiningOrStudio = true;
                Thread thread = new Thread(async () =>
                {
                    try
                    {
                        if (Directory.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Player"))
                        {
                            if (File.ReadAllText(@"C:\Windows\System32\drivers\etc\hosts").Contains("\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip") == false && WineDetector.IsRunningOnWine() == false)
                            {
                                if (MessageBox.Show("Wanna edit the hosts file to make sure that decals loads? This is recommended for best experience!\n\nIf you wanna load assets, provide your .ROBLOSECURITY in the settings section and enable Use auth for loading assets. (This will not conflict with your latest Roblox Studio)", "hosts File Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                {
                                    if (IsAdministrator())
                                    {


                                        File.AppendAllText(@"C:\Windows\System32\drivers\etc\hosts", "\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip");
                                        statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                        button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                        button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                        button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                        if (UseJoinJSONLink) SetupJoinScript(textBox1.Text, int.Parse(textBox2.Text));
                                        SetupGameFiles();
                                        LoadAssets();
                                        ProcessStartInfo ps = new ProcessStartInfo();
                                        ps.UseShellExecute = false;
                                        ps.CreateNoWindow = false;
                                        listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Player\RobloxPlayerBeta.exe"; }));
                                        this.Invoke(new Action(() => { if (UseJoinJSONLink == true) ps.Arguments = joinargument; }));
                                        ProcessStartInfo ps1 = new ProcessStartInfo();
                                        ps1.UseShellExecute = false;
                                        ps1.FileName = datafolder + @"\tools\node\node.exe";
                                        ps1.RedirectStandardOutput = true;
                                        ps1.CreateNoWindow = true;
                                        if (textBox1.Text != "localhost" && textBox1.Text != "127.0.0.1")
                                        {
                                            this.Invoke(new Action(() =>
                                            {
                                                if (Properties.Settings.Default.useAuth && internetConnected)
                                                {
                                                    if (Properties.Settings.Default.avatarR15)
                                                    {
                                                        if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    }
                                                    else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                else
                                                {
                                                    if (Properties.Settings.Default.avatarR15)
                                                    {
                                                        if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    }
                                                    else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                            }));
                                        }
                                        else
                                        {
                                            if (Properties.Settings.Default.useAuth && internetConnected)
                                            {
                                                if (Properties.Settings.Default.avatarR15)
                                                {
                                                    if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                            else
                                            {
                                                if (Properties.Settings.Default.avatarR15)
                                                {
                                                    if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                        }
                                        ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                        ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                        ProcessStartInfo ps2 = new ProcessStartInfo();
                                        ps2.FileName = datafolder + @"\tools\IPForwarder.exe";
                                        ps2.Arguments = "-ip " + textBox1.Text + " -port " + textBox2.Text;
                                        if (Properties.Settings.Default.ShowConsole == false) ps2.WindowStyle = ProcessWindowStyle.Hidden;
                                        if (textBox5.Text.Length > 2 && textBox4.Text.Length > 0)
                                        {
                                            if (useIPForwarder == true && textBox1.Text != "localhost" && textBox1.Text != "127.0.0.1" || useIPForwarder == true && textBox2.Text != "53640") Process.Start(ps2);
                                            if (IsNodeFromAppRunning() == false)
                                            {
                                                Process test = Process.Start(ps1);
                                                button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                statusText.Invoke(new Action(() => { statusText.Text = "Waiting for node server to start..."; }));
                                                test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                                {
                                                    if (e1.Data != null)
                                                    {
                                                        if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                        {

                                                            setupAvatarOnServer();

                                                            statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                            Process roblox = Process.Start(ps);
                                                            roblox.EnableRaisingEvents = true;
                                                            roblox.Exited += Roblox_Exited;
                                                            test.CancelOutputRead();
                                                            await Task.Delay(3000);
                                                            statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                            statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                        }
                                                    }
                                                });
                                                test.BeginOutputReadLine();
                                            }
                                            else
                                            {

                                                setupAvatarOnServer();
                                                statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                Process roblox = Process.Start(ps);
                                                roblox.EnableRaisingEvents = true;
                                                roblox.Exited += Roblox_Exited;
                                                await Task.Delay(3000);
                                                statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                            }
                                        }
                                        else
                                        {
                                            MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }
                                    }
                                    else
                                    {
                                        ProcessStartInfo ps = new ProcessStartInfo();
                                        ps.UseShellExecute = true;
                                        ps.FileName = Application.ExecutablePath;
                                        ps.Verb = "runas";
                                        Process.Start(ps);
                                        Application.Exit();
                                    }
                                }
                                else
                                {

                                        MessageBox.Show("Joining on Mid-2017 and older clients requires changing your hosts file!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);;
                                }
                            }
                            else
                            {
                                statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                if (UseJoinJSONLink) SetupJoinScript(textBox1.Text, int.Parse(textBox2.Text));
                                SetupGameFiles();
                                LoadAssets();
                                ProcessStartInfo ps = new ProcessStartInfo();
                                ps.UseShellExecute = false;
                                ps.CreateNoWindow = false;
                                listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Player\RobloxPlayerBeta.exe"; }));
                                this.Invoke(new Action(() => { if (UseJoinJSONLink == true) ps.Arguments = joinargument; }));
                                ProcessStartInfo ps1 = new ProcessStartInfo();
                                ps1.UseShellExecute = false;
                                ps1.RedirectStandardOutput = true;
                                ps1.CreateNoWindow = true;
                                ps1.FileName = datafolder + @"\tools\node\node.exe";
                                if (textBox1.Text != "localhost" && textBox1.Text != "127.0.0.1")
                                {
                                    this.Invoke(new Action(() =>
                                    {
                                        if (Properties.Settings.Default.useAuth && internetConnected)
                                        {
                                            if (Properties.Settings.Default.avatarR15)
                                            {
                                                if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                            else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        }
                                        else
                                        {
                                            if (Properties.Settings.Default.avatarR15)
                                            {
                                                if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining";
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining";
                                            }
                                            else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining";
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining";
                                        }
                                    }));

                                }
                                else
                                {
                                    if (Properties.Settings.Default.useAuth && internetConnected)
                                    {
                                        if (Properties.Settings.Default.avatarR15)
                                        {
                                            if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        }
                                        else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                    }
                                    else
                                    {
                                        if (Properties.Settings.Default.avatarR15)
                                        {
                                            if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "]";
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "]";
                                        }
                                        else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "]";
                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "]";
                                    }
                                }
                                ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                ProcessStartInfo ps2 = new ProcessStartInfo();
                                ps2.FileName = datafolder + @"\tools\IPForwarder.exe";
                                this.Invoke(new Action(() => { ps2.Arguments = "-ip " + textBox1.Text + " -port " + textBox2.Text; }));
                                if (Properties.Settings.Default.ShowConsole == false) ps2.WindowStyle = ProcessWindowStyle.Hidden;
                                if (textBox5.Text.Length > 2 && textBox4.Text.Length > 0)
                                {
                                    if (useIPForwarder == true && textBox1.Text != "localhost" && textBox1.Text != "127.0.0.1" || useIPForwarder == true && textBox2.Text != "53640") Process.Start(ps2);
                                    if (IsNodeFromAppRunning() == false)
                                    {
                                        button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                        button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                        button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                        Process test = Process.Start(ps1);
                                        statusText.Invoke(new Action(() => { statusText.Text = "Waiting for node server to start..."; }));
                                        test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                        {
                                            if (e1.Data != null)
                                            {
                                                if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                {

                                                    setupAvatarOnServer();

                                                    statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                    Process roblox = Process.Start(ps);
                                                    roblox.EnableRaisingEvents = true;
                                                    roblox.Exited += Roblox_Exited;
                                                    test.CancelOutputRead();
                                                    await Task.Delay(3000);
                                                    statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                    statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                }
                                            }
                                        });
                                        test.BeginOutputReadLine();
                                    }
                                    else
                                    {
                                        setupAvatarOnServer();
                                        statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                        button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                        button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                        button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                        Process roblox = Process.Start(ps);
                                        roblox.EnableRaisingEvents = true;
                                        roblox.Exited += Roblox_Exited;
                                        await Task.Delay(3000);
                                        statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                        statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                    button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                    button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                    await Task.Delay(3000);
                                    statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                    statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                }
                            }
                        }
                        else
                        {
                            if (File.ReadAllText(@"C:\Windows\System32\drivers\etc\hosts").Contains("\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip") == false && WineDetector.IsRunningOnWine() == false)
                            {
                                if (MessageBox.Show("Wanna edit the hosts file to make sure that decals loads? This is recommended for best experience!\n\nIf you wanna load assets, provide your .ROBLOSECURITY in the settings section and enable Use auth for loading assets. (This will not conflict with your latest Roblox Studio)", "hosts File Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                {
                                    if (IsAdministrator())
                                    {


                                        File.AppendAllText(@"C:\Windows\System32\drivers\etc\hosts", "\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip");
                                        statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                        button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                        button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                        button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                        if (UseJoinJSONLink) SetupJoinScript(textBox1.Text, int.Parse(textBox2.Text));
                                        SetupGameFiles();
                                        LoadAssets();
                                        ProcessStartInfo ps = new ProcessStartInfo();
                                        ps.UseShellExecute = false;
                                        ps.CreateNoWindow = false;
                                        listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Studio\RobloxStudioBeta.exe"; }));
                                        this.Invoke(new Action(() => { if (UseJoinJSONLink == true) ps.Arguments = joinargument; else ps.Arguments = joinargument + " -server " + textBox1.Text + " -port " + textBox2.Text + " -placeId 1 -universeId 2"; }));
                                        ProcessStartInfo ps1 = new ProcessStartInfo();
                                        ps1.UseShellExecute = false;
                                        ps1.FileName = datafolder + @"\tools\node\node.exe";
                                        ps1.RedirectStandardOutput = true;
                                        ps1.CreateNoWindow = true;
                                        if (textBox1.Text != "localhost" && textBox1.Text != "127.0.0.1")
                                        {
                                            this.Invoke(new Action(() =>
                                            {
                                                if (Properties.Settings.Default.useAuth && internetConnected)
                                                {
                                                    if (Properties.Settings.Default.avatarR15)
                                                    {
                                                        if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    }
                                                    else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                else
                                                {
                                                    if (Properties.Settings.Default.avatarR15)
                                                    {
                                                        if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    }
                                                    else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                            }));
                                        }
                                        else
                                        {
                                            if (Properties.Settings.Default.useAuth && internetConnected)
                                            {
                                                if (Properties.Settings.Default.avatarR15)
                                                {
                                                    if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                            else
                                            {
                                                if (Properties.Settings.Default.avatarR15)
                                                {
                                                    if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                        }
                                        ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                        ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                        ProcessStartInfo ps2 = new ProcessStartInfo();
                                        ps2.FileName = datafolder + @"\tools\IPForwarder.exe";
                                        ps2.Arguments = "-ip " + textBox1.Text + " -port " + textBox2.Text;
                                        if (Properties.Settings.Default.ShowConsole == false) ps2.WindowStyle = ProcessWindowStyle.Hidden;
                                        if (textBox5.Text.Length > 2 && textBox4.Text.Length > 0)
                                        {
                                            if (useIPForwarder == true && textBox1.Text != "localhost" && textBox1.Text != "127.0.0.1" || useIPForwarder == true && textBox2.Text != "53640") Process.Start(ps2);
                                            if (IsNodeFromAppRunning() == false)
                                            {
                                                Process test = Process.Start(ps1);
                                                button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                statusText.Invoke(new Action(() => { statusText.Text = "Waiting for node server to start..."; }));
                                                test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                                {
                                                    if (e1.Data != null)
                                                    {
                                                        if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                        {

                                                            setupAvatarOnServer();

                                                            statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                            Process roblox = Process.Start(ps);
                                                            roblox.EnableRaisingEvents = true;
                                                            roblox.Exited += Roblox_Exited;
                                                            test.CancelOutputRead();
                                                            await Task.Delay(3000);
                                                            statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                            statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                        }
                                                    }
                                                });
                                                test.BeginOutputReadLine();
                                            }
                                            else
                                            {

                                                setupAvatarOnServer();
                                                statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                Process roblox = Process.Start(ps);
                                                roblox.EnableRaisingEvents = true;
                                                roblox.Exited += Roblox_Exited;
                                                await Task.Delay(3000);
                                                statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                            }
                                        }
                                        else
                                        {
                                            MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }
                                    }
                                    else
                                    {
                                        ProcessStartInfo ps = new ProcessStartInfo();
                                        ps.UseShellExecute = true;
                                        ps.FileName = Application.ExecutablePath;
                                        ps.Verb = "runas";
                                        Process.Start(ps);
                                        Application.Exit();
                                    }
                                }
                                else
                                {
                                    statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                    ProcessStartInfo ps = new ProcessStartInfo();
                                    listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Studio\RobloxStudioBeta.exe"; }));
                                    this.Invoke(new Action(() => { ps.Arguments = joinargument + " -server " + textBox1.Text + " -port " + textBox2.Text + " -placeId 1 -universeId 2"; }));
                                    ProcessStartInfo ps2 = new ProcessStartInfo();
                                    ps2.FileName = datafolder + @"\tools\IPForwarder.exe";
                                    ps2.Arguments = "-ip " + textBox1.Text + " -port " + textBox2.Text;
                                    if (Properties.Settings.Default.ShowConsole == false) ps2.WindowStyle = ProcessWindowStyle.Hidden;
                                    statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                    if (UseJoinJSONLink == false)
                                    {
                                        if (useIPForwarder == true && textBox1.Text != "localhost" && textBox1.Text != "127.0.0.1" || useIPForwarder == true && textBox2.Text != "53640") Process.Start(ps2);
                                        Process.Start(ps);
                                    }
                                    else
                                    {
                                        MessageBox.Show("Joining on Mid-2017 and older clients requires changing your hosts file!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                    await Task.Delay(3000);
                                    statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                    statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                }
                            }
                            else
                            {
                                statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                if (UseJoinJSONLink) SetupJoinScript(textBox1.Text, int.Parse(textBox2.Text));
                                SetupGameFiles();
                                LoadAssets();
                                ProcessStartInfo ps = new ProcessStartInfo();
                                ps.UseShellExecute = false;
                                ps.CreateNoWindow = false;
                                listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Studio\RobloxStudioBeta.exe"; }));
                                this.Invoke(new Action(() => { if (UseJoinJSONLink == true) ps.Arguments = joinargument; else ps.Arguments = joinargument + " -server " + textBox1.Text + " -port " + textBox2.Text + " -placeId 1 -universeId 2"; }));
                                ProcessStartInfo ps1 = new ProcessStartInfo();
                                ps1.UseShellExecute = false;
                                ps1.RedirectStandardOutput = true;
                                ps1.CreateNoWindow = true;
                                ps1.FileName = datafolder + @"\tools\node\node.exe";
                                if (textBox1.Text != "localhost" && textBox1.Text != "127.0.0.1")
                                {
                                    this.Invoke(new Action(() =>
                                    {
                                        if (Properties.Settings.Default.useAuth && internetConnected)
                                        {
                                            if (Properties.Settings.Default.avatarR15)
                                            {
                                                if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                            else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        }
                                        else
                                        {
                                            if (Properties.Settings.Default.avatarR15)
                                            {
                                                if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining";
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining";
                                            }
                                            else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining";
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + textBox1.Text + "\" -joining";
                                        }
                                    }));

                                }
                                else
                                {
                                    if (Properties.Settings.Default.useAuth && internetConnected)
                                    {
                                        if (Properties.Settings.Default.avatarR15)
                                        {
                                            if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        }
                                        else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                    }
                                    else
                                    {
                                        if (Properties.Settings.Default.avatarR15)
                                        {
                                            if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "]";
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "]";
                                        }
                                        else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "]";
                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "]";
                                    }
                                }
                                ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                ProcessStartInfo ps2 = new ProcessStartInfo();
                                ps2.FileName = datafolder + @"\tools\IPForwarder.exe";
                                this.Invoke(new Action(() => { ps2.Arguments = "-ip " + textBox1.Text + " -port " + textBox2.Text; }));
                                if (Properties.Settings.Default.ShowConsole == false) ps2.WindowStyle = ProcessWindowStyle.Hidden;
                                if (textBox5.Text.Length > 2 && textBox4.Text.Length > 0)
                                {
                                    if (useIPForwarder == true && textBox1.Text != "localhost" && textBox1.Text != "127.0.0.1" || useIPForwarder == true && textBox2.Text != "53640") Process.Start(ps2);
                                    if (IsNodeFromAppRunning() == false)
                                    {
                                        button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                        button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                        button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                        Process test = Process.Start(ps1);
                                        statusText.Invoke(new Action(() => { statusText.Text = "Waiting for node server to start..."; }));
                                        test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                        {
                                            if (e1.Data != null)
                                            {
                                                if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                {

                                                    setupAvatarOnServer();

                                                    statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                    Process roblox = Process.Start(ps);
                                                    roblox.EnableRaisingEvents = true;
                                                    roblox.Exited += Roblox_Exited;
                                                    test.CancelOutputRead();
                                                    await Task.Delay(3000);
                                                    statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                    statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                }
                                            }
                                        });
                                        test.BeginOutputReadLine();
                                    }
                                    else
                                    {
                                        setupAvatarOnServer();
                                        statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                        button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                        button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                        button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                        Process roblox = Process.Start(ps);
                                        roblox.EnableRaisingEvents = true;
                                        roblox.Exited += Roblox_Exited;
                                        await Task.Delay(3000);
                                        statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                        statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                    button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                    button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                    await Task.Delay(3000);
                                    statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                    statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                }
                            }
                        }
                    }
                    catch (Exception e1)
                    {
                        MessageBox.Show("Something went wrong while trying to launch Studio/Player, please look in the error message for details: " + e1.Message, "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Console.WriteLine("<ERROR> Something went wrong when attempting to launch Studio/Player! Please look in the error below and report it to the developer if it's launcher-sided!\n" + e1.Message + "\nStack Trace:\n" + e1.StackTrace);
                        await Task.Delay(3000);
                        statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                        statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                    }
                });
                thread.TrySetApartmentState(ApartmentState.MTA);
                thread.Start();
            }
            else
            {
                MessageBox.Show("A client must be selected to be able to run!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1) 
            {
                isJoiningOrStudio = false;
                Thread thread = new Thread(async () =>
                {
                    try
                    {
                        if (Properties.Settings.Default.lastselectedmap != "" && listBox2.SelectedIndex != -1)
                        {
                            if (Directory.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Player")) 
                            {
                                if (Control.ModifierKeys == Keys.Shift)
                                {
                                    if (openFileDialog1.ShowDialog() == DialogResult.OK)
                                    {
                                        if (File.ReadAllText(@"C:\Windows\System32\drivers\etc\hosts").Contains("\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip") == false && WineDetector.IsRunningOnWine() == false)
                                        {
                                            if (MessageBox.Show("Wanna edit the hosts file to make sure that decals loads? This is recommended for best experience!\n\nIf you wanna load assets, provide your .ROBLOSECURITY in the settings section and enable Use auth for loading assets. (This will not conflict with your latest Roblox Studio)", "hosts File Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                            {
                                                if (IsAdministrator())
                                                {

                                                    File.AppendAllText(@"C:\Windows\System32\drivers\etc\hosts", "\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip");
                                                    statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                                    button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                                    button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                                    button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                                    if (UseJoinJSONLink) SetupJoinScript("127.0.0.1", int.Parse(textBox2.Text));
                                                    SetupGameFiles();
                                                    LoadAssets();
                                                    ProcessStartInfo ps = new ProcessStartInfo();
                                                    ps.UseShellExecute = false;
                                                    ps.CreateNoWindow = false;
                                                    listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Player\RobloxPlayerBeta.exe"; }));
                                                    ps.Arguments = hostargument;
                                                    ProcessStartInfo ps1 = new ProcessStartInfo();
                                                    ps1.UseShellExecute = false;
                                                    ps1.FileName = datafolder + @"\tools\node\node.exe";
                                                    ps1.RedirectStandardOutput = true;
                                                    ps1.CreateNoWindow = true;
                                                    if (Properties.Settings.Default.useAuth && internetConnected)
                                                    {
                                                        if (Properties.Settings.Default.avatarR15)
                                                        {
                                                            if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        }
                                                        else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    }
                                                    else
                                                    {
                                                        if (Properties.Settings.Default.avatarR15)
                                                        {
                                                            if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        }
                                                        else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    }
                                                    ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                                    ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                                    if (textBox5.Text.Length > 2 && textBox4.Text.Length > 0)
                                                    {
                                                        if (IsNodeFromAppRunning() == false)
                                                        {
                                                            Process test = Process.Start(ps1);
                                                            statusText.Invoke(new Action(() => { statusText.Text = "Waiting for node server to start..."; }));
                                                            test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                                            {
                                                                if (e1.Data != null)
                                                                {
                                                                    if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                                    {
                                                                        ServerUtils.StartListServer(Properties.Settings.Default.lastselectedversion, Properties.Settings.Default.lastselectedmap, Properties.Settings.Default.username + "'s Server",int.Parse(textBox2.Text));
                                                                        statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                                        button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                                        button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                                        button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                                        Process roblox = Process.Start(ps);
                                                                        roblox.EnableRaisingEvents = true;
                                                                        roblox.Exited += Roblox_Exited;
                                                                        test.CancelOutputRead();
                                                                        await Task.Delay(3000);
                                                                        statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                                        statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                                    }
                                                                }
                                                            });
                                                            test.BeginOutputReadLine();
                                                        }
                                                        else
                                                        {
                                                            ServerUtils.StartListServer(Properties.Settings.Default.lastselectedversion, Properties.Settings.Default.lastselectedmap, Properties.Settings.Default.username + "'s Server",int.Parse(textBox2.Text));
                                                            statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                            button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                                            button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                                            button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                                            Process roblox = Process.Start(ps);
                                                            roblox.EnableRaisingEvents = true;
                                                            roblox.Exited += Roblox_Exited;
                                                            await Task.Delay(3000);
                                                            statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                            statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                        button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                        button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                        button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                        await Task.Delay(3000);
                                                        statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                        statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                    }
                                                }
                                                else
                                                {
                                                    ProcessStartInfo ps = new ProcessStartInfo();
                                                    ps.UseShellExecute = true;
                                                    ps.FileName = Application.ExecutablePath;
                                                    ps.Verb = "runas";
                                                    Process.Start(ps);
                                                    Application.Exit();
                                                }
                                            }
                                            else
                                            {
                                                MessageBox.Show("Hosting on Mid-2017 and older clients requires changing your hosts file!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);

                                            }
                                        }
                                        else
                                        {
                                            statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                            button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                            button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                            button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                            if (UseJoinJSONLink) SetupJoinScript("127.0.0.1", int.Parse(textBox2.Text));
                                            SetupGameFiles();
                                            LoadAssets();
                                            ProcessStartInfo ps = new ProcessStartInfo();
                                            ps.UseShellExecute = false;
                                            ps.CreateNoWindow = false;
                                            listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Player\RobloxPlayerBeta.exe"; }));
                                            ps.Arguments = hostargument;
                                            ProcessStartInfo ps1 = new ProcessStartInfo();
                                            ps1.UseShellExecute = false;
                                            ps1.FileName = datafolder + @"\tools\node\node.exe";
                                            ps1.RedirectStandardOutput = true;
                                            ps1.CreateNoWindow = true;
                                            if (Properties.Settings.Default.useAuth && internetConnected)
                                            {
                                                if (Properties.Settings.Default.avatarR15)
                                                {
                                                    if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                            else
                                            {
                                                if (Properties.Settings.Default.avatarR15)
                                                {
                                                    if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                            ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                            ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                            if (textBox5.Text.Length > 2 && textBox4.Text.Length > 0)
                                            {
                                                if (IsNodeFromAppRunning() == false)
                                                {
                                                    Process test = Process.Start(ps1);
                                                    button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                    button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                    button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                    statusText.Invoke(new Action(() => { statusText.Text = "Waiting for node server to start..."; }));
                                                    test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                                    {
                                                        if (e1.Data != null)
                                                        {
                                                            if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                            {
                                                                ServerUtils.StartListServer(Properties.Settings.Default.lastselectedversion, Properties.Settings.Default.lastselectedmap, Properties.Settings.Default.username + "'s Server",int.Parse(textBox2.Text));
                                                                statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                                Process roblox = Process.Start(ps);
                                                                roblox.EnableRaisingEvents = true;
                                                                roblox.Exited += Roblox_Exited;
                                                                test.CancelOutputRead();
                                                                await Task.Delay(3000);
                                                                statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                                statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                            }
                                                        }
                                                    });
                                                    test.BeginOutputReadLine();
                                                }
                                                else
                                                {
                                                    ServerUtils.StartListServer(Properties.Settings.Default.lastselectedversion, Properties.Settings.Default.lastselectedmap, Properties.Settings.Default.username + "'s Server",int.Parse(textBox2.Text));
                                                    button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                    button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                    button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                    statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                    Process roblox = Process.Start(ps);
                                                    roblox.EnableRaisingEvents = true;
                                                    roblox.Exited += Roblox_Exited;
                                                    await Task.Delay(3000);
                                                    statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                    statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                }
                                            }
                                            else
                                            {
                                                MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                await Task.Delay(3000);
                                                statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (File.ReadAllText(@"C:\Windows\System32\drivers\etc\hosts").Contains("\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip") == false && WineDetector.IsRunningOnWine() == false)
                                    {
                                        if (MessageBox.Show("Wanna edit the hosts file to make sure that decals loads? This is recommended for best experience!\n\nIf you wanna load assets, provide your .ROBLOSECURITY in the settings section and enable Use auth for loading assets. (This will not conflict with your latest Roblox Studio)", "hosts File Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                        {
                                            if (IsAdministrator())
                                            {

                                                File.AppendAllText(@"C:\Windows\System32\drivers\etc\hosts", "\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip");
                                                statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                                button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                                button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                                button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                                if (UseJoinJSONLink) SetupJoinScript("127.0.0.1", int.Parse(textBox2.Text));
                                                SetupGameFiles();
                                                LoadAssets();
                                                ProcessStartInfo ps = new ProcessStartInfo();
                                                ps.UseShellExecute = false;
                                                ps.CreateNoWindow = false;
                                                listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Player\RobloxPlayerBeta.exe"; }));
                                                ps.Arguments = hostargument;
                                                ProcessStartInfo ps1 = new ProcessStartInfo();
                                                ps1.UseShellExecute = false;
                                                ps1.RedirectStandardOutput = true;
                                                ps1.CreateNoWindow = true;
                                                ps1.FileName = datafolder + @"\tools\node\node.exe";
                                                if (Properties.Settings.Default.useAuth && internetConnected)
                                                {
                                                    if (Properties.Settings.Default.avatarR15)
                                                    {
                                                        if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    }
                                                    else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                else
                                                {
                                                    if (Properties.Settings.Default.avatarR15)
                                                    {
                                                        if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    }
                                                    else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                                ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                                if (textBox5.Text.Length > 2 && textBox4.Text.Length > 0)
                                                {
                                                    if (IsNodeFromAppRunning() == false)
                                                    {
                                                        Process test = Process.Start(ps1);
                                                        button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                        button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                        button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                        statusText.Invoke(new Action(() => { statusText.Text = "Waiting for node server to start..."; }));
                                                        test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                                        {
                                                            if (e1.Data != null)
                                                            {
                                                                if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                                {
                                                                    ServerUtils.StartListServer(Properties.Settings.Default.lastselectedversion, Properties.Settings.Default.lastselectedmap, Properties.Settings.Default.username + "'s Server",int.Parse(textBox2.Text));
                                                                    statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                                    Process roblox = Process.Start(ps);
                                                                    roblox.EnableRaisingEvents = true;
                                                                    roblox.Exited += Roblox_Exited;
                                                                    test.CancelOutputRead();
                                                                    await Task.Delay(3000);
                                                                    statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                                    statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                                }
                                                            }
                                                        });
                                                        test.BeginOutputReadLine();
                                                    }
                                                    else
                                                    {
                                                        ServerUtils.StartListServer(Properties.Settings.Default.lastselectedversion, Properties.Settings.Default.lastselectedmap, Properties.Settings.Default.username + "'s Server",int.Parse(textBox2.Text));
                                                        button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                        button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                        button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                        statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                        Process roblox = Process.Start(ps);
                                                        roblox.EnableRaisingEvents = true;
                                                        roblox.Exited += Roblox_Exited;
                                                        await Task.Delay(3000);
                                                        statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                        statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                    }
                                                }
                                                else
                                                {
                                                    MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                    button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                    button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                    button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                    await Task.Delay(3000);
                                                    statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                    statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                }
                                            }
                                            else
                                            {
                                                ProcessStartInfo ps = new ProcessStartInfo();
                                                ps.UseShellExecute = true;
                                                ps.FileName = Application.ExecutablePath;
                                                ps.Verb = "runas";
                                                Process.Start(ps);
                                                Application.Exit();
                                            }
                                        }
                                        else
                                        {
                                            statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                            statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                            ProcessStartInfo ps = new ProcessStartInfo();
                                            listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Studio\RobloxStudioBeta.exe"; }));
                                            this.Invoke(new Action(() => { ps.Arguments = hostargument + " -localPlaceFile \"" + datafolder + @"\maps\" + listBox2.GetItemText(listBox2.SelectedItem) + "\" -port " + textBox2.Text + " -placeId 1 -universeId 2 -creatorId" + Properties.Settings.Default.UserId; }));
                                            if (UseJoinJSONLink == false) { Process.Start(ps); } else MessageBox.Show("Joining on Mid-2017 and older clients requires changing your hosts file!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            await Task.Delay(3000);
                                            statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                            statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                        }
                                    }
                                    else
                                    {
                                        statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                        button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                        button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                        button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                        if (UseJoinJSONLink) SetupJoinScript("127.0.0.1", int.Parse(textBox2.Text));
                                        SetupGameFiles();
                                        LoadAssets();
                                        ProcessStartInfo ps = new ProcessStartInfo();
                                        ps.UseShellExecute = false;
                                        ps.CreateNoWindow = false;
                                        listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Player\RobloxPlayerBeta.exe"; }));
                                        ps.Arguments = hostargument;
                                        ProcessStartInfo ps1 = new ProcessStartInfo();
                                        ps1.UseShellExecute = false;
                                        ps1.RedirectStandardOutput = true;
                                        ps1.CreateNoWindow = true;
                                        ps1.FileName = datafolder + @"\tools\node\node.exe";
                                        if (Properties.Settings.Default.useAuth && internetConnected)
                                        {
                                            if (Properties.Settings.Default.avatarR15)
                                            {
                                                if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                            else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        }
                                        else
                                        {
                                            if (Properties.Settings.Default.avatarR15)
                                            {
                                                if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                            else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        }
                                        ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                        ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                        if (textBox5.Text.Length > 2 && textBox4.Text.Length > 0)
                                        {
                                            if (IsNodeFromAppRunning() == false)
                                            {
                                                Process test = Process.Start(ps1);
                                                button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                statusText.Invoke(new Action(() => { statusText.Text = "Waiting for node server to start..."; }));
                                                test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                                {
                                                    if (e1.Data != null)
                                                    {
                                                        if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                        {
                                                            ServerUtils.StartListServer(Properties.Settings.Default.lastselectedversion, Properties.Settings.Default.lastselectedmap, Properties.Settings.Default.username + "'s Server",int.Parse(textBox2.Text));
                                                            statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                            Process roblox = Process.Start(ps);
                                                            roblox.EnableRaisingEvents = true;
                                                            roblox.Exited += Roblox_Exited;
                                                            test.CancelOutputRead();
                                                            await Task.Delay(3000);
                                                            statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                            statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                        }
                                                    }
                                                });
                                                test.BeginOutputReadLine();
                                            }
                                            else
                                            {
                                                ServerUtils.StartListServer(Properties.Settings.Default.lastselectedversion, Properties.Settings.Default.lastselectedmap, Properties.Settings.Default.username + "'s Server",int.Parse(textBox2.Text));
                                                button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                Process roblox = Process.Start(ps);
                                                roblox.EnableRaisingEvents = true;
                                                roblox.Exited += Roblox_Exited;
                                                await Task.Delay(3000);
                                                statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                            }
                                        }
                                        else
                                        {
                                            MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                            button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                            button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                            await Task.Delay(3000);
                                            statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                            statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                        }
                                    }
                                }
                            }
                            else {
                                if (Control.ModifierKeys == Keys.Shift)
                                {
                                    if (openFileDialog1.ShowDialog() == DialogResult.OK)
                                    {
                                        if (File.ReadAllText(@"C:\Windows\System32\drivers\etc\hosts").Contains("\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip") == false && WineDetector.IsRunningOnWine() == false)
                                        {
                                            if (MessageBox.Show("Wanna edit the hosts file to make sure that decals loads? This is recommended for best experience!\n\nIf you wanna load assets, provide your .ROBLOSECURITY in the settings section and enable Use auth for loading assets. (This will not conflict with your latest Roblox Studio)", "hosts File Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                            {
                                                if (IsAdministrator())
                                                {

                                                    File.AppendAllText(@"C:\Windows\System32\drivers\etc\hosts", "\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip");
                                                    statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                                    button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                                    button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                                    button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                                    if (UseJoinJSONLink) SetupJoinScript("127.0.0.1", int.Parse(textBox2.Text));
                                                    SetupGameFiles();
                                                    LoadAssets();
                                                    ProcessStartInfo ps = new ProcessStartInfo();
                                                    ps.UseShellExecute = false;
                                                    ps.CreateNoWindow = false;
                                                    listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Studio\RobloxStudioBeta.exe"; }));
                                                    this.Invoke(new Action(() => { if (UseJoinJSONLink == false) ps.Arguments = hostargument + " -localPlaceFile \"" + datafolder + @"\maps\" + listBox2.GetItemText(listBox2.SelectedItem) + "\" -port " + textBox2.Text + " -placeId 1 -universeId 2 -creatorId " + Properties.Settings.Default.UserId; else ps.Arguments = hostargument.Replace("$Port$", textBox2.Text) + (dontLoadMapFromArgument ? "" : " -fileLocation \"" + datafolder + @"\maps\" + listBox2.GetItemText(listBox2.SelectedItem)) + "\" -rbdf=\"" + openFileDialog1.FileName + "\""; }));
                                                    ProcessStartInfo ps1 = new ProcessStartInfo();
                                                    ps1.UseShellExecute = false;
                                                    ps1.FileName = datafolder + @"\tools\node\node.exe";
                                                    ps1.RedirectStandardOutput = true;
                                                    ps1.CreateNoWindow = true;
                                                    if (Properties.Settings.Default.useAuth && internetConnected)
                                                    {
                                                        if (Properties.Settings.Default.avatarR15)
                                                        {
                                                            if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        }
                                                        else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    }
                                                    else
                                                    {
                                                        if (Properties.Settings.Default.avatarR15)
                                                        {
                                                            if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        }
                                                        else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    }
                                                    ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                                    ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                                    if (textBox5.Text.Length > 2 && textBox4.Text.Length > 0)
                                                    {
                                                        if (IsNodeFromAppRunning() == false)
                                                        {
                                                            Process test = Process.Start(ps1);
                                                            statusText.Invoke(new Action(() => { statusText.Text = "Waiting for node server to start..."; }));
                                                            test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                                            {
                                                                if (e1.Data != null)
                                                                {
                                                                    if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                                    {
                                                                        ServerUtils.StartListServer(Properties.Settings.Default.lastselectedversion, Properties.Settings.Default.lastselectedmap, Properties.Settings.Default.username + "'s Server",int.Parse(textBox2.Text));
                                                                        statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                                        button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                                        button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                                        button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                                        Process roblox = Process.Start(ps);
                                                                        roblox.EnableRaisingEvents = true;
                                                                        roblox.Exited += Roblox_Exited;
                                                                        test.CancelOutputRead();
                                                                        await Task.Delay(3000);
                                                                        statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                                        statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                                    }
                                                                }
                                                            });
                                                            test.BeginOutputReadLine();
                                                        }
                                                        else
                                                        {
                                                            ServerUtils.StartListServer(Properties.Settings.Default.lastselectedversion, Properties.Settings.Default.lastselectedmap, Properties.Settings.Default.username + "'s Server",int.Parse(textBox2.Text));
                                                            statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                            button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                                            button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                                            button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                                            Process roblox = Process.Start(ps);
                                                            roblox.EnableRaisingEvents = true;
                                                            roblox.Exited += Roblox_Exited;
                                                            await Task.Delay(3000);
                                                            statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                            statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                        button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                        button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                        button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                        await Task.Delay(3000);
                                                        statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                        statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                    }
                                                }
                                                else
                                                {
                                                    ProcessStartInfo ps = new ProcessStartInfo();
                                                    ps.UseShellExecute = true;
                                                    ps.FileName = Application.ExecutablePath;
                                                    ps.Verb = "runas";
                                                    Process.Start(ps);
                                                    Application.Exit();
                                                }
                                            }
                                            else
                                            {
                                                statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                                statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                                button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                                button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                                ProcessStartInfo ps = new ProcessStartInfo();
                                                listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Studio\RobloxStudioBeta.exe"; }));
                                                this.Invoke(new Action(() => { ps.Arguments = hostargument + " -localPlaceFile \"" + datafolder + @"\maps\" + listBox2.GetItemText(listBox2.SelectedItem) + "\" -port " + textBox2.Text + " -placeId 1 -universeId 2 -creatorId" + Properties.Settings.Default.UserId; }));
                                                if (UseJoinJSONLink == false) { Process.Start(ps); } else MessageBox.Show("Hosting on Mid-2017 and older clients requires changing your hosts file!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                await Task.Delay(3000);
                                                statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                            }
                                        }
                                        else
                                        {
                                            statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                            button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                            button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                            button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                            if (UseJoinJSONLink) SetupJoinScript("127.0.0.1", int.Parse(textBox2.Text));
                                            SetupGameFiles();
                                            LoadAssets();
                                            ProcessStartInfo ps = new ProcessStartInfo();
                                            listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Studio\RobloxStudioBeta.exe"; }));
                                            this.Invoke(new Action(() => { if (UseJoinJSONLink == false) ps.Arguments = hostargument + " -localPlaceFile \"" + datafolder + @"\maps\" + listBox2.GetItemText(listBox2.SelectedItem) + "\" -port " + textBox2.Text + " -placeId 1 -universeId 2 -creatorId " + Properties.Settings.Default.UserId; else ps.Arguments = hostargument.Replace("$Port$", textBox2.Text) + (dontLoadMapFromArgument ? "" : " -fileLocation \"" + datafolder + @"\maps\" + listBox2.GetItemText(listBox2.SelectedItem)) + "\" -rbdf=\"" + openFileDialog1.FileName + "\""; }));
                                            ProcessStartInfo ps1 = new ProcessStartInfo();
                                            ps1.UseShellExecute = false;
                                            ps1.FileName = datafolder + @"\tools\node\node.exe";
                                            ps1.RedirectStandardOutput = true;
                                            ps1.CreateNoWindow = true;
                                            if (Properties.Settings.Default.useAuth && internetConnected)
                                            {
                                                if (Properties.Settings.Default.avatarR15)
                                                {
                                                    if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                            else
                                            {
                                                if (Properties.Settings.Default.avatarR15)
                                                {
                                                    if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                            ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                            ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                            if (textBox5.Text.Length > 2 && textBox4.Text.Length > 0)
                                            {
                                                if (IsNodeFromAppRunning() == false)
                                                {
                                                    Process test = Process.Start(ps1);
                                                    button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                    button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                    button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                    statusText.Invoke(new Action(() => { statusText.Text = "Waiting for node server to start..."; }));
                                                    test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                                    {
                                                        if (e1.Data != null)
                                                        {
                                                            if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                            {
                                                                ServerUtils.StartListServer(Properties.Settings.Default.lastselectedversion, Properties.Settings.Default.lastselectedmap, Properties.Settings.Default.username + "'s Server",int.Parse(textBox2.Text));
                                                                statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                                Process roblox = Process.Start(ps);
                                                                roblox.EnableRaisingEvents = true;
                                                                roblox.Exited += Roblox_Exited;
                                                                test.CancelOutputRead();
                                                                await Task.Delay(3000);
                                                                statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                                statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                            }
                                                        }
                                                    });
                                                    test.BeginOutputReadLine();
                                                }
                                                else
                                                {
                                                    ServerUtils.StartListServer(Properties.Settings.Default.lastselectedversion, Properties.Settings.Default.lastselectedmap, Properties.Settings.Default.username + "'s Server",int.Parse(textBox2.Text));
                                                    button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                    button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                    button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                    statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                    Process roblox = Process.Start(ps);
                                                    roblox.EnableRaisingEvents = true;
                                                    roblox.Exited += Roblox_Exited;
                                                    await Task.Delay(3000);
                                                    statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                    statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                }
                                            }
                                            else
                                            {
                                                MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                await Task.Delay(3000);
                                                statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (File.ReadAllText(@"C:\Windows\System32\drivers\etc\hosts").Contains("\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip") == false && WineDetector.IsRunningOnWine() == false)
                                    {
                                        if (MessageBox.Show("Wanna edit the hosts file to make sure that decals loads? This is recommended for best experience!\n\nIf you wanna load assets, provide your .ROBLOSECURITY in the settings section and enable Use auth for loading assets. (This will not conflict with your latest Roblox Studio)", "hosts File Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                        {
                                            if (IsAdministrator())
                                            {

                                                File.AppendAllText(@"C:\Windows\System32\drivers\etc\hosts", "\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip");
                                                statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                                button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                                button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                                button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                                if (UseJoinJSONLink) SetupJoinScript("127.0.0.1", int.Parse(textBox2.Text));
                                                SetupGameFiles();
                                                LoadAssets();
                                                ProcessStartInfo ps = new ProcessStartInfo();
                                                ps.UseShellExecute = false;
                                                ps.CreateNoWindow = false;
                                                listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Studio\RobloxStudioBeta.exe"; }));
                                                this.Invoke(new Action(() => { if (UseJoinJSONLink == false) ps.Arguments = hostargument + " -localPlaceFile \"" + datafolder + @"\maps\" + listBox2.GetItemText(listBox2.SelectedItem) + "\" -port " + textBox2.Text + " -placeId 1 -universeId 2 -creatorId " + Properties.Settings.Default.UserId; else ps.Arguments = hostargument.Replace("$Port$", textBox2.Text) + (dontLoadMapFromArgument ? "" : " -fileLocation \"" + datafolder + @"\maps\" + listBox2.GetItemText(listBox2.SelectedItem)) + "\" "; }));
                                                ProcessStartInfo ps1 = new ProcessStartInfo();
                                                ps1.UseShellExecute = false;
                                                ps1.RedirectStandardOutput = true;
                                                ps1.CreateNoWindow = true;
                                                ps1.FileName = datafolder + @"\tools\node\node.exe";
                                                if (Properties.Settings.Default.useAuth && internetConnected)
                                                {
                                                    if (Properties.Settings.Default.avatarR15)
                                                    {
                                                        if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    }
                                                    else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                else
                                                {
                                                    if (Properties.Settings.Default.avatarR15)
                                                    {
                                                        if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    }
                                                    else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                                ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                                if (textBox5.Text.Length > 2 && textBox4.Text.Length > 0)
                                                {
                                                    if (IsNodeFromAppRunning() == false)
                                                    {
                                                        Process test = Process.Start(ps1);
                                                        button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                        button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                        button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                        statusText.Invoke(new Action(() => { statusText.Text = "Waiting for node server to start..."; }));
                                                        test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                                        {
                                                            if (e1.Data != null)
                                                            {
                                                                if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                                {
                                                                    statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                                    Process roblox = Process.Start(ps);
                                                                    roblox.EnableRaisingEvents = true;
                                                                    roblox.Exited += Roblox_Exited;
                                                                    test.CancelOutputRead();
                                                                    await Task.Delay(3000);
                                                                    statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                                    statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                                }
                                                            }
                                                        });
                                                        test.BeginOutputReadLine();
                                                    }
                                                    else
                                                    {
                                                        ServerUtils.StartListServer(Properties.Settings.Default.lastselectedversion, Properties.Settings.Default.lastselectedmap, Properties.Settings.Default.username + "'s Server",int.Parse(textBox2.Text));
                                                        button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                        button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                        button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                        statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                        Process roblox = Process.Start(ps);
                                                        roblox.EnableRaisingEvents = true;
                                                        roblox.Exited += Roblox_Exited;
                                                        await Task.Delay(3000);
                                                        statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                        statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                    }
                                                }
                                                else
                                                {
                                                    MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                    button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                    button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                    button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                    await Task.Delay(3000);
                                                    statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                    statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                }
                                            }
                                            else
                                            {
                                                ProcessStartInfo ps = new ProcessStartInfo();
                                                ps.UseShellExecute = true;
                                                ps.FileName = Application.ExecutablePath;
                                                ps.Verb = "runas";
                                                Process.Start(ps);
                                                Application.Exit();
                                            }
                                        }
                                        else
                                        {
                                            statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                            statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                            ProcessStartInfo ps = new ProcessStartInfo();
                                            listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Studio\RobloxStudioBeta.exe"; }));
                                            this.Invoke(new Action(() => { ps.Arguments = hostargument + " -localPlaceFile \"" + datafolder + @"\maps\" + listBox2.GetItemText(listBox2.SelectedItem) + "\" -port " + textBox2.Text + " -placeId 1 -universeId 2 -creatorId" + Properties.Settings.Default.UserId; }));
                                            if (UseJoinJSONLink == false) { Process.Start(ps); } else MessageBox.Show("Joining on Mid-2017 and older clients requires changing your hosts file!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            await Task.Delay(3000);
                                            statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                            statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                        }
                                    }
                                    else
                                    {
                                        statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                        button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                        button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                        button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                        if (UseJoinJSONLink) SetupJoinScript("127.0.0.1", int.Parse(textBox2.Text));
                                        SetupGameFiles();
                                        LoadAssets();
                                        ProcessStartInfo ps = new ProcessStartInfo();
                                        ps.UseShellExecute = false;
                                        ps.CreateNoWindow = false;
                                        listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Studio\RobloxStudioBeta.exe"; }));
                                        this.Invoke(new Action(() => { if (UseJoinJSONLink == false) ps.Arguments = hostargument + " -localPlaceFile \"" + datafolder + @"\maps\" + listBox2.GetItemText(listBox2.SelectedItem) + "\" -port " + textBox2.Text + " -placeId 1 -universeId 2 -creatorId " + Properties.Settings.Default.UserId; else ps.Arguments = hostargument.Replace("$Port$", textBox2.Text) + (dontLoadMapFromArgument ? "" : " -fileLocation \"" + datafolder + @"\maps\" + listBox2.GetItemText(listBox2.SelectedItem)) + "\" "; }));
                                        ProcessStartInfo ps1 = new ProcessStartInfo();
                                        ps1.UseShellExecute = false;
                                        ps1.RedirectStandardOutput = true;
                                        ps1.CreateNoWindow = true;
                                        ps1.FileName = datafolder + @"\tools\node\node.exe";
                                        if (Properties.Settings.Default.useAuth && internetConnected)
                                        {
                                            if (Properties.Settings.Default.avatarR15)
                                            {
                                                if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                            else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        }
                                        else
                                        {
                                            if (Properties.Settings.Default.avatarR15)
                                            {
                                                if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                            else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        }
                                        ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                        ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                        if (textBox5.Text.Length > 2 && textBox4.Text.Length > 0)
                                        {
                                            if (IsNodeFromAppRunning() == false)
                                            {
                                                Process test = Process.Start(ps1);
                                                button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                statusText.Invoke(new Action(() => { statusText.Text = "Waiting for node server to start..."; }));
                                                test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                                {
                                                    if (e1.Data != null)
                                                    {
                                                        if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                        {
                                                            ServerUtils.StartListServer(Properties.Settings.Default.lastselectedversion, Properties.Settings.Default.lastselectedmap, Properties.Settings.Default.username + "'s Server",int.Parse(textBox2.Text));
                                                            statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                            Process roblox = Process.Start(ps);
                                                            roblox.EnableRaisingEvents = true;
                                                            roblox.Exited += Roblox_Exited;
                                                            test.CancelOutputRead();
                                                            await Task.Delay(3000);
                                                            statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                            statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                        }
                                                    }
                                                });
                                                test.BeginOutputReadLine();
                                            }
                                            else
                                            {
                                                ServerUtils.StartListServer(Properties.Settings.Default.lastselectedversion, Properties.Settings.Default.lastselectedmap, Properties.Settings.Default.username + "'s Server",int.Parse(textBox2.Text));
                                                button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                Process roblox = Process.Start(ps);
                                                roblox.EnableRaisingEvents = true;
                                                roblox.Exited += Roblox_Exited;
                                                await Task.Delay(3000);
                                                statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                            }
                                        }
                                        else
                                        {
                                            MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                            button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                            button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                            await Task.Delay(3000);
                                            statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                            statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("A map is required to begin hosting!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            button1.Invoke(new Action(() => { button1.Enabled = true; }));
                            button2.Invoke(new Action(() => { button2.Enabled = true; }));
                            button3.Invoke(new Action(() => { button3.Enabled = true; }));
                            await Task.Delay(3000);
                            statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                            statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                        }
                    }
                    catch (Exception e1)
                    {
                        MessageBox.Show("Something went wrong while trying to launch Studio/Player, please look in the error message for details: " + e1.Message, "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Console.WriteLine("<ERROR> Something went wrong when attempting to launch Studio/Player! Please look in the error below and report it to the developer if it's launcher-sided!\n" + e1.Message + "\nStack Trace: \n" + e1.StackTrace);
                        await Task.Delay(3000);
                        statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                        statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                    }
                });
                thread.TrySetApartmentState(ApartmentState.MTA);
                thread.Start();
            }
            else
            {
                MessageBox.Show("A client must be selected to be able to run!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (textBox3.Text.StartsWith("_|WARNING:-DO-NOT-SHARE-THIS.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items.|"))
                {
                    Properties.Settings.Default.ROBLOSECURITY = Convert.ToBase64String(Encoding.UTF8.GetBytes(textBox3.Text));
                    Properties.Settings.Default.Save();
                    MessageBox.Show("Note that the textbox will be empty on the next launch of the launch, however, your ROBLOSECURITY is saved.", "Reminder!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("A valid ROBLOSECURITY is required!", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    textBox3.Text = string.Empty;
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox3.ReadOnly = !checkBox3.Checked;
            Properties.Settings.Default.useAuth = checkBox1.Checked;
            Properties.Settings.Default.Save();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.UsePatchInStudio = checkBox2.Checked;
            Properties.Settings.Default.Save();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        { 
            Properties.Settings.Default.ClearTemp = checkBox3.Checked;
            Properties.Settings.Default.Save();
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.ShowConsole = checkBox4.Checked;
            Properties.Settings.Default.Save();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            listBox1.Visible = false;
            panel1.Visible = false;
            panel2.Visible = false;
            listBox2.Visible = true;
            button9.Visible = true;
            button10.Visible = true;
            panel3.Visible = false;
            panel5.Visible = false;
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.lastselectedmap = listBox2.GetItemText(listBox2.SelectedItem);
            Properties.Settings.Default.Save();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            listBox2.SetSelected(listBox2.SelectedIndex, false);
            listBox2.SetSelected(RandomNumber(0, listBox2.Items.Count - 1), true);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(datafolder + @"\maps")) 
            {
                listBox2.BeginUpdate();
                listBox2.Items.Clear();
                string[] directories2 = Directory.GetFiles(datafolder + @"\maps");
                foreach (string directory in directories2)
                {
                    if (directory.EndsWith(".rbxl") || directory.EndsWith(".rbxlx"))
                    {
                        listBox2.Items.Add(Path.GetFileName(directory));
                        if (Properties.Settings.Default.lastselectedmap == Path.GetFileName(directory))
                        {
                            listBox2.SetSelected(listBox2.Items.Count - 1, true);
                        }
                    }
                }
                directories2 = null;
                listBox2.EndUpdate();
            }
        }

        private void textBox4_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void textBox5_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Properties.Settings.Default.username = textBox5.Text;
                Properties.Settings.Default.Save();
            }
        }

        private void textBox4_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Properties.Settings.Default.UserId = int.Parse(textBox4.Text);
                Properties.Settings.Default.Save();
            }
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.AccountOver13 = checkBox5.Checked;
            Properties.Settings.Default.Save();
        }

        private void listBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox3.SelectedIndex != -1) 
            {
                bool clientsfound = false;
                string[] config = File.ReadAllLines(directoryasset[listBox3.SelectedIndex] + @"\ReBlox.ini");
                if (config[0] == "[Reblox]")
                {
                    for (int i = 0; i < config.Length; i++)
                    {
                        if (config[i].Trim().StartsWith("Name=\""))
                        {
                            string[] splited = config[i].Trim().Split(new char[] { '=' }, 2);
                            label11.Text = splited[1].Remove(0, 1).Remove(splited[1].Length - 2, 1);
                        }
                        else if (config[i].Trim().StartsWith("Description=\""))
                        {
                            string[] splited = config[i].Trim().Split(new char[] { '=' }, 2);
                            label15.Text = splited[1].Remove(0, 1).Remove(splited[1].Length - 2, 1);
                        }
                        else if (config[i].Trim().StartsWith("Author=\""))
                        {
                            string[] splited = config[i].Trim().Split(new char[] { '=' }, 2);
                            label13.Text = splited[1].Remove(0, 1).Remove(splited[1].Length - 2, 1);
                        }
                        else if (config[i].Trim().StartsWith("Clients="))
                        {
                            clientsfound = true;
                            string[] splited = config[i].Trim().Split(new char[] { '=' }, 2);
                            if (splited[1] == "*")
                            {
                                label29.Visible = false;
                            }
                            else
                            {
                                label29.Text = "Clients: " + splited[1];
                                label29.Visible = true;
                            }
                        }
                    }
                    if (clientsfound == false) label29.Visible = false;
                }

                if (Properties.Settings.Default.AssetPackEnabled.Contains(directoryasset[listBox3.SelectedIndex]))
                {
                    button12.Enabled = false;
                    button13.Enabled = true;
                }
                else
                {
                    button12.Enabled = true;
                    button13.Enabled = false;
                }
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            listBox1.Visible = false;
            panel1.Visible = false;
            panel2.Visible = false;
            listBox2.Visible = false;
            button9.Visible = false;
            button10.Visible = false;
            panel3.Visible = true;
            panel5.Visible = false;
        }

        private void RefreshAssetPacks(bool refreshGUI = true)
        {
            if (Directory.Exists(datafolder + @"\assetpacks"))
            {
                string[] directories3 = Directory.GetDirectories(datafolder + @"\assetpacks");
                if (directories3.Length > 0)
                {
                    listBox3.Visible = true;
                    panel4.Visible = true;
                    label31.Visible = false;
                    int prevIndex = 0;
                    listBox3.Invalidate();
                    listBox3.BeginUpdate();
                    if (refreshGUI == true)
                    {
                        listBox3.Items.Clear();
                        directoryasset.Clear();
                        label15.Text = "";
                        label13.Text = "";
                        label11.Text = "";
                        button12.Enabled = false;
                        button13.Enabled = false;
                    }
                    else
                    {
                        prevIndex = listBox3.SelectedIndex;
                        listBox3.Items.Clear();
                        directoryasset.Clear();
                    }

                    foreach (string directory in directories3)
                    {
                        if (File.Exists(directory + @"\ReBlox.ini"))
                        {
                            string[] config = File.ReadAllLines(directory + @"\ReBlox.ini");
                            if (config[0] == "[Reblox]")
                            {
                                for (int i = 0; i < config.Length; i++)
                                {
                                    if (config[i].Trim().StartsWith("Name=\""))
                                    {
                                        string[] splited = config[i].Trim().Split(new char[] { '=' }, 2);
                                        listBox3.Items.Add(new AssetPackItem((Properties.Settings.Default.AssetPackEnabled.Contains("|" + directory + "|") || Properties.Settings.Default.AssetPackEnabled.EndsWith(directory) || Properties.Settings.Default.AssetPackEnabled.StartsWith(directory + "|")) ? Color.White : Color.Red, splited[1].Remove(0, 1).Remove(splited[1].Length - 2, 1)));
                                        directoryasset.Add(directory);
                                    }
                                }
                            }
                        }
                    }
                    if (refreshGUI == false) listBox3.SetSelected(prevIndex, true);
                    listBox3.EndUpdate();
                }
                else
                {
                    listBox3.Visible = false;
                    panel4.Visible = false;
                    directoryasset.Clear();
                    listBox3.Items.Clear();
                    label31.Visible = true;
                }
            }
            else
            {
                listBox3.Visible = false;
                panel4.Visible = false;
                directoryasset.Clear();
                listBox3.Items.Clear();
                label31.Visible = true;
            }
        }
        private void button14_Click(object sender, EventArgs e)
        {
            RefreshAssetPacks();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (listBox3.SelectedIndex != -1)
            {
                if (Properties.Settings.Default.AssetPackEnabled.Contains(directoryasset[listBox3.SelectedIndex]) == false)
                {
                    if (Properties.Settings.Default.AssetPackEnabled.Length > 0)
                    {
                        Properties.Settings.Default.AssetPackEnabled = Properties.Settings.Default.AssetPackEnabled + "|" + directoryasset[listBox3.SelectedIndex];
                        Properties.Settings.Default.Save();
                        button12.Enabled = false;
                        button13.Enabled = true;
                    }
                    else
                    {
                        Properties.Settings.Default.AssetPackEnabled = Properties.Settings.Default.AssetPackEnabled + directoryasset[listBox3.SelectedIndex];
                        Properties.Settings.Default.Save();
                        button12.Enabled = false;
                        button13.Enabled = true;
                    }
                    RefreshAssetPacks(false);
                }
            }
        }
            
        private void button13_Click(object sender, EventArgs e)
        {
            if (listBox3.SelectedIndex != -1)
            {
                if (Properties.Settings.Default.AssetPackEnabled.Contains(directoryasset[listBox3.SelectedIndex]))
                {
                        string[] arraycheck = Properties.Settings.Default.AssetPackEnabled.Split('|');
                        if (arraycheck.Length > 1 && arraycheck[0] != directoryasset[listBox3.SelectedIndex])
                        {
                            Properties.Settings.Default.AssetPackEnabled = Properties.Settings.Default.AssetPackEnabled.Replace("|" + directoryasset[listBox3.SelectedIndex], "");
                            Properties.Settings.Default.Save();
                            button12.Enabled = true;
                            button13.Enabled = false;
                        }
                    else if (arraycheck.Length > 1 && arraycheck[0] == directoryasset[listBox3.SelectedIndex])
                    {
                        Properties.Settings.Default.AssetPackEnabled = Properties.Settings.Default.AssetPackEnabled.Replace(directoryasset[listBox3.SelectedIndex] + "|", "");
                        Properties.Settings.Default.Save();
                        button12.Enabled = true;
                        button13.Enabled = false;
                    }
                    else if (arraycheck.Length == 1 || arraycheck[0] == directoryasset[listBox3.SelectedIndex])
                    {
                        Properties.Settings.Default.AssetPackEnabled = Properties.Settings.Default.AssetPackEnabled.Replace(directoryasset[listBox3.SelectedIndex], "");
                        Properties.Settings.Default.Save();
                        button12.Enabled = true;
                        button13.Enabled = false;
                    }
                        RefreshAssetPacks(false);
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex != -1) 
            {
                Properties.Settings.Default.avatarR15 = (comboBox1.Items[comboBox1.SelectedIndex].ToString() == "R6") ? false : true;
                Properties.Settings.Default.Save();
                setupAvatarOnServer();
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            listBox1.Visible = false;
            panel1.Visible = false;
            panel2.Visible = false;
            listBox2.Visible = false;
            button9.Visible = false;
            button10.Visible = false;
            panel3.Visible = false;
            panel5.Visible = true;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown1.Value == 366) numericUpDown1.Value = 1000;
            if (numericUpDown1.Value == 1000) numericUpDown1.Value = 365;
            if (convertBrickColortoColor((int)numericUpDown1.Value) != Color.Empty)
            {
                Properties.Settings.Default.HeadColor = (int)numericUpDown1.Value;
                Properties.Settings.Default.Save();
                HeadPanel.BackColor = convertBrickColortoColor((int)numericUpDown1.Value);
            }
            else
            {
                Properties.Settings.Default.HeadColor = 194;
                Properties.Settings.Default.Save();
                HeadPanel.BackColor = convertBrickColortoColor(194);
            }
            setupAvatarOnServer();
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown2.Value == 366) numericUpDown2.Value = 1000;
            if (numericUpDown2.Value == 1000) numericUpDown2.Value = 365;
            if (convertBrickColortoColor((int)numericUpDown2.Value) != Color.Empty)
            {
                Properties.Settings.Default.LeftArmColor = (int)numericUpDown2.Value;
                Properties.Settings.Default.Save();
                LeftArmPanel.BackColor = convertBrickColortoColor((int)numericUpDown2.Value);
            }
            else
            {
                Properties.Settings.Default.LeftArmColor = 194;
                Properties.Settings.Default.Save();
                LeftArmPanel.BackColor = convertBrickColortoColor(194);
            }
            setupAvatarOnServer();
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown3.Value == 366) numericUpDown3.Value = 1000;
            if (numericUpDown3.Value == 1000) numericUpDown3.Value = 365;
            if (convertBrickColortoColor((int)numericUpDown3.Value) != Color.Empty)
            {
                Properties.Settings.Default.LeftLegColor = (int)numericUpDown3.Value;
                Properties.Settings.Default.Save();
                LeftLegPanel.BackColor = convertBrickColortoColor((int)numericUpDown3.Value);
            }
            else
            {
                Properties.Settings.Default.LeftLegColor = 194;
                Properties.Settings.Default.Save();
                LeftLegPanel.BackColor = convertBrickColortoColor(194);
            }
            setupAvatarOnServer();
        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown4.Value == 366) numericUpDown4.Value = 1000;
            if (numericUpDown4.Value == 1000) numericUpDown4.Value = 365;
            if (convertBrickColortoColor((int)numericUpDown4.Value) != Color.Empty)
            {
                Properties.Settings.Default.RightArmColor = (int)numericUpDown4.Value;
                Properties.Settings.Default.Save();
                RightArmPanel.BackColor = convertBrickColortoColor((int)numericUpDown4.Value);
            }
            else
            {
                Properties.Settings.Default.RightArmColor = 194;
                Properties.Settings.Default.Save();
                RightArmPanel.BackColor = convertBrickColortoColor(194);
            }
            setupAvatarOnServer();
        }

        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown5.Value == 366) numericUpDown5.Value = 1000;
            if (numericUpDown5.Value == 1000) numericUpDown5.Value = 365;
            if (convertBrickColortoColor((int)numericUpDown5.Value) != Color.Empty)
            {
                Properties.Settings.Default.RightLegColor = (int)numericUpDown5.Value;
                Properties.Settings.Default.Save();
                RightLegPanel.BackColor = convertBrickColortoColor((int)numericUpDown5.Value);
            }
            else
            {
                Properties.Settings.Default.RightLegColor = 194;
                Properties.Settings.Default.Save();
                RightLegPanel.BackColor = convertBrickColortoColor(194);
            }
            setupAvatarOnServer();
        }

        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown6.Value == 366) numericUpDown6.Value = 1000;
            if (numericUpDown6.Value == 1000) numericUpDown6.Value = 365;
            if (convertBrickColortoColor((int)numericUpDown6.Value) != Color.Empty)
            {
                Properties.Settings.Default.TorsoColor = (int)numericUpDown6.Value;
                Properties.Settings.Default.Save();
                TorsoPanel.BackColor = convertBrickColortoColor((int)numericUpDown6.Value);
            }
            else
            {
                Properties.Settings.Default.TorsoColor = 194;
                Properties.Settings.Default.Save();
                TorsoPanel.BackColor = convertBrickColortoColor(194);
            }
            setupAvatarOnServer();
        }

        private void textBox5_Leave(object sender, EventArgs e)
        {
            Properties.Settings.Default.username = textBox5.Text;
            Properties.Settings.Default.Save();
        }

        private void textBox4_Leave(object sender, EventArgs e)
        {
            Properties.Settings.Default.UserId = int.Parse(textBox4.Text);
            Properties.Settings.Default.Save();
        }

        private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (onetime == false)
            {
                e.Graphics.FillRectangle(new SolidBrush(this.BackColor), new Rectangle(0, 0, 547, 412));
                onetime = true;
            }
            TabPage page = tabControl1.TabPages[e.Index];
            e.Graphics.FillRectangle(new SolidBrush(page.BackColor), e.Bounds);

            Rectangle paddedBounds = e.Bounds;
            int yOffset = (e.State == DrawItemState.Selected) ? -2 : 1;
            paddedBounds.Offset(1, yOffset);
            TextRenderer.DrawText(e.Graphics, page.Text, e.Font, paddedBounds, page.ForeColor);
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (Properties.Settings.Default.DiscordRPC)
                {
                    if (tabControl1.SelectedTab == tabControl1.TabPages[2])
                    {
                        client.SetPresence(new RichPresence()
                        {
                            Details = "v" + Properties.Settings.Default.version,
                            State = "Customizing character",
                            Assets = new Assets()
                            {
                                LargeImageKey = "rebloxicon"
                            },
                            Timestamps = client.CurrentPresence.HasTimestamps() ? client.CurrentPresence.Timestamps : Timestamps.Now
                        });
                    }
                    else if (tabControl1.SelectedTab == tabControl1.TabPages[3])
                    {
                        client.SetPresence(new RichPresence()
                        {
                            Details = "v" + Properties.Settings.Default.version,
                            State = "Managing asset packs",
                            Assets = new Assets()
                            {
                                LargeImageKey = "rebloxicon"
                            },
                            Timestamps = client.CurrentPresence.HasTimestamps() ? client.CurrentPresence.Timestamps : Timestamps.Now
                        });
                    }
                    else if (tabControl1.SelectedTab == tabControl1.TabPages[4])
                    {
                        client.SetPresence(new RichPresence()
                        {
                            Details = "v" + Properties.Settings.Default.version,
                            State = "Looking for LAN Servers",
                            Assets = new Assets()
                            {
                                LargeImageKey = "rebloxicon"
                            },
                            Timestamps = client.CurrentPresence.HasTimestamps() ? client.CurrentPresence.Timestamps : Timestamps.Now
                        });
                    }
                    else if (tabControl1.SelectedTab == tabControl1.TabPages[5])
                    {
                        client.SetPresence(new RichPresence()
                        {
                            Details = "v" + Properties.Settings.Default.version,
                            State = "Managing settings",
                            Assets = new Assets()
                            {
                                LargeImageKey = "rebloxicon"
                            },
                            Timestamps = client.CurrentPresence.HasTimestamps() ? client.CurrentPresence.Timestamps : Timestamps.Now
                        });
                    }
                    else if (tabControl1.SelectedTab == tabControl1.TabPages[6])
                    {
                        client.SetPresence(new RichPresence()
                        {
                            Details = "v" + Properties.Settings.Default.version,
                            State = "Managing RBDF settings",
                            Assets = new Assets()
                            {
                                LargeImageKey = "rebloxicon"
                            },
                            Timestamps = client.CurrentPresence.HasTimestamps() ? client.CurrentPresence.Timestamps : Timestamps.Now
                        });
                    }
                    else
                    {

                        client.SetPresence(new RichPresence()
                        {
                            Details = "v" + Properties.Settings.Default.version,
                            State = "Viewing " + tabControl1.SelectedTab.Text.ToLower(),
                            Assets = new Assets()
                            {
                                LargeImageKey = "rebloxicon"
                            },
                            Timestamps = client.CurrentPresence.HasTimestamps() ? client.CurrentPresence.Timestamps : Timestamps.Now
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("<ERROR> " + ex.Message + "\nStack Trace: " + ex.StackTrace);
            }
            if (tabControl1.SelectedTab == tabControl1.TabPages[4])
            {
                if (searchingNetwork == false)
                {
                    listView1.Items.Clear();
                    button8.Enabled = false;
                    searchingNetwork = true;
                    Thread thread = new Thread(() => SearchNetworks());
                    thread.IsBackground = true;
                    thread.TrySetApartmentState(ApartmentState.STA);
                    thread.Start();
                }
            }
            tabControl1.Invalidate();
            onetime = false;
        }

        private void tabControl1_Validating(object sender, CancelEventArgs e)
        {
            onetime = false;
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
           // tabControl1.Invalidate();
           // onetime = false;
        }

        private void Form1_Deactivate(object sender, EventArgs e)
        {
           //tabControl1.Invalidate();
           // onetime = false;
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.EnableDataStore = checkBox6.Checked;
            Properties.Settings.Default.Save();
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.EnableBadges = checkBox8.Checked;
            Properties.Settings.Default.Save();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.ChatStyle = comboBox2.Text;
            Properties.Settings.Default.Save();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (WineDetector.IsRunningOnWine() == false)
            {
                tabControl1.Invalidate();
                onetime = false;
            }
        }

        private void textBox6_Enter(object sender, EventArgs e)
        {
            if (textBox6.ForeColor == Color.FromArgb(200,200,200))
            {
                textBox6.Text = "";
                textBox6.ForeColor = Color.FromArgb(0, 0, 0);
            }
        }

        private void textBox6_Leave(object sender, EventArgs e)
        {
            if (textBox6.Text.Trim() == "")
            {
                textBox6.ForeColor = Color.FromArgb(200, 200, 200);
                textBox6.Text = "Press enter to add it to the list";
            }
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
           if (listBox4.Items.Count > 0)
            {
                if (listBox4.SelectedIndex != -1)
                {
                    if (Properties.Settings.Default.ClothesArray.Contains(listBox4.GetItemText(listBox4.SelectedItem)))
                    {
                            string[] arraycheck = Properties.Settings.Default.ClothesArray.Split('|');
                            if (arraycheck.Length > 1)
                            {
                            if (Properties.Settings.Default.ClothesArray.EndsWith("|" + listBox4.GetItemText(listBox4.SelectedItem))) 
                            {
                                Properties.Settings.Default.ClothesArray = Properties.Settings.Default.ClothesArray.Remove(Properties.Settings.Default.ClothesArray.Length - ("|" + listBox4.GetItemText(listBox4.SelectedItem)).Length, ("|" + listBox4.GetItemText(listBox4.SelectedItem)).Length);
                            }
                            else if (Properties.Settings.Default.ClothesArray.StartsWith(listBox4.GetItemText(listBox4.SelectedItem)))
                            {
                                Properties.Settings.Default.ClothesArray = Properties.Settings.Default.ClothesArray.Remove(0, (listBox4.GetItemText(listBox4.SelectedItem) + "|").Length);
                            }
                            else
                            {
                                Properties.Settings.Default.ClothesArray = Properties.Settings.Default.ClothesArray.Replace("|" + listBox4.GetItemText(listBox4.SelectedItem) + "|", "|");
                            }
                                Properties.Settings.Default.Save();
                                button12.Enabled = true;
                                button13.Enabled = false;
                            }
                            else if (arraycheck.Length == 1 || arraycheck[0] == listBox4.GetItemText(listBox4.SelectedItem))
                            {
                                Properties.Settings.Default.ClothesArray = Properties.Settings.Default.ClothesArray.Replace(listBox4.GetItemText(listBox4.SelectedItem), "");
                                Properties.Settings.Default.Save();
                                button12.Enabled = true;
                                button13.Enabled = false;
                            }
                    }
                    listBox4.Items.RemoveAt(listBox4.SelectedIndex);
                    setupAvatarOnServer();
                }
            }
           else
            {
                MessageBox.Show("You don't have any clothes equipped on you.", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBox6_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void textBox6_KeyDown(object sender, KeyEventArgs e)
        {
             if (e.KeyCode == Keys.Enter) 
            {
                if (textBox6.Text.Trim().Length > 0) 
                {
                    if (listBox4.Items.Contains(textBox6.Text.Trim()))
                    {
                        MessageBox.Show("You already have that accessory/clothing on your avatar!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        if (Regex.IsMatch(textBox6.Text, @"^\d+$"))
                        {
                            if (Properties.Settings.Default.ClothesArray.Contains(textBox6.Text) == false)
                            {
                                if (Properties.Settings.Default.ClothesArray.Length > 0)
                                {
                                    Properties.Settings.Default.ClothesArray = Properties.Settings.Default.ClothesArray + "|" + textBox6.Text;
                                    Properties.Settings.Default.Save();
                                }
                                else
                                {
                                    Properties.Settings.Default.ClothesArray = Properties.Settings.Default.ClothesArray + textBox6.Text;
                                    Properties.Settings.Default.Save();
                                }
                            }
                            listBox4.Items.Add(textBox6.Text);
                            textBox6.Text = "";
                            setupAvatarOnServer();
                        }
                        else
                        {
                            MessageBox.Show("This is a invalid asset id!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (IsNodeFromAppRunning() && WineDetector.IsRunningOnWine() == false)
            {
                var result = MessageBox.Show("Are you sure you wanna close the launcher while the server is running? If you proceed, you may need to kill the server via Task Manager. (Pressing No will kill the server before closing the launcher)", "ReBlox", MessageBoxButtons.YesNoCancel,MessageBoxIcon.Exclamation);
                if (result == DialogResult.Yes)
                {
                    Console.WriteLine("<INFO> Ending logging session");
                    try
                    {
                        Console.SetOut(oldOut);
                        writer.Close();
                        ostrm.Close();
                    }
                    catch
                    {
                        //whatever
                    }
                }
                else if (result == DialogResult.No)
                {
                    Process[] processes = Process.GetProcessesByName("node");
                    if (processes.Length > 0)
                    {
                        foreach (Process process in processes)
                        {
                            if (process.MainModule.FileName == datafolder + @"\tools\node\node.exe")
                            {
                                Console.WriteLine("<INFO> Shutting down the node server");
                                statusText.Invoke(new Action(() => { statusText.Text = "Closing node server..."; }));
                                process.Kill();
                            }
                        }
                    }
                    processes = Process.GetProcessesByName("IPForwarder");
                    if (processes.Length > 0)
                    {
                        foreach (Process process in processes)
                        {
                            Console.WriteLine("<INFO> Shutting down IPForwarder");
                            statusText.Invoke(new Action(() => { statusText.Text = "Closing IPForwarder..."; }));
                            process.Kill();
                        }
                    }

                    ClearAssets();
                    RemoveGameFiles();

                    Console.WriteLine("<INFO> Ending logging session");
                    try
                    {
                        Console.SetOut(oldOut);
                        writer.Close();
                        ostrm.Close();
                    }
                    catch
                    {
                        //whatever
                    }
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                Console.WriteLine("<INFO> Ending logging session");
                try
                {
                    Console.SetOut(oldOut);
                    writer.Close();
                    ostrm.Close();
                }
                catch
                {
                    //whatever
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                ServerUtils.StopListServer();
                timer.Stop();
                if (client.IsDisposed == false) client.Dispose();
            }
            catch { }
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.DiscordRPC = checkBox7.Checked;
            Properties.Settings.Default.Save();
            if (checkBox7.Checked == false)
            {
                try
                {
                    timer.Stop();
                    client.Deinitialize();
                }
                catch { }
            }
            else
            {
                try
                {
                    client.Initialize();

                    client.SetPresence(new RichPresence()
                    {
                        Details = "v" + Properties.Settings.Default.version,
                        State = "Managing settings",
                        Assets = new Assets()
                        {
                            LargeImageKey = "rebloxicon"
                        }
                    });
                    timer.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("<ERROR> " + ex.Message + "\nStack Trace: " + ex.StackTrace);
                    MessageBox.Show("Unable to initialize the Discord RPC! Make sure Discord is installed and running.", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.EnableFollowing = checkBox9.Checked;
            Properties.Settings.Default.Save();
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\ca.pem"))
            {
                if (IsAdministrator())
                {
                    X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                    X509Certificate2 x509 = new X509Certificate2(X509Certificate2.CreateFromCertFile(Path.GetDirectoryName(Application.ExecutablePath) + @"\ca.pem"));
                    var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, x509.Thumbprint, false);
                    Console.WriteLine("<INFO> Installing the CA Certificate");
                    store.Close();
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(x509);
                    store.Close();
                    x509.Dispose();
                    caInstalled = true;
                    button5.Visible = false;
                    Properties.Settings.Default.CADontShow = false;
                    Properties.Settings.Default.Save();
                }
                else
                {
                    ProcessStartInfo ps = new ProcessStartInfo();
                    ps.UseShellExecute = true;
                    ps.FileName = Application.ExecutablePath;
                    ps.Verb = "runas";
                    Process.Start(ps);
                    Application.Exit();
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                int currentImage = int.Parse(Path.GetFileNameWithoutExtension(pictureBox1.ImageLocation));
                if (File.Exists(datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\images\" + (currentImage + 1) + ".png") == false) currentImage = 0;
                if (File.Exists(datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\images\" + (currentImage + 1) + ".png"))
                {
                    pictureBox1.ImageLocation = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\images\" + (currentImage + 1) + ".png";
                    pictureBox1.Image = Image.FromFile(pictureBox1.ImageLocation = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\images\" + (currentImage + 1) + ".png");
                }
            }
            catch
            {
                  
            }
        }

        private void TorsoPanel_Click(object sender, EventArgs e)
        {
            ColorPicker picker = new ColorPicker(this);
            picker.Left = Cursor.Position.X;
            picker.Top = Cursor.Position.Y;
            picker.ShowDialog();
            if (picker.DialogResult == DialogResult.OK)
            {
                Properties.Settings.Default.TorsoColor = resultBrickColor;
                TorsoPanel.BackColor = convertBrickColortoColor(resultBrickColor);
                numericUpDown6.Value = resultBrickColor;
                Properties.Settings.Default.Save();
            }
        }

        private void LeftArmPanel_Click(object sender, EventArgs e)
        {
            ColorPicker picker = new ColorPicker(this);
            picker.Left = Cursor.Position.X;
            picker.Top = Cursor.Position.Y;
            picker.ShowDialog();
            if (picker.DialogResult == DialogResult.OK)
            {
                Properties.Settings.Default.LeftArmColor = resultBrickColor;
                LeftArmPanel.BackColor = convertBrickColortoColor(resultBrickColor);
                numericUpDown2.Value = resultBrickColor;
                Properties.Settings.Default.Save();
            }
        }

        private void RightArmPanel_Click(object sender, EventArgs e)
        {
            ColorPicker picker = new ColorPicker(this);
            picker.Left = Cursor.Position.X;
            picker.Top = Cursor.Position.Y;
            picker.ShowDialog();
            if (picker.DialogResult == DialogResult.OK)
            {
                Properties.Settings.Default.RightArmColor = resultBrickColor;
                RightArmPanel.BackColor = convertBrickColortoColor(resultBrickColor);
                numericUpDown4.Value = resultBrickColor;
                Properties.Settings.Default.Save();
            }
        }

        private void HeadPanel_Click(object sender, EventArgs e)
        {
            ColorPicker picker = new ColorPicker(this);
            picker.Left = Cursor.Position.X;
            picker.Top = Cursor.Position.Y;
            picker.ShowDialog();
            if (picker.DialogResult == DialogResult.OK)
            {
                Properties.Settings.Default.HeadColor = resultBrickColor;
                HeadPanel.BackColor = convertBrickColortoColor(resultBrickColor);
                numericUpDown1.Value = resultBrickColor;
                Properties.Settings.Default.Save();
            }
        }

        private void RightLegPanel_Click(object sender, EventArgs e)
        {
            ColorPicker picker = new ColorPicker(this);
            picker.Left = Cursor.Position.X;
            picker.Top = Cursor.Position.Y;
            picker.ShowDialog();
            if (picker.DialogResult == DialogResult.OK)
            {
                Properties.Settings.Default.RightLegColor = resultBrickColor;
                RightLegPanel.BackColor = convertBrickColortoColor(resultBrickColor);
                numericUpDown5.Value = resultBrickColor;
                Properties.Settings.Default.Save();
            }
        }

        private void LeftLegPanel_Click(object sender, EventArgs e)
        {
            ColorPicker picker = new ColorPicker(this);
            picker.Left = Cursor.Position.X;
            picker.Top = Cursor.Position.Y;
            picker.ShowDialog();
            if (picker.DialogResult == DialogResult.OK)
            {
                Properties.Settings.Default.LeftLegColor = resultBrickColor;
                LeftLegPanel.BackColor = convertBrickColortoColor(resultBrickColor);
                numericUpDown3.Value = resultBrickColor;
                Properties.Settings.Default.Save();
            }
        }

        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.assetFromServer = checkBox10.Checked;
            Properties.Settings.Default.Save();
        }

        private void listBox2_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void listBox2_DragDrop(object sender, DragEventArgs e)
        {
            string[] maps = ((string[])e.Data.GetData(DataFormats.FileDrop));
            foreach (string map in maps)
            {
                if (map.EndsWith(".rbxl") || map.EndsWith(".rbxlx"))
                {
                    if (File.Exists(datafolder + @"\maps\" + Path.GetFileName(map)))
                    {
                        if (MessageBox.Show(Path.GetFileName(map) + " already exists in the maps folder, wanna replace it with the new one?", "ReBlox", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        {
                            File.Copy(map, datafolder + @"\maps\" + Path.GetFileName(map),true);
                        }
                    }
                    else
                    {
                        File.Copy(map, datafolder + @"\maps\" + Path.GetFileName(map));
                    }
                }
                else
                {
                    MessageBox.Show(Path.GetFileName(map) + " is not a valid map!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            listBox2.BeginUpdate();
            listBox2.Items.Clear();
            string[] directories2 = Directory.GetFiles(datafolder + @"\maps");
            foreach (string directory in directories2)
            {
                if (directory.EndsWith(".rbxl") || directory.EndsWith(".rbxlx"))
                {
                    listBox2.Items.Add(Path.GetFileName(directory));
                    if (Properties.Settings.Default.lastselectedmap == Path.GetFileName(directory))
                    {
                        listBox2.SetSelected(listBox2.Items.Count - 1, true);
                    }
                }
            }
            directories2 = null;
            listBox2.EndUpdate();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            if (launchershortcut == true)
            {
                this.Hide();
            }
        }

        private async void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                object shDesktop = (object)"Desktop";
                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                string shortcutAddress = (string)shell.SpecialFolders.Item(ref shDesktop) + @"\" + (useNewRoblox ? "Roblox Studio" : "ROBLOX Studio") + " - " + listBox1.GetItemText(listBox1.SelectedItem) + ".lnk";
                IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutAddress);
                shortcut.TargetPath = Application.ExecutablePath;
                shortcut.Arguments = "-launch " + listBox1.GetItemText(listBox1.SelectedItem);
                shortcut.IconLocation = datafolder + @"\clients\" + listBox1.GetItemText(listBox1.SelectedItem) + @"\Studio\RobloxStudioBeta.exe";
                shortcut.WorkingDirectory = Directory.GetCurrentDirectory();
                shortcut.Save();
                statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                statusText.Invoke(new Action(() => { statusText.Text = "Shortcut created for " + listBox1.GetItemText(listBox1.SelectedItem); }));
                await Task.Delay(3000);
                statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                statusText.Invoke(new Action(() => { statusText.Text = ""; }));
            }
            else if (e.Control && e.KeyCode == Keys.R) 
            {
                listBox1.BeginUpdate();
                listBox1.Items.Clear();
                string[] directories = null;
                if (Directory.Exists(datafolder + @"\clients")) directories = Directory.GetDirectories(datafolder + @"\clients");
                Console.WriteLine("<INFO> Adding and sorting clients to the list");
                if (Directory.Exists(datafolder + @"\clients") && directories != null && directories.Length > 0)
                {
                    for (int i = 0; i < directories.Length; i++)
                    {
                        if (directories[i].EndsWith("L") && directories[i + 1] != null && directories[i + 1].EndsWith("M"))
                        {
                            listBox1.Items.Add(Path.GetFileName(directories[i + 1]));
                            listBox1.Items.Add(Path.GetFileName(directories[i]));
                            i++;
                        }
                        else
                        {
                            listBox1.Items.Add(Path.GetFileName(directories[i]));
                        }
                    }
                    if (listBox1.Items.IndexOf(Properties.Settings.Default.lastselectedversion) > -1)
                    {
                        listBox1.SetSelected(listBox1.Items.IndexOf(Properties.Settings.Default.lastselectedversion), true);
                    }
                }
                else
                {
                    listBox1.Visible = false;
                    panel1.Visible = false;
                    label30.Visible = true;
                }
                listBox1.EndUpdate();
            }
        }
        public class AssetPackItem
        {
            public AssetPackItem(Color c, string p) 
            {
                ItemColor = c;
                AssetPackName = p;
            }
            public string AssetPackName { get; set; }
            public Color ItemColor { get; set; }
        }
        private void listBox3_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (previousIndexAsset > listBox3.Items.Count - 1)
            {
                previousIndexAsset = -1;
            }
            if (previousIndexAsset > -1) if (listBox3.GetSelected(previousIndexAsset)) e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(listBox3.BackColor.R + 26, listBox3.BackColor.G + 26, listBox3.BackColor.B + 26)), new Rectangle(0, previousIndexAsset * listBox3.ItemHeight, listBox3.Width, listBox3.ItemHeight)); else e.Graphics.FillRectangle(new SolidBrush(listBox3.BackColor), new Rectangle(0, previousIndexAsset * listBox3.ItemHeight, listBox3.Width, listBox3.ItemHeight));
            if (listBox3.GetSelected(e.Index)) e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(listBox3.BackColor.R + 26, listBox3.BackColor.G + 26, listBox3.BackColor.B + 26)), new Rectangle(0, e.Index * listBox3.ItemHeight, listBox3.Width, listBox3.ItemHeight)); else e.Graphics.FillRectangle(new SolidBrush(listBox3.BackColor), new Rectangle(0, e.Index * listBox3.ItemHeight, listBox3.Width, listBox3.ItemHeight));
            AssetPackItem item = listBox3.Items[e.Index] as AssetPackItem;
            if (item != null)
            {
                e.Graphics.DrawString(item.AssetPackName, listBox3.Font, new SolidBrush(item.ItemColor), 0, e.Index * listBox3.ItemHeight);
                if (previousIndexAsset > -1 && previousIndexAsset != e.Index) if (listBox3.Items[previousIndexAsset] as AssetPackItem != null) e.Graphics.DrawString((listBox3.Items[previousIndexAsset] as AssetPackItem).AssetPackName, listBox3.Font, new SolidBrush((listBox3.Items[previousIndexAsset] as AssetPackItem).ItemColor), 0, previousIndexAsset * listBox3.ItemHeight); else e.Graphics.DrawString(listBox3.Items[previousIndexAsset].ToString(), listBox3.Font, new SolidBrush(Color.White), 0, previousIndexAsset * listBox3.ItemHeight);
            }
            else
            {
                string newItem = listBox3.Items[e.Index].ToString();
                e.Graphics.DrawString(newItem, listBox3.Font, new SolidBrush(Color.White), 0, e.Index * listBox3.ItemHeight);
                if (previousIndexAsset > -1 && previousIndexAsset != e.Index) if (listBox3.Items[previousIndexAsset] as AssetPackItem != null) e.Graphics.DrawString((listBox3.Items[previousIndexAsset] as AssetPackItem).AssetPackName, listBox3.Font, new SolidBrush((listBox3.Items[previousIndexAsset] as AssetPackItem).ItemColor), 0, previousIndexAsset * listBox3.ItemHeight); else e.Graphics.DrawString(listBox3.Items[previousIndexAsset].ToString(), listBox3.Font, new SolidBrush(Color.White), 0, previousIndexAsset * listBox3.ItemHeight);
            }
            previousIndexAsset = listBox3.SelectedIndex;
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Membership = comboBox3.Text;
            Properties.Settings.Default.Save();
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            Process.Start(new WebClient().DownloadString(updateurl + "/ updatelink.txt"));
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control == true && e.KeyCode == Keys.U)
            {
                Thread thread = new Thread(async () =>
                {
                    if (CheckForInternetConnection())
                    {
                        try
                        {
                            WebClient client = new WebClient();
                            Console.WriteLine("<INFO> Checking the version of the client and the server reports...");
                            if (client.DownloadString(updateurl + @"/version.txt") != Properties.Settings.Default.version)
                            {
                                label33.Invoke(new Action(() => { label33.Text = "A new version is available!"; }));
                                label33.Invoke(new Action(() => { label33.Visible = true; }));
                                button6.Invoke(new Action(() => { button6.Visible = true; }));
                                Console.WriteLine("<INFO> Update is available for the ReBlox Launcher!");
                            }
                            else
                            {
                                label33.Invoke(new Action(() => { label33.Text = "This version is up to date!"; }));
                                label33.Invoke(new Action(() => { label33.Visible = true; }));
                                button6.Invoke(new Action(() => { button6.Visible = false; }));
                                Console.WriteLine("<INFO> No update is available.");
                                await Task.Delay(3000);
                                label33.Invoke(new Action(() => { label33.Visible = false; }));
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("<ERROR> Something when wrong while trying to check for updates! Look in the error for details!\r\n" + e);
                        }
                    }
                    else
                    {
                        label33.Invoke(new Action(() => { label33.Text = "Please check your internet."; }));
                        label33.Invoke(new Action(() => { label33.Visible = true; }));
                        button6.Invoke(new Action(() => { button6.Visible = false; }));
                        Console.WriteLine("<INFO> No internet detected.");
                        await Task.Delay(3000);
                        label33.Invoke(new Action(() => { label33.Visible = false; }));
                    }
                });
                thread.TrySetApartmentState(ApartmentState.STA);
                thread.Start();
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count > 0) 
            {
                button8.Enabled = true;
            }
            else
            {
                button8.Enabled = false;
            }
        }

        private void button8_Click_1(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                isJoiningOrStudio = true;
                Thread thread = new Thread(async () =>
                {
                    try
                    {
                        if (Directory.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Player"))
                        {
                            if (File.ReadAllText(@"C:\Windows\System32\drivers\etc\hosts").Contains("\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip") == false && WineDetector.IsRunningOnWine() == false)
                            {
                                if (MessageBox.Show("Wanna edit the hosts file to make sure that decals loads? This is recommended for best experience!\n\nIf you wanna load assets, provide your .ROBLOSECURITY in the settings section and enable Use auth for loading assets. (This will not conflict with your latest Roblox Studio)", "hosts File Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                {
                                    if (IsAdministrator())
                                    {


                                        File.AppendAllText(@"C:\Windows\System32\drivers\etc\hosts", "\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip");
                                        statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                        button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                        button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                        button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                        if (UseJoinJSONLink) SetupJoinScript(listView1.SelectedItems[0].SubItems[5].Text, int.Parse(listView1.SelectedItems[0].SubItems[4].Text));
                                        SetupGameFiles();
                                        LoadAssets();
                                        ProcessStartInfo ps = new ProcessStartInfo();
                                        ps.UseShellExecute = false;
                                        ps.CreateNoWindow = false;
                                        listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + (Directory.Exists(datafolder + @"\clients\" + listView1.SelectedItems[0].SubItems[0].Text) ? listView1.SelectedItems[0].SubItems[0].Text : listBox1.GetItemText(listBox1.SelectedItem)) + @"\Player\RobloxPlayerBeta.exe"; }));
                                        this.Invoke(new Action(() => { if (UseJoinJSONLink == true) ps.Arguments = joinargument; }));
                                        ProcessStartInfo ps1 = new ProcessStartInfo();
                                        ps1.UseShellExecute = false;
                                        ps1.FileName = datafolder + @"\tools\node\node.exe";
                                        ps1.RedirectStandardOutput = true;
                                        ps1.CreateNoWindow = true;
                                        if (listView1.SelectedItems[0].SubItems[5].Text != "localhost" && listView1.SelectedItems[0].SubItems[5].Text != "127.0.0.1")
                                        {
                                            this.Invoke(new Action(() =>
                                            {
                                                if (Properties.Settings.Default.useAuth)
                                                {
                                                    if (Properties.Settings.Default.avatarR15)
                                                    {
                                                        if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    }
                                                    else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                else
                                                {
                                                    if (Properties.Settings.Default.avatarR15)
                                                    {
                                                        if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    }
                                                    else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                            }));
                                        }
                                        else
                                        {
                                            if (Properties.Settings.Default.useAuth)
                                            {
                                                if (Properties.Settings.Default.avatarR15)
                                                {
                                                    if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                            else
                                            {
                                                if (Properties.Settings.Default.avatarR15)
                                                {
                                                    if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                        }
                                        ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                        ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                        ProcessStartInfo ps2 = new ProcessStartInfo();
                                        ps2.FileName = datafolder + @"\tools\IPForwarder.exe";
                                        ps2.Arguments = "-ip " + listView1.SelectedItems[0].SubItems[5].Text + " -port " + listView1.SelectedItems[0].SubItems[4].Text;
                                        if (Properties.Settings.Default.ShowConsole == false) ps2.WindowStyle = ProcessWindowStyle.Hidden;
                                        if (textBox5.Text.Length > 2 && textBox4.Text.Length > 0)
                                        {
                                            if (useIPForwarder == true && listView1.SelectedItems[0].SubItems[5].Text != "localhost" && listView1.SelectedItems[0].SubItems[5].Text != "127.0.0.1" || useIPForwarder == true && listView1.SelectedItems[0].SubItems[4].Text != "53640") Process.Start(ps2);
                                            if (IsNodeFromAppRunning() == false)
                                            {
                                                Process test = Process.Start(ps1);
                                                button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                statusText.Invoke(new Action(() => { statusText.Text = "Waiting for node server to start..."; }));
                                                test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                                {
                                                    if (e1.Data != null)
                                                    {
                                                        if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                        {

                                                            setupAvatarOnServer();

                                                            statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                            Process roblox = Process.Start(ps);
                                                            roblox.EnableRaisingEvents = true;
                                                            roblox.Exited += Roblox_Exited;
                                                            test.CancelOutputRead();
                                                            await Task.Delay(3000);
                                                            statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                            statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                        }
                                                    }
                                                });
                                                test.BeginOutputReadLine();
                                            }
                                            else
                                            {

                                                setupAvatarOnServer();
                                                statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                Process roblox = Process.Start(ps);
                                                roblox.EnableRaisingEvents = true;
                                                roblox.Exited += Roblox_Exited;
                                                await Task.Delay(3000);
                                                statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                            }
                                        }
                                        else
                                        {
                                            MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }
                                    }
                                    else
                                    {
                                        ProcessStartInfo ps = new ProcessStartInfo();
                                        ps.UseShellExecute = true;
                                        ps.FileName = Application.ExecutablePath;
                                        ps.Verb = "runas";
                                        Process.Start(ps);
                                        Application.Exit();
                                    }
                                }
                                else
                                {

                                    MessageBox.Show("Joining on Mid-2017 and older clients requires changing your hosts file!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error); ;
                                }
                            }
                            else
                            {
                                statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                if (UseJoinJSONLink) SetupJoinScript(listView1.SelectedItems[0].SubItems[5].Text, int.Parse(listView1.SelectedItems[0].SubItems[4].Text));
                                SetupGameFiles();
                                LoadAssets();
                                ProcessStartInfo ps = new ProcessStartInfo();
                                ps.UseShellExecute = false;
                                ps.CreateNoWindow = false;
                                listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + (Directory.Exists(datafolder + @"\clients\" + listView1.SelectedItems[0].SubItems[0].Text) ? listView1.SelectedItems[0].SubItems[0].Text : listBox1.GetItemText(listBox1.SelectedItem)) + @"\Player\RobloxPlayerBeta.exe"; }));
                                this.Invoke(new Action(() => { if (UseJoinJSONLink == true) ps.Arguments = joinargument; }));
                                ProcessStartInfo ps1 = new ProcessStartInfo();
                                ps1.UseShellExecute = false;
                                ps1.RedirectStandardOutput = true;
                                ps1.CreateNoWindow = true;
                                ps1.FileName = datafolder + @"\tools\node\node.exe";
                                if (listView1.SelectedItems[0].SubItems[5].Text != "localhost" && listView1.SelectedItems[0].SubItems[5].Text != "127.0.0.1")
                                {
                                    this.Invoke(new Action(() =>
                                    {
                                        if (Properties.Settings.Default.useAuth)
                                        {
                                            if (Properties.Settings.Default.avatarR15)
                                            {
                                                if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                            else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        }
                                        else
                                        {
                                            if (Properties.Settings.Default.avatarR15)
                                            {
                                                if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining";
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining";
                                            }
                                            else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining";
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining";
                                        }
                                    }));

                                }
                                else
                                {
                                    if (Properties.Settings.Default.useAuth)
                                    {
                                        if (Properties.Settings.Default.avatarR15)
                                        {
                                            if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        }
                                        else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                    }
                                    else
                                    {
                                        if (Properties.Settings.Default.avatarR15)
                                        {
                                            if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "]";
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "]";
                                        }
                                        else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "]";
                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "]";
                                    }
                                }
                                ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                ProcessStartInfo ps2 = new ProcessStartInfo();
                                ps2.FileName = datafolder + @"\tools\IPForwarder.exe";
                                this.Invoke(new Action(() => { ps2.Arguments = "-ip " + listView1.SelectedItems[0].SubItems[5].Text + " -port " + listView1.SelectedItems[0].SubItems[4].Text; }));
                                if (Properties.Settings.Default.ShowConsole == false) ps2.WindowStyle = ProcessWindowStyle.Hidden;
                                if (textBox5.Text.Length > 2 && textBox4.Text.Length > 0)
                                {
                                    if (useIPForwarder == true && listView1.SelectedItems[0].SubItems[5].Text != "localhost" && listView1.SelectedItems[0].SubItems[5].Text != "127.0.0.1" || useIPForwarder == true && listView1.SelectedItems[0].SubItems[4].Text != "53640") Process.Start(ps2);
                                    if (IsNodeFromAppRunning() == false)
                                    {
                                        button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                        button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                        button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                        Process test = Process.Start(ps1);
                                        statusText.Invoke(new Action(() => { statusText.Text = "Waiting for node server to start..."; }));
                                        test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                        {
                                            if (e1.Data != null)
                                            {
                                                if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                {

                                                    setupAvatarOnServer();

                                                    statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                    Process roblox = Process.Start(ps);
                                                    roblox.EnableRaisingEvents = true;
                                                    roblox.Exited += Roblox_Exited;
                                                    test.CancelOutputRead();
                                                    await Task.Delay(3000);
                                                    statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                    statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                }
                                            }
                                        });
                                        test.BeginOutputReadLine();
                                    }
                                    else
                                    {
                                        setupAvatarOnServer();
                                        statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                        button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                        button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                        button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                        Process roblox = Process.Start(ps);
                                        roblox.EnableRaisingEvents = true;
                                        roblox.Exited += Roblox_Exited;
                                        await Task.Delay(3000);
                                        statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                        statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                    button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                    button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                    await Task.Delay(3000);
                                    statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                    statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                }
                            }
                        }
                        else
                        {
                            if (File.ReadAllText(@"C:\Windows\System32\drivers\etc\hosts").Contains("\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip") == false && WineDetector.IsRunningOnWine() == false)
                            {
                                if (MessageBox.Show("Wanna edit the hosts file to make sure that decals loads? This is recommended for best experience!\n\nIf you wanna load assets, provide your .ROBLOSECURITY in the settings section and enable Use auth for loading assets. (This will not conflict with your latest Roblox Studio)", "hosts File Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                {
                                    if (IsAdministrator())
                                    {


                                        File.AppendAllText(@"C:\Windows\System32\drivers\etc\hosts", "\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip");
                                        statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                        button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                        button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                        button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                        if (UseJoinJSONLink) SetupJoinScript(listView1.SelectedItems[0].SubItems[5].Text, int.Parse(listView1.SelectedItems[0].SubItems[4].Text));
                                        SetupGameFiles();
                                        LoadAssets();
                                        ProcessStartInfo ps = new ProcessStartInfo();
                                        ps.UseShellExecute = false;
                                        ps.CreateNoWindow = false;
                                        listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + (Directory.Exists(datafolder + @"\clients\" + listView1.SelectedItems[0].SubItems[0].Text) ? listView1.SelectedItems[0].SubItems[0].Text : listBox1.GetItemText(listBox1.SelectedItem)) + @"\Studio\RobloxStudioBeta.exe"; }));
                                        this.Invoke(new Action(() => { if (UseJoinJSONLink == true) ps.Arguments = joinargument; else ps.Arguments = joinargument + " -server " + listView1.SelectedItems[0].SubItems[5].Text + " -port " + listView1.SelectedItems[0].SubItems[4].Text + " -placeId 1 -universeId 2"; }));
                                        ProcessStartInfo ps1 = new ProcessStartInfo();
                                        ps1.UseShellExecute = false;
                                        ps1.FileName = datafolder + @"\tools\node\node.exe";
                                        ps1.RedirectStandardOutput = true;
                                        ps1.CreateNoWindow = true;
                                        if (listView1.SelectedItems[0].SubItems[5].Text != "localhost" && listView1.SelectedItems[0].SubItems[5].Text != "127.0.0.1")
                                        {
                                            this.Invoke(new Action(() =>
                                            {
                                                if (Properties.Settings.Default.useAuth)
                                                {
                                                    if (Properties.Settings.Default.avatarR15)
                                                    {
                                                        if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    }
                                                    else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                else
                                                {
                                                    if (Properties.Settings.Default.avatarR15)
                                                    {
                                                        if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    }
                                                    else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                            }));
                                        }
                                        else
                                        {
                                            if (Properties.Settings.Default.useAuth)
                                            {
                                                if (Properties.Settings.Default.avatarR15)
                                                {
                                                    if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                            else
                                            {
                                                if (Properties.Settings.Default.avatarR15)
                                                {
                                                    if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                }
                                                else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                        }
                                        ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                        ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                        ProcessStartInfo ps2 = new ProcessStartInfo();
                                        ps2.FileName = datafolder + @"\tools\IPForwarder.exe";
                                        ps2.Arguments = "-ip " + listView1.SelectedItems[0].SubItems[5].Text + " -port " + listView1.SelectedItems[0].SubItems[4].Text;
                                        if (Properties.Settings.Default.ShowConsole == false) ps2.WindowStyle = ProcessWindowStyle.Hidden;
                                        if (textBox5.Text.Length > 2 && textBox4.Text.Length > 0)
                                        {
                                            if (useIPForwarder == true && listView1.SelectedItems[0].SubItems[5].Text != "localhost" && listView1.SelectedItems[0].SubItems[5].Text != "127.0.0.1" || useIPForwarder == true && listView1.SelectedItems[0].SubItems[4].Text != "53640") Process.Start(ps2);
                                            if (IsNodeFromAppRunning() == false)
                                            {
                                                Process test = Process.Start(ps1);
                                                button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                statusText.Invoke(new Action(() => { statusText.Text = "Waiting for node server to start..."; }));
                                                test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                                {
                                                    if (e1.Data != null)
                                                    {
                                                        if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                        {

                                                            setupAvatarOnServer();

                                                            statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                            Process roblox = Process.Start(ps);
                                                            roblox.EnableRaisingEvents = true;
                                                            roblox.Exited += Roblox_Exited;
                                                            test.CancelOutputRead();
                                                            await Task.Delay(3000);
                                                            statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                            statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                        }
                                                    }
                                                });
                                                test.BeginOutputReadLine();
                                            }
                                            else
                                            {

                                                setupAvatarOnServer();
                                                statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                                button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                                button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                                Process roblox = Process.Start(ps);
                                                roblox.EnableRaisingEvents = true;
                                                roblox.Exited += Roblox_Exited;
                                                await Task.Delay(3000);
                                                statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                            }
                                        }
                                        else
                                        {
                                            MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }
                                    }
                                    else
                                    {
                                        ProcessStartInfo ps = new ProcessStartInfo();
                                        ps.UseShellExecute = true;
                                        ps.FileName = Application.ExecutablePath;
                                        ps.Verb = "runas";
                                        Process.Start(ps);
                                        Application.Exit();
                                    }
                                }
                                else
                                {
                                    statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                    ProcessStartInfo ps = new ProcessStartInfo();
                                    listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + (Directory.Exists(datafolder + @"\clients\" + listView1.SelectedItems[0].SubItems[0].Text) ? listView1.SelectedItems[0].SubItems[0].Text : listBox1.GetItemText(listBox1.SelectedItem)) + @"\Studio\RobloxStudioBeta.exe"; }));
                                    this.Invoke(new Action(() => { ps.Arguments = joinargument + " -server " + listView1.SelectedItems[0].SubItems[5].Text + " -port " + listView1.SelectedItems[0].SubItems[4].Text + " -placeId 1 -universeId 2"; }));
                                    ProcessStartInfo ps2 = new ProcessStartInfo();
                                    ps2.FileName = datafolder + @"\tools\IPForwarder.exe";
                                    ps2.Arguments = "-ip " + listView1.SelectedItems[0].SubItems[5].Text + " -port " + listView1.SelectedItems[0].SubItems[4].Text;
                                    if (Properties.Settings.Default.ShowConsole == false) ps2.WindowStyle = ProcessWindowStyle.Hidden;
                                    statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                    if (UseJoinJSONLink == false)
                                    {
                                        if (useIPForwarder == true && listView1.SelectedItems[0].SubItems[5].Text != "localhost" && listView1.SelectedItems[0].SubItems[5].Text != "127.0.0.1" || useIPForwarder == true && listView1.SelectedItems[0].SubItems[4].Text != "53640") Process.Start(ps2);
                                        Process.Start(ps);
                                    }
                                    else
                                    {
                                        MessageBox.Show("Joining on Mid-2017 and older clients requires changing your hosts file!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                    await Task.Delay(3000);
                                    statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                    statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                }
                            }
                            else
                            {
                                statusText.Invoke(new Action(() => { statusText.Visible = true; }));
                                button1.Invoke(new Action(() => { button1.Enabled = false; }));
                                button2.Invoke(new Action(() => { button2.Enabled = false; }));
                                button3.Invoke(new Action(() => { button3.Enabled = false; }));
                                if (UseJoinJSONLink) SetupJoinScript(listView1.SelectedItems[0].SubItems[5].Text, int.Parse(listView1.SelectedItems[0].SubItems[4].Text));
                                SetupGameFiles();
                                LoadAssets();
                                ProcessStartInfo ps = new ProcessStartInfo();
                                ps.UseShellExecute = false;
                                ps.CreateNoWindow = false;
                                listBox1.Invoke(new Action(() => { ps.FileName = datafolder + @"\clients\" + (Directory.Exists(datafolder + @"\clients\" + listView1.SelectedItems[0].SubItems[0].Text) ? listView1.SelectedItems[0].SubItems[0].Text : listBox1.GetItemText(listBox1.SelectedItem)) + @"\Studio\RobloxStudioBeta.exe"; }));
                                this.Invoke(new Action(() => { if (UseJoinJSONLink == true) ps.Arguments = joinargument; else ps.Arguments = joinargument + " -server " + listView1.SelectedItems[0].SubItems[5].Text + " -port " + listView1.SelectedItems[0].SubItems[4].Text + " -placeId 1 -universeId 2"; }));
                                ProcessStartInfo ps1 = new ProcessStartInfo();
                                ps1.UseShellExecute = false;
                                ps1.RedirectStandardOutput = true;
                                ps1.CreateNoWindow = true;
                                ps1.FileName = datafolder + @"\tools\node\node.exe";
                                if (listView1.SelectedItems[0].SubItems[5].Text != "localhost" && listView1.SelectedItems[0].SubItems[5].Text != "127.0.0.1")
                                {
                                    this.Invoke(new Action(() =>
                                    {
                                        if (Properties.Settings.Default.useAuth)
                                        {
                                            if (Properties.Settings.Default.avatarR15)
                                            {
                                                if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            }
                                            else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        }
                                        else
                                        {
                                            if (Properties.Settings.Default.avatarR15)
                                            {
                                                if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining";
                                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining";
                                            }
                                            else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining";
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -ip=\"" + listView1.SelectedItems[0].SubItems[5].Text + "\" -joining";
                                        }
                                    }));

                                }
                                else
                                {
                                    if (Properties.Settings.Default.useAuth)
                                    {
                                        if (Properties.Settings.Default.avatarR15)
                                        {
                                            if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        }
                                        else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -ROBLOSECURITY=" + Encoding.UTF8.GetString(Convert.FromBase64String(Properties.Settings.Default.ROBLOSECURITY)) + " -useAuth -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer " : "") + (useOldSignature ? "-disableNewSignature " : "") + (useOldAssetFormat ? "-disableNewSignatureAsset" : "");
                                    }
                                    else
                                    {
                                        if (Properties.Settings.Default.avatarR15)
                                        {
                                            if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "]";
                                            else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "]";
                                        }
                                        else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "]";
                                        else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "]";
                                    }
                                }
                                ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                ProcessStartInfo ps2 = new ProcessStartInfo();
                                ps2.FileName = datafolder + @"\tools\IPForwarder.exe";
                                this.Invoke(new Action(() => { ps2.Arguments = "-ip " + listView1.SelectedItems[0].SubItems[5].Text + " -port " + listView1.SelectedItems[0].SubItems[4].Text; }));
                                if (Properties.Settings.Default.ShowConsole == false) ps2.WindowStyle = ProcessWindowStyle.Hidden;
                                if (textBox5.Text.Length > 2 && textBox4.Text.Length > 0)
                                {
                                    if (useIPForwarder == true && listView1.SelectedItems[0].SubItems[5].Text != "localhost" && listView1.SelectedItems[0].SubItems[5].Text != "127.0.0.1" || useIPForwarder == true && listView1.SelectedItems[0].SubItems[4].Text != "53640") Process.Start(ps2);
                                    if (IsNodeFromAppRunning() == false)
                                    {
                                        button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                        button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                        button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                        Process test = Process.Start(ps1);
                                        statusText.Invoke(new Action(() => { statusText.Text = "Waiting for node server to start..."; }));
                                        test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                        {
                                            if (e1.Data != null)
                                            {
                                                if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                {

                                                    setupAvatarOnServer();

                                                    statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                    Process roblox = Process.Start(ps);
                                                    roblox.EnableRaisingEvents = true;
                                                    roblox.Exited += Roblox_Exited;
                                                    test.CancelOutputRead();
                                                    await Task.Delay(3000);
                                                    statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                                    statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                                }
                                            }
                                        });
                                        test.BeginOutputReadLine();
                                    }
                                    else
                                    {
                                        setupAvatarOnServer();
                                        statusText.Invoke(new Action(() => { statusText.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                        button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                        button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                        button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                        Process roblox = Process.Start(ps);
                                        roblox.EnableRaisingEvents = true;
                                        roblox.Exited += Roblox_Exited;
                                        await Task.Delay(3000);
                                        statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                        statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    button1.Invoke(new Action(() => { button1.Enabled = true; }));
                                    button2.Invoke(new Action(() => { button2.Enabled = true; }));
                                    button3.Invoke(new Action(() => { button3.Enabled = true; }));
                                    await Task.Delay(3000);
                                    statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                                    statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                                }
                            }
                        }
                    }
                    catch (Exception e1)
                    {
                        MessageBox.Show("Something went wrong while trying to launch Studio/Player, please look in the error message for details: " + e1.Message, "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Console.WriteLine("<ERROR> Something went wrong when attempting to launch Studio/Player! Please look in the error below and report it to the developer if it's launcher-sided!\n" + e1.Message + "\nStack Trace:\n" + e1.StackTrace);
                        await Task.Delay(3000);
                        statusText.Invoke(new Action(() => { statusText.Visible = false; }));
                        statusText.Invoke(new Action(() => { statusText.Text = ""; }));
                    }
                });
                thread.TrySetApartmentState(ApartmentState.MTA);
                thread.Start();
            }
            else
            {
                MessageBox.Show("A client must be selected to be able to run!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button11_Click_1(object sender, EventArgs e)
        {
            button8.Enabled = false;
            listView1.Items.Clear();
            if (searchingNetwork == false)
            {
                searchingNetwork = true;
                Thread thread = new Thread(() => SearchNetworks());
                thread.IsBackground = true;
                thread.TrySetApartmentState(ApartmentState.STA);
                thread.Start();
            }
            else
            {
                searchingNetwork = true;
            }
        }

        private void listView1_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            e.NewWidth = this.listView1.Columns[e.ColumnIndex].Width;
            e.Cancel = true;
        }

        private void listBox2_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (listBox2.Items.Count > 0) {
                if (previousIndexMap > listBox2.Items.Count - 1)
                {
                    previousIndexMap = -1;
                }
                if (previousIndexMap > -1) if (listBox2.GetSelected(previousIndexMap)) e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(listBox2.BackColor.R + 26, listBox2.BackColor.G + 26, listBox2.BackColor.B + 26)), new Rectangle(0, previousIndexMap * listBox2.ItemHeight, listBox2.Width, listBox2.ItemHeight)); else e.Graphics.FillRectangle(new SolidBrush(listBox2.BackColor), new Rectangle(0, previousIndexMap * listBox2.ItemHeight, listBox2.Width, listBox2.ItemHeight));
            if (listBox2.GetSelected(e.Index)) e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(listBox2.BackColor.R + 26, listBox2.BackColor.G + 26, listBox2.BackColor.B + 26)), new Rectangle(0, e.Index * listBox2.ItemHeight, listBox2.Width, listBox2.ItemHeight)); else e.Graphics.FillRectangle(new SolidBrush(listBox2.BackColor), new Rectangle(0, e.Index * listBox2.ItemHeight, listBox2.Width, listBox2.ItemHeight));
            AssetPackItem item = listBox2.Items[e.Index] as AssetPackItem;
            if (item != null)
            {
                e.Graphics.DrawString(item.AssetPackName, listBox2.Font, new SolidBrush(item.ItemColor), 0, e.Index * listBox2.ItemHeight);
                if (previousIndexMap > -1 && previousIndexMap != e.Index) if (listBox2.Items[previousIndexMap] as AssetPackItem != null) e.Graphics.DrawString((listBox2.Items[previousIndexMap] as AssetPackItem).AssetPackName, listBox2.Font, new SolidBrush((listBox2.Items[previousIndexMap] as AssetPackItem).ItemColor), 0, previousIndexMap * listBox2.ItemHeight); else e.Graphics.DrawString(listBox2.Items[previousIndexMap].ToString(), listBox2.Font, new SolidBrush(Color.White), 0, previousIndexMap * listBox2.ItemHeight);
            }
            else
            {
                string newItem = listBox2.Items[e.Index].ToString();
                e.Graphics.DrawString(newItem, listBox2.Font, new SolidBrush(Color.White), 0, e.Index * listBox2.ItemHeight);
                if (previousIndexMap > -1 && previousIndexMap != e.Index) if (listBox2.Items[previousIndexMap] as AssetPackItem != null) e.Graphics.DrawString((listBox2.Items[previousIndexMap] as AssetPackItem).AssetPackName, listBox2.Font, new SolidBrush((listBox2.Items[previousIndexMap] as AssetPackItem).ItemColor), 0, previousIndexMap * listBox2.ItemHeight); else e.Graphics.DrawString(listBox2.Items[previousIndexMap].ToString(), listBox2.Font, new SolidBrush(Color.White), 0, previousIndexMap * listBox2.ItemHeight);
            }
            previousIndexMap = listBox2.SelectedIndex;
			}
        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (listBox1.Items.Count > 0) {
                if (previousIndexClient > listBox1.Items.Count - 1)
                {
                    previousIndexClient = -1;
                }
                if (previousIndexClient > -1) if (listBox1.GetSelected(previousIndexClient)) e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(listBox1.BackColor.R + 26, listBox1.BackColor.G + 26, listBox1.BackColor.B + 26)), new Rectangle(0, previousIndexClient * listBox1.ItemHeight, listBox1.Width, listBox1.ItemHeight)); else e.Graphics.FillRectangle(new SolidBrush(listBox1.BackColor), new Rectangle(0, previousIndexClient * listBox1.ItemHeight, listBox1.Width, listBox1.ItemHeight));
            if (listBox1.GetSelected(e.Index)) e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(listBox1.BackColor.R + 26, listBox1.BackColor.G + 26, listBox1.BackColor.B + 26)), new Rectangle(0, e.Index * listBox1.ItemHeight, listBox1.Width, listBox1.ItemHeight)); else e.Graphics.FillRectangle(new SolidBrush(listBox1.BackColor), new Rectangle(0, e.Index * listBox1.ItemHeight, listBox1.Width, listBox1.ItemHeight));
            AssetPackItem item = listBox1.Items[e.Index] as AssetPackItem;
            if (item != null)
            {
                e.Graphics.DrawString(item.AssetPackName, listBox1.Font, new SolidBrush(item.ItemColor), 0, e.Index * listBox1.ItemHeight);
                if (previousIndexClient > -1 && previousIndexClient != e.Index) if (listBox1.Items[previousIndexClient] as AssetPackItem != null) e.Graphics.DrawString((listBox1.Items[previousIndexClient] as AssetPackItem).AssetPackName, listBox1.Font, new SolidBrush((listBox1.Items[previousIndexClient] as AssetPackItem).ItemColor), 0, previousIndexClient * listBox1.ItemHeight); else e.Graphics.DrawString(listBox1.Items[previousIndexClient].ToString(), listBox1.Font, new SolidBrush(Color.White), 0, previousIndexClient * listBox1.ItemHeight);
            }
            else
            {
                string newItem = listBox1.Items[e.Index].ToString();
                e.Graphics.DrawString(newItem, listBox1.Font, new SolidBrush(Color.White), 0, e.Index * listBox1.ItemHeight);
                if (previousIndexClient > -1 && previousIndexClient != e.Index) if (listBox1.Items[previousIndexClient] as AssetPackItem != null) e.Graphics.DrawString((listBox1.Items[previousIndexClient] as AssetPackItem).AssetPackName, listBox1.Font, new SolidBrush((listBox1.Items[previousIndexClient] as AssetPackItem).ItemColor), 0, previousIndexClient * listBox1.ItemHeight); else e.Graphics.DrawString(listBox1.Items[previousIndexClient].ToString(), listBox1.Font, new SolidBrush(Color.White), 0, previousIndexClient * listBox1.ItemHeight);
            }
            previousIndexClient = listBox1.SelectedIndex;
			}
        }

        private void listBox4_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (listBox4.Items.Count > 0) {
                if (e.Index <= listBox4.Items.Count - 1)
                {
                    if (previousIndexAvatar > listBox4.Items.Count - 1) 
                    {
                        previousIndexAvatar = -1;
                    }
                        if (previousIndexAvatar > -1) if (listBox4.GetSelected(previousIndexAvatar)) e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(listBox4.BackColor.R + 26, listBox4.BackColor.G + 26, listBox4.BackColor.B + 26)), new Rectangle(0, previousIndexAvatar * listBox4.ItemHeight, listBox4.Width, listBox4.ItemHeight)); else e.Graphics.FillRectangle(new SolidBrush(listBox4.BackColor), new Rectangle(0, previousIndexAvatar * listBox4.ItemHeight, listBox4.Width, listBox4.ItemHeight));
                    if (listBox4.GetSelected(e.Index)) e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(listBox4.BackColor.R + 26, listBox4.BackColor.G + 26, listBox4.BackColor.B + 26)), new Rectangle(0, e.Index * listBox4.ItemHeight, listBox4.Width, listBox4.ItemHeight)); else e.Graphics.FillRectangle(new SolidBrush(listBox4.BackColor), new Rectangle(0, e.Index * listBox4.ItemHeight, listBox4.Width, listBox4.ItemHeight));
                    AssetPackItem item = listBox4.Items[e.Index] as AssetPackItem;
                    if (item != null)
                    {
                        e.Graphics.DrawString(item.AssetPackName, listBox4.Font, new SolidBrush(item.ItemColor), 0, e.Index * listBox4.ItemHeight);
                        if (previousIndexAvatar > -1 && previousIndexAvatar != e.Index) if (listBox4.Items[previousIndexAvatar] as AssetPackItem != null) e.Graphics.DrawString((listBox4.Items[previousIndexAvatar] as AssetPackItem).AssetPackName, listBox4.Font, new SolidBrush((listBox4.Items[previousIndexAvatar] as AssetPackItem).ItemColor), 0, previousIndexAvatar * listBox4.ItemHeight); else e.Graphics.DrawString(listBox4.Items[previousIndexAvatar].ToString(), listBox4.Font, new SolidBrush(Color.White), 0, previousIndexAvatar * listBox4.ItemHeight);
                    }
                    else
                    {
                        string newItem = listBox4.Items[e.Index].ToString();
                        e.Graphics.DrawString(newItem, listBox4.Font, new SolidBrush(Color.White), 0, e.Index * listBox4.ItemHeight);
                        if (previousIndexAvatar > -1 && previousIndexAvatar != e.Index) if (listBox4.Items[previousIndexAvatar] as AssetPackItem != null) e.Graphics.DrawString((listBox4.Items[previousIndexAvatar] as AssetPackItem).AssetPackName, listBox4.Font, new SolidBrush((listBox4.Items[previousIndexAvatar] as AssetPackItem).ItemColor), 0, previousIndexAvatar * listBox4.ItemHeight); else e.Graphics.DrawString(listBox4.Items[previousIndexAvatar].ToString(), listBox4.Font, new SolidBrush(Color.White), 0, previousIndexAvatar * listBox4.ItemHeight);
                    }
                    previousIndexAvatar = listBox4.SelectedIndex;
                }
                else 
                {
                    previousIndexAvatar = -1;
                }
			}
        }
    }
}