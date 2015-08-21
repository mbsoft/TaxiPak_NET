// StarGUIDlg.h : fichier d'en-tête
//

#pragma once


static void CALLBACK TimerRoutine(PVOID lpParam, BOOLEAN TimerOrWaitFired);


// boîte de dialogue CStarGUIDlg
class CStarGUIDlg : public CDialog
{
	// Construction
public:
	CStarGUIDlg(CWnd* pParent = NULL);	// constructeur standard
	~CStarGUIDlg();

	// Données de boîte de dialogue
	enum { IDD = IDD_STARGUI_DIALOG };


protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// Prise en charge de DDX/DDV

private:
	char * bufGmaps;

// Implémentation
protected:
	HICON m_hIcon;

	// Fonctions générées de la table des messages
	virtual BOOL OnInitDialog();
	afx_msg void OnPaint();
	afx_msg void OnOK();
	afx_msg HCURSOR OnQueryDragIcon();
	DECLARE_MESSAGE_MAP()

public:
	afx_msg void StartTimer(UINT nb_sec);
	afx_msg void StopTimer();
	afx_msg void OnTimer( UINT nIDEvent );
	afx_msg void OnBnClickedHia();
	afx_msg void OnBnClickedSignin();
	afx_msg void OnBnClickedSignoff();
	afx_msg void OnBnClickedBookoff();
	afx_msg void OnBnClickedPause();
	afx_msg void OnBnClickedBookin();
	afx_msg void OnBnClickedEndir();
	afx_msg void OnBnClickedMeteron();
	afx_msg void OnBnClickedMeteroff();
	afx_msg void OnBnClickedNoacpt();
	afx_msg void OnBnClickedAcptnew();
	afx_msg void OnBnClickedDelais();
	afx_msg void OnBnClickedChargenew();
	afx_msg void OnBnClickedSurplace();
	afx_msg void OnBnClickedOui();
	afx_msg void OnBnClickedNon();
	afx_msg void OnBnClickedEnd();
	afx_msg void OnBnClickedRang();
	afx_msg void OnBnClickedBascule();
	afx_msg void OnTcnSelchangeTab(NMHDR *pNMHDR, LRESULT *pResult);
	
	afx_msg void OnBnClickedNoncharge();
	afx_msg void OnBnClickedKillmdt();
	afx_msg void OnBnClickedAmeli();
	afx_msg void OnBnClickedGmap();
	afx_msg void OnBnClickedSon();
	afx_msg void OnBnClickedRlvcpam();
	afx_msg void OnBnClickedReleve();
	afx_msg void OnBnClickedWing1();
	afx_msg void OnBnClickedWing2();
	afx_msg void OnBnClickedAbandon();
	afx_msg void OnBnClickedStationImp();
	afx_msg void OnBnClickedBasculeOper();
	afx_msg void OnBnClickedBid1();
	afx_msg void OnBnClickedBid2();
	afx_msg void OnBnClickedBid3();
	afx_msg void OnBnClickedBid4();
	afx_msg void OnBnClickedBid5();
	afx_msg void OnBnClickedBid6();
	afx_msg void OnBnClickedRunAutomate();
	afx_msg void OnBnClickedConnect();
	afx_msg void OnBnClickedCompSortie();
	afx_msg void OnBnClickedCompRetour();
	afx_msg void OnBnClickedButStatDist();
	afx_msg void OnBnClickedButRejet();
};
