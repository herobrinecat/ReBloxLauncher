using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
namespace ReBloxLauncher
{
    internal static class Program
    {
        static FileStream ostrm;
        static StreamWriter writer;
        static TextWriter oldOut = Console.Out;
        static Random random = new Random();
        static private object syncLock = new object();
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        static public int RandomNumber(int min, int max)
        {
            lock (syncLock)
            {
                return random.Next(min, max);
            }
        }

        [STAThread]
        static void Main()
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
                catch
                {
                    //ignore, just to get rid of annoying moments
                }
                if (success)
                {
                    Console.SetOut(writer);
                    Console.WriteLine("<INFO> Logging has started!");
                }
            }
            Application.ApplicationExit += Application_ApplicationExit;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
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
}
