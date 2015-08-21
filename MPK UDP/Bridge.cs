using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Configuration;
using System.Text;
using System.Timers;
using System.Collections;
using System.Globalization;
using PI_Lib;
using log4net;
using log4net.Config;

[assembly: log4net.Config.XmlConfigurator(Watch=false)]
namespace MPKBridge
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Bridge
	{
		enum MsgDrvType
		{ ARRIVE=97, DEPART, SEND_ALL };

		private static readonly ILog log = LogManager.GetLogger(typeof(Bridge));
		private static ArrayList callList;
		private static int UdpPort = Int32.Parse(ConfigurationSettings.AppSettings.Get("UDP_PORT"));
		private static Socket soUdp;
		private static Byte[] received = new Byte[512];
		

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{

			callList = new ArrayList();
			
			IPHostEntry localHostEntry;
		
			AsyncCallback AcceptReceive = new AsyncCallback(ReceiveData);

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
			
			IPEndPoint tmpIpEndPoint = new IPEndPoint(localHostEntry.AddressList[0], UdpPort);
			EndPoint remoteEP = (tmpIpEndPoint);

			soUdp.BeginReceiveFrom(received, 0, received.Length, SocketFlags.None, ref remoteEP, AcceptReceive, null);

			log.Info("Started MPK UDP application");
			DateTime startTime = DateTime.Now;
			while (true)
			{

				System.Threading.Thread.Sleep(10000);
				Console.WriteLine("MPK UDP - {0} {1}", DateTime.Now.ToLongTimeString(), callList.Count);
					
			}
			
		}



		private static void ReceiveData(IAsyncResult result)
		{
			System.Text.Encoding iso = System.Text.Encoding.GetEncoding("iso8859-1");
			string dataReceived = "";
			string[] msgTokens;
			try
			{
				AsyncCallback AcceptReceive = new AsyncCallback(ReceiveData);
				IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, UdpPort);
				EndPoint tempRemoteEP = (EndPoint)RemoteIpEndPoint;
				int numBytes = soUdp.EndReceiveFrom(result, ref tempRemoteEP);
				dataReceived = iso.GetString(received);

				msgTokens = dataReceived.Split(new char[1]{'!'});
					
				try
				{
					log.InfoFormat("MSG_FROM_DRV: {0} {1}", msgTokens[0], msgTokens[1].ToString().TrimEnd(new char[] {'\0'}));
					Console.WriteLine("MSG_FROM_DRV: {0} {1}", msgTokens[0], msgTokens[1].ToString().TrimEnd(new char[] {'\0'}));


					Vehicle myVehicle = new Vehicle(msgTokens[0]); // init vehicle using MID string

					switch ((MsgDrvType)Enum.Parse(typeof(MsgDrvType),msgTokens[1]))
					{

						case MsgDrvType.ARRIVE:
							log.InfoFormat("Msg ARRIVE {0}", msgTokens[1].ToString().TrimEnd(new char[] {'\0'}));
							myVehicle.ConfirmArrive();
							break;
									
						case MsgDrvType.DEPART:
							log.InfoFormat("Msg DEPART {0}", msgTokens[1].ToString().TrimEnd(new char[] {'\0'}));
							myVehicle.ConfirmDepart();
							break;

						case MsgDrvType.SEND_ALL:
							log.InfoFormat("Msg REQ_ALL {0}", msgTokens[1].ToString().TrimEnd(new char[] {'\0'}));
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
				finally
				{
					soUdp.BeginReceiveFrom(received, 0, received.Length, SocketFlags.None, ref tempRemoteEP, AcceptReceive, null);
				}
			}
			catch (Exception exc)
			{
				log.InfoFormat("Error on UDP socket {0}", exc.Message);
			}
		}

	}
}
