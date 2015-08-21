using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using PI_Data;

namespace PI_Lib
{
	/// <summary>
	/// <para>PI_ZONE_INFO class is used to query the TaxiPak dispatch system
	/// for statistics in a specific zone.</para>
	/// </summary>
	/// <example><code lang="C#">
	///	// Connect to TaxiPak PI server
	///	try {
	///		myPISocket = new PIClient();
	/// }
	/// catch (System.Net.Sockets.SocketException ex) {
	///		MessageBox.Show(ex.Message, "Error connecting to TaxiPak",
	///						MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
	///		return;
	///	}
	///	
	/// // Set the appropriate message type
	///	myPISocket.SetType(MessageTypes.PI_ZONE_INFO);
	///		
	///	// Instantiate the appropriate class and populate
	///	// with data for the request
	///	PI_ZONE_INFO myZoneInfo = new PI_ZONE_INFO();
	///	myZoneInfo.Fleet = Char.Parse(txtFleet.Text);
	///	myZoneInfo.ZoneNbr = Int16.Parse(txtZoneNbr.Text);
	///		
	///	// Populate the PI send buffer with the data from the PI_ZONE_INFO class
	///	myPISocket.sendBuf = myZoneInfo.ToByteArray( );
	///
	///	// Send the request to the PI server
	///	myPISocket.SendMessage( );
	///
	///	// Get the reply back from the server
	///	myPISocket.ReceiveMessage();
	///
	///	// Populate the PI_ZONE_INFO class with the returned data
	///	myZoneInfo.Deserialize(myPISocket.recvBuf);
	///	
	///	myPISocket.CloseMe();
	///</code></example>
	public class PI_ZONE_INFO 
	{

		private char	fleet;
		private short	zonenbr;
		private short	numtaxisbookedprimary;
		private short	numtaxisbookedbackup;
		[MarshalAs(UnmanagedType.LPArray, SizeConst=32)]
		private char[]	vehattr;
		[MarshalAs(UnmanagedType.LPArray, SizeConst=32)]
		private char[]   drvattr;
		private short	unassignedcalls;

		/// <summary>
		/// Constructor for the class.
		/// </summary>
		public PI_ZONE_INFO()
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
		/// Gets/Sets the TaxiPak zone number.
		/// </summary>
		public short ZoneNbr
		{
			get { return zonenbr; }
			set { zonenbr = value; }
		}

		/// <summary>
		/// Retrieves the 
		/// value for number of taxis booked in the
		/// primary zone.
		/// </summary>
		public short NumTaxisBookedPrimary
		{
			get { return numtaxisbookedprimary; }
			set { numtaxisbookedprimary = value; }
		}

		/// <summary>
		/// Retrieves the value for number of taxis booked
		/// into all of the backup zones.
		/// </summary>
		public short NumTaxisBookedBackup
		{
			get { return numtaxisbookedbackup; }
			set { numtaxisbookedbackup = value; }
		}

		/// <summary>
		/// Retrieves the value for number of unassigned calls
		/// in the primary zone.
		/// </summary>
		public short UnassignedCalls
		{
			get { return unassignedcalls; }
			set { unassignedcalls = value; }
		}

		/// <overloads>This method has two overloads. Both methods will 
		/// extract the fields for PI_ZONE_INFO from the returned buffer.
		/// Upon successful execution of this call, the
		/// individual fields can be accessed using the classes 'get' methods.
		/// </overloads>
		/// <summary>
		/// This overload will add a new data record
		/// to the ZoneInfo table of the provided PI dataset and return the 
		/// values via the class properties.
		/// </summary>
		/// <remarks>If the data buffer received from the PI server contains
		/// an indication of an error, an ApplicationException is thrown
		/// that should be caught by the application. The exception message
		/// will contain the specific enumeration error code.</remarks>
		/// <param name="dsPI">The PI dataset which includes a zone info table.
		/// If the method execution is successful, a new row will be added
		/// to the zone info table</param>
		/// <param name="src">The byte array that contains the data packet
		/// returned by the PI server.</param>
		public void Deserialize( ref ZoneData dsZone, byte[] src)
		{
			// Throw an exception if we get an error from PI server
			if (src[6] != (byte)ErrorCodes.PI_OK)
			{
				String msg;
				msg = Enum.GetName(typeof(ErrorCodes), src[6]);
				throw( new ApplicationException(msg));
			}



			ZoneData.ZoneInfoRow newZoneInfo;
			newZoneInfo = dsZone.ZoneInfo.NewZoneInfoRow();

			Fleet = (char)src[8];
			ZoneNbr = BitConverter.ToInt16(src, 10);
			NumTaxisBookedPrimary = BitConverter.ToInt16(src, 12);
			NumTaxisBookedBackup = BitConverter.ToInt16(src, 14);
			UnassignedCalls = BitConverter.ToInt16(src, 80);

			newZoneInfo.fleet = new String((char)src[8],1);
			newZoneInfo.zone_nbr = (BitConverter.ToInt16(src, 10)).ToString();
			newZoneInfo.numtaxisprimary = (BitConverter.ToInt16(src, 12)).ToString();
			newZoneInfo.numtaxisbackup = (BitConverter.ToInt16(src, 14)).ToString();
			newZoneInfo.unassigned = (BitConverter.ToInt16(src, 80)).ToString();
			
			dsZone.ZoneInfo.AddZoneInfoRow(newZoneInfo);
		}

		/// <summary>
		/// This overload just places the returned values into the properties
		/// of the class object.
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

			Fleet = (char)src[8];
			ZoneNbr = BitConverter.ToInt16(src, 10);
			NumTaxisBookedPrimary = BitConverter.ToInt16(src, 12);
			NumTaxisBookedBackup = BitConverter.ToInt16(src, 14);
			UnassignedCalls = BitConverter.ToInt16(src, 80);
		}

		/// <summary>
		/// This method will take the necessary properties from the
		/// PI_ZONE_INFO class and pack them into a byte array in preparation
		/// for transmission to the PI server.
		/// </summary>
		/// <returns>Byte array formatted correctly for transmission
		/// with the PI message</returns>
		public Byte[] ToByteArray()
		{
			Int32	_pos	= 0;
			Byte[]	_dest	= new Byte[74];
			
			CopyCharField(ref _pos,  Fleet, _dest);
			CopyShortField(ref _pos, ZoneNbr, _dest);
	
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


	
	}

}