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
	/// Summary description for PI_GET_EXCEPTS.
	/// </summary>
	public class PI_GET_EXCEPTS
	{
		public class ExceptRec
		{
			// This matches the definition in the PI documentation v1.5
			public long		exception_number;
			public char		fleet;
			[MarshalAs(UnmanagedType.LPArray, SizeConst=7)]
			public char[]   creation_date;
			[MarshalAs(UnmanagedType.LPArray, SizeConst=5)]
			public char[]	creation_time;
			public int      exception_type;
			public char     approval;
			public short    zone_number;
			public int      call_number;
			public short    car_number;
			public long     message_number;
			public char      outstanding;

			// Constructor for the inner class
			public ExceptRec(long excpt_nbr, char fleet, char[] date, char[] time, 
							 int type, short zone, int call, short car, 
				             char approval, long msg_nbr, char outstand)
			{
				exception_number = excpt_nbr;
				fleet = fleet;
				creation_date = date;
				creation_time = time;
				exception_type = type;
				zone_number = zone;
				call_number = call;
				car_number = car;
				approval = approval;
				message_number = msg_nbr;
				outstanding = outstand;
			}


			// Overrides the base ToString method for the class
			public override string ToString()
			{
				char [] nulls = {'\0',' '};

				return String.Format("Exception: {0}\tDate: {1:5}\tTime: {2:3}\tTaxi: {3}\r\n",
					exception_number, (new String(creation_date)).TrimEnd(nulls), (new String(creation_time)).TrimEnd(nulls), car_number);
			}

		}


		// Constructor
		public PI_GET_EXCEPTS()
		{

		}

		// Converts the byte stream returned by the PI server into a 
		// collection of Exception records that are added to a dataset
		public static void Deserialize( ref ArrayList dsExcept, byte[] src)
		{
			Int32 nbr_excepts;
			char [] nulls = {'\0',' '};

			// Just break out on any type of error
			if (src[6] != (byte)ErrorCodes.PI_OK)
				return;

			// How many exceptions were returned?
			nbr_excepts = BitConverter.ToInt32(src, 8);

			// Set the proper character set
			System.Text.Encoding enc = Encoding.GetEncoding("iso-8859-1");

			//PI_Data.Except newExcept;
			//Exceptions.ExceptRow newExcept;
			for ( int i = 0; i < nbr_excepts; i++ )
			{


			}
			

		
		}

		public static Byte[] ToByteArray(Int32 except_type, Int64 message_nbr)
		{
			Int32	_pos	= 0;
			Byte[]	_dest	= new Byte[12];

			// This method formats the 'order' block of our PI request.
			// Two fields are required:
			//		except_type - INT
			//		message_nbr - LONG
			CopyIntField(ref _pos, ref except_type, _dest);
			CopyLongField(ref _pos, ref message_nbr, _dest);

			return _dest;
		}





		public static void CopyIntField( ref Int32 pos, ref Int32 field, byte[] dest)
		{
			Byte[] _fieldBytes = BitConverter.GetBytes( field);
			Int32  _fieldLen = 4;  
			
			Array.Copy( _fieldBytes, 2, dest, pos+2, 2 );
			Array.Copy( _fieldBytes, 0, dest, pos, 2);
			pos = pos + _fieldLen;
			
		}



		public static void CopyLongField( ref Int32 pos, ref Int64 field, byte[] dest)
		{
			Byte[] _fieldBytes = BitConverter.GetBytes( field);
			Int32  _fieldLen = 8;  
			
			Array.Copy( _fieldBytes, 6, dest, pos+6, 2);
			Array.Copy( _fieldBytes, 4, dest, pos+4, 2);
			Array.Copy( _fieldBytes, 2, dest, pos+2, 2 );
			Array.Copy( _fieldBytes, 0, dest, pos, 2);
			pos = pos + _fieldLen;
			
		}
	}
}
