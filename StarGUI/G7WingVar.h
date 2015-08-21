/* Configuration Serveur WING				*/
#define BUF_SIZE			255
#define BODY_SIZE			255
#define WING_PORT			10000
//#define WING_IP				"172.27.30.20"

/* Configuration Application */
/* Configuration MDT						*/
//#define STAR2000V			"1.27.14     "
#define MDT_IP				"172027030020"

/* Nombre d'onglet maxi */
#define MAX_MDT				10

/* GPS lat,long */
#define	GPS_UPDATE_TIMER	60000							/* 60 secondes en msec. */

/* Message envoyé au WING (Extrait ACC.DAT)	non vérifié		*/
#define GROUP_ASSGN				0x0003
#define	SWITCH_TO_DATA			0x0005
#define	DEAUTH					0x0006
#define	EMERGENCY				0x0015
#define	REJECT					0x0200
#define	REQ_TALK				0x0300
#define	CHANGE_GRP 				0x1000
#define	NO_SHOW 				0x1100
#define	QP 						0x1200
#define	ACCEPT					0x1400

/* Broadcast messages */
#define	ENCHERES				0x0400

/* Outbound canned messages */
#define INV_ZONE				0x0701
#define BOOKED_OFF				0x0703
#define NOT_BOOKED_IN			0x070A

// Sign in / off inbound messages
#define	AUTHORIZE				0x0004

/* Inbound canned messages */
#define	NO_ACCEPT				0x0008
#define	REJET					0x0200
#define	SIGN_OFF				0x0400
#define	CHARGE_NEW				0x0F00
#define RANG					0x0C00
#define VEH_OUT					0x0100

#define	ON_SITE 				0x0500
#define CHARGE_NO				0x0B00
#define ACCPT_NEW				0x0E00
#define	PROMPT_DISP				0x0700
#define	HIA_NOTIFICATION		0x0800
#define OPTIONS					0x0E01
#define ASSIGN_DISP				0x0C00
#define OFFER_DISP				0x0D00
#define JOB_CPLT				0x0F02
#define CPAM_REPLY				0x0E02
#define	METER_ON				0x0007
#define GPS_UPDATE				0x0000
#define STATUS_DISP				0x0900
#define MSG_DISP				0x0B00
#define ALRY_BOOKED_IN			0x0705
#define BASCULE					0x0A00
#define SEND_BOOKOFF			0x0D00
#define ACK_CENTRAL				0x0F06
#define GPS_REQUEST				0x0F0F

/* Message envoyé au WING */
#define NULL_MSG				0x0000


/* Control */
#define ACK						0xC0
#define ALIVE					0xA0


#define MAX_ENCHERES	6


/**************************************** Handle des boutons de la boite de dialogue ***********/
// Edit Box
CEdit* pWndLogCourse;
CEdit* pWndLogMsg;
CEdit* pWndLogStatus;
CEdit* pWndLogLog;

// Edit Box
CWnd* pWndVersion;
CWnd* pWndLat;
CWnd* pWndLong;
CWnd* pWndAddr;
CWnd* pWndChf;
CWnd* pWndIMEI;
CWnd* pWndRadType;
CWnd* pWndNbZone;
CWnd* pWndNbDelai;
CWnd* pWndMdt;
CWnd* pWndNSS;
CWnd* pWndCO;
CWnd* pWndSEQ;
CWnd* pWndRlvMontant;
CWnd* pWndRlvPlafond;
CWnd* pWndRlvZone;
CWnd* pWndRlvAtt;
CWnd* pWndNumCourse;
CWnd* pWndNomOperateur;
CWnd* pWndDelaiBascule;
CWnd* pWndDelaiEncheres;
CWnd *pWndAutoApproche;
CWnd *pWndAutoSurPlace;
CWnd *pWndAutoCharge;
CWnd *pWndStatDistData;

// Combo boxes
CComboBox* pWndRlvZoneFlag;
CComboBox* pWndRlvRouteFlag;

