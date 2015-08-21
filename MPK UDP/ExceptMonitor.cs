using System;
using System.Threading;
using System.Configuration;
using System.Text;
using System.Collections;
using PI_Lib;
using log4net;
using log4net.Config;

namespace MPKBridge
{
	/// <summary>
	/// Summary description for ExceptMonitor.
	/// </summary>
	public class ExceptMonitor
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(ExceptMonitor));
		
		enum MsgDrvType
		{ ARRIVE=97, DEPART, SEND_ALL };

		public void Monitor()
		{
			
			while (true)
			{

				PI_Lib.PIClient myPISocket1 = new PI_Lib.PIClient();
				ArrayList myExcepts = new ArrayList();

				myPISocket1.SetType(MessageTypes.PI_GET_EXCEPTIONS);
				myPISocket1.sendBuf = PI_GET_EXCEPTS.ToByteArray(16, 0L);
				myPISocket1.SendMessage();
				myPISocket1.ReceiveMessage();

				if ( myPISocket1.recvBuf[0] != 0x2a )
				{
					log.DebugFormat("DEBUG:Error with exception polling");
					myPISocket1.CloseMe();
					System.Threading.Thread.Sleep(10000);
					continue;
				}

				PI_GET_EXCEPTS.Deserialize(ref myExcepts, myPISocket1.recvBuf);
				myPISocket1.CloseMe();

				foreach ( PI_Data.Except excpt in myExcepts )
				{
					if ( excpt.exception_type.Equals("12") &&
						excpt.message_number.Equals("99") )
					{
						log.InfoFormat("Resolving exception #{0} NO_SHOW #{1}",
							excpt.exception_number, excpt.message_number);

						PI_Lib.PIClient myPISocket = new PI_Lib.PIClient();
						myPISocket.SetType(MessageTypes.PI_ACCEPT_EXCEPTION);
						myPISocket.sendBuf = PI_ACCEPT_EXCEPTION.ToByteArray(Convert.ToInt32(excpt.exception_number),Convert.ToChar("K"));
						myPISocket.SendMessage();
						myPISocket.ReceiveMessage();
						myPISocket.CloseMe();

						Vehicle myVehicle = new Vehicle(Int32.Parse(excpt.car_number));
						myVehicle.NoShow();
					}

					if ( excpt.exception_type.Equals("16") &&
						( ((MsgDrvType)Enum.Parse(typeof(MsgDrvType),excpt.message_number) == MsgDrvType.ARRIVE) ||
						((MsgDrvType)Enum.Parse(typeof(MsgDrvType),excpt.message_number) == MsgDrvType.DEPART) ||
						((MsgDrvType)Enum.Parse(typeof(MsgDrvType),excpt.message_number) == MsgDrvType.SEND_ALL) ) )
					{
						// resolve the exception
						log.Info(String.Format("Resolving exception #{0} MSG_FROM_DRV #{1}",
							excpt.exception_number, excpt.message_number));
						Console.WriteLine("Resolving exception #{0} MSG_FROM_DRV #{1}",
							excpt.exception_number, excpt.message_number);

						PI_Lib.PIClient myPISocket = new PI_Lib.PIClient();
						myPISocket.SetType(MessageTypes.PI_ACCEPT_EXCEPTION);
						myPISocket.sendBuf = PI_ACCEPT_EXCEPTION.ToByteArray(Convert.ToInt32(excpt.exception_number),Convert.ToChar("K"));
						myPISocket.SendMessage();
						myPISocket.ReceiveMessage();
						myPISocket.CloseMe();

						Vehicle myVehicle = new Vehicle(Int32.Parse(excpt.car_number));

						// temp: send out a text message to this car


						switch ((MsgDrvType)Enum.Parse(typeof(MsgDrvType),excpt.message_number))
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
					
				}
				myExcepts.Clear();

				
				// Check for new exceptions every 10 secs
				System.Threading.Thread.Sleep(10000);
			}
			

		}
		public ExceptMonitor()
		{
			//
			// TODO: Add constructor logic here
			//
		}
	}
}
