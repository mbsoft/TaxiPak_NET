// StarGUIDlg.cpp : fichier d'implémentation
//


#include "stdafx.h"
#include "StarGUI.h"
#include "StarGUIDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

#include "G7WingVar.h"

#include <mmsystem.h>
#pragma comment(lib, "winmm.lib")

#define _CRT_SECURE_NO_WARNINGS
#define LG_ENCHERE		7
#define LG_BUF_GMAPS	300000

static char WING_LIST[2][13] = { "172.27.30.20", "172.16.1.134" };
static int wing_number = 0;		// numéro du wing dans WING_LIST[]

struct OPER_LISTE_T {
	TCHAR nom[10];
	TCHAR code[10];

}  OPER_LISTE[2] = { {L"SFR", L"PDN1"}, {L"Bouygues", L"PDN2"} };

static int oper_number = 0;		// numéro de l'opérateur dans OPER_LIST[]

/* Algorithme d'encryptage GPS */
static char * EncodeNew(char * in) {
	static char Out [32];
	char a,b,c;
	int  i, j;

    memset(Out,'\0',32);
    for (i=0, j=0; i <=18; i+=3, j+=2)
    {
		a = in[i]-0x30;
		b = in[i+1]-0x30;
		c = in[i+2]-0x30;

		Out[j]   = ((a << 2) & 0x3C);
		Out[j]  |= ((b >> 2) & 0x03);

		Out[j+1]  = ((b << 4) & 0x30);
		Out[j+1] |= (c & 0x0F);

		Out[j]	 += 0x20;
		Out[j+1] += 0x20;
		Out[j+2]  = 0;
    }

	return Out;
}

class CString2 : public CString 
{
private : 
	char s[1025];

public :
	
	CString2 (CString ch) : CString (ch) {}

	char *GetChar() {
		memset(s, '\0',1025);
		WideCharToMultiByte(CP_ACP, 0, this->GetBuffer(), -1, s, 1024, NULL, NULL);
		return s;
	}
};


/* Envoi des msg GPS*/
char * GPS_Msg(int number)
{
	char clat[256], clong[256], gps_data[32];
	CString lat,lon;
	SYSTEMTIME st;
	
	GetSystemTime(&st);
	
	/* S'il sagit du MDT en 1er plan, on utilise le lat/long affiché */
	if(number == current_tab) {
		pWndLat->GetWindowText(lat);
		pWndLong->GetWindowText(lon);
	} else {
		lat = mdt[number].lat;
		lon = mdt[number].lon;
	}

	/* On supprime les ".", les "," et les " " */
	lat.Replace(_T("."),_T(""));
	lon.Replace(_T("."),_T(""));
	lat.Replace(_T(","),_T(""));
	lon.Replace(_T(","),_T(""));
	lat.Replace(_T(" "),_T(""));
	lon.Replace(_T(" "),_T(""));
	WideCharToMultiByte(CP_ACP, 0, lat, -1, clat, 256, NULL, NULL); 
	WideCharToMultiByte(CP_ACP, 0, lon, -1, clong, 256, NULL, NULL); 

	/* On formate le message */
	sprintf(gps_data,"%s%s%05i",&clat[1],clong,st.wHour*60*60+st.wMinute*60+st.wSecond);

	char buff[32];
	sprintf(buff,"%s",EncodeNew(gps_data));

	/* On le retourne encrypté */
	return EncodeNew(gps_data);
}

int connect_to_wing(int number)
{
	WSADATA wsaData;
	sockaddr_in clientService; 

	if (mdt[number].ConnectSocket != NULL) {
		Sleep(10);
		return 1;
	}

	//Connection au Wing...
	sprintf(mdt[number].LogMsg,"%s\r\nConnecting to WING %s...",mdt[number].LogMsg,WING_LIST[wing_number]);

	/* Socket */
	WSAStartup(MAKEWORD(2,2), &wsaData);
	mdt[number].ConnectSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);

	clientService.sin_family = AF_INET;
	clientService.sin_addr.s_addr = inet_addr(WING_LIST[wing_number]);
	clientService.sin_port = htons(WING_PORT);

	// Connect to server.
	if (connect( mdt[number].ConnectSocket, (SOCKADDR*) &clientService, sizeof(clientService) ) == SOCKET_ERROR) {
		WSACleanup();
		sprintf(mdt[number].LogMsg,"%sFAILED",mdt[number].LogMsg);
		return 0;
	} else {
		sprintf(mdt[number].LogMsg,"%sCONNECTED",mdt[number].LogMsg);
		return 1;
	}

}

int SndGPRSPacket(int number, short int MsgType, char* data, char* msg)
{
	WINGPacket Packet;
	int len;

	/* ID du MDT */
	Packet.mdt		=	mdt[number].id;
	Packet.fct		=	MsgType;
	/* TODO: impletmenter les sequences + octet de controle*/
	Packet.control	=	0;
	Packet.sequence =	mdt[number].sequence++;
	/* Calcul de la taille du packet */
	len = strlen(data);
	Packet.PacketLen = len;
	/* Insertion du corps du message */
	sprintf((char *)&Packet.BodyData,"%s",data);

	/* Envoi du packet	*/
	while(send(mdt[number].ConnectSocket,(char *)&Packet,len+13,0) == SOCKET_ERROR) {
		connect_to_wing(number);
		Sleep(1000);
	}

//	sprintf(mdt[number].LogMsg,"%s\r\nO [%i]\tFct=[%i], Data=[%s]",mdt[number].LogMsg,Packet.sequence,Packet.fct,Packet.BodyData);
	sprintf(mdt[number].LogMsg,"%s\r\n<[%.6i]\t%s",mdt[number].LogMsg,Packet.sequence,msg);

	return 0;
}

void Ack(int number, unsigned short sequence)
{
	WINGPacket Packet;

	/* ID du MDT */
	Packet.mdt		=	mdt[number].id;
	Packet.fct		=	NULL_MSG;
	/* TODO: impletmenter les sequences + octet de controle*/
	Packet.control	=	ACK;
	Packet.sequence =	sequence;
	/* Calcul de la taille du packet */
	Packet.PacketLen = 0;
	/* Insertion du corps du message */
	sprintf((char *)&Packet.BodyData,"");

	/* Envoi du packet	*/
	while(send(mdt[number].ConnectSocket,(char *)&Packet,13,0) == SOCKET_ERROR) {
		connect_to_wing(number);
		Sleep(10);
	}
//	sprintf(mdt[number].LogMsg,"%s\t[ACK %i]",mdt[number].LogMsg,sequence);
	sprintf(mdt[number].LogMsg,"%s",mdt[number].LogMsg);

}

void HIANotification()
{
	char cIMEI[256];
	WideCharToMultiByte(CP_ACP, 0, mdt[current_tab].IMEI, -1, cIMEI, 256, NULL, NULL); 

	/* HIA_NOTIFICATION */
	char BufMsg[BODY_SIZE];
	sprintf(BufMsg,"IP:%s IMEI:%s%s%c",MDT_IP,cIMEI,GPS_Msg(current_tab),mdt[current_tab].meter_status);
	SndGPRSPacket(current_tab,HIA_NOTIFICATION,BufMsg, "HIA");
}

// HIA pour Multi GPRS, avec type = C (Connexion) ou D (Déconnexion)
void HIA(TCHAR type)
{
	char BufMsg[BODY_SIZE];
	char BufData[BODY_SIZE];
	char BufLog[BODY_SIZE+20];
	CString2 imei(mdt[current_tab].IMEI);
	CString2 nomOpe(OPER_LISTE[oper_number].nom);
	CString2 codeOpe(OPER_LISTE[oper_number].code);

	if (type == 'C')
	{
		sprintf(BufData," C*IP:%s*IMEI:%s*%s*%s%c",MDT_IP,imei.GetChar(),codeOpe.GetChar(),GPS_Msg(current_tab),mdt[current_tab].meter_status);
	}
	else
	{
		sprintf(BufData," D*%s*%s%c",codeOpe.GetChar(),GPS_Msg(current_tab),mdt[current_tab].meter_status);
	}
	sprintf(BufMsg,"#21%s",BufData);
	sprintf(BufLog,"HIA%s",BufData);
	SndGPRSPacket(current_tab,NULL_MSG,BufMsg, BufLog);
}


void SendGPSUpdate(int mdt_number)
{
	char BufMsg[BODY_SIZE];
	sprintf(BufMsg,"#15D%s%c%%",GPS_Msg(mdt_number),mdt[mdt_number].meter_status);
	SndGPRSPacket(mdt_number,NULL_MSG,BufMsg, "GPS_UPDATE");
}


/* Debut de travail  */
void SignIn()
{
	char BufData[BODY_SIZE], BufMsg[BODY_SIZE], BufLog[80];
	CString2 chauffeur(mdt[current_tab].chf);
	CString2 version(mdt[current_tab].Version);
	CString2 radType(mdt[current_tab].RadType);

	sprintf(BufData, "%s*%s*2.0.38*5.6.1*2011.03*1.24.0*%s*%s%c", chauffeur.GetChar(), version.GetChar(), radType.GetChar(), 
		GPS_Msg(current_tab), mdt[current_tab].meter_status);
	sprintf(BufMsg, "#01%s", BufData);
	sprintf(BufLog, "SIGN_IN %s", BufData);
	SndGPRSPacket(current_tab, NULL_MSG, BufMsg, BufLog);
}

/* Fin de travail */
void SignOff()
{
	SndGPRSPacket(current_tab,SIGN_OFF,"","SIGN_OFF");
}

/* PAUSE */
void Pause()
{
	SndGPRSPacket(current_tab,0x0700,"", "PAUSE");
}

/* Inscription zone */
void BookIn(char *zone) 
{
	char BufMsg[BODY_SIZE];
	sprintf(BufMsg,"#02%s%%%%YYX%s%c",zone,GPS_Msg(current_tab),mdt[current_tab].meter_status);
	SndGPRSPacket(current_tab,NULL_MSG,BufMsg, "BOOK_IN");
}

/* Desinscription */
void BookOff()
{
  	char BufMsg[BODY_SIZE];
	sprintf(BufMsg,"%%YYX%s%c",GPS_Msg(current_tab),mdt[current_tab].meter_status);
	SndGPRSPacket(current_tab,SEND_BOOKOFF,BufMsg, "BOOKED_OFF");
}

/* Mise à jour du Rang */
void Rang() {
  	char BufMsg[BODY_SIZE];
	sprintf(BufMsg,"%%YYX%s%c",GPS_Msg(current_tab),mdt[current_tab].meter_status);
	SndGPRSPacket(current_tab,RANG,BufMsg, "RANG");
	//SndGPRSPacket(current_tab,RANG,"", "RANG");
}

/* Bascule entre deux types de flotte */
void Bascule() {
  	//char BufMsg[BODY_SIZE];
	//sprintf(BufMsg,"%%YYX%s%c",GPS_Msg(current_tab),mdt[current_tab].meter_status);
	SndGPRSPacket(current_tab,BASCULE,"", "BASCULE");
}

