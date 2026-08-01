using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Security.Cryptography;
namespace ReBloxLauncher
{
    public partial class BenchmarkForm : Form
    {

        int milliseconds = 0;
        int seconds = 0;
        int minutes = 0;
        int totalAssets = 0;
        bool cancelAsset = false;
        bool isJoiningOrStudio = true;
        bool timer = false;
        Label labelChange = null;
        List<string> sha1assets = new List<string>();
        string datafolder = Path.GetDirectoryName(Application.ExecutablePath) + @"\data";

        public BenchmarkForm()
        {
            InitializeComponent();
        }

        //nope, you're not slowing down threads today.
        protected override void WndProc(ref Message message)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MOVE = 0xF010;

            switch (message.Msg)
            {
                case WM_SYSCOMMAND:
                    int command = message.WParam.ToInt32() & 0xfff0;
                    if (command == SC_MOVE)
                        return;
                    break;
            }

            base.WndProc(ref message);
        }

        private void BenchmarkForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void BenchmarkForm_Load(object sender, EventArgs e)
        {
            if (Directory.Exists(datafolder) == false)
            {
                if (MessageBox.Show("You can't run the benchmark without the data folder! Use -datafolder to select a data folder.", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK)
                {
                    this.Close();
                }
            }
        }

        //Tweens

        private float Lerp(float start, float end, float t)
        {
            return start + (end - start) * t;
        }

        private double easeInOutQuart(double x)
        {
            return x < 0.5 ? 8 * x * x * x * x : 1 - Math.Pow(-2 * x + 2, 4) / 2;
        }

        //Main

        private Label getAssetResultLabel(int num)
        {
            switch (num)
            {
                case 0:
                    return label5;
                case 1:
                    return label6;
                case 2:
                    return label7;
                case 3:
                    return label8;
                case 4:
                    return label9;
                case 5:
                    return label10;
                default:
                    return null;
            }
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

        private string getFileMD5(string filename)
        {
            if (File.Exists(filename))
            {
                using (FileStream fs = new FileStream(filename, FileMode.Open))
                {
                    using (BufferedStream bs = new BufferedStream(fs))
                    {
                        using (var md5 = MD5.Create())
                        {
                            byte[] hash = md5.ComputeHash(bs);
                            StringBuilder formatted = new StringBuilder(2 * hash.Length);
                            foreach (byte b in hash)
                            {
                                formatted.AppendFormat("{0:X2}", b);
                            }
                            return formatted.ToString();
                        }
                    }
                }
            }
            else
            {
                return "";
            }
        }

        bool IsDigitsOnly(string str) { return str.All(c => c >= '0' && c <= '9'); }

        private void LoadAssets(string client = null, bool builtin = false)
        {
            totalAssets = 0;
            string[] splited = builtin ? @".\data\assetpacks\Basic Roblox Assets|.\data\assetpacks\ReBlox Accessories (2016E-)|.\data\assetpacks\ReBlox Accessories (2016M+)|.\data\assetpacks\ReBlox Clothing|.\data\assetpacks\ReBlox Faces".Split('|') : Properties.Settings.Default.AssetPackEnabled.Split('|');
            foreach (string s in splited)
            {
                string parsedPath = "";
                if (s.StartsWith(".\\") && datafolder == Path.GetDirectoryName(Application.ExecutablePath) + @"\data")
                {
                    parsedPath = datafolder + s.Remove(0, 6);
                }
                else
                {
                    parsedPath = s;
                }
                if ((datafolder == Path.GetDirectoryName(Application.ExecutablePath) + @"\data" && Directory.Exists(parsedPath) && s.StartsWith(".\\")) || (datafolder != Path.GetDirectoryName(Application.ExecutablePath) + @"\data" && parsedPath.StartsWith(datafolder) && Directory.Exists(parsedPath)))
                {
                    bool compatible = true;
                    string[] config = File.ReadAllLines(parsedPath + @"\ReBlox.ini");
                    for (int i = 0; i < config.Length; i++)
                    {
                        if (config[i].Trim().StartsWith("Clients="))
                        {
                            if (client != null)
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
                                        if (version == client)
                                        {
                                            valuefound = true;
                                            compatible = true;
                                            break;
                                        }
                                    }
                                    if (valuefound == false) compatible = false; break;
                                }
                                else if (splited1[1] == client)
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
                                            string year1 = client.Substring(0, 4);
                                            string daterange1 = client.Substring(4, 1);
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
                                            Console.WriteLine("<WARN> Skipping asset pack \"" + Path.GetFileName(parsedPath) + "\" due to invalid clients");
                                            compatible = false; break;
                                        }
                                    }
                                    catch
                                    {
                                        Console.WriteLine("<WARN> Skipping asset pack \"" + Path.GetFileName(parsedPath) + "\" due to invalid clients");
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
                                            string year1 = client.Substring(0, 4);
                                            string daterange1 = client.Substring(4, 1);
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
                                            Console.WriteLine("<WARN> Skipping asset pack \"" + Path.GetFileName(parsedPath) + "\" due to invalid clients");
                                            compatible = false; break;
                                        }
                                    }
                                    catch
                                    {
                                        Console.WriteLine("<WARN> Skipping asset pack \"" + Path.GetFileName(parsedPath) + "\" due to invalid clients");
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
                                            Console.WriteLine("<WARN> Skipping asset pack \"" + Path.GetFileName(parsedPath) + "\" due to invalid clients");
                                            compatible = false; break;
                                        }
                                    }
                                    catch
                                    {
                                        Console.WriteLine("<WARN> Skipping asset pack \"" + Path.GetFileName(parsedPath) + "\" due to invalid clients");
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
                                            Console.WriteLine("<WARN> Skipping asset pack \"" + Path.GetFileName(parsedPath) + "\" due to invalid clients");
                                            compatible = false; break;
                                        }
                                    }
                                    catch
                                    {
                                        Console.WriteLine("<WARN> Skipping asset pack \"" + Path.GetFileName(parsedPath) + "\" due to invalid clients");
                                        compatible = false; break;
                                    }
                                }
                                else
                                {
                                    compatible = false; break;
                                }
                            }
                        }
                    }
                    if (compatible && cancelAsset == false)
                    {
                        string[] files = Directory.GetFiles(parsedPath);
                        for (int i = 0; i < files.Length; i++)
                        {
                            if (cancelAsset == true) return;
                            if (files[i].EndsWith(".ini") == false)
                            {
                                if (isJoiningOrStudio == true)
                                {
                                    if (IsDigitsOnly(Path.GetFileNameWithoutExtension(files[i])))
                                    {
                                        if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\assets\" + Path.GetFileName(files[i])))
                                        {
                                            if (i < sha1assets.Count)
                                            {
                                                if (getFileMD5(files[i]) != sha1assets[i])
                                                {
                                                    Console.WriteLine("<BENCHMARK> Loading " + files[i]);
                                                    File.Copy(files[i], datafolder + @"\tools\RobloxAssetFixer\assets\" + Path.GetFileName(files[i]), true);
                                                    sha1assets[i] = getFileMD5(files[i]);
                                                }
                                            }
                                            else
                                            {
                                                sha1assets.Add(getFileMD5(files[i]));
                                            }
                                        }
                                        else
                                        {
                                            File.Copy(files[i], datafolder + @"\tools\RobloxAssetFixer\assets\" + Path.GetFileName(files[i]), true);
                                            if (i < sha1assets.Count)
                                            {
                                                sha1assets.Insert(i, getFileMD5(files[i]));
                                            }
                                            else
                                            {
                                                sha1assets.Add(getFileMD5(files[i]));
                                            }
                                        }
                                        totalAssets++;
                                    }
                                }
                            }
                        }
                    }

                }
                else
                {
                    Console.WriteLine("<WARN> Unable to load assets in \"" + parsedPath + "\", does it even exist?");
                }
            }

            if (sha1assets.Count > totalAssets && cancelAsset == false)
            {
                sha1assets.RemoveRange(sha1assets.Count - 1 - (sha1assets.Count - totalAssets - 1), sha1assets.Count - totalAssets - 1);
            }

            if (client != null && cancelAsset == false)
            {
                if (Directory.Exists(datafolder + @"\clients\" + client + @"\assets"))
                {
                    string[] files = Directory.GetFiles(datafolder + @"\clients\" + client + @"\assets");
                    string[] directories1 = GetFileNamesWithoutExtension(datafolder + @"\tools\RobloxAssetFixer\assets");
                    string[] directories2 = Directory.GetFiles(datafolder + @"\tools\RobloxAssetFixer\assets");
                    if (files.Length > 0)
                    {
                        for (int i = 0; i < files.Length; i++)
                        {
                            if (cancelAsset == true) return;
                            if (isJoiningOrStudio == true)
                            {
                                if (directories1.Contains(Path.GetFileNameWithoutExtension(files[i])))
                                {
                                    for (int i1 = 0; i1 < directories2.Length; i1++)
                                    {
                                        if (Path.GetFileNameWithoutExtension(directories2[i1]) == Path.GetFileNameWithoutExtension(files[i]) && Path.GetExtension(directories2[i1]) != Path.GetExtension(files[i]) && Path.GetExtension(directories2[i1]) != ".png" && Path.GetExtension(directories2[i1]) != ".jpg" && Path.GetExtension(directories2[i1]) != ".jpeg" && Path.GetExtension(directories2[i1]) != ".bmp")
                                        {
                                            File.Delete(directories2[i1]);
                                        }
                                    }
                                }
                                File.Copy(files[i], datafolder + @"\tools\RobloxAssetFixer\assets\" + Path.GetFileName(files[i]), true);
                            }
                        }
                    }
                }
            }
            else
            {
                if (Directory.Exists(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\assets") && cancelAsset == false)
                {
                    string[] files = Directory.GetFiles(datafolder + @"\clients\" + Properties.Settings.Default.lastselectedversion + @"\assets");
                    string[] directories1 = GetFileNamesWithoutExtension(datafolder + @"\tools\RobloxAssetFixer\assets");
                    string[] directories2 = Directory.GetFiles(datafolder + @"\tools\RobloxAssetFixer\assets");
                    if (files.Length > 0)
                    {
                        for (int i = 0; i < files.Length; i++)
                        {
                            if (cancelAsset == true) return;
                            if (isJoiningOrStudio == true)
                            {
                                if (directories1.Contains(Path.GetFileNameWithoutExtension(files[i])))
                                {
                                    for (int i1 = 0; i1 < directories2.Length; i1++)
                                    {
                                        if (Path.GetFileNameWithoutExtension(directories2[i1]) == Path.GetFileNameWithoutExtension(files[i]) && Path.GetExtension(directories2[i1]) != Path.GetExtension(files[i]) && Path.GetExtension(directories2[i1]) != ".png" && Path.GetExtension(directories2[i1]) != ".jpg" && Path.GetExtension(directories2[i1]) != ".jpeg" && Path.GetExtension(directories2[i1]) != ".bmp")
                                        {
                                            File.Delete(directories2[i1]);
                                        }
                                    }
                                }
                                File.Copy(files[i], datafolder + @"\tools\RobloxAssetFixer\assets\" + Path.GetFileName(files[i]), true);
                            }
                        }
                    }
                }
                else
                {
                    if (cancelAsset) return;
                }

            }
        }

        private void ClearAssets()
        {
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

        private string[] GetFileNamesWithoutExtension(string path)
        {
            if (Directory.Exists(path))
            {
                List<string> result = new List<string>();
                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    result.Add(Path.GetFileNameWithoutExtension(file));
                }
                return result.ToArray();
            }
            else
            {
                return new string[] { };
            }
        }

        private void PageSlide(Panel page1, Panel page2, ArrowDirection direction = ArrowDirection.Left, int speed = 5)
        {
            Thread thread = new Thread(() =>
            {
                foreach (Control control in page1.Controls)
                {
                    if (control is Button)
                    {
                        (control as Button).Enabled = false;
                    }
                }
                foreach (Control control in page2.Controls)
                {
                    if (control is Button)
                    {
                        (control as Button).Enabled = false;
                    }
                }
                Point destPoint = page1.Location;

                while (page2.Location != destPoint)
                {
                    if (page2.Location.Y > destPoint.Y)
                    {
                        int yCoord = page2.Location.Y;
                        page2.Invoke(new Action(() => { page2.Location = new Point(page2.Location.X, yCoord - speed); }));
                        page1.Invoke(new Action(() => { page1.Location = new Point(page1.Location.X, page1.Location.Y - speed); }));
                        if (yCoord - speed < destPoint.Y && page2.Location.X == destPoint.X)
                        {
                            page2.Invoke(new Action(() => { page2.Location = destPoint; }));
                            break;
                        }
                    }
                    else if (page2.Location.Y < destPoint.Y)
                    {
                        int yCoord = page2.Location.Y;
                        page2.Invoke(new Action(() => { page2.Location = new Point(page2.Location.X, yCoord + speed); }));
                        page1.Invoke(new Action(() => { page1.Location = new Point(page1.Location.X, page1.Location.Y + speed); }));
                        if (yCoord + speed > destPoint.Y && page2.Location.X == destPoint.X)
                        {
                            page2.Invoke(new Action(() => { page2.Location = destPoint; }));
                            break;
                        }
                    }

                    if (page2.Location.X > destPoint.X)
                    {
                        int xCoord = page2.Location.X;
                        page2.Invoke(new Action(() => { page2.Location = new Point(xCoord - speed, page2.Location.Y); }));
                        page1.Invoke(new Action(() => { page1.Location = new Point(page1.Location.X - speed, page1.Location.Y); }));
                        if (xCoord - speed < destPoint.X)
                        {
                            page2.Invoke(new Action(() => { page2.Location = destPoint; }));
                            break;
                        }
                    }
                    else if (page2.Location.X < destPoint.X)
                    {
                        int xCoord = page2.Location.X;
                        page2.Invoke(new Action(() => { page2.Location = new Point(xCoord + speed, page2.Location.Y); }));
                        page1.Invoke(new Action(() => { page1.Location = new Point(page1.Location.X + speed, page1.Location.Y); }));
                        if (xCoord + speed > destPoint.X)
                        {
                            page2.Invoke(new Action(() => { page2.Location = destPoint; }));
                            break;
                        }
                    }
                    Thread.Sleep(1);
                }
                foreach (Control control in page1.Controls)
                {
                    if (control is Button)
                    {
                        (control as Button).Enabled = true;
                    }
                }
                foreach (Control control in page2.Controls)
                {
                    if (control is Button)
                    {
                        (control as Button).Enabled = true;
                    }
                }
            });
            thread.TrySetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void PageSlideTween(Panel page1, Panel page2, ArrowDirection direction = ArrowDirection.Left, double speed = 0.05)
        {
            Thread thread = new Thread(() =>
            {
                double progress = 0;
                foreach (Control control in page1.Controls)
                {
                    if (control is Button)
                    {
                        (control as Button).Enabled = false;
                    }
                }
                foreach (Control control in page2.Controls)
                {
                    if (control is Button)
                    {
                        (control as Button).Enabled = false;
                    }
                }
                Point destPoint = page1.Location;
                int yCoord = page2.Location.Y;
                int xCoord = page2.Location.X;
                while (page2.Location != destPoint)
                {
                    float easedProgress = (float)easeInOutQuart(progress);
                    float easedProgress1 = (float)easeInOutQuart(progress);

                    int page2x = (int)Math.Floor(Lerp(xCoord, destPoint.X, easedProgress));
                    int page2y = (int)Math.Floor(Lerp(yCoord, destPoint.Y, easedProgress));
                    int page1x = (int)Math.Floor(Lerp(destPoint.X, destPoint.X - Math.Abs(xCoord - destPoint.X), easedProgress));
                    int page1y = (int)Math.Floor(Lerp(destPoint.Y, destPoint.Y - Math.Abs(yCoord - destPoint.Y), easedProgress));

                    page2.Invoke(new Action(() => { page2.Location = new Point(page2x, page2y); }));
                    page1.Invoke(new Action(() => { page1.Location = new Point(page1x, page1y); }));
                    progress += speed;
                    Thread.Sleep(1);
                }

                foreach (Control control in page1.Controls)
                {
                    if (control is Button)
                    {
                        (control as Button).Enabled = true;
                    }
                }
                foreach (Control control in page2.Controls)
                {
                    if (control is Button)
                    {
                        (control as Button).Enabled = true;
                    }
                }
            });
            thread.TrySetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PageSlideTween(StartPage, AssetPage, ArrowDirection.Left, 0.01);
        }

        private void ChangeColorResult(Label labelToChange)
        {
            if (seconds < 5)
            {
                labelChange.Invoke(new Action(() => { labelToChange.ForeColor = Color.Green; }));
            }
            else if (seconds >= 5 && seconds < 10)
            {
                labelChange.Invoke(new Action(() => { labelToChange.ForeColor = Color.Yellow; }));
            }
            else if (seconds >= 10 && seconds < 21)
            {
                labelChange.Invoke(new Action(() => { labelToChange.ForeColor = Color.Orange; }));
            }
            else
            {
                labelChange.Invoke(new Action(() => { labelToChange.ForeColor = Color.Red; }));
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            Thread thread2;

            Thread thread = new Thread(() =>
            {
                for (int i = 0; i < 6; i++)
                {
                    thread2 = new Thread(() =>
                    {
                        while (timer == true)
                        {
                            if (seconds + 1 == 60) minutes++;
                            if (milliseconds + 1 == 1000) seconds = (seconds + 1) % 60;

                            milliseconds = (milliseconds + 1) % 1000;

                            if (labelChange != null)
                            {
                                labelChange.Text = labelChange.Text.Remove(2) + " " + minutes.ToString().PadLeft(2, '0') + ":" + seconds.ToString().PadLeft(2, '0') + "." + milliseconds.ToString().PadLeft(3, '0');
                            }
                        }
                        Application.ExitThread();
                    });
                    thread2.IsBackground = true;
                    thread2.TrySetApartmentState(ApartmentState.STA);
                    seconds = 0;
                    milliseconds = 0;
                    minutes = 0;
                    timer = true;
                    labelChange = getAssetResultLabel(i);
                    thread2.Start();
                    AssetStatus.Invoke(new Action(() => { AssetStatus.Text = "Loading assets..."; }));
                    LoadAssets("2016L", true);
                    timer = false;
                    AssetStatus.Invoke(new Action(() => { AssetStatus.Text = "Cleaning up assets..."; }));
                    ChangeColorResult(labelChange);
                    ClearAssets();
                }
                AssetStatus.Invoke(new Action(() => { AssetStatus.Text = "Test finished!"; }));
            });
            thread.TrySetApartmentState(ApartmentState.MTA);
            thread.Start();
           
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (seconds + 1 == 60) minutes++;
            if (milliseconds + 1 == 1000) seconds = (seconds + 1) % 60;

            milliseconds = (milliseconds + 1) % 1000;

            if (labelChange != null)
            {
                labelChange.Text = labelChange.Text.Remove(2) + " " + minutes.ToString().PadLeft(2, '0') + ":" + seconds.ToString().PadLeft(2, '0') + "." + milliseconds.ToString().PadLeft(3, '0');
            }
        }
    }
}
