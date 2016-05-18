using System;
using System.Configuration;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Net.Sockets;
using System.IO;
using log4net;
using log4net.Config;

namespace PI_Lib
{
	/// <summary>
	/// <paFra>A derived class providing access to TCP client communications with a 
	/// designated PI server.</para>
	/// </summary>
	/// <example>This sample shows how to use this class to establish a connection
	/// to the TaxiPak PI server
	/// <code>
	/// class MyPIClient
	/// {
	///		public MyPIClient
	///		{
	///			// connect using a specified host/port combination
	///			PIClient m_client = new PIClient( "192.168.1.120", 3000 );
	///			m_client.Close();
	///			
	///			// connect using settings from the application configuration
	///			// with a setting for 'PIServer' and 'PIPort' in the App.config file
	///			PIClient m_client = new PIClient();
	///			m_client.Close();
	///		}
	///	}
	/// </code>
	/// </example>
	public class PIClient : TcpClient
	{
		private const int PI_DISPATCH_CALL_LEN=380;
		private const int PI_GET_CALL_LEN=4;
		private const int PI_CANCEL_CALL_LEN=4;
        private const int PI_GPS_RQST_LEN = 4;
		private const int PI_DISPATCH_ACCOUNT_CALL_LEN=441;
		private const int PI_ZONE_INFO_LEN=74;
		private const int PI_SEND_MESSAGE_LEN=544;
		private const int PI_GET_EXCEPTIONS_LEN=12;
		private const int PI_ACCEPT_EXCEPTION_LEN=5;
		private const int PI_LINE_MGR_ORDER_LEN=68;
        private const int PI_UPDATE_CALL_LEN = 425;
		private const int PI_OVERHEAD_LEN=4;

		private byte pacnum;
		private string host;
		private int port;

		private class Header
		{
			public byte Head;
			public byte Len1;
			public byte Len2;
			public byte PacNum;
			public byte Msg1;
			public byte Msg2;

			public Header(){}
		}
		
		private class Footer
		{
			public byte Tail;
			public Footer()
			{}
		}
		public byte[] sendBuf;
		public byte[] recvBuf;
		Header myHeader = new Header();
		Footer myFooter = new Footer();

		/// <summary>
		/// Instantiates the PIClient class with a Hostname and service port. Sets the packet number to 0x00.
		/// </summary>
		/// <param name="Host">Hostname for the TaxiPak PI server.</param>
		/// <param name="Port">Service port that TaxiPak PI server is listening to.</param>

		public PIClient(string Host, int Port) : base()
		{
			this.PIServer = Host;
			this.PIPort = Port;
			this.PacNum = 0x00;
			this.Connect(this.PIServer, this.PIPort);
			
			myHeader.Head = 0x2A;
			myHeader.Len1 = 0x04;
			myHeader.Len2 = 0x00;
			myHeader.PacNum = this.PacNum;
			myHeader.Msg1 = 0x00;
			myHeader.Msg2 = 0x00;
			myFooter.Tail = 0x23;
		}
		
		/// <summary>
		/// Instantiates the PIClient class without a hostname or service port. 
		/// Sets the packet number to 0x01.
		/// </summary>
		public PIClient() : base()
		{
			this.Connect(ConfigurationSettings.AppSettings["PIServer"],
				Int32.Parse(ConfigurationSettings.AppSettings["PIPort"]));

			myHeader.Head = 0x2A;
			myHeader.Len1 = 0x04;
			myHeader.Len2 = 0x00;
			myHeader.PacNum = this.PacNum;
			myHeader.Msg1 = 0x00;
			myHeader.Msg2 = 0x00;
			myFooter.Tail = 0x23;

		}
		/// <summary>
		/// Gets or sets the value of the TaxiPak PI server as an IP address or hostname.
		/// </summary>
		public string PIServer
		{
			get { return host; }
			set { host = value; }
		}

		/// <summary>
		/// Gets or sets the service port upon which the TaxiPak PI server is listening for connections.
		/// </summary>
		public int PIPort
		{
			get { return port; }
			set { port = value; }
		}

		/// <summary>
		/// Gets or sets the packet number for the current session between
		/// the client and the server. Valid packet numbers are from 0x00 and 0xff.
		/// The TaxiPak PI server enforces that these are in sequential order.
		/// </summary>
		public byte PacNum
		{
			get { return pacnum; }
			set { pacnum = value; }
		}

		/// <summary>
		/// Increments the packet number for the session. If the value exceeds
		/// 0xff, the value is rolled back to 0x00.
		/// </summary>
		private void IncrementPacNum()
		{
			PacNum++;
			if ( PacNum == 0xff )
				PacNum = 0x00;
		}

