using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReBloxLauncher
{
    public partial class LaunchScreen : Form
    {
        string robloxversion = "";
        string datafolder = Path.GetDirectoryName(Application.ExecutablePath) + @"\data";
        string hostargument = "";
        string joinargument = "";
        bool UseJoinJSONLink = false;
        bool useIPForwarder = false;
        bool ReserveAssetIdForMap = false;
        int placeid = 1;
        private object syncLock = new object();
        bool shutdown = false;
        bool useNewRoblox = false;
        bool useOldSignature = false;
        bool useOldAssetFormat = false;
        bool dontLoadMapinArgument = false;
        public LaunchScreen(string version, string customDataFolder = null)
        {
            InitializeComponent();
            robloxversion = version;
            if (customDataFolder != null)
            {
                datafolder = customDataFolder;
            }
        }

        private string GenerateUUID()
        {
            lock (syncLock)
            {
                return Guid.NewGuid().ToString();
            }
        }
        private void SetupJoinScript(string ipaddr, int port)
        {
            Console.WriteLine("<INFO> Setting up join script for " + Properties.Settings.Default.lastselectedversion);
            label1.Invoke(new Action(() => { label1.Text = "Setting up join script..."; }));
            string waitingForCharacterGuid = GenerateUUID().ToLower();
            string sessionId = GenerateUUID().ToLower();
            if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt")) File.Delete(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt");
            File.WriteAllText(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt", @"--rbxsig%PwqWEjzB5MasktbXBzHRjH8kwI5ltVM/lIAaQnwpristI8AQsHzKQGJGBrne9l2OFJLiao7SFrJ86rwehnjlHbBC63KBr4ihUTE2EHFHaWde95xF1Jb37jJ3wg1uJRDEXq9lZWCcYsz/2HOIYRMddAytnxX4ZLBig6mfLfcrOWA=%
{""ClientPort"":0,""MachineAddress"":""" + ipaddr + @""",""ServerPort"":" + port.ToString() + @",""PingUrl"":"""",""PingInterval"":120,""UserName"":""" + Properties.Settings.Default.username + @""",""SeleniumTestMode"":false,""UserId"":" + Properties.Settings.Default.UserId + @",""SuperSafeChat"":false,""CharacterAppearance"":""http://assetgame.reblox.zip/Asset/CharacterFetch.ashx?userId=" + Properties.Settings.Default.UserId + @"&placeId=1"",""ClientTicket"":""" + DateTime.UtcNow.ToString("G") + @";h0eeFX/hZrNHXjP01PeaXT8dA8yVZbGKSMR6omd818fXJwuc/RceXUA8EJwdlfn7IWDfqjF2e22EhFyPXhucHqxQjY3GQd+zPAfS7KfQzItRVIFnjXbfWEGPKKFFEP4QcTs9Q141sd3G83ye9ZdGbOXPjy9VwpdvEnFToarYX7Q=;TCtJG0d2d0pFaHYnHDzJQttKfZlZyHZmcRtUNcy9vyivgiwQtB/illTbHvaUc/9w+oy8XRi+giLEvwuRmRttGKKnpA5Qt7dwCyXz2UIzt5/8TSJYqIKT99iPjBg0/PQFmguI7LoSk1KfElEDwzCWGT3tryAiT7S7a1SjInteSAU="",""GameId"":""00000000-0000-0000-0000-000000000000"",""PlaceId"":" + (ReserveAssetIdForMap ? placeid : 1) + @",""MeasurementUrl"":"""",""WaitingForCharacterGuid"":""" + waitingForCharacterGuid + @""",""BaseUrl"":""http://www.reblox.zip/"",""ChatStyle"":""" + Properties.Settings.Default.ChatStyle + @""",""VendorId"":0,""ScreenShotInfo"":"""",""VideoInfo"":""<?xml version=\""1.0\""?><entry xmlns=\""http://www.w3.org/2005/Atom\"" xmlns:media=\""http://search.yahoo.com/mrss/\"" xmlns:yt=\""http://gdata.youtube.com/schemas/2007\""><media:group><media:title type=\""plain\""><![CDATA[ROBLOX Place]]></media:title><media:description type=\""plain\""><![CDATA[ For more games visit http://www.roblox.com]]></media:description><media:category scheme=\""http://gdata.youtube.com/schemas/2007/categories.cat\"">Games</media:category><media:keywords>ROBLOX, video, free game, online virtual world</media:keywords></media:group></entry>"",""CreatorId"":1,""CreatorTypeEnum"":""User"",""MembershipType"":""None"",""AccountAge"":365,""CookieStoreFirstTimePlayKey"":""rbx_evt_ftp"",""CookieStoreFiveMinutePlayKey"":""rbx_evt_fmp"",""CookieStoreEnabled"":true,""IsRobloxPlace"":false,""GenerateTeleportJoin"":false,""IsUnknownOrUnder13"":" + (!Properties.Settings.Default.AccountOver13).ToString().ToLower() + @",""SessionId"":""" + sessionId + @"|00000000-0000-0000-0000-000000000000|0|204.236.226.210|8|" + DateTime.UtcNow.ToString("") + @"Z|0|null|null|null|null"",""DataCenterId"":0,""UniverseId"":2,""BrowserTrackerId"":0,""UsePortraitMode"":false,""FollowUserId"":0,""characterAppearanceId"":0}");
        }

        private void SetupGameFiles()
        {
            label1.Invoke(new Action(() => { label1.Text = "Setting up game files..."; }));
            Console.WriteLine("<INFO> Checking for FFlags for " + Properties.Settings.Default.lastselectedversion);
            if (Directory.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings"))
            {
                Console.WriteLine("<INFO> Copying the ClientAppSettings.json to the RobloxAssetFixer");
                if (File.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json"))
                {
                    File.Copy(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\Studio\ClientSettings\ClientAppSettings.json", datafolder + @"\tools\RobloxAssetFixer\ClientAppSettings.json", true);
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
                        if (shutdown) return;
                        if (Path.GetFileName(file) == "join.ashx")
                        {
                            if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file))) File.Delete(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file));
                            File.WriteAllText(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file), File.ReadAllText(file).Replace("{ip}", "127.0.0.1").Replace("{port}", "53640").Replace("{username}", Properties.Settings.Default.username).Replace("{id}", Properties.Settings.Default.UserId.ToString()).Replace("{13}", (!Properties.Settings.Default.AccountOver13).ToString().ToLower()));
                        }
                        else if (Path.GetFileName(file) == "gameserver.ashx")
                        {
                            if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file))) File.Delete(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file));
                            File.WriteAllText(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file), File.ReadAllText(file).Replace("{ip}", "127.0.0.1").Replace("{port}", "53640").Replace("{username}", Properties.Settings.Default.username).Replace("{id}", Properties.Settings.Default.UserId.ToString()).Replace("{13}", (!Properties.Settings.Default.AccountOver13).ToString().ToLower()));
                        }
                        else if (Path.GetFileName(file) == "placespecificscript.ashx")
                        {
                            if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file))) File.Delete(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file));
                            File.WriteAllText(datafolder + @"\tools\RobloxAssetFixer\game\" + Path.GetFileName(file), File.ReadAllText(file).Replace("{id}", placeid.ToString()));
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
            Console.WriteLine("<INFO> Cleaning up game files and removing the ClientAppSettings file");
            if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\ClientAppSettings.json")) File.Delete(datafolder + @"\tools\RobloxAssetFixer\ClientAppSettings.json");

            if (Directory.Exists(datafolder + @"\tools\RobloxAssetFixer\clothes") == true) Directory.Delete(datafolder + @"\tools\RobloxAssetFixer\clothes", true);

            if (UseJoinJSONLink == true)
            {
                if (Directory.Exists(datafolder + @"\tools\RobloxAssetFixer\game") == true) Directory.Delete(datafolder + @"\tools\RobloxAssetFixer\game", true);
                if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt")) File.Delete(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt");
            }
        }

        private void ClearAssets()
        {
            string[] files = Directory.GetFiles(datafolder + @"\tools\RobloxAssetFixer\assets");
            foreach (string file in files)
            {
                if (file.EndsWith("avatar.png") == false && file.EndsWith("gameicon.png") == false && file.EndsWith("headshot.png") == false)
                {
                    File.Delete(file);
                }
            }
        }

        private void Roblox_Exited(object sender, EventArgs e)
        {
            Process[] processesr = Process.GetProcessesByName("RobloxStudioBeta");
            if (processesr.Length <= 0)
            {
                Process[] processes = Process.GetProcessesByName("node");
                if (processes.Length > 0)
                {
                    foreach (Process process in processes)
                    {
                        if (process.MainModule.FileName == datafolder + @"\tools\node\node.exe")
                        {
                            Console.WriteLine("<INFO> Shutting down the node server");
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
                        process.Kill();
                    }
                }

                if (Properties.Settings.Default.ClearTemp)
                {
                    Console.WriteLine("<INFO> Clearing ROBLOX's temporary files");
                    if (Directory.Exists(Path.GetTempPath() + "Roblox")) Directory.Delete(Path.GetTempPath() + "Roblox", true);
                }
                RemoveGameFiles();
                ClearAssets();
                Application.Exit();
            }
        }

        private int CountAssets() 
        {
            label1.Invoke(new Action(() => { label1.Text = "Loading assets..."; }));
            string[] splited = Properties.Settings.Default.AssetPackEnabled.Split('|');
            int count = 0;
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
                            else
                            {
                                compatible = false; break;
                            }
                        }
                    }
                    if (compatible)
                    {
                        string[] files = Directory.GetFiles(s);
                        count = count + (files.Length - 1);
                    }

                }
            }
            return count;
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
            progressBar1.Invoke(new Action(() => { progressBar1.Style = ProgressBarStyle.Continuous; }));
            progressBar1.Invoke(new Action(() => { progressBar1.Value = 0; }));
            progressBar1.Invoke(new Action(() => { progressBar1.Maximum = CountAssets(); }));
            label1.Invoke(new Action(() => { label1.Text = "Loading assets..."; }));
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
                                }
                                catch
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
                                }
                                catch
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
                                File.Copy(file, datafolder + @"\tools\RobloxAssetFixer\assets\" + Path.GetFileName(file), true);
                                progressBar1.Invoke(new Action(() => { progressBar1.Value++; }));
                                if (shutdown) return;
                            }
                        }
                    }

                }
                if (shutdown) return;
            }
            progressBar1.Invoke(new Action(() => { progressBar1.Style = ProgressBarStyle.Marquee; }));
            progressBar1.Invoke(new Action(() => { progressBar1.Value = 0; }));
            progressBar1.Invoke(new Action(() => { progressBar1.Maximum = 100; }));
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
            if (IsNodeFromAppRunning())
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
                }
                catch
                {
                    // do nothing
                }
            }
        }
        public bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator);
        }
        private void LaunchScreen_Load(object sender, EventArgs e)
        {
            if (File.Exists(datafolder + @"\clients\" + robloxversion + @"\ReBlox.ini"))
            {
                string[] config = File.ReadAllLines(datafolder + @"\clients\" + robloxversion + @"\ReBlox.ini");
                if (config[0] == "[Reblox]")
                {
                    placeid = -1;
                    UseJoinJSONLink = false;
                    useIPForwarder = false;
                    ReserveAssetIdForMap = false;
                    useNewRoblox = false;
                    useOldAssetFormat = false;
                    useOldSignature = false;
                    dontLoadMapinArgument = false;

                    for (int i = 0; i < config.Length; i++)
                    {
                        if (config[i].Trim().StartsWith("JoinArgument=\""))
                        {
                            string[] splited = config[i].Trim().Split(new char[] { '=' }, 2);
                            joinargument = splited[1].Remove(0, 1).Remove(splited[1].Length - 2, 1);
                        }
                        else if (config[i].Trim().StartsWith("HostArgument=\""))
                        {
                            string[] splited = config[i].Trim().Split(new char[] { '=' }, 2);
                            hostargument = splited[1].Remove(0, 1).Remove(splited[1].Length - 2, 1);
                        }
                        else if (config[i].Trim() == "UseJoinScript=true")
                        {
                            UseJoinJSONLink = true;
                        }
                        else if (config[i].Trim() == "UseJoinScript=false")
                        {
                            UseJoinJSONLink = false;
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
                                Application.Exit();
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
                        else if (config[i].Trim() == "DontLoadMapfromArgument=true")
                        {
                            dontLoadMapinArgument = true;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Config file failed to load!", "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                }

            }
            this.Text = (useNewRoblox ? "Roblox Studio" : "ROBLOX Studio");
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

                                File.AppendAllText(@"C:\Windows\System32\drivers\etc\hosts", "\r\n127.0.0.1 reblox.zip\r\n127.0.0.1 www.reblox.zip\r\n127.0.0.1 api.reblox.zip\r\n127.0.0.1 assetgame.reblox.zip\r\n127.0.0.1 auth.reblox.zip\r\n127.0.0.1 assetdelivery.reblox.zip\r\n127.0.0.1 develop.reblox.zip\r\n127.0.0.1 clientsettings.api.reblox.zip\r\n127.0.0.1 gamepersistence.reblox.zip\r\n127.0.0.1 avatar.reblox.zip\r\n127.0.0.1 thumbnails.reblox.zip\r\n127.0.0.1 groups.reblox.zip\r\n127.0.0.1 clientsettingscdn.reblox.zip\r\n127.0.0.1 catalog.reblox.zip\r\n127.0.0.1 apis.reblox.zip\r\n127.0.0.1 games.reblox.zip\r\n127.0.0.1 friends.reblox.zip\r\n127.0.0.1 economy.reblox.zip\r\n127.0.0.1 badges.reblox.zip\r\n127.0.0.1 users.reblox.zip");
                                if (Properties.Settings.Default.UsePatchInStudio)
                                {
                                    if (UseJoinJSONLink) SetupJoinScript("127.0.0.1", 53640);
                                    SetupGameFiles();
                                    LoadAssets();
                                }
                                if (shutdown == false)
                                {
                                    button1.Invoke(new Action(() => { button1.Visible = false; }));
                                    ProcessStartInfo ps = new ProcessStartInfo();
                                    ps.UseShellExecute = false;
                                    ps.CreateNoWindow = false;
                                    ps.FileName = datafolder + @"\clients\" + robloxversion + @"\Studio\RobloxStudioBeta.exe";
                                    ProcessStartInfo ps1 = new ProcessStartInfo();
                                    ps1.UseShellExecute = false;
                                    ps1.FileName = datafolder + @"\tools\node\node.exe";
                                    ps1.RedirectStandardOutput = true;
                                    ps1.CreateNoWindow = true;
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
                                    ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";
                                    ps1.WindowStyle = ProcessWindowStyle.Hidden;
                                    if (Properties.Settings.Default.username.Length > 2 && Properties.Settings.Default.UserId >= 0)
                                    {
                                        if (IsNodeFromAppRunning() == false)
                                        {
                                            Process test = Process.Start(ps1);
                                            label1.Invoke(new Action(() => { label1.Text = "Waiting for node server to start..."; }));
                                            test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                            {
                                                if (e1.Data != null)
                                                {
                                                    if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                    {
                                                        setupAvatarOnServer();
                                                        label1.Invoke(new Action(() => { label1.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                        Process roblox = Process.Start(ps);
                                                        roblox.EnableRaisingEvents = true;
                                                        roblox.Exited += Roblox_Exited;
                                                        test.CancelOutputRead();
                                                        await Task.Delay(5000);
                                                        this.Invoke(new Action(() => { this.Hide(); }));
                                                    }
                                                }
                                            });
                                            test.BeginOutputReadLine();
                                        }
                                        else
                                        {
                                            setupAvatarOnServer();
                                            button1.Invoke(new Action(() => { button1.Visible = false; }));
                                            label1.Invoke(new Action(() => { label1.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                            Process roblox = Process.Start(ps);
                                            roblox.EnableRaisingEvents = true;
                                            roblox.Exited += Roblox_Exited;
                                            await Task.Delay(5000);
                                            this.Invoke(new Action(() => { this.Hide(); }));
                                        }

                                    }
                                    else
                                    {
                                        MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
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
                            button1.Invoke(new Action(() => { button1.Visible = false; }));
                            label1.Invoke(new Action(() => { label1.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                            ProcessStartInfo ps = new ProcessStartInfo();
                            ps.FileName = datafolder + @"\clients\" + robloxversion + @"\Studio\RobloxStudioBeta.exe";
                            Process.Start(ps);
                            await Task.Delay(5000);
                            this.Invoke(new Action(() => { this.Hide(); }));
                        }
                    }
                    else
                    {
                        if (Properties.Settings.Default.UsePatchInStudio)
                        {
                            if (UseJoinJSONLink) SetupJoinScript("127.0.0.1", 53640);
                            SetupGameFiles();
                            LoadAssets();
                        }
                       if (shutdown == false)
                        {
                            button1.Invoke(new Action(() => { button1.Visible = false; }));
                            ProcessStartInfo ps = new ProcessStartInfo();
                            ps.UseShellExecute = false;
                            ps.CreateNoWindow = false;
                            ps.FileName = datafolder + @"\clients\" + robloxversion + @"\Studio\RobloxStudioBeta.exe";
                            ProcessStartInfo ps1 = new ProcessStartInfo();
                            ps1.UseShellExecute = false;
                            ps1.FileName = datafolder + @"\tools\node\node.exe";
                            ps1.RedirectStandardOutput = true;
                            ps1.CreateNoWindow = true;

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
                                    if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                                    else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -r15 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                                }
                                else if (Properties.Settings.Default.AccountOver13) ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                                else ps1.Arguments = datafolder + @"\tools\RobloxAssetFixer\index.js -username=" + Properties.Settings.Default.username + " -userid=" + Properties.Settings.Default.UserId + " -accountUnder13 -bodycolor=[" + Properties.Settings.Default.HeadColor + "," + Properties.Settings.Default.LeftArmColor + "," + Properties.Settings.Default.LeftLegColor + "," + Properties.Settings.Default.RightArmColor + "," + Properties.Settings.Default.RightLegColor + "," + Properties.Settings.Default.TorsoColor + "] -clothes=[" + Properties.Settings.Default.ClothesArray.Replace("|", ",") + "] " + (Properties.Settings.Default.EnableDataStore ? "" : "-disableDataStore ") + (Properties.Settings.Default.EnableBadges ? "" : "-disableBadges ") + (Properties.Settings.Default.EnableFollowing ? "" : "-disableFollowing ") + (Properties.Settings.Default.assetFromServer ? "-assetFromServer" : "");
                            }
                            ps1.WindowStyle = ProcessWindowStyle.Hidden;
                            ps1.WorkingDirectory = datafolder + @"\tools\RobloxAssetFixer";

                            if (Properties.Settings.Default.username.Length > 2 && Properties.Settings.Default.UserId >= 0)
                            {

                                if (Properties.Settings.Default.UsePatchInStudio)
                                {
                                    if (IsNodeFromAppRunning() == false)
                                    {
                                        Process test = Process.Start(ps1);

                                        label1.Invoke(new Action(() => { label1.Text = "Waiting for node server to start..."; }));
                                        test.OutputDataReceived += new DataReceivedEventHandler(async (object sender1, DataReceivedEventArgs e1) =>
                                        {
                                            if (e1.Data != null)
                                            {
                                                if (e1.Data.Contains("<INFO> Started a HTTP"))
                                                {
                                                    setupAvatarOnServer();
                                                    label1.Invoke(new Action(() => { label1.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                                    Process roblox = Process.Start(ps);
                                                    roblox.EnableRaisingEvents = true;
                                                    roblox.Exited += Roblox_Exited;
                                                    test.CancelOutputRead();
                                                    await Task.Delay(5000);
                                                    this.Invoke(new Action(() => { this.Hide(); }));
                                                }
                                            }
                                        });
                                        test.BeginOutputReadLine();
                                    }
                                    else
                                    {
                                        setupAvatarOnServer();
                                        label1.Invoke(new Action(() => { label1.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                        Process roblox = Process.Start(ps);
                                        roblox.EnableRaisingEvents = true;
                                        roblox.Exited += Roblox_Exited;
                                        await Task.Delay(5000);
                                        this.Invoke(new Action(() => { this.Hide(); }));
                                    }
                                }
                                else
                                {
                                    label1.Invoke(new Action(() => { label1.Text = (useNewRoblox ? "Starting Roblox..." : "Starting ROBLOX..."); }));
                                    Process roblox = Process.Start(ps);
                                    roblox.EnableRaisingEvents = true;
                                    roblox.Exited += Roblox_Exited;
                                    await Task.Delay(5000);
                                    this.Invoke(new Action(() => { this.Hide(); }));
                                }

                            }
                            else
                            {
                                MessageBox.Show("A valid username or UserId is required!", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }

                    }
                }
                catch (Exception e1)
                {
                    MessageBox.Show("Something went wrong while trying to launch Studio, please look in the error message for details: " + e1.Message, "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine("<ERROR> Something went wrong when attempting to launch Studio! Please look in the error below and report it to the developer if it's launcher-sided!\n" + e1.Message + "\nStack Trace:\n" + e1.StackTrace);
                }
            });
            thread.TrySetApartmentState(ApartmentState.MTA);
            thread.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ClearAssets();
            RemoveGameFiles();
            Application.Exit();
        }
    }
    }

