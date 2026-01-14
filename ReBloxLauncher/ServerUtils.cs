using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace ReBloxLauncher
{
    public class ServerUtils
    {
        static readonly UdpClient serverUdpClient = new UdpClient(50358);
        static readonly UdpClient clientUdpClient = new UdpClient();

        static bool serverOn = false;
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
                    Thread thread = new Thread(() => {
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
            } catch (Exception ex) 
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
