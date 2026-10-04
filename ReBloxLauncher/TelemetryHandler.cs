using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Management;
using System.IO;
using System.IO.Compression;
using System.Globalization;
using Newtonsoft.Json;

namespace ReBloxLauncher
{
    public class TelemetryHandler
    {
        private static byte[] GzipCompress(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(stream, CompressionLevel.Optimal))
                {
                    using (BufferedStream bufferedStream = new BufferedStream(gzipStream))
                    {
                        bufferedStream.Write(bytes, 0, bytes.Length);
                    }
                }
                return stream.ToArray();
            }
        }

        private static byte[] GzipDecompress(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                using (MemoryStream outputStream = new MemoryStream())
                {
                    using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Decompress))
                    {
                        using (BufferedStream bufferedStream = new BufferedStream(gzipStream))
                        {
                            bufferedStream.CopyTo(outputStream);
                        }
                    }
                    return outputStream.ToArray();
                }
            }
        }

        private static bool CheckForInternetConnection(int timeoutMs = 10000, string url = null, bool logResult = false)
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
                    if (logResult) Console.WriteLine("<INFO> Internet test successful!");
                    return true;
                }
            }
            catch
            {
                if (logResult) Console.WriteLine("<INFO> Failed to check the internet, not checking updates...");
                return false;
            }
        }

        private static string[] getCPUNames()
        {
            List<string> cpus = new List<string>();
            ManagementObjectSearcher mos = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");

            foreach (ManagementObject mo in mos.Get())
            {
                cpus.Add(mo["Name"].ToString());
            }

            return cpus.ToArray();
        }

        private static string[] getGPUNames()
        {
            List<string> gpus = new List<string>();
            ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

            foreach (ManagementObject mo in mos.Get())
            {
                gpus.Add(mo["Name"].ToString());
            }

            return gpus.ToArray();
        }

        public class telemetryData
        {
            public string uuid { get; set; }
            public string eventText { get; set; } = "";
            public string logFile { get; set; } = "";
            public string[] cpuNames { get; set; } = getCPUNames();
            public string launcherVersion { get; set; } = Properties.Settings.Default.version + (Properties.Settings.Default.minorVersion > 0 ? "-" + Properties.Settings.Default.minorVersion : "");
            public double windowsVersion { get; set; } = WineDetector.getOSVersion();
            public string[] gpuList { get; set; } = getGPUNames();
        }

        private class telemetryResult
        {
            public bool success { get; set; }
            public string message { get; set; }
        }

        public static void UploadTelemetry(string logFilePath = "", string eventText = "")
        {
            if (Properties.Settings.Default.TelemetryEnabled == false) return;
            if (CheckForInternetConnection() == false) return;
            telemetryData data;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://rebloxfileserver.servehttp.com/UploadLogFile");

            request.Method = "POST";
            request.ContentType = "application/json";
            request.UserAgent = "ReBlox/" + Properties.Settings.Default.version + (Properties.Settings.Default.minorVersion > 0 ? "-" + Properties.Settings.Default.minorVersion : "") + " (Windows NT " + WineDetector.getOSVersion() + (WineDetector.IsRunningOnWine() ? "; WINE " + WineDetector.getWineVersion() + ")" : ")");

            if (File.Exists(logFilePath) && logFilePath.EndsWith(".log"))
            {
                Program.stopLogging();
                data = new telemetryData
                {
                    uuid = Properties.Settings.Default.uuid.ToString(),
                    logFile = Convert.ToBase64String(GzipCompress(File.ReadAllBytes(logFilePath))),
                    eventText = eventText != "" ? eventText : ""
                };
                Program.startLogging();
            }
            else
            {
                data = new telemetryData
                {
                    uuid = Properties.Settings.Default.uuid.ToString(),
                    eventText = eventText != "" ? eventText : ""
                };
            }

            try
            {
                string jsonResult = JsonConvert.SerializeObject(data);
                byte[] resultBytes = Encoding.UTF8.GetBytes(jsonResult);
                request.GetRequestStream().Write(resultBytes, 0, resultBytes.Length);
                WebResponse response = request.GetResponse();
                byte[] result = null;
                using (Stream stream = response.GetResponseStream())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int count = 0;
                        do
                        {
                            byte[] buf = new byte[1024];
                            count = stream.Read(buf, 0, 1024);
                            ms.Write(buf, 0, count);
                        } while (stream.CanRead && count > 0);
                        result = ms.ToArray();
                    }
                }
                string responseResult = Encoding.UTF8.GetString(result);

                telemetryResult responseJson = JsonConvert.DeserializeObject<telemetryResult>(responseResult);
                if (responseJson.success == false)
                {
                    Console.WriteLine("<INFO> Something went wrong while trying to send telemetry data as the server returned the reason as \"" + responseJson.message + "\"");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("<INFO> Something went wrong while trying to send telemetry data, please look in the error below:\r\n" + e);
            }
        }

    }
}