/* Inscription en direction de */
void EnDirection(char *zone)
{
	char BufMsg[BODY_SIZE];
	sprintf(BufMsg,"#03%s%%%%YYX%s%c",zone,GPS_Msg(current_tab),mdt[current_tab].meter_status);
	SndGPRSPacket(current_tab,NULL_MSG,BufMsg, "EN DIRECTION");
}


/* */
void MeterOn(int tabNum = current_tab)
{
	char BufMsg[BODY_SIZE];
	mdt[tabNum].meter_status = 'E';
	sprintf(BufMsg,"%%YYX%s%c",GPS_Msg(tabNum),mdt[tabNum].meter_status);
	SndGPRSPacket(tabNum,METER_ON,BufMsg, "METER_ON");
}


void MeterOff(int tabNum = current_tab)
{
	char BufMsg[BODY_SIZE];
	mdt[tabNum].meter_status = 'A';
	sprintf(BufMsg,"#15G%s%c%%",GPS_Msg(tabNum),mdt[tabNum].meter_status);
	SndGPRSPacket(tabNum,NULL_MSG,BufMsg, "MOFF_GPS");
}

/* Refus de la course */
void NoAccept()
{
	char BufMsg[BODY_SIZE];
	sprintf(BufMsg,"%%YYX%s%c",GPS_Msg(current_tab),mdt[current_tab].meter_status);
	SndGPRSPacket(current_tab,NO_ACCEPT,BufMsg, "NO_ACCEPT");
}

/* Delai */
void AcceptDelai(int delai)
{
	char BufMsg[BODY_SIZE];
	//sprintf(BufMsg,"#11%02i%%%%YYX%s%c",delai,GPS_Msg(current_tab),mdt[current_tab].meter_status);
	sprintf(BufMsg,"#11%02i%%%%YYX",delai);
	SndGPRSPacket(current_tab,NULL_MSG,BufMsg, "DELAI");
}

/* Course acceptée */
void AcceptNew(int tabNum = current_tab)
{
	char BufMsg[BODY_SIZE];
	sprintf(BufMsg,"%%YYX%s%c",GPS_Msg(tabNum),mdt[current_tab].meter_status);
	//SndGPRSPacket(tabNum,ACCPT_NEW,BufMsg, "ACCEPT NEW");
	SndGPRSPacket(tabNum,ACCPT_NEW,"", "ACCEPT NEW");
}

void CStarGUIDlg::OnBnClickedButRejet()
{
	SndGPRSPacket(current_tab, REJET, "", "REJET");
}

/* Indique que l'on est arrivé sur place */
void SurPlace(int tabNum = current_tab)
{
	char BufMsg[BODY_SIZE];
	sprintf(BufMsg,"%%YYX%s%c",GPS_Msg(tabNum),mdt[tabNum].meter_status);
	SndGPRSPacket(tabNum,ON_SITE,BufMsg, "SUR_PLACE");
}

/* Surplace mais pas de client */
void NonCharge()
{
	char BufMsg[BODY_SIZE];
	sprintf(BufMsg,"%%YYX%s%c",GPS_Msg(current_tab),mdt[current_tab].meter_status);
	SndGPRSPacket(current_tab,CHARGE_NO,BufMsg, "NON_CHARGE");
}

/* Indique à TaxiPak qu'on a chargé un client */
void ChargeNew(int tabNum = current_tab)
{
	char BufMsg[BODY_SIZE];
	sprintf(BufMsg,"%%YYX%s%c",GPS_Msg(tabNum),mdt[tabNum].meter_status);
	SndGPRSPacket(tabNum,CHARGE_NEW,BufMsg, "CHARGE_NEW");
}

void MsgChauffeur(int num, char* lib)
{
	char BufMsg[BODY_SIZE];
	sprintf(BufMsg,"#04%d%%%%YYX%s%c",num,GPS_Msg(current_tab),mdt[current_tab].meter_status);
	SndGPRSPacket(current_tab,NULL_MSG,BufMsg,lib);
}

/* OUI */
void Oui()
{
	MsgChauffeur(1,"OUI");
}

/* NON */
void Non()
{
	MsgChauffeur(2,"NON");
}

/* cpamRlv */
void CpamRlvMsg() 
{
	char BufMsg[BODY_SIZE];
	char MsgSend[BODY_SIZE];
	char s[256];
	WideCharToMultiByte(CP_ACP, 0, mdt[current_tab].chf, -1, s, 256, NULL, NULL); 
	sprintf(BufMsg,"#19%s;00042;0100005293;9999999999;184103604405747;;;017523692;000000;091213;1;1;10;;;091084;X;1;;090909;000000;0000000000;;;0000000000;;;123456789;123;9980ZZ92;GRANDVEA;MAR;B;002500;000000;000000;05;002375;001544;000831;2;%%D%%E",s);
	sprintf(MsgSend,"RLV_CPAM %s",BufMsg);
	SndGPRSPacket(current_tab,NULL_MSG,BufMsg, MsgSend);
}

/* AMELI */
void AmeliMsg(char *seq, char *nss, char *co) 
{
	char BufMsg[BODY_SIZE];
	char MsgSend[BODY_SIZE];
	sprintf(BufMsg,"#20%s;%s;%s",seq,nss,co);
	sprintf(MsgSend,"CPAM_REQUEST %s (nss=%s co=%s)",seq,nss,co);
	SndGPRSPacket(current_tab,NULL_MSG,BufMsg, MsgSend);
}

/* Envoi d'un relevé de course */
void Releve()
{
	char BufMsg[BODY_SIZE];
	char MsgSend[BODY_SIZE];

	pWndRlvMontant->GetWindowTextW(mdt[current_tab].RlvMontant);
	pWndRlvPlafond->GetWindowTextW(mdt[current_tab].RlvPlafond);
	pWndRlvZone->GetWindowTextW(mdt[current_tab].RlvZone);
	pWndRlvAtt->GetWindowTextW(mdt[current_tab].RlvAtt);
	pWndRlvRouteFlag->GetWindowTextW(mdt[current_tab].RlvRouteFlag);
	pWndRlvZoneFlag->GetWindowTextW(mdt[current_tab].RlvZoneFlag);
	pWndNumCourse->GetWindowTextW(mdt[current_tab].NumCourse);

	CString2 montant(mdt[current_tab].RlvMontant);
	CString2 plafond(mdt[current_tab].RlvPlafond);
	CString2 zone(mdt[current_tab].RlvZone);
	CString2 attente(mdt[current_tab].RlvAtt);
	CString2 numCourse(mdt[current_tab].NumCourse);
	CString2 routeFlag(mdt[current_tab].RlvRouteFlag);	
	char *flag = "";

	switch ((char)*routeFlag.GetChar())
	{
	case 'P': { flag = "PC"; break; }
	case 'A': { flag = "AR"; break; }
	case 'I': { flag = "IT"; break; }
	case 'N': { flag = "NC"; break; }
	}
	sprintf(BufMsg,"#17%#06sE*%s*0*%s*000*%s*%s*%s*%s*",montant.GetChar(),zone.GetChar(),attente.GetChar(),numCourse.GetChar(),
		plafond.GetChar(),flag,(mdt[current_tab].RlvZoneFlag == "Zone Automatique" ? "ZA" : "ZM"));
	sprintf(MsgSend,"RLV_NEW %s",BufMsg);
	SndGPRSPacket(current_tab,NULL_MSG,BufMsg,MsgSend);
}


void ChoixWing(int num)
{
	wing_number = num;
	if (num == 0) {
		pWndWing1->SetCheck(TRUE);
		pWndWing2->SetCheck(FALSE);
	}
	else {
		pWndWing1->SetCheck(FALSE);
		pWndWing2->SetCheck(TRUE);
	}
}


// Bascule d'un opérateur à l'autre
void BasculeOperateur()
{
	CString delaiSaisi;

	HIA('D');
	// récupération du délai
	pWndDelaiBascule->GetWindowTextW(delaiSaisi);
	// gel pendant ce délai
	CString2 delai(delaiSaisi);
	Sleep(atoi(delai.GetChar())*1000);
	// changement d'opérateur
	oper_number = (oper_number+1)%2;
	pWndNomOperateur->SetWindowTextW(OPER_LISTE[oper_number].nom);
	// envoi du HIA reconnexion
	HIA('C');
}


/* Postuler à une enchère */
void ConditionalBookIn(char *zone)
{
	char BufMsg[BODY_SIZE];
	char msg[80];

	sprintf(BufMsg,"#06%s*%s%c",zone,GPS_Msg(current_tab),mdt[current_tab].meter_status);
	sprintf(msg,"COND_BOOK %s",zone);
	SndGPRSPacket(current_tab,NULL_MSG,BufMsg,msg);
}


void notifyMdt(int mdt_number)
{
	/* On affiche une petite étoile si c'est pas l'onglet courant */
	if(current_tab != mdt_number) {
		TCITEM TempTab;
		CString tmp;
		tmp =  mdt[mdt_number].chf + _T("*");
		TempTab.pszText = tmp.GetBuffer();
		TempTab.mask = TCIF_TEXT;
		pWndTab->SetItem(mdt_number,&TempTab);
	}


	/* On joue le son de reception de course */
	if(son)
		PlaySound(_T("j_offer.wav"),NULL, SND_FILENAME);

}


void ResetBoutonsEncheres (int num)
{
	for (int i=0; i<MAX_ENCHERES; i++)
	{
		mdt[num].Enchere[i] = L"";
		pWndEncheres[i]->SetWindowTextW(L"");
		pWndEncheres[i]->EnableWindow(FALSE);
	}
}


DWORD WINAPI ResetEncheres (LPVOID lpParam)
{ 
	DWORD mdt_number;
	mdt_number = current_tab;

	// on ne fait rien si le thread n'est pas associé au tab courant
//	if (GetThreadId(hTimerEncheres[current_tab]) != dwTimerEncheresId[current_tab])
	if (GetCurrentThreadId() != dwTimerEncheresId[current_tab])
		return 0;

	sprintf(mdt[mdt_number].LogMsg,"%s\r\nTHREAD [Timer Encheres (%i)] CREATED",mdt[mdt_number].LogMsg,mdt_number);

	CString delai;
	pWndDelaiEncheres->GetWindowTextW(delai);
	CString2 delai2(delai);
	Sleep(atoi(delai2.GetChar())*1000);
	ResetBoutonsEncheres(mdt_number);
	CloseHandle(hTimerEncheres[mdt_number]);
	hTimerEncheres[mdt_number] = NULL;
	sprintf(mdt[mdt_number].LogMsg,"%s\r\nTHREAD [Timer Encheres (%i)] STOPPED",mdt[mdt_number].LogMsg,mdt_number);
	return 0; 
} 


