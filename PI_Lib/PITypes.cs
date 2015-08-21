using System;

namespace PI_Lib
{


	/// <summary>
	/// Supported PI message types for interacting with the TaxiPak
	/// PI server.
	/// </summary>
	public enum MessageTypes : int
	{
		/// <summary>
		/// Sent when an unknown order is received by PI
		/// </summary>
		PI_UNKNOWN_ORDER=0, 
		/// <summary>
		/// Zone the incoming address delivered by the PI client
		/// </summary>
		PI_ZONE_ADDRESS=1,
		/// <summary>
		/// Dispatch the call through PI 
		/// </summary>
		PI_DISPATCH_CALL=2,
		/// <summary>
		/// Cancel an existing order
		/// </summary>
		PI_CANCEL_CALL=3,
		/// <summary>
		/// Retrieve all the details for an existing call
		/// </summary>
		PI_GET_CALL=4,
		/// <summary>
		/// Used to retrieve the car that is assigned to a specific call.  
		/// If no car is connected to the call, the car_number field is set to zero(0)
		/// </summary>
		PI_GET_CAR=5,
		/// <summary>
		/// This function finds exception of a given exception type.  
		/// When using the PI_GET_EXCEPTIONS the answer will be number_of_exceptions 
		/// followed by a number of EXCEPTION structures equivalent to the number of 
		/// exceptions of the requested type.  If there are no exceptions of the type, 
		/// the number_of_exceptions is set to zero(0) and there will not be any EXCEPTION 
		/// data structures 
		/// </summary>
		PI_GET_EXCEPTIONS=6,
		/// <summary>
		/// This function is used to tell PI to report on certain exceptions as they 
		/// are generated in TaxiPak.  The on_off_switch is a boolean 0x01=0n 0x00=off 
		/// (CHAR size 1 byte) telling whether to report the exception or not.  
		/// The 0x00 and 0x01 following PI_OK indicates whether there is an exception 
		/// of the designated type in TaxiPak.
		/// </summary>
		PI_REPORT_EXCEPTIONS=7,
		/// <summary>
		/// Used to approve/disapprove an exception.  Field approval is either 
		/// ‘Y/J/K’=approved or ‘N/N/E’=disapproved 
		/// </summary>
		PI_ACCEPT_EXCEPTION=8,
		/// <summary>
		/// Sends a message to vehicles,drivers,users,dispatchers 
		/// based on contents of the MESSAGE data structure
		/// </summary>
		PI_SEND_MESSAGE=9,
		/// <summary>
		/// Retrieves the contents of a message from the TaxiPak message table
		/// </summary>
		PI_GET_MESSAGE=10,
		/// <summary>
		/// Function used to determine the state of PI
		/// </summary>
		PI_ALIVE=11,
		/// <summary>
		/// Function used to ask PI whether it is OK to quit the connection
		/// </summary>
		PI_QUIT=12,
		/// <summary>
		/// Dispatch an order from the FTJ/Planet system.  The PI must set the FTJ/Planet 
		/// order number in the ftj field (FTJ=0 non-ftj call, FTJ > 0 FTJ call) 
		/// </summary>
		PI_DISP_EXT_CALL=13,
		/// <summary>
		/// Generates exceptions that are specific to the Samplan system (FTJ). 
		/// Exception class = ‘1’ (Samplan) with exception type in the range 29-43
		/// </summary>
		PI_GENERATE_EXCEPTION=14,
		/// <summary>
		/// Generate an order in TaxiPak based on the address/passenger details
		/// associated with the provided account number.
		/// </summary>
		PI_DISPATCH_ACCOUNT_CALL=15,
		/// <summary>
		/// Used to get zone information from the TaxiPak system.  Client queries 
		/// TaxiPak based on fleet, zone number, and attributes and is returned 
		/// information on number of trips, vehicles etc.
		/// </summary>
		PI_ZONE_INFO=20,
		/// <summary>
		/// Retrieves information on the availability of a time call 
		/// booking for a given fleet, zone, and date
		/// </summary>
		PI_PRE_BOOK_INFO=22,
		/// <summary>
		/// Provides interface for caller identification and callback order requests
		/// </summary>
		PI_LINE_MGR_ORDER=26,
		/// <summary>
		/// Client requests a GPS polling message sent to the requested taxi
		/// </summary>
		PI_RQST_GPS=29,

        PI_UPDATE_CALL=35
	};

