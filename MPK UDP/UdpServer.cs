using System;
using System.Net;
using System.Configuration;
using System.Collections;
using System.Net.Sockets;
using System.Threading;
using log4net;
using log4net.Config;

namespace MPKBridge
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class UdpServer
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(UdpServer));
		private int UdpPort = Int32.Parse(ConfigurationSettings.AppSettings.Get("UDP_PORT"));
		public Thread UdpThread;
		private Socket soUdp;

		enum MsgDrvType
		{ ARRIVE=97, DEPART, SEND_ALL };

		private static object syncObj = new object();

		public UdpServer()
		{
			try
			{
				UdpThread = new Thread(new ThreadStart(StartReceiveFrom));
				UdpThread.Name = "UdpServer";
				UdpThread.Start();
			}
			catch (Exception e)
			{
				Console.WriteLine("UDP exception occurred " + e.Message);
				UdpThread.Abort();
			}
		}

		public void Suspend()
		{
			UdpThread.Suspend();
		}

		public void Resume()
		{
			UdpThread.Resume();
		}

		public void Terminate()
		{
			soUdp.Close();
			UdpThread.Join();
			UdpThread.Abort();
		}

		public void StartReceiveFrom()
		{
			IPHostEntry localHostEntry;
			try
			{
				soUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				try
				{
					localHostEntry = Dns.GetHostByName(Dns.GetHostName());
					//localHostEntry = Dns.GetHostByName("192.168.1.139");
					log.InfoFormat("Host name: {0}", Dns.GetHostName());
					IPAddress [] addr2 = localHostEntry.AddressList;
					//for ( i = 0; i < addr2.Length; i++ )
					log.InfoFormat("IP Addr {0}: {1} ", 0, addr2[0].ToString());

				}
				catch (Exception)
				{
					log.Info("Localhost not found");
					return;
				}
			
				

				IPEndPoint localIpEndPoint = new IPEndPoint(localHostEntry.AddressList[0], UdpPort);
				soUdp.Bind(localIpEndPoint);
				
				String dataReceived = "";
				Byte[] received = new Byte[512];
				IPEndPoint tmpIpEndPoint = new IPEndPoint(localHostEntry.AddressList[0], UdpPort);
				EndPoint remoteEP = (tmpIpEndPoint);
				System.Text.Encoding iso = System.Text.Encoding.GetEncoding("iso8859-1");
				int bytesReceived = 0;
				string[] msgTokens;

				while (true)
				{
					received.Initialize();
					
					bytesReceived = soUdp.ReceiveFrom(received, ref remoteEP);

					dataReceived = iso.GetString(received);
					
					msgTokens = dataReceived.Split(new char[1]{'!'});
					
					try
					{
						log.InfoFormat("MSG_FROM_DRV: {0} {1}", msgTokens[0], msgTokens[1].ToString().TrimEnd(new char[] {'\0'}));

						Vehicle myVehicle = new Vehicle(msgTokens[0]); // init vehicle using MID string

						switch ((MsgDrvType)Enum.Parse(typeof(MsgDrvType),msgTokens[1]))
						{

							case MsgDrvType.ARRIVE:
								myVehicle.ConfirmArrive();
								break;
									
							case MsgDrvType.DEPART:
								myVehicle.ConfirmDepart();
								break;

							case MsgDrvType.SEND_ALL:
								myVehicle.GetAllStops();
								break;

							default:
								break;
						}

						
					}
					catch (Exception exc)
					{
						log.ErrorFormat("Problem with MSG_FROM_DRV: {0}", exc.Message);
					}
					
				}
			}
			catch (SocketException se)
			{
				Console.WriteLine("Socket exception occurred - " + se.Message);
			}
		}



	}
}