void AfficherEncheres(char *data)
{
	if (strlen(data) < LG_ENCHERE)
		return;	// pas d'enchère à traiter

	for (int i = 0; (i <= MAX_ENCHERES) && ((LG_ENCHERE+1)*i < strlen(data)-1); i++)
	{
		char bid[LG_ENCHERE+1];
		memset(bid,0,LG_ENCHERE+1);
		strncpy(bid,&data[(LG_ENCHERE+1)*i],LG_ENCHERE);
		mdt[current_tab].Enchere[i] = bid;
		pWndEncheres[i]->SetWindowTextW(mdt[current_tab].Enchere[i]);
		pWndEncheres[i]->EnableWindow(TRUE);
	}
	
	// On lance le thread du timer de reset des boutons d'enchères
	if(hTimerEncheres[current_tab] == NULL) {
		hTimerEncheres[current_tab] = CreateThread(NULL, 0, ResetEncheres, NULL, 0, &dwTimerEncheresId[current_tab]);
	}
}


/* Ajout d'un nouveau MDT */
void newMdt( DWORD num, CString chf, CString id, CString IMEI, CString zone,
			CString delai, CString lat, CString lon, CString addr,
			CString NSS, CString CO, CString SEQ )
{
		char MdtIDChar [32] = {0};
		char  *stopstring;

		/* On Insert un nouvel onglet */
		pWndTab->InsertItem(num,L"");
		pWndTab->SetCurSel(num);

		/* On met des valeurs par défaut */
		mdt[num].chf	= chf;
		
		memset(MdtIDChar,'\0',32);
		WideCharToMultiByte(CP_ACP, 0, id, -1, MdtIDChar, 32, NULL, NULL); 
		mdt[num].id	= atoi(MdtIDChar);
		mdt[num].ConnectSocket = NULL;

		mdt[num].Version	= L"M1G1.27.14.0";
		mdt[num].RadType	= L"4";
		mdt[num].autorise	= FALSE;
		mdt[num].IMEI		= IMEI;
		mdt[num].zone		= zone;
		mdt[num].delai		= delai;
		mdt[num].lat		= lat;
		mdt[num].lon		= lon;
		mdt[num].addr		= addr;
		mdt[num].NSS		= NSS;
		mdt[num].CO			= CO;
		mdt[num].SEQ		= SEQ;
		mdt[num].RlvMontant	= L"600";
		mdt[num].RlvPlafond	= L"";
		mdt[num].RlvZone	= L"313";
		mdt[num].RlvAtt		= L"0";
		mdt[num].RlvRouteFlag = L"";
		mdt[num].RlvZoneFlag = L"Zone Automatique";
		mdt[num].NumCourse = L"";
		mdt[num].DelaiBascule = L"60";
		mdt[num].DelaiEncheres = L"40";
		mdt[num].meter_status = 'A';
		mdt[num].sequence = 0;
		mdt[num].block	= 0;
		mdt[num].fin	= 0;
		mdt[num].AutoApproche = L"7";
		mdt[num].AutoSurPlace = L"5";
		mdt[num].AutoCharge = L"20";
		mdt[num].StatDistData = L"DIST 1 9099 125698342 2013/02/04 09:45:25 19 46 2;1;A;W;6;21;80 2;1;P;V;2;1;0 2;1;B;V;4;3;0 3;1;B;W;0;1;0 3;1;B;V;3;4;0 3;2;B;V;3;6;100 3;2;B;W;5;22;100";

		pWndMeterOff->EnableWindow(FALSE);
		pWndMeterOn->EnableWindow(TRUE);
		hRcvGPRSPacket[num] = NULL;
		hGPSUpdate[num] = NULL;
		sprintf(mdt[num].LogMsg,"");
		sprintf(mdt[num].PromptMsg,"");
		sprintf(mdt[num].StatusMsg,"");

		// Création du thread
		hRcvGPRSPacket[num]	= CreateThread(NULL,0,RcvGPRSPacket,NULL,0,&dwRcvGPRSPacketId[num]);

		//Affichage des edit box
		pWndNbZone->SetWindowTextW(mdt[num].zone);
		pWndNbDelai->SetWindowTextW(mdt[num].delai);
		pWndLat->SetWindowTextW(mdt[num].lat);
		pWndLong->SetWindowTextW(mdt[num].lon);
		pWndChf->SetWindowTextW(mdt[num].chf);
		pWndIMEI->SetWindowTextW(mdt[num].IMEI);
		pWndNSS->SetWindowTextW(mdt[num].NSS);
		pWndCO->SetWindowTextW(mdt[num].CO);
		pWndRlvMontant->SetWindowTextW(mdt[num].RlvMontant);
		pWndRlvPlafond->SetWindowTextW(mdt[num].RlvPlafond);
		pWndRlvZone->SetWindowTextW(mdt[num].RlvZone);
		pWndRlvAtt->SetWindowTextW(mdt[num].RlvAtt);
		pWndRlvRouteFlag->SetWindowTextW(mdt[num].RlvRouteFlag);
		pWndRlvZoneFlag->SetWindowTextW(mdt[num].RlvZoneFlag);
		pWndNumCourse->SetWindowTextW(mdt[num].NumCourse);
		CString MdtIDCString (_itoa (mdt[num].id, MdtIDChar, 10));
		pWndMdt->SetWindowTextW(MdtIDCString);
		pWndMdt->SetWindowTextW(mdt[num].DelaiEncheres);
		ResetBoutonsEncheres(num);
		pWndAutoApproche->SetWindowTextW(mdt[num].AutoApproche);
		pWndAutoSurPlace->SetWindowTextW(mdt[num].AutoSurPlace);
		pWndAutoCharge->SetWindowTextW(mdt[num].AutoCharge);
		pWndStatDistData->SetWindowTextW(mdt[num].StatDistData);
}

/* Reception et traitement des messages du WING */
DWORD WINAPI RcvGPRSPacket( LPVOID lpParam ) 
{ 
	int LenWingMsg;
	DWORD mdt_number;
	mdt_number = current_tab;
	sprintf(mdt[mdt_number].LogMsg,"THREAD [RcvGPRSPacket (%i)] CREATED",mdt_number);

	if(!connect_to_wing(mdt_number))
		return 1;

	while(!mdt[mdt_number].fin) {
		LenWingMsg = recv(mdt[mdt_number].ConnectSocket,(char *)&mdt[mdt_number].WingMsg,BUF_SIZE,0);
		if(!LenWingMsg){
			//Erreur de reception
			// sprintf(mdt[mdt_number].LogMsg,"%s\r\nErreur de socket...",mdt[mdt_number].LogMsg);
			while(!connect_to_wing(mdt_number))
				Sleep(10);
		}
		else
		{
			//Analyse du packet reçu...
			switch(mdt[mdt_number].WingMsg.fct){
				case NULL_MSG:
					/* On a recu un ACK */
					if(mdt[mdt_number].WingMsg.control == ACK ){
//						sprintf(mdt[mdt_number].LogMsg,"%s\t[ACK %i]",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence);
						sprintf(mdt[mdt_number].LogMsg,"%s",mdt[mdt_number].LogMsg);
						break;
					}
					/* On a recu un ALIVE */
					if(mdt[mdt_number].WingMsg.mdt == 0 && mdt[mdt_number].WingMsg.PacketLen == 0 && mdt[mdt_number].WingMsg.control == ALIVE) {
						sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tALIVE",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence);
						break;
					}
					/* HELLO Packet */
					if(!strcmp(mdt[mdt_number].WingMsg.BodyData,"300"))
					{
						sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tHELLO",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence);
						//HIANotification();
						break;
					}

				/* Ne devrait jamais arriver */
				case SWITCH_TO_DATA:
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tSWITCH_TO_DATA",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					break;

				// Réponse Taxipak au SIGN_IN
				case AUTHORIZE:
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tAUTORISE",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					mdt[mdt_number].autorise = TRUE;
					sprintf(mdt[mdt_number].StatusMsg,"IDENTIFIE");
					/* On joue le son sign in*/
					if(son)
						PlaySound(_T("signin.wav"),NULL, SND_FILENAME);
					break;

				/* GROUP ASSIGN */
				case GROUP_ASSGN:
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tGROUP_ASSGN (%s)",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence,mdt[mdt_number].WingMsg.BodyData);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					break;

				/* DISP */
				case PROMPT_DISP:
					sprintf(mdt[mdt_number].PromptMsg,"%s",mdt[mdt_number].WingMsg.BodyData);
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tPROMPT_DISP (%s)",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence,mdt[mdt_number].WingMsg.BodyData);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					/* CODE INVAL */
					if(!strcmp(mdt[mdt_number].WingMsg.BodyData,"CODE INVAL")) {
						sprintf(mdt[mdt_number].StatusMsg,"");
					}
					if(!strcmp(mdt[mdt_number].WingMsg.BodyData,"PAS IDENTIFIE")) {
						sprintf(mdt[mdt_number].StatusMsg,"");
					}
					break;

				/* Offre de course */
				case OFFER_DISP:
					sprintf(mdt[mdt_number].StatusMsg,"");
					sprintf(mdt[mdt_number].CourseMsg,"%s",mdt[mdt_number].WingMsg.BodyData);
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tOFFER_DISP (%s)",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence,mdt[mdt_number].WingMsg.BodyData);
					notifyMdt(mdt_number);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					break;

				case ASSIGN_DISP:
					sprintf(mdt[mdt_number].StatusMsg,"");
					sprintf(mdt[mdt_number].CourseMsg,"%s",mdt[mdt_number].WingMsg.BodyData);
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tASSIGN_DISP (%s)",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence,mdt[mdt_number].WingMsg.BodyData);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					break;

				/* JOB CPLT */
				case JOB_CPLT:
					sprintf(mdt[mdt_number].StatusMsg,"");
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tJOB_CPLT (%s)",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence,mdt[mdt_number].WingMsg.BodyData);
					{
						CString buf(mdt[mdt_number].WingMsg.BodyData, 10);
						mdt[mdt_number].NumCourse = buf;
					}
					pWndNumCourse->SetWindowTextW(mdt[current_tab].NumCourse);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					break;
								
				case DEAUTH:
					sprintf(mdt[mdt_number].CourseMsg,"");
					sprintf(mdt[mdt_number].PromptMsg,"");
					sprintf(mdt[mdt_number].StatusMsg,"");
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tDEAUTH",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					mdt[mdt_number].autorise = FALSE;

					char BufMsg[BODY_SIZE];
					sprintf(BufMsg, "#22%%%%YYX%s%c", GPS_Msg(current_tab), mdt[current_tab].meter_status);
					SndGPRSPacket(current_tab, NULL_MSG, BufMsg, "VEH_IN");
					break;

				case OPTIONS:
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tOPTIONS (%s)",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence, mdt[mdt_number].WingMsg.BodyData);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					break;
				
				case STATUS_DISP:
					sprintf(mdt[mdt_number].StatusMsg,"%s",mdt[mdt_number].WingMsg.BodyData);		
					sprintf(mdt[mdt_number].PromptMsg,"");
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tSTATUS_DISP (%s)",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence, mdt[mdt_number].WingMsg.BodyData);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					/* On joue le son sign in*/
					if(son)
						PlaySound(_T("book.wav"),NULL, SND_FILENAME);
					break;

				case MSG_DISP:
					sprintf(mdt[mdt_number].CourseMsg,"%s",mdt[mdt_number].WingMsg.BodyData);
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tMSG_DISP (%s)",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence, mdt[mdt_number].WingMsg.BodyData);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					break;

				case ALRY_BOOKED_IN:
					sprintf(mdt[mdt_number].PromptMsg,"%s",mdt[mdt_number].WingMsg.BodyData);
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tALRY_BOOKED_IN (%s)",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence, mdt[mdt_number].WingMsg.BodyData);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					break;

				case NOT_BOOKED_IN:
					sprintf(mdt[mdt_number].PromptMsg,"%s",mdt[mdt_number].WingMsg.BodyData);
					sprintf(mdt[mdt_number].StatusMsg,"");
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tNOT_BOOKED_IN",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					break;

				case INV_ZONE:
					strcpy(mdt[mdt_number].PromptMsg,"");
					strcpy(mdt[mdt_number].StatusMsg,"IDENTIFIE");
					sprintf(mdt[mdt_number].CourseMsg,"");
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tINV_ZONE",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					break;

				case BOOKED_OFF:
					sprintf(mdt[mdt_number].PromptMsg,"%s",mdt[mdt_number].WingMsg.BodyData);
					sprintf(mdt[mdt_number].StatusMsg,"");
					sprintf(mdt[mdt_number].CourseMsg,"");
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tBOOKED_OFF",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					break;

				/* AMELI */
				case CPAM_REPLY:
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tCPAM_REPLY %s",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence, mdt[mdt_number].WingMsg.BodyData);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					break;

				// Réponse Taxipak sur RLV_NEW ou RLV_CPAM
				case ACK_CENTRAL:
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tACK_CENTRAL %s",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence, mdt[mdt_number].WingMsg.BodyData);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					break;

				// Requête G : demande l'envoi d'un message GPS_UPDATE
				case GPS_REQUEST:
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tGPS_REQUEST",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					SendGPSUpdate(mdt_number);
					break;

				case ENCHERES:
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tENCHERES (ZONE_BCST %s)",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence, mdt[mdt_number].WingMsg.BodyData);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					AfficherEncheres(mdt[mdt_number].WingMsg.BodyData);
					break;

				default : 
					sprintf(mdt[mdt_number].LogMsg,"%s\r\n>[%.6i]\tUNKNOW! (MID=[%i], Fct=[%i], Data=[%s], Size=[%i], Ctrl=[%i])",mdt[mdt_number].LogMsg,mdt[mdt_number].WingMsg.sequence,mdt[mdt_number].WingMsg.mdt,mdt[mdt_number].WingMsg.fct,mdt[mdt_number].WingMsg.BodyData,mdt[mdt_number].WingMsg.PacketLen,mdt[mdt_number].WingMsg.control);
					Ack(mdt_number,mdt[mdt_number].WingMsg.sequence);
					break;
			}

			/* On remet le packet de reception à 0 */
			memset((void *)&mdt[mdt_number].WingMsg,'\0',sizeof(WINGPacket));
		}
	}

	//Attente des packets...
	sprintf(mdt[mdt_number].LogMsg,"THREAD [RcvGPRSPacket (%i)] STOPPED",mdt_number);
	return 1; 
} 


