using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PaperPlaneTools;


public class EScript : MonoBehaviour
{

    public GameObject grid;
    public GameObject LoadingIndicator;
    public GameObject tnameTextObj;
    public GameObject tstatusTextObj;
    public GameObject quitButtonObj;
    public GameObject ChatInputTextObj;
    public GameObject ChatDisplayTextObj;
    public GameObject AddBotsObject;

    public int[] canChallengeConquerBits;
    public int[] FriendlyOnlineBits;
    public static bool processingCommsUnderway = false ;
    public static int MaxPlayersSupported = 8;
    public static bool skipFirstMessageRequests = true;
    public List<string> MessageQueue;

    void Start()
    {        
        WTRequester.isWatch = false;
        WTRequester.isBusy = false;
        WTRequester._LastMessageTag = 0;
        WTRequester.clearOpponentInfo();
        skipFirstMessageRequests = true;
        quitButtonObj.SetActive(false);
        AddBotsObject.SetActive(false);
        processingCommsUnderway = false;
        MessageQueue = new List<string>();
        if ( WTRequester._player_id > 0 && WTRequester._tournament_id > 0 )
        {
            LoadingIndicator.SetActive(false);
            InvokeRepeating("EFetcher", 0f, 30f);
            InvokeRepeating("GetComms", 0f, 3f);
            InvokeRepeating("IterateComms", 0f, 1f);            
        }
        else
        {
            SceneManager.LoadScene(Scenes.TGrid);
        }
        tnameTextObj.GetComponent<Text>().text = WTRequester.EnteredTournamentName;
        ChatDisplayTextObj.GetComponent<Text>().text = "";
        tstatusTextObj.GetComponent<Text>().text = "";
        canChallengeConquerBits = new int[MaxPlayersSupported];//Max Supported Players        
        FriendlyOnlineBits = new int[MaxPlayersSupported];
    }

    public void HidePlayerGrid()
    {
        for (int i = 0; i < 8; i++)
        {
            grid.transform.Find(String.Format("PlayerRow ({0})", i + 1)).gameObject.SetActive(false);
        }
    }

    public void EFetcher()
    {
        print("Refreshing");        
        //LoadingIndicator.SetActive(true);
        //HidePlayerGrid();                
        try
        {
            WTRequester.request(this, "afterEFetch", new string[] { Actions.WElegantFetcher,
                Convert.ToString(WTRequester._tournament_id),
                Convert.ToString(WTRequester._player_id)//FetcherID            
            });
        }
        catch(Exception e)
        {
            print(e.Message);
        }        
    }

