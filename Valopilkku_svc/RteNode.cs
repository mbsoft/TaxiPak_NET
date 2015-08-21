using System;
using System.Text;
using System.Collections;

namespace SUTI_svc
{
	/// <summary>
	/// Summary description for RteNode.
	/// </summary>
	public class RteNode
	{
		private Int32 _nodeID;
		public Int32 NodeID 
		{
			get { return _nodeID; }
			set { _nodeID = value; }
		}
		public Int32 RteID
		{
			get { return _rteID; }
			set { _rteID = value; }
		}
		private Int32 _rteID;
		public Int16 SeqNbr
		{
			get { return _seq_nbr; }
			set { _seq_nbr = value; }
		}
		private Int16 _seq_nbr;
		public char NodeType
		{
			get { return _nodeType; }
			set { _nodeType = value; }
		}
		private char  _nodeType;
		public string Street
		{
			get { return _street; }
			set { _street = value; }
		}
		private string _street;
		public string StreetNbr
		{
			get { return _streetNbr; }
			set { _streetNbr = value; }
		}
		private string _streetNbr;
		public string StreetNbrLtr
		{
			get { return _streetNbrLtr; }
			set { _streetNbrLtr = value; }
		}
		private string _streetNbrLtr;
		public string Locality
		{
			get { return _locality; }
			set { _locality = value; }
		}
		private string _locality;
		public string DueTime
		{
			get { return _duetime; }
			set {_duetime = value; }
		}
		private string _duetime;
		public string Zone
		{
			get { return _zone; }
			set {_zone = value; }
		}
		private string _zone;

		private string _description;
		public string Description
		{
			get { return _description;}
			set { _description = value; }
		}

		public ArrayList Contents = new ArrayList();

		public RteNode()
		{

		}
	}
}
