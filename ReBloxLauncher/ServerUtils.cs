using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Globalization;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Management;
using Newtonsoft.Json;

namespace ReBloxLauncher
{
    public class ServerUtils
    {
        static string datafolder = Path.GetDirectoryName(Application.ExecutablePath) + @"\data";
        static UdpClient serverUdpClient;
        static readonly UdpClient clientUdpClient = new UdpClient();
        static TcpListener tcpListener;
        static bool serverOn = false;
        static bool serverComOn = false;
        static readonly object syncLock = new object();

        private static bool CheckForInternetConnection(int timeoutMs = 10000, string url = null)
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
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string GenerateUUID()
        {
            lock (syncLock)
            {
                return Guid.NewGuid().ToString();
            }
        }

        public static UdpClient GetClient(int clientType)
        {
            if (clientType == 0)
            {
                return clientUdpClient;
            }
            else if (clientType == 1)
            {
                return serverUdpClient;
            }
            else
            {
                return null;
            }
        }

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

        public static void SetDataFolder(string datafolderNew)
        {
            if (Directory.Exists(datafolderNew))
            {
                datafolder = datafolderNew;
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

        public static void SetupJoinScriptEx(string ipaddr, int port, long userId, string username, ulong placeid, string membership, bool isTeleport = false)
        {
            if (Directory.Exists(datafolder + @"\tools\RobloxAssetFixer"))
            {
                Console.WriteLine("<INFO> Generating joinscript...");
                string waitingForCharacterGuid = GenerateUUID().ToLower();
                string sessionId = GenerateUUID().ToLower();
                if (File.Exists(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt")) File.Delete(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt");
                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                {
                    if (File.Exists(datafolder + @"\private.txt"))
                    {
                        string currentUTCDate = DateTime.UtcNow.ToString("G");
                        RSA.ImportCspBlob(Convert.FromBase64String(File.ReadAllText(datafolder + @"\private.txt")));

                        string signature1Raw = userId + "\n" + (username != string.Empty ? username : Properties.Settings.Default.username) + "\n" + "http://assetgame.reblox.zip/Asset/CharacterFetch.ashx?userId=" + userId + "&placeId=" + placeid + "\nTest\n" + currentUTCDate;
                        string signature2Raw = userId + "\nTest\n" + currentUTCDate;

                        byte[] signedSignature1 = RSA.SignData(Encoding.UTF8.GetBytes(signature1Raw), SHA1.Create());
                        byte[] signedSignature2 = RSA.SignData(Encoding.UTF8.GetBytes(signature2Raw), SHA1.Create());

                        using (StreamWriter writer = File.AppendText(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt"))
                        {
                            writer.Write(@"{""ClientPort"":0,""MachineAddress"":""" + (ipaddr == string.Empty ? "127.0.0.1" : ipaddr) + @""",""ServerPort"":" + (port > 0 ? port : 53640) + @",""PingUrl"":"""",""PingInterval"":120,""UserName"":""" + (username != string.Empty ? username : Properties.Settings.Default.username) + @""",""SeleniumTestMode"":false,""UserId"":" + userId + @",""SuperSafeChat"":false,""CharacterAppearance"":""http://assetgame.reblox.zip/Asset/CharacterFetch.ashx?userId=" + userId + @"&placeId=" + placeid + @""",""ClientTicket"":""" + currentUTCDate + @";" + Convert.ToBase64String(signedSignature1) + @";" + Convert.ToBase64String(signedSignature2) + @""",""GameId"":""00000000-0000-0000-0000-000000000000"",""PlaceId"":" + placeid + @",""MeasurementUrl"":"""",""WaitingForCharacterGuid"":""" + waitingForCharacterGuid + @""",""BaseUrl"":""http://www.reblox.zip"",""ChatStyle"":""" + Properties.Settings.Default.ChatStyle + @""",""VendorId"":0,""ScreenShotInfo"":"""",""VideoInfo"":""<?xml version=\""1.0\""?><entry xmlns=\""http://www.w3.org/2005/Atom\"" xmlns:media=\""http://search.yahoo.com/mrss/\"" xmlns:yt=\""http://gdata.youtube.com/schemas/2007\""><media:group><media:title type=\""plain\""><![CDATA[ROBLOX Place]]></media:title><media:description type=\""plain\""><![CDATA[ For more games visit http://www.roblox.com]]></media:description><media:category scheme=\""http://gdata.youtube.com/schemas/2007/categories.cat\"">Games</media:category><media:keywords>ROBLOX, video, free game, online virtual world</media:keywords></media:group></entry>"",""CreatorId"":1,""CreatorTypeEnum"":""User"",""MembershipType"":""" + (membership != "" ? membership : Properties.Settings.Default.Membership.Replace(" ", "")) + @""",""AccountAge"":365,""CookieStoreFirstTimePlayKey"":""rbx_evt_ftp"",""CookieStoreFiveMinutePlayKey"":""rbx_evt_fmp"",""CookieStoreEnabled"":true,""IsRobloxPlace"":false,""GenerateTeleportJoin"":" + isTeleport.ToString().ToLower() + @",""IsUnknownOrUnder13"":" +  Properties.Settings.Default.AccountOver13.ToString().ToLower() + @",""SessionId"":""" + sessionId + @"|00000000-0000-0000-0000-000000000000|0|www.reblox.zip|0|" + DateTime.UtcNow.ToString("O") + @"|0|null|null|null|null"",""DataCenterId"":0,""UniverseId"":2,""BrowserTrackerId"":0,""UsePortraitMode"":false,""FollowUserId"":0,""characterAppearanceId"":0}");
                        }
                    }
                    else
                    {
                        using (StreamWriter writer = File.AppendText(datafolder + @"\tools\RobloxAssetFixer\joinscript.txt"))
                        {
                            writer.Write(@"{""ClientPort"":0,""MachineAddress"":""" + (ipaddr == string.Empty ? "127.0.0.1" : ipaddr) + @""",""ServerPort"":" + (port > 0 ? port : 53640) + @",""PingUrl"":"""",""PingInterval"":120,""UserName"":""" + (username != string.Empty ? username : Properties.Settings.Default.username) + @""",""SeleniumTestMode"":false,""UserId"":" + userId + @",""SuperSafeChat"":false,""CharacterAppearance"":""http://assetgame.reblox.zip/Asset/CharacterFetch.ashx?userId=" + userId + @"&placeId=" + placeid + @""",""ClientTicket"":""" + DateTime.UtcNow.ToString("G") + @";h0eeFX/hZrNHXjP01PeaXT8dA8yVZbGKSMR6omd818fXJwuc/RceXUA8EJwdlfn7IWDfqjF2e22EhFyPXhucHqxQjY3GQd+zPAfS7KfQzItRVIFnjXbfWEGPKKFFEP4QcTs9Q141sd3G83ye9ZdGbOXPjy9VwpdvEnFToarYX7Q=;TCtJG0d2d0pFaHYnHDzJQttKfZlZyHZmcRtUNcy9vyivgiwQtB/illTbHvaUc/9w+oy8XRi+giLEvwuRmRttGKKnpA5Qt7dwCyXz2UIzt5/8TSJYqIKT99iPjBg0/PQFmguI7LoSk1KfElEDwzCWGT3tryAiT7S7a1SjInteSAU="",""GameId"":""00000000-0000-0000-0000-000000000000"",""PlaceId"":" + placeid + @",""MeasurementUrl"":"""",""WaitingForCharacterGuid"":""" + waitingForCharacterGuid + @""",""BaseUrl"":""http://www.reblox.zip"",""ChatStyle"":""" + Properties.Settings.Default.ChatStyle + @""",""VendorId"":0,""ScreenShotInfo"":"""",""VideoInfo"":""<?xml version=\""1.0\""?><entry xmlns=\""http://www.w3.org/2005/Atom\"" xmlns:media=\""http://search.yahoo.com/mrss/\"" xmlns:yt=\""http://gdata.youtube.com/schemas/2007\""><media:group><media:title type=\""plain\""><![CDATA[ROBLOX Place]]></media:title><media:description type=\""plain\""><![CDATA[ For more games visit http://www.roblox.com]]></media:description><media:category scheme=\""http://gdata.youtube.com/schemas/2007/categories.cat\"">Games</media:category><media:keywords>ROBLOX, video, free game, online virtual world</media:keywords></media:group></entry>"",""CreatorId"":1,""CreatorTypeEnum"":""User"",""MembershipType"":""" + (membership != "" ? membership : Properties.Settings.Default.Membership.Replace(" ", "")) + @""",""AccountAge"":365,""CookieStoreFirstTimePlayKey"":""rbx_evt_ftp"",""CookieStoreFiveMinutePlayKey"":""rbx_evt_fmp"",""CookieStoreEnabled"":true,""IsRobloxPlace"":false,""GenerateTeleportJoin"":" + isTeleport.ToString().ToLower() + @",""IsUnknownOrUnder13"":" + Properties.Settings.Default.AccountOver13.ToString().ToLower() + @",""SessionId"":""" + sessionId + @"|00000000-0000-0000-0000-000000000000|0|www.reblox.zip|0|" + DateTime.UtcNow.ToString("O") + @"|0|null|null|null|null"",""DataCenterId"":0,""UniverseId"":2,""BrowserTrackerId"":0,""UsePortraitMode"":false,""FollowUserId"":0,""characterAppearanceId"":0}");
                        }
                    }
                }

            }
            else
            {
                throw new DirectoryNotFoundException("It looks like the RobloxAssetFixer directory is missing, please reinstall ReBlox or use the source version of RobloxAssetFixer.");
            }
        }

        private static bool CallTryParse(string stringToConvert, NumberStyles styles)
        {
            CultureInfo provider;

            if ((styles & NumberStyles.AllowCurrencySymbol) > 0)
                provider = new CultureInfo("en-US");
            else
                provider = CultureInfo.InvariantCulture;

            bool success = long.TryParse(stringToConvert, styles, provider, out long number);

            return success;
        }

        private static bool CallTryParseBool(string stringToConvert)
        {
            bool success = bool.TryParse(stringToConvert, out bool result);

            return success;
        }

        private static string getTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmss");
        }

        public static bool checkPortTcp(int port)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpComInfoArray = ipGlobalProperties.GetActiveTcpListeners();

            return tcpComInfoArray.Any(endpoint => endpoint.Port == port);
        }

        public static bool checkPortUdp(int port)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] udpComInfoArray = ipGlobalProperties.GetActiveUdpListeners();

            return udpComInfoArray.Any(endpoint => endpoint.Port == port);
        }

        public static void StartServerCom()
        {
            Thread thread = new Thread(() =>
            {
                if (checkPortTcp(50355))
                {
                    MessageBox.Show("It appears that another program/launcher is using the port that ReBlox uses, please close the program that uses the port and relaunch the launcher.", "ReBlox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;  
                }
                else
                {
                    tcpListener = new TcpListener(IPAddress.Any, 50355);
                }
                Console.WriteLine("<INFO> Starting TCP Server for RobloxAssetFixer integration with port 50355");
                serverComOn = true;
                tcpListener.Start();

                while (serverComOn)
                {
                    try
                    {
                        Console.WriteLine("<INFO> Waiting for RobloxAssetFixer request...");

                        TcpClient client = tcpListener.AcceptTcpClient();
                        Console.WriteLine("<INFO> Potential RobloxAssetFixer server detected! Verifying...");

                        NetworkStream stream = client.GetStream();
                        byte[] buffer = new byte[2048];
                        int bytesRead;

                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            if (CallTryParse(buffer[0].ToString("x2") + buffer[1].ToString("x2") + buffer[2].ToString("x2") + buffer[3].ToString("x2"), NumberStyles.HexNumber))
                            {
                                if (int.Parse(buffer[0].ToString("x2") + buffer[1].ToString("x2") + buffer[2].ToString("x2") + buffer[3].ToString("x2"), NumberStyles.HexNumber) == 276312498)
                                {
                                    if (buffer[4] == 0x55 && buffer[5] == 0x52 && buffer[6] == 0x53)
                                    {
                                        byte[] newarray = new byte[buffer.Length - 7];
                                        Buffer.BlockCopy(buffer, 7, newarray, 0, newarray.Length);

                                        byte[] decompressedBuffer = GzipDecompress(newarray);
                                        string roblosecurity = Encoding.UTF8.GetString(decompressedBuffer);

                                        if (roblosecurity.StartsWith("_|WARNING:-DO-NOT-SHARE-THIS.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items.|"))
                                        {
                                            Console.WriteLine("<INFO> Updating .ROBLOSECURITY");
                                            Properties.Settings.Default.ROBLOSECURITY = Convert.ToBase64String(Encoding.UTF8.GetBytes(roblosecurity.Trim(new char[] { '\0' })));
                                            Properties.Settings.Default.Save();
                                            Console.WriteLine("<INFO> .ROBLOSECURITY has been updated! This should cause less 401 errors.");
                                            stream.Write(Encoding.UTF8.GetBytes("200"), 0, Encoding.UTF8.GetBytes("200").Length);
                                            client.Close();
                                        }
                                        else
                                        {
                                            Console.WriteLine("<INFO> Invalid .ROBLOSECURITY detected, not moving on...");
                                            stream.Write(Encoding.UTF8.GetBytes("invalid"), 0, Encoding.UTF8.GetBytes("invalid").Length);
                                        }
                                    }
                                    else if (buffer[4] == 0x47 && buffer[5] == 0x55 && buffer[6] == 0x53)
                                    {
                                        Console.WriteLine("<INFO> Sending user settings to " + (client.Client.RemoteEndPoint as IPEndPoint).Address);
                                        stream.Write(Encoding.UTF8.GetBytes("{\"username\": \"" + Properties.Settings.Default.username + "\", \"id\": " + Properties.Settings.Default.UserId + ", \"accountOver13\": " + Properties.Settings.Default.AccountOver13 + ", \"membership\": \"" + Properties.Settings.Default.Membership.Replace(" ", "") + "\"}"), 0, Encoding.UTF8.GetBytes("{\"username\": \"" + Properties.Settings.Default.username + "\", \"id\": " + Properties.Settings.Default.UserId + ", \"accountOver13\": " + Properties.Settings.Default.AccountOver13 + ", \"membership\": \"" + Properties.Settings.Default.Membership.Replace(" ", "") + "\"}").Length);
                                        Console.WriteLine("<INFO> User settings has been sent to the server!");
                                        client.Close();
                                    }
                                    else if (buffer[4] == 0x47 && buffer[5] == 0x52 && buffer[6] == 0x53)
                                    {
                                        string timestamp = getTimestamp(DateTime.UtcNow);
                                        byte[] newarray = new byte[buffer.Length - 7];
                                        Buffer.BlockCopy(buffer, 7, newarray, 0, newarray.Length);
                                        if (Encoding.UTF8.GetString(newarray).Trim(new char[] { '\0' }) == timestamp)
                                        {
                                            byte[] roblosecurity = GzipCompress(Encoding.UTF8.GetBytes(Properties.Settings.Default.ROBLOSECURITY));
                                            Console.WriteLine("<INFO> Sending .ROBLOSECURITY to server...");
                                            stream.Write(roblosecurity, 0, roblosecurity.Length);
                                            Console.WriteLine("<INFO> .ROBLOSECURITY sent to the server!");
                                            client.Close();
                                        }
                                        else
                                        {
                                            stream.Write(Encoding.UTF8.GetBytes("invalid"), 0, Encoding.UTF8.GetBytes("invalid").Length);
                                            Console.WriteLine("<ERROR> A server attempted to grab your ROBLOSECURITY! Server IP: " + (client.Client.RemoteEndPoint as IPEndPoint).Address + " Expected time: " + timestamp + " Received time: " + Encoding.UTF8.GetString(newarray).Trim(new char[] { '\0' }));
                                        }
                                    }
                                    else if (buffer[4] == 0x4A && buffer[5] == 0x53 && buffer[6] == 0x47)
                                    {
                                        byte[] newarray = new byte[buffer.Length - 8];
                                        Buffer.BlockCopy(buffer, 8, newarray, 0, newarray.Length);
                                        string[] data = Encoding.UTF8.GetString(newarray).Trim(new char[] { '\0' }).Split('\n');
                                        if (data.Length == 7)
                                        {
                                            SetupJoinScriptEx(data[0], CallTryParse(data[1], NumberStyles.Integer) ? int.Parse(data[1]) : 53640, CallTryParse(data[2], NumberStyles.Integer) ? long.Parse(data[2]) : (Properties.Settings.Default.LongUserIdExperiment ? Properties.Settings.Default.UserIdLong : Properties.Settings.Default.UserId), data[3], CallTryParse(data[4], NumberStyles.Integer) ? ulong.Parse(data[4]) : 1818, data[5], CallTryParseBool(data[6]) ? bool.Parse(data[6]) : false);
                                            stream.Write(Encoding.UTF8.GetBytes("200"), 0, Encoding.UTF8.GetBytes("200").Length);
                                            client.Close();
                                        }
                                        else
                                        {
                                            Console.WriteLine("<INFO> Invalid length sent from " + (client.Client.RemoteEndPoint as IPEndPoint).Address + " (Expected length of 7, received " + data.Length + ")");
                                            stream.Write(Encoding.UTF8.GetBytes("invalid"), 0, Encoding.UTF8.GetBytes("invalid").Length);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("<INFO> Invalid data sent from " + (client.Client.RemoteEndPoint as IPEndPoint).Address);
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("<INFO> Failed to verify the session of " + (client.Client.RemoteEndPoint as IPEndPoint).Address);
                                stream.Write(Encoding.UTF8.GetBytes("invalid"), 0, Encoding.UTF8.GetBytes("invalid").Length);
                            }

                        }
                        if (client.Connected) client.Close();
                        Console.WriteLine("<INFO> RobloxAssetFixer server disconnected from the launcher.");
                    }
                    catch (SocketException)
                    {
                        //ignore
                    }
                    catch (ObjectDisposedException)
                    {
                        //the server died, ignore please.
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("<WARN> Something went wrong while running the TCP server! RobloxAssetFixer integration may not be available.\r\n\r\nMessage: " + e.Message + "\r\n\r\nStack Trace:\r\n" + e.StackTrace);
                    }
                }


            });
            thread.TrySetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        public static void stopServerCom()
        {
            if (serverComOn)
            {
                serverComOn = false;
                tcpListener.Stop();
            }
        }

        public static void StartListServer(string version, string mapName, string serverName, int port = 53640)
        {
            try
            {

                if (serverOn == false)
                {
                    if (checkPortUdp(50358))
                    {
                        return;
                    }
                    else
                    {
                        if (serverUdpClient == null)
                        {
                            serverUdpClient = new UdpClient(50358);
                        }
                    }
                    serverOn = true;
                    NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface n in nics)
                    {
                        IPInterfaceProperties ip_properties = n.GetIPProperties();
                        if (!n.GetIPProperties().MulticastAddresses.Any()) continue; // most of VPN adapters will be skipped
                        if (!n.SupportsMulticast)
                            continue; // Multicast is meaningless for this type of connection
                        if (OperationalStatus.Up != n.OperationalStatus)
                            continue; // This adapter is off or not connected
                        IPv4InterfaceProperties p = n.GetIPProperties().GetIPv4Properties();
                        if (null == p)
                            continue; // IPv4 is not configured on this adapter
                        serverUdpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, (int)IPAddress.HostToNetworkOrder(p.Index));
                    }
                    serverUdpClient.JoinMulticastGroup(IPAddress.Parse("231.100.2.3"));
                    serverUdpClient.Client.ReceiveTimeout = 5000;
                    serverUdpClient.Client.SendTimeout = 5000;
                    Thread thread = new Thread(() =>
                    {
                        IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        while (serverOn == true)
                        {
                            try
                            {
                                byte[] data = serverUdpClient.Receive(ref ipEndPoint);

                                var convertstring = Encoding.UTF8.GetString(data);
                                if (convertstring == "ping")
                                {
                                    serverUdpClient.Send(Encoding.UTF8.GetBytes("pong|" + version + "|" + mapName + "|" + Properties.Settings.Default.version + "|" + port + "|0|" + serverName), Encoding.UTF8.GetByteCount("pong|" + version + "|" + mapName + "|" + Properties.Settings.Default.version + "|" + port + "|0|" + serverName), ipEndPoint);
                                }
                            }
                            catch (SocketException)
                            {

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("<WARN> Something went wrong while running the UDP Server! Server listing may not be available! " + ex);
                            }
                        }
                    });
                    thread.IsBackground = true;
                    thread.TrySetApartmentState(ApartmentState.STA);
                    thread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("<WARN> Something went wrong while running the UDP Server! Server listing may not be available! " + ex.Message + "\r\n\r\nStack Trace:\r\n" + ex.StackTrace);
            }
        }

        public static void StopListServer()
        {
            if (serverOn == true)
            {
                serverUdpClient.DropMulticastGroup(IPAddress.Parse("231.100.2.3"));
            }
            serverOn = false;

        }
    }
}
