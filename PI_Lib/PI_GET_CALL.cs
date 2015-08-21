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
	/// Summary description for PI_GET_CALL.
	/// </summary>
	public class PI_GET_CALL
	{
        /// <summary>
        /// Order number that is assigned by TaxiPak
        /// </summary>
        public int call_number;

		public PI_GET_CALL()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public  void Deserialize(ref PI_DISPATCH_CALL theCall, byte[] src)
		{
			char [] nulls = {'\0',' '};

			// Just break out on any type of error
			if (src[6] != (byte)ErrorCodes.PI_OK)
				return;

			// Set the proper character set
			System.Text.Encoding enc = Encoding.GetEncoding("iso-8859-1");
            if (System.Configuration.ConfigurationSettings.AppSettings["AIX"].Equals("YES"))
                theCall.call_number = (src[12]<<24)|(src[13]<<16)|(src[14]<<8)|src[15];
            else
			    theCall.call_number = (BitConverter.ToInt32(src, 12));
			theCall.from_addr_street = enc.GetString(src, 32, 20).TrimEnd(nulls).ToCharArray();
            if (System.Configuration.ConfigurationSettings.AppSettings["AIX"].Equals("YES"))
                theCall.car_number = (short)((src[210] << 8) | src[211]);
            else
			    theCall.car_number = (BitConverter.ToInt16(src, 210));
            if (System.Configuration.ConfigurationSettings.AppSettings["AIX"].Equals("YES"))
                theCall.call_status = (short)((src[384] << 8)|src[385]);
            else
                theCall.call_status = (short)(BitConverter.ToUInt16(src, 384)); 
		}

		public Byte[] ToByteArray()
		{
			Int32	_pos	= 0;
			Byte[]	_dest	= new Byte[4];

			// This method formats the 'order' block of our PI request.
			// Two fields are required:
			//		call_nbr - LONG
			CopyIntField(ref _pos, call_number, _dest);

			return _dest;
		}


		public static void CopyIntField( ref Int32 pos, Int32 field, byte[] dest)
		{
			Byte[] _fieldBytes = BitConverter.GetBytes( field);
			Int32  _fieldLen = 4;

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
