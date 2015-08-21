using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;

namespace MPKService
{
	public class Group
	{
		[SoapAttribute (Namespace="http://taxipak.com/webservices")]
		public string GroupName;
		[SoapAttribute (DataType="base64Binary")]
		public Byte [] GroupNumber;

		[SoapAttribute(DataType="date",AttributeName="CreationDate")]
		public DateTime Today;
		[SoapElement(DataType="nonNegativeInteger", ElementName="PosInt")]
		public string PositiveInt;
		[SoapIgnore]
		public bool IgnoreThis;

		public GroupType Grouptype;

		[SoapInclude(typeof(Car))]
		public Vehicle myCar(string licNumber)
		{
			Vehicle v;
			if (licNumber == "")
			{
				v = new Car();
				v.licenseNumber = "!!!!!";
			}
			else
			{
				v = new Car();
				v.licenseNumber = licNumber;
			}
			return v;
		}
	}

	public abstract class Vehicle
	{
		public string licenseNumber;
		public DateTime makeDate;
	}

	public class Car : Vehicle
	{
	}

	public enum GroupType
	{
		// These enums can be overridden.
		small,
		large
	}

	public class ping
	{
		public string message;
	}
	
	[Serializable]
	public struct passenger
	{
		public string name;
		public string phone;
		public string promised_pickup;
		public string extra_people;
		public string pickup_note;
		public string dropoff_note;
		public string recipient_phone;
		public string pickup;
		public string dropoff;
	}

	[Serializable]
	public struct address
	{
		public string street;
		public string city;
		public string note;
	}

	[Serializable]
	public struct stop
	{
		public address address;
		public string location;
		public string estimated_arrival;
	}

	[Serializable]
	public struct capacity_need
	{
		public string passengers;
		public string wheelchairs;
	}

	[Serializable]
	public struct route
	{
		[SoapAttribute (Namespace="www.cpandl.com")]
		public string id;
		[SoapAttribute (Namespace="www.cpandl.com")]
		public string version;
		public passenger passenger;
		public stop[] stop;
		public string estimated_length;
		public string mandatory;
		public string previous_route;
		public capacity_need capacity_need;
		public string price_group;
		public string assign_before;
	}

	[Serializable]
	public struct vehicle_capacity
	{
		public string passengers;
		public string wheelchairs;
	}

	[Serializable]
	public struct route_accept
	{
		public string id;
		public string version;
		public string accept;
		public string vehicle;
		public vehicle_capacity vehicle_capacity;
		public string price_group;
	}

	/// <summary>
	/// Summary description for Service1.
	/// </summary>
	/// 
	[WebService(Name="HTD-MPK Webservices", Namespace="http://localhost/webservices/MPK",Description="Prototype of HTD-MPK WebService")]
	public class HTD_MPK : System.Web.Services.WebService
	{
		public MD5Header Credentials;

		public HTD_MPK()
		{
			//CODEGEN: This call is required by the ASP.NET Web Services Designer
			InitializeComponent();
		}



		#region Component Designer generated code
		
		//Required by the Web Services Designer 
		private IContainer components = null;
				
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			

		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if(disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);		
		}
		
		#endregion


		[SoapHeader ("Credentials", Direction=SoapHeaderDirection.InOut)]
		[WebMethod(
			 Description="Ping used to test communication link periodically")]
		public string Ping()
		{
			
			return "R1.0";
		}

		[SoapHeader ("Credentials", Direction=SoapHeaderDirection.InOut)]
		[WebMethod( Description="ROUTE carries a description of the entire path of a driver's journey")]
		public route_accept Route(route route)
		{
			route_accept route_accept = new route_accept();

			return route_accept;
		}


		[SoapHeader ("Credentials", Direction=SoapHeaderDirection.InOut)]
		[WebMethod( Description="Group Test...")]
		public Group GroupIt(Group inGroup)
		{
			return (new Group());
		}
		public class MD5Header : SoapHeader
		{
			public string MD5Check;
		}
	}
}
