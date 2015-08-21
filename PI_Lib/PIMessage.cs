using System;
using System.Runtime.Serialization;
using System.Net;

namespace PI_Lib
{
	/// <summary>
	/// The PIMessage class contains structure definitions for
	/// the overhead associated with a PI message. These include
	/// a Header and a Footer to the message.
	/// </summary>
	
	public class PIMessage
	{
		public struct Header
		{
			public byte Head;
			public byte Len1;
			public byte Len2;
			public byte PacNum;
			public byte Msg1;
			public byte Msg2;
		} 

		public struct Footer
		{
			public byte Tail;
		}
		
		public Header myHeader;
		public Footer myFooter;

		public PIMessage(PIClient myPISock)
		{

			myHeader.Head = 0x2A;
			myHeader.Len1 = 0x04;
			myHeader.Len2 = 0x00;
			myHeader.PacNum = myPISock.PacNum;
			myHeader.Msg1 = 0x00;
			myHeader.Msg2 = 0x00;
			myFooter.Tail = 0x23;		

		}
		
		public void SetType(MessageTypes myType)
		{
			myHeader.Msg2 = (byte)myType;
		}

		public int GetMessageLength()
		{
			return((int)myHeader.Len1 + ((int)myHeader.Len2)*256);
		}

		public void SetMessageLength(int Length)
		{
			myHeader.Len1 = (byte)(Length % 256);
			myHeader.Len2 = (byte)(Length/256);
		}
	}
}