	/// <summary>
	/// 1 = PENDING
	/// 2 = UNASSIGNED
	/// 3 = ASSIGNED
	/// 4 = PICKUP
	/// 5 = COMPLETE
	/// 6 = CANCELLED
	/// </summary>
	public enum StatusTypes : int 
	{
		/// <summary>
		/// PENDING
		/// </summary>
		ODOTTAA=1,
		/// <summary>
		/// UNASSIGNED
		/// </summary>
		AVOIN=2,
		/// <summary>
		/// ASSIGNED
		/// </summary>
		VLITETTY=3,
		/// <summary>
		/// PICKUP
		/// </summary>
		NOUTO=4,
		/// <summary>
		/// COMPLETE
		/// </summary>
		VALMIS=5,
		/// <summary>
		/// CANCELLED
		/// </summary>
		PERUTTUS=6,
		/// <summary>
		/// NON EXISTENT STATUS
		/// </summary>
		NOEXIST=7
	};

	/// <summary>
	/// Enumeration of all the possible codes returned by the PI server
	/// when a request is made by the client.
	/// </summary>
	public enum ErrorCodes : int
	{
		/// <summary>
		/// No failure
		/// <note type="implementnotes">Applies to all message types</note>
		/// </summary>
		PI_OK = 0,
		/// <summary>
		/// The data block has an illegal length
		/// <note type="implementnotes">Applies to all message types</note>
		/// </summary>
		PI_INVALID_LEN=1,
		/// <summary>
		/// Unable to get values from the data-block
		/// <note type="implementnotes">Applies to all message types</note>
		/// </summary>
		/// 
		PI_INVALID_VALUE=2,
		/// <summary>
		/// Fleet is outside the range A-H
		/// <note type="implementnotes">Applies to all message types</note>
		/// </summary>
		/// 
		PI_INVALID_FLEET=3,
		/// <summary>
		/// database operation fails – closing down
		/// <note type="implementnotes">Applies to all message types</note>
		/// </summary>
		/// 
		PI_DB_OP_FAIL=4,
		/// <summary>
		/// trip’s priority is outside 6-63
		/// <note type="implementnotes">Applies to PI_ZONE_ADDRESS and PI_DISPATCH_CALL</note>
		/// </summary>
		/// 
		PI_INVALID_PRIO=10,
		/// <summary>
		/// # of vehicles not within 1-9
		/// <note type="implementnotes">Applies to PI_ZONE_ADDRESS and PI_DISPATCH_CALL</note>
		/// </summary>
		/// 
		PI_INVALID_VEH=11,
		/// <summary>
		/// incorrect call type:Pass,Deliv,Extra,Wakeup
		/// <note type="implementnotes">Applies to PI_ZONE_ADDRESS and PI_DISPATCH_CALL</note>
		/// </summary>
		/// 
		PI_INVALID_CALL_1=12,
		/// <summary>
		/// incorrect call-type: Autobooker
		/// <note type="implementnotes">Applies to PI_ZONE_ADDRESS and PI_DISPATCH_CALL</note>
		/// </summary>
		/// 
		PI_INVALID_CALL_2=13,
		/// <summary>
		/// incorrect call-type: Timecall, subscription
		/// <note type="implementnotes">Applies to PI_ZONE_ADDRESS and PI_DISPATCH_CALL</note>
		/// </summary>
		/// 
		PI_INVALID_CALL_3=14,
		/// <summary>
		/// incorrect call-type: Charge
		/// <note type="implementnotes">Applies to PI_ZONE_ADDRESS and PI_DISPATCH_CALL</note>
		/// </summary>
		/// 
		PI_INVALID_CALL_4=15,
		/// <summary>
		/// incorrect call-type: Multi
		/// <note type="implementnotes">Applies to PI_ZONE_ADDRESS and PI_DISPATCH_CALL</note>
		/// </summary>
		/// 
		PI_INVALID_CALL_5=16,
		/// <summary>
		/// invalid streetname
		/// <note type="implementnotes">Applies to PI_ZONE_ADDRESS and PI_DISPATCH_CALL</note>
		/// </summary>
		/// 
		PI_INVALID_ADDR_STR=17,
		/// <summary>
		/// invalid street number
		/// <note type="implementnotes">Applies to PI_ZONE_ADDRESS and PI_DISPATCH_CALL</note>
		/// </summary>
		/// 
		PI_INVALID_ADDR_NUM=18,
		/// <summary>
		/// invalid city abbreviation
		/// <note type="implementnotes">Applies to PI_ZONE_ADDRESS and PI_DISPATCH_CALL</note>
		/// </summary>
		/// 
		PI_INVALID_CITY=19,
		/// <summary>
		/// one of the vehicle attributes does not match Y/N
		/// <note type="implementnotes">Applies to PI_ZONE_ADDRESS and PI_DISPATCH_CALL</note>
		/// </summary>
		/// 
		PI_INVALID_VEH_ATTRIB=20,
		/// <summary>
		/// one of the driver attributes does not match Y/N
		/// <note type="implementnotes">Applies to PI_ZONE_ADDRESS and PI_DISPATCH_CALL</note>
		/// </summary>
		/// 
		PI_INVALID_DRV_ATTRIB=21,
		/// <summary>
		/// due time for future call is too old
		/// <note type="implementnotes">Applies to PI_ZONE_ADDRESS and PI_DISPATCH_CALL</note>
		/// </summary>
		/// 
		PI_INVALID_DUE_TIME=22,
		/// <summary>
		/// pickup zone is not valid
		/// <note type="implementnotes">Applies to PI_ZONE_ADDRESS and PI_DISPATCH_CALL</note>
		/// </summary>
		/// 
		PI_INVALID_ZONE=23,
		/// <summary>
		/// Call does not exist
		/// <note type="implementnotes">Applies to PI_CANCEL_CALL, PI_GET_CALL, and PI_GET_CAR</note>
		/// </summary>
		PI_NO_CALL=30,
		/// <summary>
		/// exception-type does not exist
		/// <note type="implementnotes">Applies to PI_GET_EXCEPTIONS</note>
		/// </summary>
		PI_INVALID_EXCEPTION_TYPE=60,
		/// <summary>
		/// exception-message-number is not valid
		/// <note type="implementnotes">Applies to PI_GET_EXCEPTIONS</note>
		/// </summary>
		PI_INVALID_EXCEPTION_MSG=61,
		/// <summary>
		/// exception does not exist
		/// <note type="implementnotes">Applies to PI_ACCEPT_EXCEPTION</note>
		/// </summary>
		PI_INVALID_EXCEPTION=80,
		/// <summary>
		/// incorrect approval type (Y/N)
		/// <note type="implementnotes">Applies to PI_ACCEPT_EXCEPTION</note>
		/// </summary>
		PI_INVALID_APPROVAL=81,
		/// <summary>
		/// receiver-group doesn’t exist
		/// <note type="implementnotes">Applies to PI_SEND_MESSAGE</note>
		/// </summary>
		PI_INVALID_GROUP=90,
		/// <summary>
		/// receiver doesn’t exist
		/// <note type="implementnotes">Applies to PI_SEND_MESSAGE</note>
		/// </summary>
		PI_INVALID_RECEIVER=91,
		/// <summary>
		/// message was not received
		/// <note type="implementnotes">Applies to PI_SEND_MESSAGE</note>
		/// </summary>
		PI_NOT_RECEIVED=92,
		/// <summary>
		/// range 0x20 to 0x5F is violated for MMP message
		/// <note type="implementnotes">Applies to PI_SEND_MESSAGE</note>
		/// </summary>
		PI_INVALID_MMP_CHAR=93,
		/// <summary>
		/// group not defined (1=valid)
		/// <note type="implementnotes">Applies to PI_GENERATE_EXCEPTION</note>
		/// </summary>
		PI_INV_EXCP_GROUP=110,
		/// <summary>
		/// exception number not in range
		/// <note type="implementnotes">Applies to PI_GENERATE_EXCEPTION</note>
		/// </summary>
		PI_INV_EXCP_NBR=111,
		/// <summary>
		/// not in range (1-10)
		/// <note type="implementnotes">Applies to PI_DISPATCH_EXT_CALL</note>
		/// </summary>
		PI_INV_OFFER_TYPE=120,
		/// <summary>
		/// not in range (1-10)
		/// <note type="implementnotes">Applies to PI_DISPATCH_EXT_CALL</note>
		/// </summary>
		PI_INV_DETAIL_TYPE=121,
		/// <summary>
		/// not in range (0-3)
		/// <note type="implementnotes">Applies to PI_DISPATCH_EXT_CALL</note>
		/// </summary>
		PI_INV_NBR_CALL_MSGS=122,
		/// <summary>
		/// not in range (0-MAXINT)
		/// <note type="implementnotes">Applies to PI_DISPATCH_EXT_CALL</note>
		/// </summary>
		PI_INV_FTJ_NBR=123,
		/// <summary>
		/// not in range (0-2)
		/// <note type="implementnotes">Applies to PI_DISPATCH_EXT_CALL</note>
		/// </summary>
		PI_INV_CMSG_DEVICE=130,
		/// <summary>
		/// multi-call # (1-3)
		/// <note type="implementnotes">Applies to PI_DISPATCH_EXT_CALL</note>
		/// </summary>
		PI_INV_CMSG_MULTI_NUM=131
	};