		/// <summary>
		/// Send the previously formatted PI message to the TaxiPak PI server using the established TCP client socket.
		/// </summary>
		public void SendMessage()
		{
			byte[] Head = new byte[6];
			byte[] Tail = new byte[1];


			NetworkStream ns = this.GetStream();
			ns.WriteByte(myHeader.Head);
			ns.WriteByte(myHeader.Len1);
			ns.WriteByte(myHeader.Len2);
			ns.WriteByte(PacNum);
			ns.WriteByte(myHeader.Msg1);
			ns.WriteByte(myHeader.Msg2);

			if ( this.sendBuf != null )
				ns.Write(this.sendBuf,0,this.sendBuf.Length);

			ns.WriteByte(myFooter.Tail);

			this.IncrementPacNum();

			return;
		}

		/// <summary>
		/// Receive the reply from the TaxiPak PI server.
		/// </summary>
		/// <returns></returns>
		public unsafe byte[] ReceiveMessage()
		{
			NetworkStream stream = this.GetStream();
	
			// Read until HEAD found
			int count = 0;
			while ( recvBuf[0] != 0x2a )
			{
				if ( ++count > 5000 )
					return(null); // bad network connection

				recvBuf[0] = (byte)stream.ReadByte(); // Head
			}

			recvBuf[1] = (byte)stream.ReadByte(); // Length 1
			recvBuf[2] = (byte)stream.ReadByte(); // Length 2
			int len = (int)recvBuf[1] + ((int)recvBuf[2])*256;

			int i = 3;
			
			while ( i <= len+2 )
			{
				if ( stream.Read(recvBuf,i,1) > 0 )
					i++;
			}

			return( recvBuf );

		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public int GetMessageLength()
		{
			return((int)myHeader.Len1 + ((int)myHeader.Len2)*256);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Length"></param>
		private void SetMessageLength(int Length)
		{
			myHeader.Len1 = (byte)(Length % 256);
			myHeader.Len2 = (byte)(Length/256);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="myType"></param>
		public void SetType(MessageTypes myType)
		{
			myHeader.Msg2 = (byte)myType;
			switch (myType)
			{
				case MessageTypes.PI_DISPATCH_CALL:
                case MessageTypes.PI_ZONE_ADDRESS:
					SetMessageLength(PI_DISPATCH_CALL_LEN + PI_OVERHEAD_LEN + 16);
					recvBuf = new byte[1024];
					break;
				case MessageTypes.PI_DISPATCH_ACCOUNT_CALL:
					SetMessageLength(PI_DISPATCH_ACCOUNT_CALL_LEN + PI_OVERHEAD_LEN);
					recvBuf = new byte[1024];
					break;
				case MessageTypes.PI_GET_EXCEPTIONS:
					SetMessageLength(PI_GET_EXCEPTIONS_LEN + PI_OVERHEAD_LEN);
					recvBuf = new byte[1024];
					break;
				case MessageTypes.PI_ACCEPT_EXCEPTION:
					SetMessageLength(PI_ACCEPT_EXCEPTION_LEN + PI_OVERHEAD_LEN);
					recvBuf = new byte[1024];
					break;

				case MessageTypes.PI_ZONE_INFO:
					this.SetMessageLength(PI_ZONE_INFO_LEN + PI_OVERHEAD_LEN);
					this.recvBuf = new byte[1024];
					break;

				case MessageTypes.PI_LINE_MGR_ORDER:
					this.SetMessageLength(PI_LINE_MGR_ORDER_LEN + PI_OVERHEAD_LEN);
					this.recvBuf = new byte[1024];
					break;

				case MessageTypes.PI_SEND_MESSAGE:
					this.SetMessageLength(PI_SEND_MESSAGE_LEN + PI_OVERHEAD_LEN);
					this.recvBuf = new byte[1024];
					break;

				case MessageTypes.PI_GET_CALL:
					this.SetMessageLength(PI_GET_CALL_LEN + PI_OVERHEAD_LEN);
					this.recvBuf = new byte[1024];
					break;

				case MessageTypes.PI_CANCEL_CALL:
					this.SetMessageLength(PI_CANCEL_CALL_LEN + PI_OVERHEAD_LEN);
					this.recvBuf = new byte[1024];
					break;
                
                case MessageTypes.PI_RQST_GPS:
                    this.SetMessageLength(PI_GPS_RQST_LEN + PI_OVERHEAD_LEN);
                    this.recvBuf = new byte[1024];
                    break;

                case MessageTypes.PI_UPDATE_CALL:
                    this.SetMessageLength(PI_UPDATE_CALL_LEN + PI_OVERHEAD_LEN);
                    this.recvBuf = new byte[1024];
                    break;

				default:
					break;
			}
		}

		/// <summary>
		/// Flushes all data on the socket and closes the socket connection.
		/// </summary>
		public void CloseMe()
		{

			NetworkStream ns = this.GetStream();
			ns.Flush();
			ns.Close();
			this.Close();
			this.Dispose(true);
			
		}
		
	}
}
