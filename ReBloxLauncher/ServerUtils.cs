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

namespace ReBloxLauncher
{
    public class ServerUtils
    {
        static readonly UdpClient serverUdpClient = new UdpClient(50358);
        static readonly UdpClient clientUdpClient = new UdpClient();
        static readonly TcpListener tcpListener = new TcpListener(IPAddress.Any, 50355);
        static bool serverOn = false;
        static bool serverComOn = false;

        static int playerCount = 0;
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

        private static bool CallTryParse(string stringToConvert, NumberStyles styles)
        {
            CultureInfo provider;

            if ((styles & NumberStyles.AllowCurrencySymbol) > 0)
                provider = new CultureInfo("en-US");
            else
                provider = CultureInfo.InvariantCulture;

            bool success = int.TryParse(stringToConvert, styles, provider, out int number);

            return success;
        }

        public static void StartServerCom()
        {
            Thread thread = new Thread(() =>
            {
                Console.WriteLine("<INFO> Starting TCP Server for RobloxAssetFixer integration with port 50355");
                serverComOn = true;
                tcpListener.Start();

                while (serverComOn)
                {
                    try
                    {
                        Console.WriteLine("<INFO> Waiting for RobloxAssetFixer request...");

                        var client = tcpListener.AcceptTcpClient();
                        Console.WriteLine("<INFO> Potential RobloxAssetFixer server detected! Verifying...");

                        var stream = client.GetStream();
                        var buffer = new byte[2048];
                        int bytesRead;

                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            if (CallTryParse(buffer[0].ToString("x2") + buffer[1].ToString("x2") + buffer[2].ToString("x2") + buffer[3].ToString("x2"), NumberStyles.HexNumber))
                            {
                                if (int.Parse(buffer[0].ToString("x2") + buffer[1].ToString("x2") + buffer[2].ToString("x2") + buffer[3].ToString("x2"), NumberStyles.HexNumber) == 276312498)
                                {
                                    if (buffer[4] == 0x55 && buffer[5] == 0x52 && buffer[6] == 0x53)
                                    {
                                        var newarray = new byte[buffer.Length - 7];
                                        Buffer.BlockCopy(buffer, 7, newarray, 0, newarray.Length);

                                        var roblosecurity = Encoding.UTF8.GetString(newarray);

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
                    catch
                    {
                        Console.WriteLine("<WARN> Something went wrong while running the TCP server! RobloxAssetFixer integration may not be available.");
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