	/// <summary>
	/// Defined TaxiPak exception types.
	/// </summary>
	public enum ExceptionTypes : int
	{
		/// <summary>
		/// driver pushed the alarm button
		/// </summary>
		EMERGENCY=1,
		/// <summary>
		/// request to cancel a call
		/// </summary>
		CALL_CANCEL=2,
		/// <summary>
		/// call cannot be zoned by TaxiPak
		/// </summary>
		UNZONED=3,
		/// <summary>
		/// 
		/// </summary>
		CALL_W_MSG=4,
		/// <summary>
		/// 
		/// </summary>
		CALL_NO_MSG=5,
		/// <summary>
		/// more taxis required than 9
		/// </summary>
		EXTRA_TAXIS=6,
		/// <summary>
		/// customer has called back to call center requesting status of trip
		/// </summary>
		CALLBACK=7,
		/// <summary>
		/// call has not been assigned before timer expiration
		/// </summary>
		CALL_TIMEOUT=8,
		/// <summary>
		/// 
		/// </summary>
		REJECT=9,
		/// <summary>
		/// taxi is late in arrival to customer
		/// </summary>
		LATE_METER=10,
		/// <summary>
		/// driver wants to pickup a customer on the street
		/// </summary>
		FLAG_REQ=11,
		/// <summary>
		/// customer was not available for pickup
		/// </summary>
		NO_SHOW=12,
		/// <summary>
		/// call the customer and inform that taxi has arrived
		/// </summary>
		CALLOUT=13,
		/// <summary>
		/// driver has requested to talk on voice channel
		/// </summary>
		REQ_TO_TALK=14,
		/// <summary>
		/// estimated time of arrival as reported by driver
		/// </summary>
		ETA=15,
		/// <summary>
		/// driver has sent a predefined message
		/// </summary>
		MESSAGE=16,
		/// <summary>
		/// No longer supported.
		/// </summary>
		BID=17,
		/// <summary>
		/// call center user wants to send a message to a driver/vehicle
		/// </summary>
		DRV_MSG=18,
		/// <summary>
		/// a customer requires a 'wake-up' call
		/// </summary>
		WAKE_UP=19,
		/// <summary>
		/// taximeter has been on for too short a period 
		/// </summary>
		SHORT_METER=20,
		/// <summary>
		/// alert that a subscription time call was generated on a day that is
		/// a designated holiday.
		/// </summary>
		HOL_TM_CALL=21,
		/// <summary>
		/// any type of system malfunction (ACC,filesystem full etc)
		/// </summary>
		SYS_ERR=22,
		/// <summary>
		/// 
		/// </summary>
		SUP_HOLD=23,
		/// <summary>
		/// 
		/// </summary>
		MANUAL=24,
		/// <summary>
		/// 
		/// </summary>
		PERSONAL=25,
		/// <summary>
		/// MDT generates meter fault message
		/// </summary>
		METER_FAULT=26,
		/// <summary>
		/// 
		/// </summary>
		MULTITRIP=27,
		/// <summary>
		/// 
		/// </summary>
		MISSING_RECPT_RETRY_LIMIT=28,
		/// <summary>
		/// 
		/// </summary>
		ITM_INVOICE_NAK=29,
		/// <summary>
		/// 
		/// </summary>
		ATTRIBUTE_HOLD=30,
		/// <summary>
		/// 
		/// </summary>
		NS_SHORT=31
	};

