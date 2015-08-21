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
	/// Summary description for PI_DISPATCH_CALL.
	/// </summary>
	public class PI_DISPATCH_CALL
	{
		/// <summary>
		/// Fleet identifier where the order should be dispatched H - Taxi Helsinki
		/// </summary>
		public char		fleet;			// 0 + 1 = 1
		/// <summary>
		/// Order number that is assigned by TaxiPak
		/// </summary>
		public int		call_number;	// 1 + 4 = 5
		/// <summary>
		/// Order priority setting 1-25
		/// </summary>
		public short	priority;		// 5 + 2 = 7
		/// <summary>
		/// Number of orders in the call group
		/// </summary>
		public int		call_group;		// 7 + 4 = 11
		/// <summary>
		/// Position in a list of multiple calls
		/// </summary>
		public char		call_position;	// 11 + 1 = 12
		/// <summary>
		/// Number of individual orders to enter for this request
		/// </summary>
		public char		number_of_calls;	// 12 + 1 = 13
		/// <summary>
		/// Number of cars to request for this order
		/// </summary>
		public char		number_of_vehicles;	// 13 + 1 = 14
		/// <summary>
		/// Indicator of type of call
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=5)]
		public char[]	call_type;			// 14 + 5 = 19
		/// <summary>
		/// Pickup address street name
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=21)]
		public char[]   from_addr_street;	// 19 + 21 = 40
		/// <summary>
		/// Pickup address street number
		/// </summary>
		public int		from_addr_number;	// 40 + 4 = 44
		/// <summary>
		/// Pickup address street number suffix
		/// </summary>
		public char		from_addr_suffix;	// 44 + 1 = 45
		/// <summary>
		/// Pickup address apartment identifier
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=6)]
		public char[]	from_addr_apart;	// 45 + 6 = 51
		/// <summary>
		/// Three letter abbreviation for the pickup address city i.e. HEL
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=4)]
		public char[]	from_addr_city;		// 51 + 4 = 55
		/// <summary>
		/// Pickup address zone in TaxiPak
		/// </summary>
		public short	from_addr_zone;		// 55 + 2 = 57
		/// <summary>
		/// Comment specific to the pickup address
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=31)]
		public char[]	from_addr_cmnt;		// 57 + 31 = 88
		/// <summary>
		/// Passenger's name
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=21)]
		public char[]	passenger;			// 88 + 21 = 109
		/// <summary>
		/// Telephone number of client
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=11)]
		public char[]	phone;				// 109 + 11 = 120
		/// <summary>
		/// Destination address street name
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=21)]
		public char[]   to_addr_street;		// 120 + 21 = 141
		/// <summary>
		/// Destination address street number
		/// </summary>
		public int		to_addr_number;		// 141 + 4 = 145
		/// <summary>
		/// Destination address number suffix
		/// </summary>
		public char		to_addr_suffix;		// 145 + 1 = 146
		/// <summary>
		/// Destination address apartment identifier
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=6)]
		public char[]	to_addr_apart;		// 146 + 6 = 152
		/// <summary>
		/// Three letter abbreviation for the destination city i.e. HEL
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=4)]
		public char[]	to_addr_city;		// 152 + 4 = 156
		/// <summary>
		/// Destination address zone in TaxiPak
		/// </summary>
		public short	to_addr_zone;		// 156 + 2 = 158
		/// <summary>
		/// Comment specific to the destination address
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=31)]
		public char[]	to_addr_cmnt;		// 158 + 31 = 189
		/// <summary>
		/// Unique car number for the taxi that is servicing this order.
		/// </summary>
		public short	car_number;			// 189 + 2 = 191
		/// <summary>
		/// Set of fields indicating which driver attributes
		/// are set for this order. Each character position contains
		/// a value of ({Y/N},{J/N}, or {K/E}) to indicate whether this
		/// attribute is set for the order
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=32)]
		public char[]	car_attrib;			// 191 + 32 = 223
		/// <summary>
		/// Driver identification number of driver servicing this order
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=6)]
		public char[]	driver_id;			// 223 + 6 = 229
		/// <summary>
		/// Set of fields indicating which driver attributes
		/// are set for this order. Each character position contains
		/// a value of ({Y/N},{J/N}, or {K/E}) to indicate whether this
		/// attribute is set for the order
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=32)]
		public char[]	driver_attrib;		// 229 + 32 = 261
		/// <summary>
		/// User ID of the calltaker entering the order
		/// </summary>
		public short	creator;			// 261 + 2 = 263
		/// <summary>
		/// Date that the order was created in TaxiPak
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=7)]
		public char[]	creation_date;		// 263 + 7 = 270
		/// <summary>
		/// Time that the order was created in TaxiPak
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=5)]
		public char[]	creation_time;		// 270 + 5 = 275
		/// <summary>
		/// Date that the passenger is due for pickup
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=7)]
		public char[]	due_date;			// 275 + 7 = 282
		/// <summary>
		/// Time that the passenger is due for pickup
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=5)]
		public char[]	due_time;			// 282 + 5 = 287
		/// <summary>
		/// Time that the customer was picked up by the taxi
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=5)]
		public char[]	pickup_time;		// 287 + 5 = 292
		/// <summary>
		/// Time that the order was closed in TaxiPak
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=5)]
		public char[]	close_time;			// 292 + 5 = 297
		/// <summary>
		/// General comment field
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=65)]
		public char[]	call_comment;		// 297 + 65 = 362
		/// <summary>
		/// Numeric value indicating the status of the order
		/// </summary>
		public short	call_status;		// 362 + 2 = 364
		/// <summary>
		/// GPS position on the X axis / Longitude
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=8)]
		public char[]	gpsx;				// 364 + 8 = 372
		/// <summary>
		/// GPS position on the Y axis / Latitude
		/// </summary>
		[MarshalAs(UnmanagedType.LPArray, SizeConst=8)]
		public char[]	gpsy;				// 372 + 8 = 380
		/// <summary>
		/// 
		/// </summary>

		public PI_DISPATCH_CALL()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		/// <summary>
		/// This method will extract a confirmation call number
		/// corresponding to the database record number (CALLS table) that
		/// resulted from successful entry of the order.
		/// Upon successful execution, the confirmation number is accessed
		/// using the call_number property of the class.
		/// </summary>
		/// <remarks>If the data buffer received from the PI server contains
		/// an indication of an error, an ApplicationException is thrown
		/// that should be caught by the application. The exception message
		/// will contain the specific enumeration error code.</remarks>
		/// <param name="src">The byte array that contains the data packet
		/// returned by the PI server.</param>
		public  void Deserialize(byte[] src)
		{
			// Throw an exception if we get an error from PI server
			if (src[6] != (byte)ErrorCodes.PI_OK)
			{
				String msg;
				msg = Enum.GetName(typeof(ErrorCodes), src[6]);
				throw( new ApplicationException(msg));
			}
            if (System.Configuration.ConfigurationSettings.AppSettings["AIX"].Equals("YES"))
                this.call_number = (src[8] << 24) | (src[9] << 16) | (src[10] << 8) | src[11];
            else
			    this.call_number = BitConverter.ToInt32(src, 8);
			//System.Text.Encoding enc = Encoding.GetEncoding("iso-8859-1");
			

			

		}

		/// <summary>
		/// This method will take the necessary properties from the
		/// PI_DISPATCH_CALL class and pack them into a byte array in preparation
		/// for transmission to the PI server.
		/// </summary>
		/// <returns>Byte array formatted correctly for transmission
		/// with the PI message</returns>
		public  Byte[] ToByteArray()
		{
			Int32	_pos	= 0;
			Byte[]	_dest	= new Byte[396];

			CopyCharField(ref _pos, fleet, _dest);
			_pos = _pos + 2;
			CopyIntField(ref _pos, call_number, _dest);
			CopyShortField(ref _pos, priority, _dest);
			_pos = _pos + 2;
			CopyIntField(ref _pos, call_group, _dest);
			CopyCharField(ref _pos, call_position, _dest);
			_pos--;
			CopyCharField(ref _pos, number_of_calls, _dest);
			_pos--;
			CopyCharField(ref _pos, number_of_vehicles, _dest);
			_pos--;
			CopyStringField(ref _pos, call_type, _dest, 5);
			CopyStringField(ref _pos, from_addr_street, _dest, 21);
			_pos= _pos + 3;
			CopyIntField(ref _pos, from_addr_number, _dest);
			
			CopyCharField(ref _pos, from_addr_suffix, _dest);
			_pos--;
			CopyStringField(ref _pos, from_addr_apart, _dest, 6);
			CopyStringField(ref _pos, from_addr_city, _dest, 4);
			_pos++;
			CopyShortField(ref _pos, from_addr_zone, _dest);
			CopyStringField(ref _pos, from_addr_cmnt, _dest, 31);
			CopyStringField(ref _pos, passenger, _dest, 21);
			CopyStringField(ref _pos, phone, _dest, 11);
			CopyStringField(ref _pos, to_addr_street, _dest, 21);
			_pos= _pos + 2;
			CopyIntField(ref _pos, to_addr_number, _dest);
			CopyCharField(ref _pos, to_addr_suffix, _dest);
			_pos--;
			CopyStringField(ref _pos, to_addr_apart, _dest, 6);
			CopyStringField(ref _pos, to_addr_city, _dest, 4);
			_pos++;
			CopyShortField(ref _pos, to_addr_zone, _dest);
			CopyStringField(ref _pos, to_addr_cmnt, _dest, 31);
			_pos++;
			CopyShortField(ref _pos, car_number, _dest);
			CopyStringField(ref _pos, car_attrib, _dest, 32);
			CopyStringField(ref _pos, driver_id, _dest, 6);
			CopyStringField(ref _pos, driver_attrib, _dest, 32);
			CopyShortField(ref _pos, creator, _dest);
			CopyStringField(ref _pos, creation_date, _dest, 7);
			CopyStringField(ref _pos, creation_time, _dest, 5);
			CopyStringField(ref _pos, due_date, _dest, 7);
			CopyStringField(ref _pos, due_time, _dest, 5);
			CopyStringField(ref _pos, pickup_time, _dest, 5);
			CopyStringField(ref _pos, close_time, _dest, 5);
			CopyStringField(ref _pos, call_comment, _dest, 65);
			_pos++;
			CopyShortField(ref _pos, call_status, _dest);
			CopyStringField(ref _pos, gpsx, _dest, 8);
			CopyStringField(ref _pos, gpsy, _dest, 8);

			return _dest;

		}

		private  void CopyByteField( ref Int32 pos,  char field, byte[] dest)
		{
			Byte[] _fieldBytes = BitConverter.GetBytes( field);
			Int32  _fieldLen = 1;  // Marshal.SizeOf( Int32) == 4


		{
			Array.Copy( _fieldBytes, 0, dest, pos, _fieldLen);
			pos = pos + _fieldLen;
		}
		}

		private  void CopyStringField( ref Int32 pos,  char[] field, byte[] dest, Int32 fieldLen )
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

		private  void CopyCharField( ref Int32 pos,  Char field, byte[] dest)
		{
			Byte[] _fieldBytes = BitConverter.GetBytes( field);
			Int32  _fieldLen = 2;  // Marshal.SizeOf( Int32) == 4


		{
			Array.Copy( _fieldBytes, 0, dest, pos, _fieldLen);
			pos = pos + _fieldLen;
		}
		}

		private  void CopyShortField( ref Int32 pos,  short field, byte[] dest)
		{
			Byte[] _fieldBytes = BitConverter.GetBytes( field);
			Int32  _fieldLen = 2;  // Marshal.SizeOf( Int32) == 4

            if (System.Configuration.ConfigurationSettings.AppSettings["AIX"].Equals("YES"))
            {
                Byte[] _tmpBytes = BitConverter.GetBytes(field);
                _fieldBytes[0] = _tmpBytes[1];
                _fieldBytes[1] = _tmpBytes[0];
            }

            Array.Copy(_fieldBytes, 0, dest, pos, _fieldLen);
            pos = pos + _fieldLen;
            
		
		}


		private  void CopyIntField( ref Int32 pos,  Int32 field, byte[] dest)
		{
			Byte[] _fieldBytes = BitConverter.GetBytes( field);
			Int32  _fieldLen = 4;  // Marshal.SizeOf( Int32) == 4

            if (System.Configuration.ConfigurationSettings.AppSettings["AIX"].Equals("YES"))
            {
                Byte[] _tmpBytes = BitConverter.GetBytes(field);
                _fieldBytes[0] = _tmpBytes[3];
                _fieldBytes[1] = _tmpBytes[2];
                _fieldBytes[2] = _tmpBytes[1];
                _fieldBytes[3] = _tmpBytes[0];
            }

			Array.Copy( _fieldBytes, 2, dest, pos+2, 2 );
			Array.Copy( _fieldBytes, 0, dest, pos, 2);
			pos = pos + _fieldLen;
		    
		}

	}
}
