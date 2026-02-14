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
        [StructLayout(LayoutKind.Sequential)]
        public struct OSVERSIONINFOEXA
        {
            public uint dwOsVersionInfoSize;
            public uint dwMajorVersion;
            public uint dwMinorVersion;
            public uint dwBuildNumber;
            public uint dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
            public UInt16 wServicePackMajor;
            public UInt16 wServicePackMinor;
            public UInt16 wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }
        [DllImport("kernel32.dll")]
        public static extern bool GetVersionEx(ref OSVERSIONINFOEXA lpVersionInformation);
        public static bool IsRunningOnWine()
        {
            try
            {
                if (wine_get_version() != null && wine_get_version() != "")
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
        public static double getOSVersion()
        {
            OSVERSIONINFOEXA info = new OSVERSIONINFOEXA();
            info.dwOsVersionInfoSize = (uint)Marshal.SizeOf(info);
            GetVersionEx(ref info);

            return (int)(info.dwMajorVersion) + ((double)(info.dwMinorVersion)/10);
        }
    }
}
