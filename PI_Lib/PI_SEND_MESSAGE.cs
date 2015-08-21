using System;
using System.Collections;
using System.Data;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;


namespace PI_Lib
{
	/// <summary>
	/// <para>PI_SEND_MESSAGE class allows the client to send a text message
	/// to an individual user or group of users of the TaxiPak system.
	/// </para>
	/// </summary>
	/// <example>
	/// <code lang="C#">
	/// try 
	/// {
	///      myPISocket = new PIClient();
	/// }
	/// catch (System.Net.Sockets.SocketException ex)
	/// {
	///      MessageBox.Show(ex.Message, "Error connecting to TaxiPak",
	///      MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
	///      return;
	/// }
	///
	/// myPISocket.SetType(MessageTypes.PI_SEND_MESSAGE);
	///
	/// PI_SEND_MESSAGE mySendMessage = new PI_SEND_MESSAGE();
	///
	/// mySendMessage.Fleet = Char.Parse("H");
	/// mySendMessage.ReceiveGroup = (byte)LangFinnish.VEHICLE_GROUP;
	/// mySendMessage.MessageText = txtMessage.Text.ToCharArray();
	/// mySendMessage.ReceiveID = txtReceiverID.Text.ToCharArray();
	///
	/// myPISocket.sendBuf = mySendMessage.ToByteArray();
	///
	/// try
	/// {
	///      myPISocket.SendMessage();
	/// }
	/// catch (System.Net.Sockets.SocketException ex)
	/// {
	///      MessageBox.Show(ex.Message, "Socket error",
	///      MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
	///      return;
	/// }
	///
	/// try
	/// {
	///      myPISocket.ReceiveMessage();
	/// }
	/// catch (System.Net.Sockets.SocketException ex)
	/// {
	///      MessageBox.Show(ex.Message, "Socket error",
	///      MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
	///      return;
	/// }
	/// catch (System.ApplicationException ex)
	/// {
	///      MessageBox.Show(ex.Message, "Application error",
	///      MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
	///      return;
	/// }
	///
	/// try
	/// {
	///      mySendMessage.Deserialize( myPISocket.recvBuf );
	///      lblConfirm.Text = "Message confirmed with ref #" + mySendMessage.MsgNbr;
	/// }
	/// catch (ApplicationException ex)
	/// {
	///      MessageBox.Show(ex.Message, "Failed message",
	///      MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
	///      return;
	/// }
	///
	/// myPISocket.CloseMe();
	/// </code> 
	/// </example>
	public class PI_SEND_MESSAGE
	{
		private const int PI_SEND_MESSAGE_LEN=544;

		// This matches the definition in the PI documentation v1.5
		private	char	fleet;
		private int		message_number;
		private short	creator;
		[MarshalAs(UnmanagedType.LPArray, SizeConst=7)]
		private char[]   creation_date;
		[MarshalAs(UnmanagedType.LPArray, SizeConst=5)]
		private char[]	creation_time;
		private char	receive_group;
		[MarshalAs(UnmanagedType.LPArray, SizeConst=7)]
		private char[]	receive_id;
		[MarshalAs(UnmanagedType.LPArray, SizeConst=513)]
		private char[]	message_text;


		/// <summary>
		/// Constructor for the class.
		/// </summary>
		public PI_SEND_MESSAGE()
		{
		}



		/// <summary>
		/// Gets/Sets the value for the TaxiPak fleet.
		/// </summary>
		public char Fleet
		{
			get { return fleet; }
			set { fleet = value; }
		}

		/// <summary>
		/// Used for retrieving the database record number for the
		/// message that was sent to the taxi. Setting the value has no
		/// meaning.
		/// </summary>
		public int MsgNbr
		{
			get { return message_number; }
			set { message_number = value; }
		}

		/// <summary>
		/// Gets/Sets the value for the creator of the message.
		/// The TaxiPak PI server automatically sets this to 888 for
		/// messages so whatever value is set by the client will
		/// be overwritten.
		/// </summary>
		public short Creator
		{
			get { return creator; }
			set { creator = value; }
		}

		/// <summary>
		/// Gets/Sets the value for the date of the message
		/// creation. Since the PI server will set the date according
		/// to the system clock, doing this on the client side is
		/// not necessary.
		/// </summary>
		public char[] CreationDate
		{
			get { return creation_date; }
			set { creation_date = value; }
		}

		/// <summary>
		/// Gets/Sets the value for the time of the message
		/// creation. Since the PI server will set the time according
		/// to the system clock, doing this on the client side is
		/// not necessary.
		/// </summary>
		public char[] CreationTime
		{
			get { return creation_time; }
			set { creation_time = value; }
		}