    public void afterEFetch()
    {
        //ShowFetchedPlayerDetails();
        LoadingIndicator.SetActive(false);
        int PlayerCountInsideLobby = isLobbyFull();
        bool isPlayerLobbyCreator = false ;
        for (int i = 0; i < WTRequester.players.GetLength(0); i++)
        {            
            if (WTRequester.players[i, 0] == "SUCCESS")
            {
                /*
                SUCCESS
                player_id
                joinOrder           : is Player Lobby Creator
                nickname 			*
                ELO points			*
                wins				=
                draws               =
                total               =
                Lastonline			*
                LastGame			*

                score				*

                matchstatus			*
                countdowntimer		*


                00F015FF - online
                FFF700FF - playing
                DFD7E2FF-offline
                */
                if (    (WTRequester.handleInt(WTRequester.players[i, 1]) == WTRequester._player_id) &&
                        (WTRequester.handleInt(WTRequester.players[i, 2]) == 1) )                   
                {
                    isPlayerLobbyCreator = true;
                }
                                    

                grid.transform.Find(String.Format("PlayerRow ({0})", i + 1)).Find("PlayerName").GetComponent<Text>().text
                    = WTRequester.players[i, 3] + " ( " + WTRequester.players[i, 4] + " )";

                grid.transform.Find(String.Format("PlayerRow ({0})", i + 1)).Find("PlayerScore").GetComponent<Text>().text
                    = Convert.ToString ( WTRequester.handleInt( WTRequester.players[i, 10] ) ) ;

                #region Last Online and Last Game
                string strLastOnlineSince = WTRequester.players[i, 8] ;
                string strLastGameSince = WTRequester.players[i, 9] ;                

                bool isInGameIndicator = false; 
                if ((!String.IsNullOrEmpty(strLastGameSince)))
                {
                    int lastGameSince = WTRequester.handleInt(strLastGameSince);
                    if ( lastGameSince < 5 )
                    {
                        isInGameIndicator = true;                        
                        grid.transform.Find(String.Format("PlayerRow ({0})", i + 1))
                            .Find("OnlineDot").GetComponent<Image>().color = new Color(0f, 0.37f, 0.95f, 1f); //Blue //015FF5FF


                        string notionG = "";                        
                        if ( lastGameSince <= 1 )
                        {
                            notionG = "Min";
                        }
                        else
                        {
                            notionG = "Mins";
                        }

                        grid.transform.Find(String.Format("PlayerRow ({0})", i + 1))
                            .Find("OnlineTime").GetComponent<Text>().text =
                            lastGameSince == 0 ? "(Playing...)" : lastGameSince+" "+notionG+" (In Battle)"; 

                    }                    
                }
                bool isOnlineIndicator = false;                
                if (!isInGameIndicator)
                {                    
                    if ((!String.IsNullOrEmpty(strLastOnlineSince)))
                    {
                        int lastOnlineSince = WTRequester.handleInt(strLastOnlineSince);
                        if ( lastOnlineSince < 1 )
                        {
                            isOnlineIndicator = true;
                            grid.transform.Find(String.Format("PlayerRow ({0})", i + 1))
                                .Find("OnlineDot").GetComponent<Image>().color = new Color(0f, 0.95f, 0.25f, 1f); //Green //00F015FF
                            grid.transform.Find(String.Format("PlayerRow ({0})", i + 1))
                                .Find("OnlineTime").GetComponent<Text>().text = "Online";
                        }                        
                    }
                    if ( !isOnlineIndicator )
                    {
                        grid.transform.Find(String.Format("PlayerRow ({0})", i + 1))
                            .Find("OnlineDot").GetComponent<Image>().color = new Color(0.81f, 0.81f, 0.81f, 1f); //Grey
                        //---------
                        int lastMins = WTRequester.handleInt(strLastOnlineSince);
                        string notion = "";
                        if ( lastMins >= 1 && lastMins < 60 )
                        {                            
                            if (lastMins == 1)
                            {
                                notion = "Min";
                            }
                            else
                            {
                                notion = "Mins";
                            }
                        }
                        else if ( lastMins >= 60 && lastMins < ( 60 * 16 ) )
                        {
                            lastMins = (lastMins / 60);
                            if ( lastMins <= 1 )
                            {
                                notion = "Hour";
                            }
                            else
                            {
                                notion = "Hours";
                            }                            
                        }
                        else
                        {
                            lastMins = -1;
                            notion = "Offline";                            
                        }
                        //---------                        
                        grid.transform.Find(String.Format("PlayerRow ({0})", i + 1))
                            .Find("OnlineTime").GetComponent<Text>().text = 
                            lastMins == -1 ? "Offline" : lastMins+" "+notion+" (Since Offline)";
                    }
                }
                #endregion

                #region Status and Conquer Manager
                FriendlyOnlineBits[i] = 0; // Only When Players are waiting and online
                if (PlayerCountInsideLobby == MaxPlayersSupported)
                {                    
                    string strConquerCountDownTime = WTRequester.players[i, 12];
                    int thresholdCCD = 9999; // value defined in stored proc
                    int ConquerCountDown;
                    if (String.IsNullOrEmpty(strConquerCountDownTime) || (WTRequester.handleInt(strConquerCountDownTime) == 9999))
                    {
                        // Yet to play
                        ConquerCountDown = thresholdCCD;
                    }
                    else
                    {
                        ConquerCountDown = WTRequester.handleInt(strConquerCountDownTime);
                    }
                    if (WTRequester._player_id != WTRequester.handleInt(WTRequester.players[i, 1]))// See if not self
                    {
                        string strMatchStatus = WTRequester.players[i, 11].Trim();
                        if ((!String.IsNullOrEmpty(strMatchStatus)))
                        {
                            string StatusMsg = "";
                            if (strMatchStatus == "W")
                            {
                                StatusMsg = "Victory(+2)";
                            }
                            else if (strMatchStatus == "L")
                            {
                                StatusMsg = "Defeat";
                            }
                            else if (strMatchStatus == "D")
                            {
                                StatusMsg = "Draw(+1)";
                            }
                            else if (strMatchStatus == "C")
                            {
                                StatusMsg = "You Conquered(+1)";
                            }
                            else if (strMatchStatus == "S")
                            {
                                StatusMsg = "You Were Captured (When Offline)";
                            }
                            else if (strMatchStatus == "R")
                            {
                                StatusMsg = "Opponent Resigned(+1)";
                            }
                            else
                            {
                                StatusMsg = "";//Fatal Case here
                            }
                            canChallengeConquerBits[i] = 0;
                            grid.transform.Find(String.Format("PlayerRow ({0})", i + 1)).Find("Status").GetComponent<Text>().text
                                    = StatusMsg;
                        }
                        else
                        {
                            // we need to challenge or conquer only these guys...rest players click is invalid 
                            if (!isInGameIndicator)
                            {
                                if (isOnlineIndicator)
                                {
                                    grid.transform.Find(String.Format("PlayerRow ({0})", i + 1)).Find("Status").GetComponent<Text>().text
                                        = "Challenge Opponent";
                                    canChallengeConquerBits[i] = 1; // Can Challenge
                                }
                                else
                                {
                                    string conquerUpdate = "";
                                    if (ConquerCountDown == thresholdCCD)
                                    {
                                        // Fatal Case ... Do Nothing
                                    }
                                    else
                                    {                                        
                                        if (ConquerCountDown <= 0)
                                        {
                                            conquerUpdate = "Conquering...";
                                        }
                                        else if (ConquerCountDown == 1)
                                        {
                                            conquerUpdate = "Conquering in 1 Min";
                                        }
                                        else if (ConquerCountDown <= 59)
                                        {
                                            conquerUpdate = "Conquering in " + ConquerCountDown + " Mins";
                                        }
                                        else
                                        {
                                            conquerUpdate = "Conquering in 59 Mins"; // Fatal May be 60
                                        }
                                    }
                                    if ( conquerUpdate == "" )
                                    {
                                        grid.transform.Find(String.Format("PlayerRow ({0})", i + 1)).Find("Status").GetComponent<Text>().text
                                        = "Conquer";
                                        canChallengeConquerBits[i] = 2; // Can Conquer
                                    }
                                    else
                                    {
                                        grid.transform.Find(String.Format("PlayerRow ({0})", i + 1)).Find("Status").GetComponent<Text>().text
                                        = conquerUpdate ;
                                        canChallengeConquerBits[i] = 0; // Conquering in progress
                                    }                                    
                                }
                            }
                            else
                            {
                                canChallengeConquerBits[i] = 0;//Cannot challenge someone who is already in battle
                                grid.transform.Find(String.Format("PlayerRow ({0})", i + 1)).Find("Status").GetComponent<Text>().text
                                        = "";
                            }
                            //else : For in Game players must complete their current battle
                        }
                    }
                    else// Cannot Challenge Self
                    {                        
                        canChallengeConquerBits[i] = 0;
                        grid.transform.Find(String.Format("PlayerRow ({0})", i + 1)).Find("Status").GetComponent<Text>().text
                                        = "";
                    }
                    
                }
                else
                {   
                    if ( isOnlineIndicator )
                    {
                        FriendlyOnlineBits[i] = 1;
                    }                    
                    canChallengeConquerBits[i] = 0;
                    grid.transform.Find(String.Format("PlayerRow ({0})", i + 1)).Find("Status").GetComponent<Text>().text
                                    = "";
                }

                #endregion

                grid.transform.Find(String.Format("PlayerRow ({0})", i + 1)).gameObject.SetActive(true);
            }
            else
            {
                canChallengeConquerBits[i] = 0;
                grid.transform.Find(String.Format("PlayerRow ({0})", i + 1)).gameObject.SetActive(false);                
            }
        }
        if ( PlayerCountInsideLobby == MaxPlayersSupported )
        {
            tstatusTextObj.GetComponent<Text>().text = "Tournament Started";
        }
        else
        {
            tstatusTextObj.GetComponent<Text>().text = "Begins When All Players Join ("+PlayerCountInsideLobby+"/"+MaxPlayersSupported+")";            
        }
        if (PlayerCountInsideLobby >= 4 && (PlayerCountInsideLobby != MaxPlayersSupported) )
        {
            AddBotsObject.SetActive(true);
        }
        else
        {
            AddBotsObject.SetActive(false);
        }
        #region Quit Handler
        if ( isPlayerLobbyCreator )
        {
            if (PlayerCountInsideLobby == 1 || 
                PlayerCountInsideLobby == MaxPlayersSupported)//For Resign Option Enabling
            {
                quitButtonObj.SetActive(true);
            }
            else 
            {
                quitButtonObj.SetActive(false);
            }
        }
        else
        {            
            quitButtonObj.SetActive(true);
        }
        if (PlayerCountInsideLobby == MaxPlayersSupported)
        {            
            quitButtonObj.transform.Find("Text").gameObject.GetComponent<Text>().text = "Resign";            
        }
        else
        {
            quitButtonObj.transform.Find("Text").gameObject.GetComponent<Text>().text = "Quit";         
        }
        #endregion
    }

