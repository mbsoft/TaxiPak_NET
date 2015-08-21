using System;
using System.Collections;
using System.Data;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Configuration;

namespace PI_Lib
{
	/// <summary>
	/// Summary description for PI_GET_CALL.
	/// </summary>
	public class PI_RQST_GPS
	{
        /// <summary>
        /// Order number that is assigned by TaxiPak
        /// </summary>
        public int veh_nbr;

        public PI_RQST_GPS()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public  void Deserialize(ref PI_RQST_GPS theGPSRqst, byte[] src)
		{
			char [] nulls = {'\0',' '};

            // Just break out on any type of error
            if (src[6] != (byte)ErrorCodes.PI_OK)
            {
                String msg;
                msg = Enum.GetName(typeof(ErrorCodes), src[6]);
                throw (new ApplicationException(msg));
            }
		}

        public Byte[] ToByteArray(Int32 veh_nbr)
        {
            Int32 _pos = 0;
            Byte[] _dest = new Byte[4];

            // This method formats the 'order' block of our PI request.
            // Two fields are required:
            //		call_nbr - LONG
            CopyIntField(ref _pos, ref veh_nbr, _dest);

            return _dest;
        }


		public static void CopyIntField( ref Int32 pos, ref Int32 field, byte[] dest)
		{
			Byte[] _fieldBytes = BitConverter.GetBytes( field);
			Int32  _fieldLen = 4;

            if (ConfigurationSettings.AppSettings["AIX"].Equals("YES"))
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