// Bouton
CButton *pWndMeterOn;
CButton *pWndMeterOff;
CButton *pWndHia;
CButton *pWndSignIn;
CButton *pWndSignOff;
CButton *pWndBookOff;
CButton *pWndBookIn;
CButton *pWndPause;
CButton *pWndEnDir;
CButton *pWndAcptNew;
CButton *pWndNoAcpt;
CButton *pWndAcptDelai;
CButton *pWndSurPlace;
CButton *pWndChargeNew;
CButton *pWndOuiCharge;
CButton *pWndNonCharge;
CButton *pWndRang;
CButton *pWndBascule;
CButton *pWndChargeNo;
CButton *pWndKillmdt;
CButton *pWndAmeli;
CButton *pWndGmap;
CButton *pWndSon;
CButton *pWndRlvCpam;
CButton *pWndReleve;
CTabCtrl *pWndTab;
CButton *pWndWing1;
CButton *pWndWing2;
CButton *pWndAbandon;
CButton *pWndStationImp;
CButton *pWndBasculeOper;
CButton *pWndEncheres[MAX_ENCHERES];
CButton *pWndRunAuto;
CButton *pWndStatDistButton;
CButton *pWndRejet;

/****************************************** Thread ********************************************************/
DWORD WINAPI RcvGPRSPacket( LPVOID lpParam );		/* Thread Reception des messages WING et traitement	*/
DWORD WINAPI SndGPS_UPDATE( LPVOID lpParam );		/* Thread GPS_UPDATE	*/
DWORD WINAPI MyThreadLog( LPVOID pParam );			/* Thread affichage des logs */



/***************************************** Structure de données WING *****************************************/
struct WINGPacket {
	unsigned int mdt;					/* 4 Octets			*/
	unsigned int PacketLen; 			/* 4 Octets			*/
	unsigned short int sequence;		/* 2 Octets			*/
	unsigned short int fct;				/* 2 Octets			*/
	unsigned char control;				/* 1 Octet			*/
	char BodyData[BODY_SIZE];			/* BODY_SIZE Octets	*/
};


struct MDT {
	char meter_status;		/* A= Libre, E=chargé */
	short int sequence;
	unsigned int id;
	int block;
	int fin;
	BOOLEAN autorise;	// AUTORISE reçu ou non
	CString Version;
	CString zone;
	CString delai;
	CString lat;
	CString lon;
	CString addr;
	CString chf;
	CString IMEI;
	CString NSS;
	CString CO;
	CString SEQ;
	CString RadType;
	CString RlvMontant;
	CString RlvPlafond;
	CString RlvZone;
	CString RlvAtt;
	CString RlvRouteFlag; 
	CString RlvZoneFlag; 
	CString NumCourse;
	CString DelaiBascule;
	CString DelaiEncheres;
	CString Enchere[MAX_ENCHERES];
	CString AutoApproche;
	CString AutoSurPlace;
	CString AutoCharge;
	CString StatDistData;

	WINGPacket WingMsg;
	SOCKET ConnectSocket;
	char LogMsg[BODY_SIZE*16*32*2*2];
	char StatusMsg[BODY_SIZE*16];
	char CourseMsg[BODY_SIZE*16];
	char PromptMsg[BODY_SIZE*16];
};

/***************************************** Variable globales *****************************************/
MDT mdt[MAX_MDT];
DWORD   dwRcvGPRSPacketId[MAX_MDT], dwGPSUpdateId[MAX_MDT], dwMyThreadLogId, dwTimerEncheresId[MAX_MDT], dwTimerBasculeId[MAX_MDT],
		dwThreadAutomateId[MAX_MDT];
HANDLE  hRcvGPRSPacket[MAX_MDT], hGPSUpdate[MAX_MDT], hMyThreadLog, hTimerEncheres[MAX_MDT], hTimerBascule[MAX_MDT], hThreadAutomate[MAX_MDT]; 
bool son = true;

int maxtab=0,current_tab=0, StopLog = 0;

/* Font du log */
CFont pNewFont;