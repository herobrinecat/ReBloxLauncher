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
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Console.WriteLine("<ERROR> Something went wrong in the launcher that's not handled on the main thread! The error can be seen below:\r\n" + e.ExceptionObject.ToString());
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Console.WriteLine("<ERROR> Something went wrong in the launcher that's not observed! The error can be seen below:\r\n" + e.Exception);
                e.SetObserved();
            };

            Application.ThreadException += (sender, e) =>
            {
                Console.WriteLine("<ERROR> Something went wrong in the launcher that's not handled! The error can be seen below:\r\n" + e.Exception);
            };

            if (Directory.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\logs"))
            {
                DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(Application.ExecutablePath) + @"\logs");
                if (di.Attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    MessageBox.Show("It appears that the logs folder is read-only! Please check your drive and/or logs folder to ensure that it's writable.", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    bool success = false;
                    try
                    {
                        if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\logs\log.log"))
                        {
                            File.Move(Path.GetDirectoryName(Application.ExecutablePath) + @"\logs\log.log", Path.GetDirectoryName(Application.ExecutablePath) + @"\logs\log" + RandomNumber(10000, 99999) + ".log");
                        }
                        writer = File.AppendText("./logs/log.log");
                        writer.AutoFlush = true;
                        success = true;
                    }
                    catch
                    {
                        //ignore, just to get rid of annoying moments
                    }
                    if (success)
                    {
                        Console.SetOut(writer);
                        Console.WriteLine("ReBlox Launcher Log (" + DateTime.Now.ToString("D") + ")\r\n");
                        Console.WriteLine("<INFO> Logging has started!");
                        Console.WriteLine("<INFO> ReBlox Version: " + Properties.Settings.Default.version);
                    }
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
                if (Console.Out != oldOut)
                {
                    Console.SetOut(oldOut);
                    writer.Close();
                }
            }
            catch
            {
                //whatever
            }
        }
    }
}