		/// <summary>
		/// Gets/Sets the value for the destination type that will
		/// be receiving the message. It is easiest to use the language
		/// specific enumerations for these. <see cref="LangFinnish">LangFinnish</see>
		/// and <see cref="LangEnglish">LangEnglish</see>
		/// </summary>
		public char ReceiveGroup
		{
			get { return receive_group; }
			set { receive_group = value; }
		}

		/// <summary>
		/// Gets/Sets the unique identifier for the receiver of the message
		/// that is being sent from the PI client.
		/// </summary>
		public char[] ReceiveID
		{
			get { return receive_id; }
			set { receive_id = value; }
		}

		/// <summary>
		/// Gets/Sets the message text to be sent. PI enforces a length
		/// limit of 200 bytes on these messages.
		/// </summary>
		public char[] MessageText
		{
			get { return message_text; }
			set { message_text = value; }
		}



		/// <summary>
		/// This method will extract a confirmation number
		/// corresponding to the database record number (MSGLOG table) that
		/// resulted from successful transmission of the message.
		/// Upon successful execution, the confirmation number is accessed
		/// using the MsgNbr property of the class.
		/// </summary>
		/// <remarks>If the data buffer received from the PI server contains
		/// an indication of an error, an ApplicationException is thrown
		/// that should be caught by the application. The exception message
		/// will contain the specific enumeration error code.</remarks>
		/// <param name="src">The byte array that contains the data packet
		/// returned by the PI server.</param>
		public void Deserialize(byte[] src)
		{
			// Throw an exception if we get an error from PI server
			if (src[6] != (byte)ErrorCodes.PI_OK)
			{
				String msg;
				msg = Enum.GetName(typeof(ErrorCodes), src[6]);
				throw( new ApplicationException(msg));
			}

			MsgNbr = BitConverter.ToInt32(src, 8);

		}

		/// <summary>
		/// This method will take the necessary properties from the
		/// PI_SEND_MESSAGE class and pack them into a byte array in preparation
		/// for transmission to the PI server.
		/// </summary>
		/// <returns>Byte array formatted correctly for transmission
		/// with the PI message</returns>
		public Byte[] ToByteArray()
		{
			Int32	_pos	= 0;
			Byte[]	_dest	= new Byte[PI_SEND_MESSAGE_LEN];

			CopyCharField(ref _pos,  Fleet, _dest);
			++_pos;
			++_pos;
			CopyIntField(ref _pos, MsgNbr, _dest);
			CopyShortField(ref _pos, Creator, _dest);
			CopyStringField(ref _pos, CreationDate, _dest, 7);
			CopyStringField(ref _pos, CreationTime, _dest, 5);
			CopyCharField(ref _pos, ReceiveGroup, _dest);
			--_pos;
			CopyStringField(ref _pos, ReceiveID, _dest, 7);
			CopyStringField(ref _pos, MessageText, _dest, 201);

			return _dest;
		}


		private static void CopyCharField( ref Int32 pos,  Char field, byte[] dest)
		{
			Byte[] _fieldBytes = BitConverter.GetBytes( field);
			Int32  _fieldLen = 2;  // Marshal.SizeOf( Int32) == 4

			Array.Copy( _fieldBytes, 0, dest, pos, _fieldLen);
			pos = pos + _fieldLen;
		}
		private static void CopyShortField( ref Int32 pos,  short field, byte[] dest)
		{
			Byte[] _fieldBytes = BitConverter.GetBytes( field);
			Int32  _fieldLen = 2;  // Marshal.SizeOf( Int32) == 4

			Array.Copy( _fieldBytes, 0, dest, pos, _fieldLen);
			pos = pos + _fieldLen;
		}



		private static void CopyIntField( ref Int32 pos, Int32 field, byte[] dest)
		{
			Byte[] _fieldBytes = BitConverter.GetBytes( field);
			Int32  _fieldLen = 4;  
			
			Array.Copy( _fieldBytes, 2, dest, pos+2, 2 );
			Array.Copy( _fieldBytes, 0, dest, pos, 2);
			pos = pos + _fieldLen;
			
		}

		private static void CopyStringField( ref Int32 pos,  char[] field, byte[] dest, Int32 fieldLen )
		{
			if ( field == null )
				pos = pos + fieldLen;
			else
			{
				System.Text.Encoding enc = Encoding.GetEncoding("iso-8859-1");
				Byte[] _fieldBytes = enc.GetBytes(field, 0, field.Length);
				Array.Copy( _fieldBytes, 0, dest, pos, field.Length );
				pos = pos + fieldLen;
			}
		}
	}
}