    public void ShowFetchedPlayerDetails()
    {
        string fetchedData = "";
        for (int s = 0; s < WTRequester.players.GetLength(0); s++)
        {
            // Columns 0 to 12 inclusive
            for ( int c = 0 ; c <= 12; c++ )
            {
                fetchedData += WTRequester.players[s, c] + ",";
            }
            fetchedData += "\n";
        }
        print(fetchedData);
    }

    public void DebugThis()
    {
        print("ErrorCame:LastResult:|" + WTRequester.LastResult + "|ERROR");
    }

    public static int isLobbyFull()
    {
        int pCountItr = 0;
        for (int i = 0; i < WTRequester.players.GetLength(0); i++)
        {
            if (WTRequester.players[i, 0] == "SUCCESS")
            {
                pCountItr++;
            }            
        }
        return pCountItr;
    }

    public static int getJoinOrder( string input_player_id )
    {
        int tmp_joinOrder = 0;
        for (int i = 0; i < WTRequester.players.GetLength(0); i++)
        {
            if (WTRequester.players[i, 0] == "SUCCESS")
            {
                if ( input_player_id == WTRequester.players[i, 1] )
                {
                    tmp_joinOrder = WTRequester.handleInt(WTRequester.players[i, 2]);
                    break;
                }                
            }
        }
        return tmp_joinOrder ;
    }

