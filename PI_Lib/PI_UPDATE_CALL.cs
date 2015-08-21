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
	/// <para> PI_DISPATCH_ACCOUNT_CALL allows a client to enter an order
	/// in TaxiPak according to the address settings for a particular account.
	/// Optionally, the client can also specify the pickup/destination address for the
	/// order rather than using the default pickup address that is stored in the
	/// account's record.</para>
	/// </summary>
	///	<example>
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
	/// myPISocket.SetType(MessageTypes.PI_DISPATCH_ACCOUNT_CALL);
	/// PI_DISPATCH_ACCOUNT_CALL myAcctCall = new new PI_DISPATCH_ACCOUNT_CALL();
	/// 
	/// // Generate an immediate trip dispatched to the address associated with this account
	/// myAcctCall.fleet = Char.Parse("H"); // Taxi Helsinki fleet
	/// myAcctCall.acct_nbr = txtAccountID.Text.ToCharArray();
	/// 
	/// myPISocket.sendBuf = myAcctCall.ToByteArray();
	/// try
	/// {
	///		myPISocket.SendMessage();
	///	}
	/// catch (System.Net.Sockets.SocketException ex)
	/// {
	///      MessageBox.Show(ex.Message, "Socket error",
	///      MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
	///      return;
	/// }
	/// 
	/// // Successful 'send' to PI server. Now get the reply from the server
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
	///      myAcctCall.Deserialize( myPISocket.recvBuf );
	///      lblConfirm.Text = "Order confirmed with ref #" + myAcctCall.call_number.ToString();
	/// }
	/// catch (ApplicationException ex)
	/// {
	///      MessageBox.Show(ex.Message, "Failed order",
	///      MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
	///      return;
	/// }
	///
	/// myPISocket.CloseMe();
	/// </code></example>
	
	public class PI_UPDATE_CALL
	{
		/// <summary>
		/// Account number identifier. Size=16
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=16)]
		public char[]		acct_nbr;
		/// <summary>
		/// fleet identifier (A-H):  Main HTD fleet is designated
		/// as Fleet H
		/// </summary>
		public char         fleet;
		
		/// <summary>
		/// stores call number returned by the PI server. This is the unique
		/// identifier for the order within TaxiPak. Size=8
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=8)]
		public char[]		call_number;
		/// <summary>
		/// priority for order 1(highest priority)- 25(lowest priority). Size=2
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=2)]
		public char[]		priority;
		/// <summary>
		/// how many trips to order for this account/address (1-9)
		/// </summary>
		public char			number_of_calls;
		/// <summary>
		/// combination of types of call i.e. account, multi, time call. Size=5
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=5)]
		public char[]		call_type;
		/// <summary>
		/// streetname of pickup address. Size=21
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=21)]
		public char[]		from_addr_street;
		/// <summary>
		/// streetnumber of pickup address. Size=6
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=6)]
		public char[]		from_addr_number;
		/// <summary>
		/// streetnumber suffix letter
		/// </summary>
		public char			from_addr_suffix;
		/// <summary>
		/// apartment or room number of pickup address. Size=6
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=6)]
		public char[]		from_addr_apart;
		/// <summary>
		/// TaxiPak abbreviation of city of pickup address (Helsinki=HEL). Size=4
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=4)]
		public char[]		from_addr_city;
		/// <summary>
		/// zone of pickup address in TaxiPak geodatabase. This field is not
		/// required but the pickup address must be 'zonable' within the TaxiPak
		/// geographic database scheme. If successful, the PI server will return the
		/// correct zone number in the reply along with the entered order number. Size=3
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=3)]
		public char[]		from_addr_zone;
		/// <summary>
		/// pickup address comment. Size=31
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=31)]
		public char[]		from_addr_cmnt;
		/// <summary>
		/// name of passenger. Size=21
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=21)]
		public char[]		passenger;
		/// <summary>
		/// person who made the request for the order. Size=21
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=21)]
		public char[]		requested_by;
		/// <summary>
		/// phone number of customer. Size=11
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=11)]
		public char[]		phone;
		/// <summary>
		/// streetname of destination address. Size=21
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=21)]
		public char[]		to_addr_street;
		/// <summary>
		/// streetnumber of destination address. Size=6
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=6)]
		public char[]       to_addr_number;
		/// <summary>
		/// streetnumber suffix letter for destination
		/// </summary>
		public char			to_addr_suffix;
		/// <summary>
		/// apartment or room number of destination address. Size=6
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=6)]
		public char[]		to_addr_apart;
		/// <summary>
		/// TaxiPak abbreviation of city of destination address
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=4)]
		public char[]		to_addr_city;
		/// <summary>
		/// zone of destination address. Size=3
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=3)]
		public char[]		to_addr_zone;
		/// <summary>
		/// destination address comment. Size=31
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=31)]
		public char[]		to_addr_cmnt;
		/// <summary>
		/// car number assigned to call. Size=6
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=6)]
		public char[]       car_number;
		/// <summary>
		/// vehicle attributes for trip (Y/N)(J/N)(K/E) Size=32
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=32)]
		public char[]		car_attrib;
		/// <summary>
		/// driver number assigned to trip. Size=6
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=6)]
		public char[]		driver_id;
		/// <summary>
		/// driver attributes for trip (Y/N)(J/N)(K/E) Size=32
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=32)]
		public char[]		driver_attrib;
		/// <summary>
		/// user ID of the creator of the call. Size=4
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=4)]
		public char[]		creator;
		/// <summary>
		/// date the call is created/received YYMMDD Size=7
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=7)]
		public char[]		creation_date;
		/// <summary>
		/// time the call is created/received HHMM Size=5
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=5)]
		public char[]		creation_time;
		/// <summary>
		/// date the call is due YYMMDD Size=7
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=7)]
		public char[]		due_date;
		/// <summary>
		/// time the call is due HHMM Size=5
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=5)]
		public char[]		due_time;
		/// <summary>
		/// time the the customer is picked up by the taxi HHMM Size=5
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=5)]
		public char[]		pickup_time;
		/// <summary>
		/// time that the trip is completed Size=5
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=5)]
		public char[]		close_time;
		/// <summary>
		/// general comment for trip Size=65
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=65)]
		public char[]		call_comment;
		/// <summary>
		/// GPS coordinate X for pickup address Size=8
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=8)]
		public char[]		gpsx;
		/// <summary>
		/// GPS coordinate Y for pickup address
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=8)]
		public char[]		gpsy;

		/// <summary>
		/// Constructor for the class.
		/// </summary>
		public PI_UPDATE_CALL()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		#region Class Property Get/Set code
		/// <summary>
		/// Account number identifier
		/// </summary>
		public char[] AcctNbr
		{
			get { return acct_nbr; }
			set { acct_nbr= value; }
		}

		/// <summary>
		/// fleet identifier (A-H)
		/// </summary>
		public char Fleet
		{
			get { return fleet; }
			set { fleet= value; }
		}

		/// <summary>
		/// stores call number 
		/// </summary>
		public char[] CallNbr
		{
			get { return call_number; }
			set { call_number = value; }
		}

		/// <summary>
		/// priority for order 0-25
		/// </summary>
		public char[] Priority
		{
			get { return priority; }
			set { priority = value; }
		}

		/// <summary>
		/// how many trips to order
		/// </summary>
		public char NbrCalls
		{
			get { return number_of_calls; }
			set { number_of_calls = value; }
		}

		/// <summary>
		/// combination of types of call i.e. account, multi, time call
		/// </summary>
		public char[] CallType
		{
			get { return call_type; }
			set { call_type = value; }
		}

		/// <summary>
		/// streetname of pickup address
		/// </summary>
		public char[] FromAddrStreet
		{
			get { return from_addr_street; }
			set { from_addr_street = value; }
		}

		/// <summary>
		/// streetnumber of pickup address
		/// </summary>
		public char[] FromAddrNbr
		{
			get { return from_addr_number; }
			set { from_addr_number = value; }
		}

		/// <summary>
		/// streetnumber suffix letter
		/// </summary>
		public char FromAddrSuffix
		{
			get { return from_addr_suffix; }
			set { from_addr_suffix = value; }
		}

		/// <summary>
		/// apartment or room number of pickup address
		/// </summary>
		public char[] FromAddrApart
		{
			get { return from_addr_apart; }
			set { from_addr_apart = value; }
		}

		/// <summary>
		/// TaxiPak abbreviation of city of pickup address
		/// </summary>
		public char[] FromAddrCity
		{
			get { return from_addr_city; }
			set { from_addr_city = value; }
		}

		/// <summary>
		/// zone of pickup address
		/// </summary>
		public char[] FromAddrZone
		{
			get { return from_addr_zone; }
			set { from_addr_zone = value; }
		}

		/// <summary>
		/// pickup address comment
		/// </summary>
		public char[] FromAddrCmnt
		{
			get { return from_addr_cmnt; }
			set { from_addr_cmnt= value; }
		}

		/// <summary>
		/// name of passenger
		/// </summary>
		public char[] Passenger
		{
			get { return passenger; }
			set { passenger= value; }
		}

		/// <summary>
		/// person who made the request for the order
		/// </summary>
		public char[] RequestedBy
		{
			get { return requested_by; }
			set { requested_by= value; }
		}

		/// <summary>
		/// phonenumber of customer
		/// </summary>
		public char[] Phone
		{
			get { return phone; }
			set { phone = value; }
		}

		/// <summary>
		/// streetname of destination address
		/// </summary>
		public char[] ToAddrStreet
		{
			get { return to_addr_street; }
			set { to_addr_street = value; }
		}

		/// <summary>
		/// streetnumber of destination address
		/// </summary>
		public char[] ToAddrNbr
		{
			get { return to_addr_number; }
			set { to_addr_number= value; }
		}

		/// <summary>
		/// streetnumber suffix letter
		/// </summary>
		public char ToAddrSuffix
		{
			get { return to_addr_suffix; }
			set { to_addr_suffix= value; }
		}

		/// <summary>
		/// apartment or room number of destination address
		/// </summary>
		public char[] ToAddrApart
		{
			get { return to_addr_apart; }
			set { to_addr_apart= value; }
		}

		/// <summary>
		/// TaxiPak abbreviation of city of destination address
		/// </summary>
		public char[] ToAddrCity
		{
			get { return to_addr_city; }
			set { to_addr_city= value; }
		}

		/// <summary>
		/// zone of destination address
		/// </summary>
		public char[] ToAddrZone
		{
			get { return to_addr_zone; }
			set { to_addr_zone= value; }
		}

		/// <summary>
		/// destination address comment
		/// </summary>
		public char[] ToAddrCmnt
		{
			get { return to_addr_cmnt; }
			set { to_addr_cmnt= value; }
		}

		/// <summary>
		/// car number assigned to call
		/// </summary>
		public char[] CarNbr
		{
			get { return car_number; }
			set { car_number= value; }
		}

		/// <summary>
		/// vehicle attributes for trip (Y/N)(J/N)(K/E)
		/// </summary>
		public char[] CarAttrib
		{
			get { return car_attrib; }
			set { car_attrib= value; }
		}

		/// <summary>
		/// driver number assigned to trip
		/// </summary>
		public char[] DriverID
		{
			get { return driver_id; }
			set { driver_id= value; }
		}

		/// <summary>
		/// driver attributes for trip (Y/N)(J/N)(K/E)
		/// </summary>
		public char[] DriverAttrib
		{
			get { return driver_attrib; }
			set { driver_attrib= value; }
		}

		/// <summary>
		/// user ID of the creator of the call
		/// </summary>
		public char[] Creator
		{
			get { return creator; }
			set { creator= value; }
		}

		/// <summary>
		/// date the call is created/received YYMMDD
		/// </summary>
		public char[] CreationDate
		{
			get { return creation_date; }
			set { creation_date= value; }
		}

		/// <summary>
		/// time the call is created/received HHMM
		/// </summary>
		public char[] CreationTime
		{
			get { return creation_time; }
			set { creation_time= value; }
		}

		/// <summary>
		/// date the call is due YYMMDD
		/// </summary>
		public char[] DueDate
		{
			get { return due_date; }
			set { due_date= value; }
		}

		/// <summary>
		/// time the call is created/received HHMM
		/// </summary>
		public char[] DueTime
		{
			get { return due_time; }
			set { due_time= value; }
		}

		/// <summary>
		/// time the the customer is picked up by the taxi HHMM
		/// </summary>
		public char[] PickupTime
		{
			get { return pickup_time; }
			set { pickup_time= value; }
		}

		/// <summary>
		/// time that the trip is completed
		/// </summary>
		public char[] CloseTime
		{
			get { return close_time; }
			set { close_time= value; }
		}

		/// <summary>
		/// general comment for trip
		/// </summary>
		public char[] CallComment
		{
			get { return call_comment; }
			set { call_comment = value; }
		}

		/// <summary>
		/// GPS coordinate X for pickup address
		/// </summary>
		public char[] GPSX
		{
			get { return gpsx; }
			set { gpsx= value; }
		}

		/// <summary>
		/// GPS coordinate Y for pickup address
		/// </summary>
		public char[] GPSY
		{
			get { return gpsy; }
			set { gpsy= value; }
		}
