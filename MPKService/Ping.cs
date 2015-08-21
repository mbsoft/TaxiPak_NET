using System;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;

namespace MPKService
{
	/// <summary>
	/// Summary description for Ping.
	/// </summary>
	public class Ping
	{

		private Ack reply;

		public Ping(XmlNode pingNode, string checksum)
		{
			reply = new Ack();
			reply.ackType = MPKService.Ack.AckType.ping;
			reply.CheckSum = checksum;

		}

		public string ReplyPing()
		{
			return reply.BuildAck();
		}
	}
}