/* GPS_UPDATE, envoie une trace GPS toutes les GPS_UPDATE_TIMER */
DWORD WINAPI SndGPS_UPDATE( LPVOID lpParam ) 
{ 
	DWORD mdt_number;
	mdt_number = current_tab;
	sprintf(mdt[mdt_number].LogMsg,"%s\r\nTHREAD [GPS_UPDATE (%i)] CREATED",mdt[mdt_number].LogMsg,mdt_number);
	int i=0;

	while(!mdt[mdt_number].fin) {
		Sleep(1000); i+=10;
		if(i >= GPS_UPDATE_TIMER) {
			SendGPSUpdate(mdt_number);
			i=0;
		}
	}

	sprintf(mdt[mdt_number].LogMsg,"%s\r\nTHREAD [GPS_UPDATE (%i)] STOPPED",mdt[mdt_number].LogMsg,mdt_number);
	return 1; 
} 


/* Affichage du log */
DWORD WINAPI MyThreadLog( LPVOID pParam )
{
	CString temp;
	int lenlog = 0,lennew = 0, oldtab = 0;

	while(StopLog){
		//Str différentes ?
		if( lenlog != lennew || current_tab != oldtab) {
			temp = CString(&mdt[current_tab].CourseMsg[0]);
			temp.Replace(_T("%R"),_T("\r\n"));
			pWndLogCourse->SetWindowTextW(temp);
			pWndLogStatus->SetWindowTextW(CString(&mdt[current_tab].StatusMsg[0]));
			pWndLogMsg->SetWindowTextW(CString(&mdt[current_tab].PromptMsg[0]));
			pWndLogLog->SetWindowTextW (CString(&mdt[current_tab].LogMsg[0]));// + _T("\r\n"));
			pWndLogLog->SetSel(0xffff, 0xffff);  //select position after last char in editbox
			lenlog = lennew;
			oldtab = current_tab;
			// mise à jour de l'accessibilité des champs Version et Rad Type
			pWndVersion->EnableWindow(! mdt[current_tab].autorise);
			pWndRadType->EnableWindow(! mdt[current_tab].autorise);
		}
		lennew = strlen(mdt[current_tab].StatusMsg) + strlen(mdt[current_tab].PromptMsg) + strlen(mdt[current_tab].LogMsg);
		Sleep(10);
	}

	return 0;
}



CStarGUIDlg::CStarGUIDlg(CWnd* pParent /*=NULL*/)
	: CDialog(CStarGUIDlg::IDD, pParent)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
	this->bufGmaps = (char *)malloc(LG_BUF_GMAPS);
}

void CStarGUIDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CStarGUIDlg, CDialog)
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	//}}AFX_MSG_MAP
	ON_BN_CLICKED(IDC_HIA, &CStarGUIDlg::OnBnClickedHia)
	ON_BN_CLICKED(IDC_SIGNIN, &CStarGUIDlg::OnBnClickedSignin)
	ON_BN_CLICKED(IDC_SIGNOFF, &CStarGUIDlg::OnBnClickedSignoff)
	ON_BN_CLICKED(IDC_BOOKOFF, &CStarGUIDlg::OnBnClickedBookoff)
	ON_BN_CLICKED(IDC_PAUSE, &CStarGUIDlg::OnBnClickedPause)
	ON_BN_CLICKED(IDC_BOOKIN, &CStarGUIDlg::OnBnClickedBookin)
	ON_BN_CLICKED(IDC_ENDIR, &CStarGUIDlg::OnBnClickedEndir)
	ON_BN_CLICKED(IDC_METERON, &CStarGUIDlg::OnBnClickedMeteron)
	ON_BN_CLICKED(IDC_METEROFF, &CStarGUIDlg::OnBnClickedMeteroff)
	ON_BN_CLICKED(IDC_NOACPT, &CStarGUIDlg::OnBnClickedNoacpt)
	ON_BN_CLICKED(IDC_ACPTNEW, &CStarGUIDlg::OnBnClickedAcptnew)
	ON_BN_CLICKED(IDC_DELAIS, &CStarGUIDlg::OnBnClickedDelais)
	ON_BN_CLICKED(IDC_CHARGENEW, &CStarGUIDlg::OnBnClickedChargenew)
	ON_BN_CLICKED(IDC_SURPLACE, &CStarGUIDlg::OnBnClickedSurplace)
	ON_BN_CLICKED(IDC_OUI, &CStarGUIDlg::OnBnClickedOui)
	ON_BN_CLICKED(IDC_NON, &CStarGUIDlg::OnBnClickedNon)
	ON_BN_CLICKED(IDC_RANG, &CStarGUIDlg::OnBnClickedRang)
	ON_BN_CLICKED(IDC_BASCULE, &CStarGUIDlg::OnBnClickedBascule)
	ON_NOTIFY(TCN_SELCHANGE, IDC_TAB, &CStarGUIDlg::OnTcnSelchangeTab)
	ON_BN_CLICKED(IDC_NONCHARGE, &CStarGUIDlg::OnBnClickedNoncharge)
	ON_BN_CLICKED(IDC_KILLMDT, &CStarGUIDlg::OnBnClickedKillmdt)
	ON_BN_CLICKED(IDC_AMELI, &CStarGUIDlg::OnBnClickedAmeli)
	ON_BN_CLICKED(IDC_GMAP, &CStarGUIDlg::OnBnClickedGmap)
	ON_BN_CLICKED(IDC_SON, &CStarGUIDlg::OnBnClickedSon)
	ON_BN_CLICKED(IDC_RLVCPAM, &CStarGUIDlg::OnBnClickedRlvcpam)
	ON_BN_CLICKED(IDC_RELEVE, &CStarGUIDlg::OnBnClickedReleve)
	ON_BN_CLICKED(IDC_WING1, &CStarGUIDlg::OnBnClickedWing1)
	ON_BN_CLICKED(IDC_WING2, &CStarGUIDlg::OnBnClickedWing2)
	ON_BN_CLICKED(IDC_ABANDON, &CStarGUIDlg::OnBnClickedAbandon)
	ON_BN_CLICKED(IDC_STATION_IMP, &CStarGUIDlg::OnBnClickedStationImp)
	ON_BN_CLICKED(IDC_BASCULE_OPER, &CStarGUIDlg::OnBnClickedBasculeOper)
	ON_BN_CLICKED(IDC_BID1, &CStarGUIDlg::OnBnClickedBid1)
	ON_BN_CLICKED(IDC_BID2, &CStarGUIDlg::OnBnClickedBid2)
	ON_BN_CLICKED(IDC_BID3, &CStarGUIDlg::OnBnClickedBid3)
	ON_BN_CLICKED(IDC_BID4, &CStarGUIDlg::OnBnClickedBid4)
	ON_BN_CLICKED(IDC_BID5, &CStarGUIDlg::OnBnClickedBid5)
	ON_BN_CLICKED(IDC_BID6, &CStarGUIDlg::OnBnClickedBid6)
	ON_BN_CLICKED(IDC_AUTO_RUN, &CStarGUIDlg::OnBnClickedRunAutomate)
	ON_BN_CLICKED(IDC_CONNECT, &CStarGUIDlg::OnBnClickedConnect)
	ON_BN_CLICKED(IDC_COMP_SORTIE, &CStarGUIDlg::OnBnClickedCompSortie)
	ON_BN_CLICKED(IDC_COMP_RETOUR, &CStarGUIDlg::OnBnClickedCompRetour)
	ON_BN_CLICKED(IDC_BUT_STAT_DIST, &CStarGUIDlg::OnBnClickedButStatDist)
	ON_BN_CLICKED(IDC_BUT_REJET, &CStarGUIDlg::OnBnClickedButRejet)
