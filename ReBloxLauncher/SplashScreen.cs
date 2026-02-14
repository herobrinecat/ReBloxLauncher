using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace ReBloxLauncher
{
    public partial class SplashScreen : Form
    {

        string datafolder = Path.GetDirectoryName(Application.ExecutablePath) + @"\data";
        readonly object syncLock = new object();
        Random random = new Random();
        public SplashScreen(string dfolder = null)
        {
            InitializeComponent();
            if (dfolder != null)
            {
                if (Directory.Exists(dfolder)) {
                    datafolder = dfolder;
                }
            }
        }

        public int RandomNumber(int min, int max)
        {
            lock (syncLock)
            {
                return random.Next(min, max);
            }
        }

        private void SplashScreen_Load(object sender, EventArgs e)
        {
            if (Directory.Exists(datafolder + @"\splashscreens"))
            {
                string[] directories = Directory.GetFiles(datafolder + @"\splashscreens");

                if (directories.Length > 0)
                {
                    int randomchoose = RandomNumber(0, directories.Length);
                    if (directories[randomchoose].EndsWith(".png") || directories[randomchoose].EndsWith(".jpg") || directories[randomchoose].EndsWith(".jpeg") || directories[randomchoose].EndsWith(".bmp") || directories[randomchoose].EndsWith(".gif"))
                    {
                        this.BackgroundImage = Image.FromFile(directories[randomchoose]);
                    }
                    else
                    {
                        if (File.Exists(datafolder + @"\splashscreen.png"))
                        {
                            this.BackgroundImage = Image.FromFile(datafolder + @"\splashscreen.png");
                        }
                        else
                        {
                            this.BackgroundImage = Properties.Resources.splashscreen;
                        }
                    }
                    randomchoose = 0;
                }
                else
                {
                    if (File.Exists(datafolder + @"\splashscreen.png"))
                    {
                        this.BackgroundImage = Image.FromFile(datafolder + @"\splashscreen.png");
                    }
                    else
                    {
                        this.BackgroundImage = Properties.Resources.splashscreen;
                    }
                }
                directories = null;
            }
            else
            {
                if (File.Exists(datafolder + @"\splashscreen.png"))
                {
                    this.BackgroundImage = Image.FromFile(datafolder + @"\splashscreen.png");
                }
                else
                {
                    this.BackgroundImage = Properties.Resources.splashscreen;
                }
            }
        }
    }
}
