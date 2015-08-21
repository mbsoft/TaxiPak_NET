using System;
using System.Data;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace PI_Lib
{
	/// <summary>
	/// Summary description for PI_LINE_MGR_ORDER.
	/// </summary>
	public class PI_LINE_MGR_ORDER
	{
		public char		frame_type;
		public char		customer_id;
		[MarshalAs(UnmanagedType.LPArray, SizeConst=6)]
		public char[]   agent_id;
		[MarshalAs(UnmanagedType.LPArray, SizeConst=20)]
		public char[]	a_number;
		[MarshalAs(UnmanagedType.LPArray, SizeConst=14)]
		public char[]   pin_number;
		[MarshalAs(UnmanagedType.LPArray, SizeConst=8)]
		public char[]   due_time;
		[MarshalAs(UnmanagedType.LPArray, SizeConst=8)]
		public char[]   due_date;
		[MarshalAs(UnmanagedType.LPArray, SizeConst=10)]
		public char[]   call_nbr;


		public PI_LINE_MGR_ORDER()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public static void Deserialize(ref PI_LINE_MGR_ORDER LineMgrOrder, byte[] src)
		{
			System.Text.Encoding enc = Encoding.GetEncoding("iso-8859-1");


			return;
		}

		public static Byte[] ToByteArray(ref PI_LINE_MGR_ORDER src)
		{
			Int32	_pos	= 0;
			Byte[]	_dest	= new Byte[68];

			CopyCharField(ref _pos, ref src.frame_type, _dest);
			CopyCharField(ref _pos, ref src.customer_id, _dest);
			
			CopyStringField(ref _pos, ref src.agent_id, _dest, 6); 
			CopyStringField(ref _pos, ref src.a_number, _dest, 20); 
			CopyStringField(ref _pos, ref src.pin_number, _dest, 14);
			CopyStringField(ref _pos, ref src.due_time, _dest, 8);
			CopyStringField(ref _pos, ref src.due_date, _dest, 8);
			CopyStringField(ref _pos, ref src.call_nbr, _dest, 10);

			return _dest;
		}

		public static void CopyStringField( ref Int32 pos, ref char[] field, byte[] dest, Int32 fieldLen )
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

		public static void CopyCharField( ref Int32 pos, ref Char field, byte[] dest)
		{
			Byte[] _fieldBytes = BitConverter.GetBytes( field);
			Int32  _fieldLen = 1;  

			Array.Copy( _fieldBytes, 0, dest, pos, _fieldLen);
			pos = pos + _fieldLen;
		}
	}
}
