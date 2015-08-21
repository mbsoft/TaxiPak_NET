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
	/// Summary description for PI_ACCEPT_EXCEPTION.
	/// </summary>
	public class PI_ACCEPT_EXCEPTION
	{
		private const int PI_ACCEPT_EXCEPTION_LEN=9;
		public PI_ACCEPT_EXCEPTION()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public static void Deserialize( byte[] src)
		{

		}

		public static Byte[] ToByteArray(Int32 except_nbr, char approval)
		{
			Int32	_pos	= 0;
			Byte[]	_dest	= new Byte[5];

			// This method formats the 'order' block of our PI request.
			// Two fields are required:
			//		except_nbr - INT
			//		approval - CHAR
			CopyIntField(ref _pos,  except_nbr, _dest);
			CopyCharField(ref _pos,  approval, _dest);

			return _dest;
		}

		private static void CopyCharField( ref Int32 pos,  Char field, byte[] dest)
		{
			Byte[] _fieldBytes = BitConverter.GetBytes( field);
			Int32  _fieldLen = 1;  // Marshal.SizeOf( Int32) == 4

			Array.Copy( _fieldBytes, 0, dest, pos, _fieldLen);
			pos = pos + _fieldLen;
		}

		public static void CopyIntField( ref Int32 pos, Int32 field, byte[] dest)
		{
			Byte[] _fieldBytes = BitConverter.GetBytes( field);
			Int32  _fieldLen = 4;  
			
			Array.Copy( _fieldBytes, 2, dest, pos+2, 2 );
			Array.Copy( _fieldBytes, 0, dest, pos, 2);
			pos = pos + _fieldLen;
			
		}
	}
}