    public void onQuitButton()
    {
        // Are You Sure To Quit
        string preFixQuit = String.Empty;
        string preTitle = String.Empty;
        if ( isLobbyFull() == MaxPlayersSupported )
        {
            preFixQuit = "(Rating Penalty) ";
            preTitle = "Resign";
        }
        else
        {
            preFixQuit = String.Empty;
            preTitle = "Quit";
        }
        new Alert( preTitle, preFixQuit+"Are you sure ?")
            .SetPositiveButton("Yes", () => {
                onQuitButton_Confirm();
            })
            .SetNegativeButton("No", () => {
                // Do Nothing
            })
            .Show();
    }

    public void onQuitButton_Confirm()
    {
        WTRequester.isBusy = false;// prioritizing quits or resigns than normal queries
        if ( isLobbyFull() == MaxPlayersSupported )
        {
            try
            {
                WTRequester.request(this, "afterQuitResign", new string[] { Actions.Resign,
                Convert.ToString(WTRequester._tournament_id),
                Convert.ToString(WTRequester._player_id)
            });
            }
            catch (Exception e)
            {
                print(e.Message);
            }
        }
        else
        {
            try
            {
                WTRequester.request(this, "afterQuitResign", new string[] { Actions.QuitTournament,
                Convert.ToString(WTRequester._player_id),
                Convert.ToString(WTRequester._tournament_id)                
            });
            }
            catch (Exception e)
            {
                print(e.Message);
            }
        }
    }

    public void afterQuitResign()
    {
        WTRequester.EnteredTournamentName = "";
        SceneManager.LoadScene(Scenes.TGrid);
    }