	/// <summary>
	/// Finnish Language specifications taken from TaxiPak baseline code
	/// file language.h
	/// </summary>
	public enum LangFinnish : byte
	{
		/// <summary>
		/// Represents YES
		/// </summary>
		YES=(byte)'K',
		/// <summary>
		/// Represents NO
		/// </summary>
		NO=(byte)'E',
		/// <summary>
		/// Used in PI_SEND_MESSAGE for ReceiveGroup field
		/// </summary>
		ZONE_GROUP=(byte)'R',
		/// <summary>
		/// Used in PI_SEND_MESSAGE for ReceiveGroup field
		/// </summary>
		VEHICLE_GROUP=(byte)'T',
		/// <summary>
		/// Used in PI_SEND_MESSAGE for ReceiveGroup field
		/// </summary>
		DRIVER_GROUP=(byte)'A'
	}

	/// <summary>
	/// English Language specifications taken from TaxiPak baseline code
	/// file language.h
	/// </summary>
	public enum LangEnglish : byte
	{
		/// <summary>
		/// Represents YES
		/// </summary>
		YES=(byte)'Y',
		/// <summary>
		/// Represents NO
		/// </summary>
		NO=(byte)'N',
		/// <summary>
		/// Used in PI_SEND_MESSAGE for ReceiveGroup field
		/// </summary>
		ZONE_GROUP=(byte)'Z',
		/// <summary>
		/// Used in PI_SEND_MESSAGE for ReceiveGroup field
		/// </summary>
		VEHICLE_GROUP=(byte)'T',
		/// <summary>
		/// Used in PI_SEND_MESSAGE for ReceiveGroup field
		/// </summary>
		DRIVER_GROUP=(byte)'D'
	}
}