END_MESSAGE_MAP()


// pour éviter de fermer la fenêtre sur appui de la touche ENTER
void CStarGUIDlg::OnOK()
{
}


void SetUpperWindowState(boolean state)
{
	pWndChf->EnableWindow(state);
	pWndIMEI->EnableWindow(state);
	pWndMdt->EnableWindow(state);
	pWndHia->EnableWindow(state);
}

void SetLowerWindowState(boolean state)
{
	pWndBascule->EnableWindow(state);
	pWndSon->EnableWindow(state);
	pWndRlvCpam->EnableWindow(state);
	pWndReleve->EnableWindow(state);
	pWndBasculeOper->EnableWindow(state);
	pWndMeterOn->EnableWindow(state);
	pWndMeterOff->EnableWindow(state);
	pWndKillmdt->EnableWindow(state);
	pWndRang->EnableWindow(state);
	pWndBascule->EnableWindow(state);
	pWndSignIn->EnableWindow(state);
	pWndSignOff->EnableWindow(state);
	pWndBookOff->EnableWindow(state);
	pWndBookIn->EnableWindow(state);
	pWndPause->EnableWindow(state);
	pWndEnDir->EnableWindow(state);
	pWndAcptNew->EnableWindow(state);
	pWndNoAcpt->EnableWindow(state);
	pWndAcptDelai->EnableWindow(state);
	pWndSurPlace->EnableWindow(state);
	pWndChargeNew->EnableWindow(state);
	pWndOuiCharge->EnableWindow(state);
	pWndNonCharge->EnableWindow(state);
	pWndChargeNo->EnableWindow(state);
	pWndAmeli->EnableWindow(state);
	pWndGmap->EnableWindow(state);
	pWndNSS->EnableWindow(state);
	pWndCO->EnableWindow(state);
	pWndSEQ->EnableWindow(state);
	pWndNbDelai->EnableWindow(state);
	pWndNbZone->EnableWindow(state);
	pWndRlvMontant->EnableWindow(state);
	pWndRlvPlafond->EnableWindow(state);
	pWndRlvZone->EnableWindow(state);
	pWndRlvAtt->EnableWindow(state);
	pWndRlvRouteFlag->EnableWindow(state);
	pWndRlvZoneFlag->EnableWindow(state);
	pWndNumCourse->EnableWindow(state);
	pWndAbandon->EnableWindow(state);
	pWndStationImp->EnableWindow(state);
	// Bouton Bascule actif si pas de bascule en cours
	//pWndBasculeOper->EnableWindow(hTimerBascule[current_tab] == NULL ? TRUE : FALSE);
	pWndAutoApproche->EnableWindow(state);
	pWndAutoSurPlace->EnableWindow(state);
	pWndAutoCharge->EnableWindow(state);
	pWndRunAuto->EnableWindow(state);
	for (int i=0; i<MAX_ENCHERES; i++)
	{
		pWndEncheres[i]->EnableWindow(state);
	}
	pWndStatDistButton->EnableWindow(state);
	pWndRejet->EnableWindow(state);
}

void updateAffichage(int tabNum = current_tab)
{
	char MdtIdChar [32] = {0};
	TCITEM TempTab;

	//Affichage des edit box avec les nouvelles valeurs
	pWndVersion->SetWindowTextW(mdt[current_tab].Version);
	pWndRadType->SetWindowTextW(mdt[current_tab].RadType);
	pWndNbZone->SetWindowTextW(mdt[current_tab].zone);
	pWndNbDelai->SetWindowTextW(mdt[current_tab].delai);
	pWndLat->SetWindowTextW(mdt[current_tab].lat);
	pWndLong->SetWindowTextW(mdt[current_tab].lon);
	pWndAddr->SetWindowTextW(mdt[current_tab].addr);
	pWndChf->SetWindowTextW(mdt[current_tab].chf);
	pWndIMEI->SetWindowTextW(mdt[current_tab].IMEI);
	pWndNSS->SetWindowTextW(mdt[current_tab].NSS);
	pWndCO->SetWindowTextW(mdt[current_tab].CO);
	pWndSEQ->SetWindowTextW(mdt[current_tab].SEQ);
	pWndRlvMontant->SetWindowTextW(mdt[current_tab].RlvMontant);
	pWndRlvPlafond->SetWindowTextW(mdt[current_tab].RlvPlafond);
	pWndRlvZone->SetWindowTextW(mdt[current_tab].RlvZone);
	pWndRlvAtt->SetWindowTextW(mdt[current_tab].RlvAtt);
	pWndRlvRouteFlag->SetWindowTextW(mdt[current_tab].RlvRouteFlag);
	pWndRlvZoneFlag->SetWindowTextW(mdt[current_tab].RlvZoneFlag);
	pWndNumCourse->SetWindowTextW(mdt[current_tab].NumCourse);
	CString MdtIDCString (_itoa (mdt[current_tab].id, MdtIdChar, 10));
	pWndMdt->SetWindowTextW(MdtIDCString);
	pWndDelaiBascule->SetWindowTextW(mdt[current_tab].DelaiBascule);
	pWndDelaiEncheres->SetWindowTextW(mdt[current_tab].DelaiEncheres);
	for (int i=0; i<MAX_ENCHERES; i++)
	{
		pWndEncheres[i]->SetWindowTextW(mdt[current_tab].Enchere[i]);
	}
	pWndAutoApproche->SetWindowTextW(mdt[current_tab].AutoApproche);
	pWndAutoSurPlace->SetWindowTextW(mdt[current_tab].AutoSurPlace);
	pWndAutoCharge->SetWindowTextW(mdt[current_tab].AutoCharge);
	pWndStatDistData->SetWindowTextW(mdt[current_tab].StatDistData);

	/* Meter ON/OFF activé */
	if(mdt[current_tab].meter_status == 'A') 
	{
		pWndMeterOff->EnableWindow(FALSE);
		pWndMeterOn->EnableWindow(TRUE);
	}
	else 
	{
		pWndMeterOff->EnableWindow(TRUE);
		pWndMeterOn->EnableWindow(FALSE);
	}

	/* Le MDT est bloqué (Message HIA envoyé) */
	if(mdt[current_tab].block == 1) 
	{
		/* On met l'onglet comme il faut */
		TempTab.pszText = mdt[current_tab].chf.GetBuffer();
		TempTab.mask = TCIF_TEXT;
		pWndTab->SetItem(current_tab,&TempTab);

		SetUpperWindowState(FALSE);
		SetLowerWindowState(TRUE);
	} 
	else
	{
		SetUpperWindowState(TRUE);
		SetLowerWindowState(FALSE);
	}

	pWndVersion->EnableWindow(! mdt[current_tab].autorise);
	pWndRadType->EnableWindow(! mdt[current_tab].autorise);

}

BOOL CStarGUIDlg::OnInitDialog()
{
	CDialog::OnInitDialog();

	// Définir l'icône de cette boîte de dialogue. L'infrastructure effectue cela automatiquement
	//  lorsque la fenêtre principale de l'application n'est pas une boîte de dialogue
	SetIcon(m_hIcon, TRUE);			// Définir une grande icône
	SetIcon(m_hIcon, FALSE);		// Définir une petite icône

	// TODO : ajoutez ici une initialisation supplémentaire
	pWndLogMsg			= (CEdit*) GetDlgItem(IDC_MSG);
	pWndLogCourse		= (CEdit*) GetDlgItem(IDC_COURSE);
	pWndLogStatus		= (CEdit*) GetDlgItem(IDC_STATUS);
	pWndLogLog			= (CEdit*) GetDlgItem(IDC_LOG);
	pNewFont.CreateFont(
	   11,                        // nHeight
	   4,                         // nWidth
	   0,                         // nEscapement
	   0,                         // nOrientation
	   FW_THIN,                   // nWeight
	   FALSE,                     // bItalic
	   FALSE,                     // bUnderline
	   0,                         // cStrikeOut
	   ANSI_CHARSET,              // nCharSet
	   OUT_DEFAULT_PRECIS,        // nOutPrecision
	   CLIP_DEFAULT_PRECIS,       // nClipPrecision
	   DEFAULT_QUALITY,           // nQuality
	   FIXED_PITCH | FF_MODERN,  // nPitchAndFamily
	   L"verdana");                 // lpszFacename

	pWndLogLog->SetFont(&pNewFont,1);
	
	StopLog			= 1;
	hMyThreadLog	= CreateThread(NULL,0,MyThreadLog,NULL,0,&dwMyThreadLogId);

	pWndLat				= GetDlgItem(IDC_LAT);
	pWndLong			= GetDlgItem(IDC_LONG);
	pWndAddr			= GetDlgItem(IDC_ADDR);
	pWndChf				= GetDlgItem(IDC_CHF);
	pWndVersion			= GetDlgItem(IDC_VERSION);
	pWndRadType			= GetDlgItem(IDC_RAD_TYPE);
	pWndIMEI			= GetDlgItem(IDC_IMEI);
	pWndNbZone			= GetDlgItem(IDC_NBZONE);
	pWndNbDelai			= GetDlgItem(IDC_NBDELAI);
	pWndMdt				= GetDlgItem(IDC_MDT);
	pWndNSS				= GetDlgItem(IDC_NSS);
	pWndCO				= GetDlgItem(IDC_CO);
	pWndSEQ				= GetDlgItem(IDC_SEQ);
	pWndRlvMontant		= GetDlgItem(IDC_RLV_MONTANT);
	pWndRlvPlafond		= GetDlgItem(IDC_RLV_PLAFOND);
	pWndRlvZone			= GetDlgItem(IDC_RLV_ZONE);
	pWndRlvAtt			= GetDlgItem(IDC_RLV_ATT);
	pWndRlvRouteFlag	= (CComboBox *) GetDlgItem(IDC_RLV_ROUTE_FLAG);
	pWndRlvZoneFlag		= (CComboBox *) GetDlgItem(IDC_RLV_ZONE_FLAG);
	pWndRlvRouteFlag->AddString(_T(""));
	pWndRlvRouteFlag->AddString(_T("Plusieurs courses"));
	pWndRlvRouteFlag->AddString(_T("Aller Retour"));
	pWndRlvRouteFlag->AddString(_T("Imprévu sur trajet"));
	pWndRlvRouteFlag->AddString(_T("Non Charge"));
	pWndRlvZoneFlag->AddString(_T("Zone Automatique"));
	pWndRlvZoneFlag->AddString(_T("Zone Manuelle"));
	pWndNumCourse = GetDlgItem(IDC_NUM_COURSE);
	pWndNomOperateur = GetDlgItem(IDC_OPER_GSM);
	pWndNomOperateur->SetWindowTextW(OPER_LISTE[oper_number].nom);
	pWndDelaiBascule = GetDlgItem(IDC_DELAI_BASCULE);
//	pWndDelaiBascule->SetWindowTextW(_T("60"));
	pWndDelaiEncheres = GetDlgItem(IDC_DELAI_ENCHERES);
//	pWndDelaiEncheres->SetWindowTextW(_T("40"));
	pWndAutoApproche = GetDlgItem(IDC_AUTO_APPROCHE);
	pWndAutoSurPlace = GetDlgItem(IDC_AUTO_SURPLACE);
	pWndAutoCharge = GetDlgItem(IDC_AUTO_CHARGE);
	pWndStatDistData = GetDlgItem(IDC_EDT_STAT_DIST);

	//Initialisation des boutons
	pWndHia				= (CButton *) GetDlgItem(IDC_HIA);
	pWndMeterOn			= (CButton *) GetDlgItem(IDC_METERON);
	pWndMeterOff		= (CButton *) GetDlgItem(IDC_METEROFF);
	pWndRang			= (CButton *) GetDlgItem(IDC_RANG);
	pWndBascule			= (CButton *) GetDlgItem(IDC_BASCULE);
	pWndSignIn			= (CButton *) GetDlgItem(IDC_SIGNIN);
	pWndSignOff			= (CButton *) GetDlgItem(IDC_SIGNOFF);
	pWndBookOff			= (CButton *) GetDlgItem(IDC_BOOKOFF);
	pWndBookIn			= (CButton *) GetDlgItem(IDC_BOOKIN);
	pWndPause			= (CButton *) GetDlgItem(IDC_PAUSE);
	pWndEnDir			= (CButton *) GetDlgItem(IDC_ENDIR);
	pWndAcptNew			= (CButton *) GetDlgItem(IDC_ACPTNEW);
	pWndNoAcpt			= (CButton *) GetDlgItem(IDC_NOACPT);
	pWndAcptDelai		= (CButton *) GetDlgItem(IDC_DELAIS);
	pWndSurPlace		= (CButton *) GetDlgItem(IDC_SURPLACE);
	pWndChargeNew		= (CButton *) GetDlgItem(IDC_CHARGENEW);
	pWndOuiCharge		= (CButton *) GetDlgItem(IDC_OUI);
	pWndNonCharge		= (CButton *) GetDlgItem(IDC_NON);
	pWndChargeNo		= (CButton *) GetDlgItem(IDC_NONCHARGE);
	pWndKillmdt			= (CButton *) GetDlgItem(IDC_KILLMDT);
	pWndAmeli			= (CButton *) GetDlgItem(IDC_AMELI);
	pWndGmap			= (CButton *) GetDlgItem(IDC_GMAP);
	pWndSon				= (CButton *) GetDlgItem(IDC_SON);
	pWndRlvCpam			= (CButton *) GetDlgItem(IDC_RLVCPAM);
	pWndReleve			= (CButton *) GetDlgItem(IDC_RELEVE);
	pWndWing1			= (CButton *) GetDlgItem(IDC_WING1);
	pWndWing2			= (CButton *) GetDlgItem(IDC_WING2);
	pWndWing1->SetCheck(TRUE);
	pWndAbandon			= (CButton *) GetDlgItem(IDC_ABANDON);
	pWndStationImp		= (CButton *) GetDlgItem(IDC_STATION_IMP);
	pWndBasculeOper		= (CButton *) GetDlgItem(IDC_BASCULE_OPER);
	for (int i=0; i<MAX_ENCHERES; i++)
	{
		pWndEncheres[i] = (CButton *) GetDlgItem(IDC_BID1+i);
	}
	pWndRunAuto			= (CButton *) GetDlgItem(IDC_AUTO_RUN);
	pWndStatDistButton	= (CButton *) GetDlgItem(IDC_BUT_STAT_DIST);
	pWndRejet			= (CButton *) GetDlgItem(IDC_BUT_REJET);

	pWndTab = (CTabCtrl *) GetDlgItem(IDC_TAB);
	maxtab = 0;

	SetUpperWindowState(FALSE);
	SetLowerWindowState(FALSE);

	return TRUE;
}