    public void onClickPlayerButton(int t)
    {
        t--; // because we used 1-indexing for simplicity in the ui call setting        
        string hitPlayerID = WTRequester.players[t, 1];
        //XStatics.PopUp("Hit on :" + WTRequester.players[t, 3]);
        if (canChallengeConquerBits[t] > 0)
        {
            if ( isLobbyFull() == MaxPlayersSupported )
            {
                if (canChallengeConquerBits[t] == 1)// Throw a challenge to opponent : MsgQueue
                {
                    // 2 Jump or 3 Jump ? we musk ask them to talk before match ... and then start...
                    // when a player send msgQueue it reaches only intended opponent
                    // if opponent accepts it then we have a match there
                    // else the triggerer may wait in Open Room
                    // Trigger this call and sit in room.....with the variant  
                                      
                    Comms(XStatics.socialName, "B", hitPlayerID);
                    grid.transform.Find(String.Format("PlayerRow ({0})", t + 1)).Find("Status").GetComponent<Text>().text = "You Challenged For Battle";
                    canChallengeConquerBits[t] = 0;// disabling multiple hit requests

                    //XStatics.PopUp("Challenge");
                }
                else if (canChallengeConquerBits[t] == 2) // Throw a fire and forget of conquer...upon success lock this guy
                {                    
                    try
                    {
                        WTRequester.request(this, "afterConquer", new string[] { Actions.Conquer,
                            Convert.ToString(WTRequester._tournament_id),
                            Convert.ToString(WTRequester._player_id),
                            Convert.ToString(hitPlayerID)
                        });                        
                    }
                    catch (Exception e)
                    {
                        print(e.Message);
                    }
                    //XStatics.PopUp("PutConquerRequest");
                    canChallengeConquerBits[t] = 0;
                    FriendlyOnlineBits[t] = 0;
                }
                else
                {
                    // Fatal Case
                    //XStatics.PopUp("DirtyBit");
                }
            }
            else
            {
                if (canChallengeConquerBits[t] == 1 && FriendlyOnlineBits[t] == 1 )
                {
                    // Trigger Friendly
                    //XStatics.PopUp("Friendly");       
                                 
                    Comms(XStatics.socialName, "F", hitPlayerID);
                    grid.transform.Find(String.Format("PlayerRow ({0})", t + 1)).Find("Status").GetComponent<Text>().text = "Friendly Match Requested";
                    canChallengeConquerBits[t] = 0;// disabling multiple hit requests
                    FriendlyOnlineBits[t] = 0;
                }
            }
        }
        else
        {
            if(isLobbyFull() != MaxPlayersSupported)// Request Friendly On Double Tap : State of Art Ergonomics
            {
                if (canChallengeConquerBits[t] == 0 && FriendlyOnlineBits[t] == 1 && (WTRequester._player_id != WTRequester.handleInt(WTRequester.players[t, 1])) )// Cannot Friendly Self                    
                {                    
                    canChallengeConquerBits[t] = 1;
                    grid.transform.Find(String.Format("PlayerRow ({0})", t + 1)).Find("Status").GetComponent<Text>().text
                        = "Tap Again For Friendly Match" ;
                }
            }
        }
        
        // Else : in Future use this to show stats like win to loss ratio or winning percentage out of total
        // XStatics.PopUp("Bit Prevent");
        string playerstats = "";// w 5 , d 6 , t 7 
        int percentageOfHonour = 0;                
        int totalMatchesSoFar = 0 ;
        totalMatchesSoFar = WTRequester.handleInt(WTRequester.players[t, 7]);
        if ( totalMatchesSoFar > 0 )
        {
            percentageOfHonour = (int)((((WTRequester.handleInt(WTRequester.players[t, 5])*2)+ WTRequester.handleInt(WTRequester.players[t, 6])) / (float)(totalMatchesSoFar*2)) * 100) ;            
            // Percentage of Losses : may discourage few players mindset
            // Total matches to show is good , but we are short on space
        }
        playerstats = String.Format("Honour:{0}%", percentageOfHonour);            
        grid.transform.Find(String.Format("PlayerRow ({0})", t + 1)).Find("PlayerName").GetComponent<Text>().text
                = WTRequester.players[t, 3] + " ( " + WTRequester.players[t, 4] + " ) " +  "[ "+ playerstats +" ]";
                
    }    

    public void afterConquer()
    {
        //XStatics.PopUp("Opponent will be conquered if he remains offline for one hour");
        EFetcher();
    }

