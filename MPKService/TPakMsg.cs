using System;
using System.Configuration;
using System.Text;
using System.Collections;
using System.Net;
using PI_Lib;
using log4net;
using log4net.Config;

namespace MPKService
{
	/// <summary>
	/// Summary description for TPakMsg.
	/// </summary>
	public class TPakMsg
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(TPakMsg));
		private string destination;
		private string fleet;
		private string type;
		private string _msg;
		public string Msg
		{ 
			get { return _msg; }
			set { _msg = value; }
		}

		public TPakMsg(string _dest_id, string _fleet, string _to_type)
		{
			destination = _dest_id;
			fleet = _fleet;
			type = _to_type;
		}

		public void Send()
		{

			PI_Lib.PIClient myPISocket = new PI_Lib.PIClient();
			myPISocket.SetType(MessageTypes.PI_SEND_MESSAGE);
			PI_SEND_MESSAGE mySendMessage = new PI_SEND_MESSAGE();
			mySendMessage.Fleet = Char.Parse(fleet);
			mySendMessage.ReceiveGroup = Convert.ToChar(type);
			mySendMessage.ReceiveID = destination.ToCharArray();
			try
			{
				if ( Msg.Length > 200 )
				{
					//split up into multiple messages
					for (int i = 0; i < Msg.Length; i+=199)
					{
						mySendMessage.MessageText = Msg.Substring(i, ((i+199<Msg.Length)?i+199:(Msg.Length-i))).ToCharArray();
						myPISocket.sendBuf = mySendMessage.ToByteArray();
						myPISocket.SendMessage();
						System.Threading.Thread.Sleep(1000);
					}
				}
				else
				{
					mySendMessage.MessageText = Msg.ToCharArray();
					myPISocket.sendBuf = mySendMessage.ToByteArray();

					myPISocket.SendMessage();
				}
			}
			catch (Exception exc)
			{
				log.InfoFormat("Error - TPakMsg.Send - {0}", exc.Message);
			}

			myPISocket.CloseMe();
			System.Threading.Thread.Sleep(1000);
		}
	}
}