CStarGUIDlg::~CStarGUIDlg()
{
	// Close all thread handles and free memory allocations.
	StopLog = 0;
	for(int i=0;i<maxtab;i++)
	{
		mdt[i].fin = 1;
		closesocket(mdt[i].ConnectSocket);

		CloseHandle(hRcvGPRSPacket[i]);
		if(mdt[i].block == 1)
			CloseHandle(hGPSUpdate[i]);
	}
	CloseHandle(hMyThreadLog);

	Sleep(100);

	free(this->bufGmaps);
}


void CStarGUIDlg::OnPaint()
{
	if (IsIconic())
	{
		CPaintDC dc(this); // contexte de périphérique pour la peinture

		SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

		// Centrer l'icône dans le rectangle client
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// Dessiner l'icône
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialog::OnPaint();
	}
}

HCURSOR CStarGUIDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}

void Clear()
{
	sprintf(mdt[current_tab].CourseMsg,"");
	sprintf(mdt[current_tab].PromptMsg,"");
	sprintf(mdt[current_tab].StatusMsg,"");
}

void CStarGUIDlg::OnBnClickedHia()
{
	TCITEM TempTab;
	CString MdtID;

	/* Récupère les paramètres HIA et les autres saisissables */
	pWndChf->GetWindowTextW(mdt[current_tab].chf);
	pWndIMEI->GetWindowTextW(mdt[current_tab].IMEI);
	pWndVersion->GetWindowTextW(mdt[current_tab].Version);
	pWndRadType->GetWindowTextW(mdt[current_tab].RadType);
	pWndAutoApproche->GetWindowTextW(mdt[current_tab].AutoApproche);
	pWndAutoSurPlace->GetWindowTextW(mdt[current_tab].AutoSurPlace);
	pWndAutoCharge->GetWindowTextW(mdt[current_tab].AutoCharge);
	pWndMdt->GetWindowTextW(MdtID);
	mdt[current_tab].id	= _wtoi(MdtID);
	mdt[current_tab].block	= 1; /* Bloque le changement de paramètre */

	/* Désactive les boutons */
	updateAffichage();

	// Choix du WiNG non autorisé
	pWndWing1->EnableWindow(FALSE);
	pWndWing2->EnableWindow(FALSE);

	/* On change le nom de l'onglet par le CHF */
	TempTab.pszText = mdt[current_tab].chf.GetBuffer();
	TempTab.mask = TCIF_TEXT;
	pWndTab->SetItem(current_tab,&TempTab);
	
	/* On lance le thread de GPS Update*/
	if(hGPSUpdate[current_tab] == NULL) {
		hGPSUpdate[current_tab]	= CreateThread(NULL,0,SndGPS_UPDATE,NULL,0,&dwGPSUpdateId[current_tab]);
	}

	/* On envoie le message HIA */
	HIA('C');
}

void CStarGUIDlg::OnBnClickedSignin()
{
	Clear();
	SignIn();
}

void CStarGUIDlg::OnBnClickedSignoff()
{
	Clear();
	SignOff();
}

void CStarGUIDlg::OnBnClickedBookoff()
{
	Clear();
	BookOff();
}

void CStarGUIDlg::OnBnClickedPause()
{
	Clear();
	Pause();
}

void CStarGUIDlg::OnBnClickedBookin()
{
	CString zone;
	char s[256];
	pWndNbZone->GetWindowText(zone);
	WideCharToMultiByte(CP_ACP, 0, zone, -1, s, 256, NULL, NULL); 
	
	Clear();
	BookIn(s) ;
}

void CStarGUIDlg::OnBnClickedEndir()
{
	CString zone;
	char s[256];
	pWndNbZone->GetWindowText(zone);
	WideCharToMultiByte(CP_ACP, 0, zone, -1, s, 256, NULL, NULL); 

	Clear();
	EnDirection(s);
}

void CStarGUIDlg::OnBnClickedMeteron()
{
	sprintf(mdt[current_tab].PromptMsg,"");
	sprintf(mdt[current_tab].StatusMsg,"");
	pWndMeterOff->EnableWindow(TRUE);
	pWndMeterOn->EnableWindow(FALSE);
	MeterOn();
}

void CStarGUIDlg::OnBnClickedMeteroff()
{
	sprintf(mdt[current_tab].PromptMsg,"");
	sprintf(mdt[current_tab].StatusMsg,"");
	pWndMeterOff->EnableWindow(FALSE);
	pWndMeterOn->EnableWindow(TRUE);
	Clear();
	MeterOff();
}

void CStarGUIDlg::OnBnClickedNoacpt()
{
	Clear();
	NoAccept();
}

void CStarGUIDlg::OnBnClickedAcptnew()
{
	Clear();
	AcceptNew();
}

void CStarGUIDlg::OnBnClickedDelais()
{

	char s[256];
	pWndNbDelai->GetWindowText(mdt[current_tab].delai);
	WideCharToMultiByte(CP_ACP, 0, mdt[current_tab].delai, -1, s, 256, NULL, NULL); 

	Clear();
	AcceptDelai(atoi(s));
}

void CStarGUIDlg::OnBnClickedChargenew()
{
	Clear();
	ChargeNew();
}

void CStarGUIDlg::OnBnClickedSurplace()
{
	Clear();
	SurPlace();
}

void CStarGUIDlg::OnBnClickedOui()
{
	Clear();
	Oui();
}

void CStarGUIDlg::OnBnClickedNon()
{
	Clear();
	Non();
}

void CStarGUIDlg::OnBnClickedRang()
{
	Clear();
	Rang();
}


void CStarGUIDlg::OnBnClickedBascule()
{
	Clear();
	Bascule();
}


void CStarGUIDlg::OnBnClickedNoncharge()
{
	NonCharge();
	Clear();
}


void CStarGUIDlg::OnBnClickedRlvcpam()
{
	Clear();
	CpamRlvMsg();
}


void CStarGUIDlg::OnBnClickedAmeli()
{
	char seq[256];
	pWndSEQ->GetWindowText(mdt[current_tab].SEQ);
	WideCharToMultiByte(CP_ACP, 0, mdt[current_tab].SEQ, -1, seq, 256, NULL, NULL); 

	char co[256];
	pWndCO->GetWindowText(mdt[current_tab].CO);
	WideCharToMultiByte(CP_ACP, 0, mdt[current_tab].CO, -1, co, 256, NULL, NULL); 

	char nss[256];
	pWndNSS->GetWindowText(mdt[current_tab].NSS);
	WideCharToMultiByte(CP_ACP, 0, mdt[current_tab].NSS, -1, nss, 256, NULL, NULL); 

	AmeliMsg(seq,nss,co);
}