    public static string formatCrispyChatMessage( string rawMessage, int sender_join_order )
    {
        string tmpColor = "#ffffffff";//White
        if (sender_join_order == 1)
        {
            tmpColor = "#008000FF";//DarkGreen            
        }
        else if (sender_join_order == 2)
        {
            tmpColor = "#0000FFFF";///Blue            
        }
        else if (sender_join_order == 3)
        {
            tmpColor = "#E600FFFF";//Pink            
        }
        else if (sender_join_order == 4)
        {
            tmpColor = "#FF0000FF";//Red
        }
        else if (sender_join_order == 5)
        {
            tmpColor = "#016F79FF";//Sky Blue
        }
        else if (sender_join_order == 6)
        {
            tmpColor = "#493B1DFF";///Brown
        }
        else if (sender_join_order == 7)
        {
            tmpColor = "#646A00FF";//Dark Yellow
        }
        else if (sender_join_order == 8)
        {
            tmpColor = "#7C00FFFF";//Kangxi 21
        }
        else
        {
            tmpColor = "#FFFFFFFF";//White
        }         
        return "<color=" + tmpColor + ">" + rawMessage + "</color>";
    }    

    public void ECrispyChat(string msg)
    {
        if ( msg != String.Empty )
        {
            msg = formatCrispyChatMessage( XStatics.socialName + " : " + msg , getJoinOrder( Convert.ToString(WTRequester._player_id) ) );
            Comms(msg, "N", "0");// Normal Message To All Players
        }
        ChatInputTextObj.GetComponent<InputField>().text = string.Empty;
    }

    public void TryComms(string input_msg, string input_type, string input_opp_id)
    {
        try
        {
            WTRequester.request(this, String.Empty, new string[] { Actions.ZAddMessage,// Room is Opened after Comms are received ( receive immediately after sending once )
                Convert.ToString(WTRequester._tournament_id),
                Convert.ToString(WTRequester._player_id),
                input_opp_id,
                input_msg,
                input_type
            });
        }
        catch (Exception e)
        {
            print(e.Message);
        }
    }
    
    public void IterateComms()
    {
        try
        {
            if (!WTRequester.isBusy)
            {
                if (MessageQueue.Count > 0)
                {
                    string[] msg_parts = MessageQueue[0].Split('^');
                    TryComms( msg_parts[0] , msg_parts[1] , msg_parts[2] );
                    MessageQueue.RemoveAt(0);
                }
            }
        }
        catch (Exception e)
        {
            print(e.Message);
        }
    } 

    public void Comms( string input_msg , string input_type , string input_opp_id )
    {
        MessageQueue.Add(input_msg+"^"+input_type+"^"+input_opp_id);
    }

    public void GetComms()
    {
        if ( ! processingCommsUnderway )
        {
            try
            {
                WTRequester.request(this, "afterGetComms", new string[] { Actions.WMessageFetcher ,
                Convert.ToString(WTRequester._tournament_id),
                Convert.ToString(WTRequester._player_id),
                Convert.ToString(WTRequester._LastMessageTag)
            });
            }
            catch (Exception e)
            {
                print(e.Message);
            }
        }        
    }

