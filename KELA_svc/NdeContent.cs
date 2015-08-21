using System;


namespace SUTI_svc
{
	/// <summary>
	/// Summary description for NdeContents.
	/// </summary>
	public class NdeContent
	{
		private Int32 _contents_id;
		public Int32 ContentsID
		{
			get { return _contents_id; }
			set { _contents_id = value; }
		}
		private Int32 _node_id;
		public Int32 NodeId
		{
			get { return _node_id; }
			set { _node_id = value; }
		}
		private char _content_type;
		public char ContentType
		{
			get { return _content_type; }
			set { _content_type = value; }
		}
		private string _name;
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}
		private string _description;
		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}
		private string _contact_phone;
		public string ContactPhone
		{
			get { return _contact_phone; }
			set { _contact_phone = value; }
		}

		public NdeContent()
		{
			//
			// TODO: Add constructor logic here
			//
		}
	}
}
