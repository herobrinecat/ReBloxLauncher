using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace ReBloxLauncher
{
    public class WineDetector
    {
        [DllImport("ntdll.dll")]
        public static extern string wine_get_version();
        public static bool IsRunningOnWine()
        {
            try
            {
                if (wine_get_version() != null || wine_get_version() != "")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string getWineVersion()
        {
            try
            {
                if (wine_get_version() != "")
                {
                    return wine_get_version();
                }
                else
                {
                    return "";
                }
            }
            catch
            {
                return "";
            }
        }
    }
}