    public void afterGetComms()
    {
        /*
            'SUCCESS',
		    message_id, : highest should go for lastMessageTag
		    message_text,
		    player_id,
		    opponent_id,
		    message_type,		: N ( normal ) , F ( Friendly ) , B ( Battle )
		    TIMESTAMPDIFF(MINUTE,message_time,NOW())
        */
        processingCommsUnderway = true;
        string newMessages = "";        
        for (int i = WTRequester.messages.GetLength(0) - 1; i >= 0; i--)// Reverse Iteration
        {
            if (WTRequester.messages[i, 0] == "SUCCESS") // see cases of over pasting all fetches and see cases of ignoring some messages
            {
                if (WTRequester.messages[i, 5] == "N") // normal messages
                {
                    newMessages += ( "\n" + WTRequester.messages[i, 2] ) ;
                }               
                else
                {
                    //Fatal : Handled seperately
                }
                if (i == 0)
                {
                    WTRequester._LastMessageTag = WTRequester.handleInt(WTRequester.messages[i, 1]);
                }
            }
        }

        #region Incoming Requests
        if (!skipFirstMessageRequests)
        {            
            // Incoming Requests
            bool isLobbyOpened = false;
            for (int i = WTRequester.messages.GetLength(0) - 1; (!WTRequester.isTriggered) && (!isLobbyOpened) && (i >= 0); i--)// Reverse Iteration , also when Lobby not opened
            {
                if (WTRequester.messages[i, 0] == "SUCCESS") // see cases of over pasting all fetches and see cases of ignoring some messages
                {
                    int timeSinceRequested = WTRequester.handleInt(WTRequester.messages[i, 6]);
                    if (timeSinceRequested <= 1)
                    {
                        string matchType = WTRequester.messages[i, 5];

                        if ( // add skipper Logic
                             (matchType == "B" || // Battle Requests
                              matchType == "F")
                           )   // Friendly Requests
                        {
                            string Requester = WTRequester.messages[i, 3];
                            string Opponent = WTRequester.messages[i, 4];
                            if (WTRequester.messages[i, 2] == "ACCEPTED")
                            {
                                isLobbyOpened = true;
                                WTRequester.isTriggered = true;
                                OpenLobby(Requester, Opponent, matchType == "B" ? true : false);
                            }
                            else
                            {
                                if (Requester != Convert.ToString(WTRequester._player_id))
                                {
                                    int msg_id = WTRequester.handleInt(WTRequester.messages[i, 1]);
                                    if (!WTRequester.deniedRequests.Contains(msg_id))// To Avoid Denied Messages Executing Again and Again
                                    {
                                        new Alert("Request", WTRequester.messages[i, 2] + " wants to battle you " + (matchType == "B" ? "(Rated Match)" : "(Friendly Match)"))
                                        .SetPositiveButton("Battle", () =>
                                        {
                                            Comms("ACCEPTED", matchType, Requester);
                                            isLobbyOpened = true; // Skipping other requests
                                        })
                                        .SetNegativeButton("Deny", () =>
                                        {
                                            WTRequester.deniedRequests.Add(msg_id);// denied msg id
                                        })
                                        .Show();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        skipFirstMessageRequests = false;// This will allow requests from next fetch on
        #endregion
        ChatDisplayTextObj.GetComponent<Text>().text += newMessages ;
        processingCommsUnderway = false;// Completed processing comms        
    }
    
    public void OpenLobby( string _Requester, string _Opponent, bool _isRealBattle)
    {
#if UNITY_EDITOR
        if ( XStatics.socialName == "dRunkenClient")
        {
            print(_Requester+","+_Opponent+","+_isRealBattle);
            return;
        }
#endif        
        KID.StripRooms();                 
        if ( _isRealBattle )// Remember somethings like the opponent id, opponent Rating, opponent RD
        {
            WTRequester._Opp_ID = WTRequester.handleInt( _Requester == Convert.ToString(WTRequester._player_id) ? _Opponent : _Requester );                
            WTRequester._Opp_Rating = 1500; //Defaults            
            for (int i = 0; i < WTRequester.players.GetLength(0); i++)
            {
                if (WTRequester.players[i, 0] == "SUCCESS")
                {
                    if ( Convert.ToString(WTRequester._Opp_ID) == WTRequester.players[i, 1])
                    {
                        WTRequester._Opp_Rating = WTRequester.handleInt(WTRequester.players[i, 4]);
                        WTRequester._Opp_Name = WTRequester.players[i, 3]+"("+ WTRequester.players[i, 4] + ")";
                        break;
                    }
                }
            }            
        }
        string UniqueMatchCode = WTRequester._tournament_id + (_isRealBattle?"B":"F") + getJoinOrder(_Requester) + getJoinOrder(_Opponent);
        print("Opening Lobby :" + UniqueMatchCode);        
        XStatics.PasswordText = UniqueMatchCode;
        int gameVariant = MenuScript.UniqueInteger(XStatics.PasswordText.Trim(), 0);
        XStatics.isMultiPlayer = true;
        XStatics.NOP = 2;// standard 1v1 matches for now
        KID.ShowQuickGameUI( XStatics.NOP - 1 , gameVariant);        // number of opponents = 1 , gameVariant
    }

    public void AddBots()
    {
        StartCoroutine(WebConnsAddBots());
    }

    IEnumerator WebConnsAddBots()
    {
        WWWForm form = new WWWForm();
        form.AddField("to", Convert.ToString(WTRequester._tournament_id) );
        String url = "http://mrwdata.website/process/addbots.php";
        WWW w = new WWW(url, form);
        yield return w;
        string result = w.text.Trim();
        if (result.Equals("SUCCESS"))
        {
            XStatics.PopUp("Added Bots");
            EFetcher();
        }
        else
        {
            XStatics.PopUp("Failed To Add Bots");
        }
    }