void CStarGUIDlg::OnTcnSelchangeTab(NMHDR *pNMHDR, LRESULT *pResult)
{
	CString MdtID;
	int oldtab;

	//On Sauvegarde les anciennes valeurs dans l'objet mdt
	pWndVersion->GetWindowTextW(mdt[current_tab].Version);
	pWndRadType->GetWindowTextW(mdt[current_tab].RadType);
	pWndNbZone->GetWindowTextW(mdt[current_tab].zone);
	pWndNbDelai->GetWindowTextW(mdt[current_tab].delai);
	pWndLat->GetWindowTextW(mdt[current_tab].lat);
	pWndLong->GetWindowTextW(mdt[current_tab].lon);
	pWndAddr->GetWindowTextW(mdt[current_tab].addr);
	pWndChf->GetWindowTextW(mdt[current_tab].chf);
	pWndIMEI->GetWindowTextW(mdt[current_tab].IMEI);
	pWndNSS->GetWindowTextW(mdt[current_tab].NSS);
	pWndCO->GetWindowTextW(mdt[current_tab].CO);
	pWndSEQ->GetWindowTextW(mdt[current_tab].SEQ);
	pWndRlvMontant->GetWindowTextW(mdt[current_tab].RlvMontant);
	pWndRlvPlafond->GetWindowTextW(mdt[current_tab].RlvPlafond);
	pWndRlvZone->GetWindowTextW(mdt[current_tab].RlvZone);
	pWndRlvAtt->GetWindowTextW(mdt[current_tab].RlvAtt);
	pWndRlvRouteFlag->GetWindowTextW(mdt[current_tab].RlvRouteFlag);
	pWndRlvZoneFlag->GetWindowTextW(mdt[current_tab].RlvZoneFlag);
	pWndMdt->GetWindowTextW(MdtID);
	mdt[current_tab].id = _wtoi(MdtID);
	pWndDelaiBascule->GetWindowTextW(mdt[current_tab].DelaiBascule);
	pWndDelaiEncheres->GetWindowTextW(mdt[current_tab].DelaiEncheres);
	for (int i=0; i<MAX_ENCHERES; i++)
	{
		pWndEncheres[i]->GetWindowTextW(mdt[current_tab].Enchere[i]);
	}

	//On change de tab
	oldtab		= current_tab;
	current_tab = pWndTab->GetCurSel();

	//Est-ce un nouveau tab ?
	if(current_tab == maxtab) {
		if(maxtab < MAX_MDT){
			//Création d'un nouveau terminal
			newMdt(current_tab,_T("9090"), _T("2266"), _T("000000000000001"), _T("911"), _T("5"), _T("48.89908"), _T("2.30382"), _T("22-28 Rue Henri Barbusse Clichy"), _T("184103604405747"), _T("019236597"), _T("000432") );
			maxtab++;
		} else {
			//On est au max d'onglet possible
			current_tab = oldtab;
			pWndTab->SetCurSel(oldtab);
		}
	} 

	// Reinitialise l'affichage (current_tab)
	updateAffichage();

}

void CStarGUIDlg::OnBnClickedKillmdt()
{
	if(maxtab > 0)	{
		mdt[current_tab].fin = 1;
		closesocket(mdt[current_tab].ConnectSocket);
		Sleep(100);
		CloseHandle(hRcvGPRSPacket[current_tab]);
		if(mdt[current_tab].block == 1)
			CloseHandle(hGPSUpdate[current_tab]);

		pWndTab->DeleteItem(current_tab);
		newMdt(current_tab,_T("9090"), _T("2266"), _T("000000000000001"), _T("911"), _T("5"), _T("48.89908"), _T("2.30382"), _T("22-28 Rue Henri Barbusse Clichy"), _T("184103604405747"), _T("019236597"), _T("000432") );
		updateAffichage();
	}
}

bool ZoneCtrl()
{
	WSADATA wsaData;
	sockaddr_in clientService; 
	SOCKET ConnectSocket;
	struct hostent *dns;
	CString rqt,Lat,Lon;
	CString zone,infoZone;
	char s[1024],response[1024];
	int startZone,stopZone;

	dns = gethostbyname("weber");

	WSAStartup(MAKEWORD(2,2), &wsaData);
	ConnectSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
	clientService.sin_family		= AF_INET;
	clientService.sin_addr.s_addr	= *(DWORD*) dns->h_addr_list[0];
	clientService.sin_port			= htons(5554);

	// Connect to server.
	if (connect( ConnectSocket, (SOCKADDR*) &clientService, sizeof(clientService) ) == SOCKET_ERROR) {
		WSACleanup();
		sprintf(mdt[current_tab].LogMsg,"%s\r\n<\t\tFailed to connect WEBER",mdt[current_tab].LogMsg);
	} else {
		memset(s,'\0',sizeof(s));
		pWndLat->GetWindowTextW(Lat);
		pWndLong->GetWindowTextW(Lon);
		rqt = _T("<AdrZoneRqst><LatLon><Lat>") + Lat + _T("</Lat><Lon>") + Lon + _T("</Lon></LatLon></AdrZoneRqst>\n");
		WideCharToMultiByte(CP_ACP, 0, rqt, -1, s, 1024, NULL, NULL); 
		send(ConnectSocket,s,strlen(s),0); //On envoie la requete
		recv(ConnectSocket,response,sizeof(response),0); //réponse
		closesocket(ConnectSocket);

		CString rsp(response);
		if(rsp.Find(_T("ADR_ZONE_PBM")) == -1) {
			//OK
			startZone = rsp.Find(_T("<ZoneNum>")) + strlen("<ZoneNum>");
			stopZone  = rsp.Find(_T("</ZoneNum>"));
			zone = rsp.Mid(startZone,stopZone-startZone);

			startZone = rsp.Find(_T("<ZoneName>")) + strlen("<ZoneName>");
			stopZone  = rsp.Find(_T("</ZoneName>"));
			infoZone = rsp.Mid(startZone,stopZone-startZone);
			WideCharToMultiByte(CP_ACP, 0, infoZone, -1, response, 1024, NULL, NULL); 

			pWndNbZone->SetWindowTextW(zone);
			mdt[current_tab].zone = zone;

			sprintf(mdt[current_tab].LogMsg,"%s\r\n<\t\tlat/long to zone OK (%s)",mdt[current_tab].LogMsg,response);
			return true;
		} else {
			//Probleme de zone
			sprintf(mdt[current_tab].LogMsg,"%s\r\n<\t\tFailed: ADR_ZONE_PBM",mdt[current_tab].LogMsg);
			return false;
		}
	}

	return false;
}


CString FormateLatLong(CString data, int startLatLong, CString prefixeLatLong, int longueur)
{
	CString latLong;

	startLatLong = data.Find(prefixeLatLong, startLatLong) + prefixeLatLong.GetLength();
	latLong = data.Mid(startLatLong, longueur);

	// suppression de la virgule si présente
	int posVirg = latLong.Find(_T(","), 0);
	if (posVirg > 0)
	{
		latLong = latLong.Mid(0, posVirg-1);
	}

	// right trim
	latLong = CString(latLong + L"0000").Mid(0, longueur);

	return latLong;
}


void CStarGUIDlg::OnBnClickedGmap()
{
	WSADATA wsaData;
	sockaddr_in clientService; 
	SOCKET ConnectSocket;
	struct hostent *dns;
	char s[1024],response[1024];
	CString rsp,tmp,lat,lng;
	int len,startLat,startLng;

	//Recherche inversée des coordonnées
	pWndAddr->GetWindowTextW(mdt[current_tab].addr);
	tmp = mdt[current_tab].addr;
	sprintf(mdt[current_tab].LogMsg,"%s\r\n>\t\tSending ADDR RQT to google maps...",mdt[current_tab].LogMsg);

	//On formate la rqt
	tmp.Replace(_T("  "),_T(" ")); tmp.Replace(_T("  "),_T(" ")); tmp.Replace(_T("  "),_T(" ")); tmp.Replace(_T("  "),_T(" "));
	tmp.Replace(_T(","),_T(" "));
	tmp.Replace(_T(" "),_T("+"));
	tmp.Replace(_T("é"),_T("e")); tmp.Replace(_T("è"),_T("e")); tmp.Replace(_T("ê"),_T("e")); tmp.Replace(_T("ë"),_T("e"));
	tmp.Replace(_T("à"),_T("a"));
	tmp.Replace(_T("î"),_T("i")); tmp.Replace(_T("ï"),_T("i"));
	tmp.Replace(_T("û"),_T("u")); tmp.Replace(_T("ü"),_T("u"));

//	tmp = _T("GET /maps?hl=fr&q=") + tmp + _T("&num=1&ie=UTF-8&msa=N&z=20\r\nConnection: Close\r\n\r\n");
//	dns = gethostbyname("maps.google.com");
	tmp = _T("GET /maps/api/geocode/json?address=") + tmp + _T("&sensor=false\r\nConnection: Close\r\n\r\n");
	dns = gethostbyname("maps.googleapis.com");

	if(!dns) {
		sprintf(mdt[current_tab].LogMsg,"%s\r\n<\t\tFailed to retrieve google maps IP...",mdt[current_tab].LogMsg);
	} else {

		WSAStartup(MAKEWORD(2,2), &wsaData);
		ConnectSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);

		//memcpy(&clientService.sin_addr.s_addr,dns->h_addr,dns->h_length);
		clientService.sin_family		= AF_INET;
		clientService.sin_addr.s_addr	= *(DWORD*) dns->h_addr_list[0];
		clientService.sin_port			= htons( 80 );

		// Connect to server.
		if (connect( ConnectSocket, (SOCKADDR*) &clientService, sizeof(clientService) ) == SOCKET_ERROR) {
			WSACleanup();
			sprintf(mdt[current_tab].LogMsg,"%s\r\n<\t\tFailed to connect google maps...",mdt[current_tab].LogMsg);
		} else {
			memset(s, '\0', 1024);
			WideCharToMultiByte(CP_ACP, 0, tmp, -1, s, 1024, NULL, NULL); 
			send(ConnectSocket,s,strlen(s),0); //On envoie la requete
			Sleep(1000);
			//sprintf(mdt[current_tab].LogMsg, "");

			len = 1;
			while (len != NULL)
			{
				memset(this->bufGmaps, '\0', LG_BUF_GMAPS);
				len = recv(ConnectSocket, this->bufGmaps, LG_BUF_GMAPS, 0); //On recoit le contenu de la page
				Sleep(50);
				//sprintf(mdt[current_tab].LogMsg, "%s\r\n%d octets lus", mdt[current_tab].LogMsg, len);
				CString cs(this->bufGmaps);
				//sprintf(mdt[current_tab].LogMsg, "%s\r\n**************\r\n%s\r\n**************", mdt[current_tab].LogMsg, this->bufGmaps);
				rsp = rsp + cs;
			}
			closesocket(ConnectSocket);

			FILE * fd = fopen("c:\\temp\\gmap.log", "w");
			if (fd != NULL)
			{
				int len = rsp.GetLength();
				for (int i=0 ; i <= len-1000; i += 1000)
				{
					CString2 log(rsp.Mid(i,1000));
					fputs(log.GetChar(), fd);
				}
				fclose(fd);
			}

			startLat = rsp.Find(_T("\"location\" : {"));
			if (startLat > 0)
			{
				lat = FormateLatLong(rsp, startLat, L"lat\" : ", 8);
				lng = FormateLatLong(rsp, startLat, L"lng\" : ", 7);

				sprintf(mdt[current_tab].LogMsg,"%s\r\n<\t\tReceived google maps info !",mdt[current_tab].LogMsg);

				CString oldLat,oldLong;
				pWndLat->GetWindowTextW(oldLat);
				pWndLong->GetWindowTextW(oldLong);

				pWndLat->SetWindowTextW(lat);
				pWndLong->SetWindowTextW(lng);
				if(!ZoneCtrl()){
					sprintf(mdt[current_tab].LogMsg,"%s\r\n<\t\tError: lat/long/zone !",mdt[current_tab].LogMsg);
					pWndLat->SetWindowTextW(oldLat);
					pWndLong->SetWindowTextW(oldLong);
				}
			}
			else
			{
				sprintf(mdt[current_tab].LogMsg,"%s\r\n<\t\tError: lat/long introuvable !",mdt[current_tab].LogMsg);
				pWndLat->SetWindowTextW(L"");
				pWndLong->SetWindowTextW(L"");
			}
			//CString2 buf(rsp);
			//sprintf(mdt[current_tab].LogMsg, "%s\r\n%s", mdt[current_tab].LogMsg, buf.GetChar());
		}
	}
}