#endregion


		/// <summary>
		/// This method will extract a confirmation call number
		/// corresponding to the database record number (CALLS table) that
		/// resulted from successful entry of the account order.
		/// Upon successful execution, the confirmation number is accessed
		/// using the call_number property of the class.
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

			this.call_number = (BitConverter.ToInt32(src, 6).ToString()).ToCharArray();


		}

		/// <summary>
		/// This method will take the necessary properties from the
		/// PI_DISPATCH_ACCOUNT_CALL class and pack them into a byte array in preparation
		/// for transmission to the PI server.
		/// </summary>
		/// <returns>Byte array formatted correctly for transmission
		/// with the PI message</returns>
		public Byte[] ToByteArray()
		{
			Int32	_pos	= 0;
			Byte[]	_dest	= new Byte[425];
			
			//Array.Copy(acct_nbr, 0,_dest, acct_nbr.Length);
			CopyStringField(ref _pos, acct_nbr, _dest, 16); 
			CopyCharField(ref _pos, fleet, _dest);
			CopyStringField(ref _pos, call_number, _dest, 8); 
			CopyStringField(ref _pos, priority, _dest, 2); 
			CopyCharField(ref _pos, number_of_calls, _dest);
			CopyStringField(ref _pos, call_type, _dest, 5); 
			CopyStringField(ref _pos, from_addr_street, _dest, 21); 
			CopyStringField(ref _pos, from_addr_number, _dest, 6); 
			CopyCharField(ref _pos, from_addr_suffix, _dest);
			CopyStringField(ref _pos, from_addr_apart, _dest, 6);  
			CopyStringField(ref _pos, from_addr_city, _dest, 4); 
			CopyStringField(ref _pos, from_addr_zone, _dest, 3); 
			CopyStringField(ref _pos, from_addr_cmnt, _dest, 31); 
			CopyStringField(ref _pos, passenger, _dest, 21); 
			CopyStringField(ref _pos, requested_by, _dest, 21);
			CopyStringField(ref _pos, phone, _dest, 11); 
			CopyStringField(ref _pos, to_addr_street, _dest, 21); 
			CopyStringField(ref _pos, to_addr_number, _dest, 6); 
			CopyCharField(ref _pos, to_addr_suffix, _dest);
			CopyStringField(ref _pos, to_addr_apart, _dest, 6);  
			CopyStringField(ref _pos, to_addr_city, _dest, 4); 
			CopyStringField(ref _pos, to_addr_zone, _dest, 3); 
			CopyStringField(ref _pos, to_addr_cmnt, _dest, 31); 
			CopyStringField(ref _pos, car_number, _dest, 6); 
			CopyStringField(ref _pos, car_attrib, _dest, 32); 
			CopyStringField(ref _pos, driver_id, _dest, 6); 
			CopyStringField(ref _pos, driver_attrib, _dest, 32); 
			CopyStringField(ref _pos, creator, _dest, 4); 
			CopyStringField(ref _pos, creation_date, _dest, 7); 
			CopyStringField(ref _pos, creation_time, _dest, 5); 
			CopyStringField(ref _pos, due_date, _dest, 7); 
			CopyStringField(ref _pos, due_time, _dest, 5); 
			CopyStringField(ref _pos, pickup_time, _dest, 5); 
			CopyStringField(ref _pos, close_time, _dest, 5); 
			CopyStringField(ref _pos, call_comment, _dest, 65); 
			CopyStringField(ref _pos, gpsx, _dest, 8); 
			CopyStringField(ref _pos, gpsy, _dest, 8); 

			return _dest;
		}


		private static void CopyByteField( ref Int32 pos, char field, byte[] dest)
		{
			Byte[] _fieldBytes = BitConverter.GetBytes( field);
			Int32  _fieldLen = 1;  // Marshal.SizeOf( Int32) == 4

			Array.Copy( _fieldBytes, 0, dest, pos, _fieldLen);
			pos = pos + _fieldLen;
		}
		

		private static void CopyStringField( ref Int32 pos, char[] field, byte[] dest, Int32 fieldLen )
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

		private static void CopyCharField( ref Int32 pos, Char field, byte[] dest)
		{
			Byte[] _fieldBytes = BitConverter.GetBytes( field);
			Int32  _fieldLen = 1;  // Marshal.SizeOf( Int32) == 4


		{
			Array.Copy( _fieldBytes, 0, dest, pos, _fieldLen);
			pos = pos + _fieldLen;
		}
		}

		private static void CopyShortField( ref Int32 pos, short field, byte[] dest)
		{
			Byte[] _fieldBytes = BitConverter.GetBytes( field);
			Int32  _fieldLen = 2;  // Marshal.SizeOf( Int32) == 4

			Array.Copy( _fieldBytes, 0, dest, pos, _fieldLen);
			pos = pos + _fieldLen;
		}


		private static void CopyIntField( ref Int32 pos, Int32 field, byte[] dest)
		{
			Byte[] _fieldBytes = BitConverter.GetBytes( field);
			Int32  _fieldLen = 4;  // Marshal.SizeOf( Int32) == 4

			Array.Copy( _fieldBytes, 0, dest, pos, _fieldLen);
			pos = pos + _fieldLen;
		}


	}
}