void CStarGUIDlg::OnBnClickedSon()
{
	if(son){
		son = false;
		pWndSon->SetWindowTextW(_T("ON"));

	} else {
		son = true;
		pWndSon->SetWindowTextW(_T("OFF"));
	}

}


void CStarGUIDlg::OnBnClickedReleve()
{
	Releve();
}


void CStarGUIDlg::OnBnClickedWing1()
{
	ChoixWing(0);
}


void CStarGUIDlg::OnBnClickedWing2()
{
	ChoixWing(1);
}


void CStarGUIDlg::OnBnClickedAbandon()
{
	MsgChauffeur(3,"ABANDON");
}


void CStarGUIDlg::OnBnClickedStationImp()
{
	MsgChauffeur(4,"STATIONNEMENT IMPOSSIBLE");
}


DWORD WINAPI ThreadBascule(LPVOID lpParam)
{ 
	DWORD mdt_number = current_tab; // mémorisation du tab au lancement du thread

	// on ne fait rien si le thread n'est pas associé au tab courant
	//if (GetThreadId(hTimerBascule[mdt_number]) != dwTimerBasculeId[mdt_number])
	if (GetCurrentThreadId() != dwTimerBasculeId[mdt_number])
		return 0;

	sprintf(mdt[mdt_number].LogMsg, "%s\r\nTHREAD Bascule CREATED", mdt[mdt_number].LogMsg);

	// déconnexion
	HIA('D');

	// attente de reconnexion
	CString delai;
	pWndDelaiBascule->GetWindowTextW(delai);
	CString2 delai2(delai);
	Sleep(atoi(delai2.GetChar())*1000);

	// changement d'opérateur
	oper_number = (oper_number+1)%2;
	pWndNomOperateur->SetWindowTextW(OPER_LISTE[oper_number].nom);
	// envoi du HIA reconnexion
	HIA('C');

	CloseHandle(hTimerBascule[mdt_number]);
	hTimerBascule[mdt_number] = NULL;
	sprintf(mdt[mdt_number].LogMsg, "%s\r\nTHREAD Bascule STOPPED", mdt[mdt_number].LogMsg);

	// réactivation du bouton Bascule
	pWndBasculeOper->EnableWindow(TRUE);

	return 0; 
} 

void CStarGUIDlg::OnBnClickedBasculeOper()
{
	// désactivation du bouton Bascule
	pWndBasculeOper->EnableWindow(FALSE);

	// On lance le thread de gestion de la bascule opérateur
	if(hTimerBascule[current_tab] == NULL) {
		hTimerBascule[current_tab] = CreateThread(NULL, 0, ThreadBascule, NULL, 0, &dwTimerBasculeId[current_tab]);
	}
}


void OnBnClickedBid(int num)
{
	CString lib;
	char zone[10];

	pWndEncheres[num]->GetWindowTextW(lib);
	CString2 lib2(lib);
	strcpy(zone,lib2.GetChar());
	zone[3] = '\0';
	ConditionalBookIn(zone);
}


void CStarGUIDlg::OnBnClickedBid1()
{
	OnBnClickedBid(0);
}

void CStarGUIDlg::OnBnClickedBid2()
{
	OnBnClickedBid(1);
}

void CStarGUIDlg::OnBnClickedBid3()
{
	OnBnClickedBid(2);
}

void CStarGUIDlg::OnBnClickedBid4()
{
	OnBnClickedBid(3);
}

void CStarGUIDlg::OnBnClickedBid5()
{
	OnBnClickedBid(4);
}

void CStarGUIDlg::OnBnClickedBid6()
{
	OnBnClickedBid(5);
}


DWORD WINAPI ThreadAutomate (LPVOID lpParam)
{ 
	DWORD mdt_number;
	mdt_number = current_tab;

	// on ne fait rien si le thread n'est pas associé au tab courant
//	if (GetThreadId(hThreadAutomate[current_tab]) != dwThreadAutomateId[current_tab])
	if (GetCurrentThreadId() != dwThreadAutomateId[current_tab])
		return 0;

	sprintf(mdt[mdt_number].LogMsg,"%s\r\nTHREAD Automate CREATED",mdt[mdt_number].LogMsg,mdt_number);

	// acceptation de la course, meter on et lancement délai approche
	AcceptNew(mdt_number);
	updateAffichage(mdt_number);
	Sleep(10000);
	MeterOn(mdt_number);
	updateAffichage(mdt_number);
	CString delaiApproche;
	pWndAutoApproche->GetWindowTextW(delaiApproche);
	CString2 delai2(delaiApproche);
	Sleep(atoi(delai2.GetChar())*1000*60);

	// Sur Place
	SurPlace(mdt_number);
	updateAffichage(mdt_number);
	CString delaiSurPlace;
	pWndAutoSurPlace->GetWindowTextW(delaiSurPlace);
	CString2 delai3(delaiSurPlace);
	Sleep(atoi(delai3.GetChar())*1000*60);

	// Charge
	ChargeNew(mdt_number);
	updateAffichage(mdt_number);
	CString delaiCharge;
	pWndAutoCharge->GetWindowTextW(delaiCharge);
	CString2 delai4(delaiCharge);
	Sleep(atoi(delai4.GetChar())*1000*60);
	MeterOff(mdt_number);
	updateAffichage(mdt_number);

	CloseHandle(hThreadAutomate[mdt_number]);
	hThreadAutomate[mdt_number] = NULL;
	sprintf(mdt[mdt_number].LogMsg,"%s\r\nTHREAD Automate STOPPED",mdt[mdt_number].LogMsg,mdt_number);
	return 0; 
} 

void CStarGUIDlg::OnBnClickedRunAutomate()
{
	// désactivation du bouton Run
	pWndRunAuto->EnableWindow(FALSE);

	// mémorisation des délais saisis
	pWndAutoApproche->GetWindowTextW(mdt[current_tab].AutoApproche);
	pWndAutoSurPlace->GetWindowTextW(mdt[current_tab].AutoSurPlace);
	pWndAutoCharge->GetWindowTextW(mdt[current_tab].AutoCharge);

	// On lance le thread de gestion de l'automate
	if(hThreadAutomate[current_tab] == NULL) {
		hThreadAutomate[current_tab] = CreateThread(NULL, 0, ThreadAutomate, NULL, 0, &dwThreadAutomateId[current_tab]);
	}}


void CStarGUIDlg::OnBnClickedConnect()
{
	/* lecture du fichier de config si existant */
	char buffer[1024];
	char filename[1024];
	CString tmp,cs,chf,id,imei,zone,delai,lat,lon,addr,nss,co,seq;

	TCHAR szDir[MAX_PATH];
	GetCurrentDirectory(sizeof(szDir) - 1, szDir);

	tmp  = CString (szDir);
	tmp += _T("\\data.ini");
	WideCharToMultiByte(CP_ACP, 0, tmp, -1, filename, 1024, NULL, NULL); 

	FILE *fd	= fopen(filename,"r");
	int pos = 0;
	if (fd != NULL)
	{
		while (fgets((char*)buffer,1024,fd))
		{
			cs		= CString (buffer);
			pos		= 0;
			chf		= cs.Tokenize(_T(";"),pos);
			id		= cs.Tokenize(_T(";"),pos);
			imei	= cs.Tokenize(_T(";"),pos);
			zone	= cs.Tokenize(_T(";"),pos);
			delai	= cs.Tokenize(_T(";"),pos);
			lat		= cs.Tokenize(_T(";"),pos);
			lon		= cs.Tokenize(_T(";"),pos);
			addr	= cs.Tokenize(_T(";"),pos);
			nss		= cs.Tokenize(_T(";"),pos);
			co		= cs.Tokenize(_T(";"),pos);
			seq		= cs.Tokenize(_T(";"),pos);
			current_tab = maxtab;
			newMdt(maxtab++,chf,id,imei,zone,delai,lat,lon,addr,nss,co,seq);
			Sleep(100);
			//OnBnClickedHia();	// HIA Automatique
			//Sleep(100);
		}

	} 
	else
	{
		current_tab = maxtab;
		newMdt(maxtab++,_T("9090"),  _T("2266"), _T("000000000000001"), _T("911"), _T("5"), _T("48.89908"), _T("2.30382"), _T("22-28 Rue Henri Barbusse Clichy"), _T("184103604405747"), _T("019236597"), _T("000432") );
	}

	pWndTab->InsertItem(maxtab,L"    +");
	updateAffichage();

	CWnd *pWnd = (CButton *) GetDlgItem(IDC_CONNECT);
	pWnd->EnableWindow(FALSE);
}


void CStarGUIDlg::OnBnClickedCompSortie()
{
	char BufMsg[BODY_SIZE];
	sprintf(BufMsg, "%%YYX%s%c", GPS_Msg(current_tab), mdt[current_tab].meter_status);
	SndGPRSPacket(current_tab, VEH_OUT, BufMsg, "VEH_OUT");
}


void CStarGUIDlg::OnBnClickedCompRetour()
{
	char BufMsg[BODY_SIZE];
	sprintf(BufMsg, "#22%%%%YYX%s%c", GPS_Msg(current_tab), mdt[current_tab].meter_status);
	SndGPRSPacket(current_tab, NULL_MSG, BufMsg, "VEH_IN");
}


void CStarGUIDlg::OnBnClickedButStatDist()
{
	char BufMsg[BODY_SIZE];
	char Msg[255];
	pWndStatDistData->GetWindowTextW(mdt[current_tab].StatDistData);
	CString2 data(mdt[current_tab].StatDistData);
	sprintf(BufMsg, "#10%s", data.GetChar());
	sprintf(Msg, "STAT %s", data.GetChar());
	SndGPRSPacket(current_tab, NULL_MSG, BufMsg, Msg);
}
