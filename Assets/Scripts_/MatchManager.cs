using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    #region Singleton
    public static MatchManager matchMgrInstance_;

    private void Awake()
    {
        if (matchMgrInstance_ != null)
        {
            Destroy(this);
        }
        else
        {
            matchMgrInstance_ = this;
        }
    }
    #endregion

    public enum EventCodes : byte
    {
        NewPlayer,
        ListPlayers,
        UpdateStat,
        NextMatch,
        TimerSync
    }

    public List<PlayerInfo> allPlayers_ = new List<PlayerInfo>();
    private int index_;

    private List<LeaderboardPlayer> leaderBoardPlayersList_ = new List<LeaderboardPlayer>();

    public enum GameState
    {
        Waiting,
        Playing,
        Ending
    }

    public int killsToWin_ = 3;
    public Transform mapCamPoint_;
    public GameState gameState_ = GameState.Waiting;
    public float waitAfterEnding_ = 5f;

    public bool perpetual_;

    public float matchLength_ = 180f;
    private float currMatchTime_;
    private float sendTimer_;

    // Start is called before the first frame update
    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(0);
        }
        else
        {
            NewPlayerSend(PhotonNetwork.NickName);

            gameState_ = GameState.Playing;

            SetupTimer();

            if(!PhotonNetwork.IsMasterClient)
            {
                UI_Controller.uiControllerInstance_.timerText_.gameObject.SetActive(false);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        DisplayLeaderBoard();

        if (PhotonNetwork.IsMasterClient)
        {
            if (currMatchTime_ > 0f && gameState_ == GameState.Playing)
            {
                currMatchTime_ -= Time.deltaTime;

                if (currMatchTime_ <= 0)
                {
                    currMatchTime_ = 0f;
                    gameState_ = GameState.Ending;
                    ListPlayersSend();
                    StateCheck();
                }

                UpdateTimerDisplay();

                sendTimer_ -= Time.deltaTime;

                if(sendTimer_ <= 0)
                {
                    sendTimer_ += 1f;
                    TimerSend();
                }
            }
        }
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code < 200)
        {
            EventCodes theEvent = (EventCodes)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;

            //Debug.Log("Revived Event " + theEvent);

            switch (theEvent)
            {
                case EventCodes.NewPlayer:

                    NewPlayerReceive(data);
                    break;

                case EventCodes.ListPlayers:

                    ListPlayersReceive(data);
                    break;

                case EventCodes.UpdateStat:

                    UpdateStatsReceive(data);
                    break;

                case EventCodes.NextMatch:

                    NextMatchReceive();
                    break;

                case EventCodes.TimerSync:

                    TimerReceive(data);
                    break;
            }
        }
    }

    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void NewPlayerSend(string userName)
    {
        object[] package = new object[4];
        package[0] = userName;
        package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[2] = 0;
        package[3] = 0;

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
            );
    }

    public void NewPlayerReceive(object[] dataReceived)
    {
        PlayerInfo newPlayer = new PlayerInfo((string)dataReceived[0], (int)dataReceived[1], (int)dataReceived[2], (int)dataReceived[3]);

        allPlayers_.Add(newPlayer);

        ListPlayersSend();
    }

    public void ListPlayersSend()
    {
        object[] package = new object[allPlayers_.Count + 1];

        package[0] = gameState_;

        for (int i = 0; i < allPlayers_.Count; i++)
        {
            object[] piece = new object[4];

            piece[0] = allPlayers_[i].name_;
            piece[1] = allPlayers_[i].actor_;
            piece[2] = allPlayers_[i].kills_;
            piece[3] = allPlayers_[i].deaths_;

            package[i + 1] = piece;
        }

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ListPlayers,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    public void ListPlayersReceive(object[] dataReceived)
    {
        allPlayers_.Clear();

        gameState_ = (GameState)dataReceived[0];

        for (int i = 1; i < dataReceived.Length; i++)
        {
            object[] piece = (object[])dataReceived[i];

            PlayerInfo player = new PlayerInfo(
                (string)piece[0],
                (int)piece[1],
                (int)piece[2],
                (int)piece[3]
                );

            allPlayers_.Add(player);

            if (PhotonNetwork.LocalPlayer.ActorNumber == player.actor_)
            {
                index_ = i - 1;
            }
        }

        StateCheck();
    }

    public void UpdateStatsSend(int actorSending, int statToUpdate, int amountToChange)
    {
        object[] package = new object[] { actorSending, statToUpdate, amountToChange };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdateStat,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    public void UpdateStatsReceive(object[] dataReceived)
    {
        int actor = (int)dataReceived[0];
        int statType = (int)dataReceived[1];
        int amount = (int)dataReceived[2];

        for (int i = 0; i < allPlayers_.Count; i++)
        {
            if (allPlayers_[i].actor_ == actor)
            {
                switch (statType)
                {
                    case 0: //Kills
                        allPlayers_[i].kills_ += amount;
                        Debug.Log("Players " + allPlayers_[i].name_ + " : kills " + allPlayers_[i].kills_);
                        break;

                    case 1: //Deaths
                        allPlayers_[i].deaths_ += amount;
                        Debug.Log("Players " + allPlayers_[i].name_ + " : deaths " + allPlayers_[i].deaths_);
                        break;
                }

                if (i == index_)
                {
                    UpdateStatsDisplay();
                }

                if (UI_Controller.uiControllerInstance_.leaderBoard_.activeInHierarchy)
                {
                    ShowLeaderBoard();
                }

                break;
            }
        }

        ScoreCheck();
    }

    public void UpdateStatsDisplay()
    {
        if (allPlayers_.Count > index_)
        {
            UI_Controller.uiControllerInstance_.killsText_.text = "Kills: " + allPlayers_[index_].kills_;
            UI_Controller.uiControllerInstance_.deathsText_.text = "Deaths: " + allPlayers_[index_].deaths_;
        }
        else
        {
            UI_Controller.uiControllerInstance_.killsText_.text = "Kills: 0";
            UI_Controller.uiControllerInstance_.deathsText_.text = "Deaths: 0";
        }
    }

    private void ShowLeaderBoard()
    {
        UI_Controller.uiControllerInstance_.leaderBoard_.SetActive(true);

        foreach (LeaderboardPlayer lp in leaderBoardPlayersList_)
        {
            Destroy(lp.gameObject);
        }

        leaderBoardPlayersList_.Clear();

        UI_Controller.uiControllerInstance_.leaderBoardPlayerDisplay_.gameObject.SetActive(false);

        List<PlayerInfo> sorted = SortPlayers(allPlayers_);

        foreach (PlayerInfo player in sorted)
        {
            LeaderboardPlayer newPlayerDisplay = Instantiate(UI_Controller.uiControllerInstance_.leaderBoardPlayerDisplay_, UI_Controller.uiControllerInstance_.leaderBoardPlayerDisplay_.transform.parent);

            newPlayerDisplay.SetDetails(player.name_, player.kills_, player.deaths_);

            newPlayerDisplay.gameObject.SetActive(true);

            leaderBoardPlayersList_.Add(newPlayerDisplay);
        }
    }

    private void DisplayLeaderBoard()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && gameState_ != GameState.Ending)
        {
            if (UI_Controller.uiControllerInstance_.leaderBoard_.activeInHierarchy)
            {
                UI_Controller.uiControllerInstance_.leaderBoard_.SetActive(false);
            }
            else
            {
                ShowLeaderBoard();
            }
        }
    }


    private List<PlayerInfo> SortPlayers(List<PlayerInfo> players)
    {
        List<PlayerInfo> sortedPlayers_ = new List<PlayerInfo>();

        while (sortedPlayers_.Count < players.Count)
        {
            int highest = -1;
            PlayerInfo selectedPlayer = players[0];

            foreach (PlayerInfo player in players)
            {
                if (!sortedPlayers_.Contains(player))
                {
                    if (player.kills_ > highest)
                    {
                        selectedPlayer = player;
                        highest = player.kills_;
                    }
                }
            }

            sortedPlayers_.Add(selectedPlayer);
        }

        return sortedPlayers_;
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();

        SceneManager.LoadScene(0);
    }

    private void ScoreCheck()
    {
        bool winnerFound = false;

        foreach (PlayerInfo player in allPlayers_)
        {
            if (player.kills_ >= killsToWin_ && killsToWin_ > 0)
            {
                winnerFound = true;
                break;
            }
        }

        if (winnerFound)
        {
            if (PhotonNetwork.IsMasterClient && gameState_ != GameState.Ending)
            {
                gameState_ = GameState.Ending;
                ListPlayersSend();
            }
        }
    }

    private void StateCheck()
    {
        if (gameState_ == GameState.Ending)
        {
            EndGame();
        }
    }

    private void EndGame()
    {
        gameState_ = GameState.Ending;

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }

        UI_Controller.uiControllerInstance_.endScreen_.SetActive(true);
        ShowLeaderBoard();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Camera.main.transform.position = mapCamPoint_.position;
        Camera.main.transform.rotation = mapCamPoint_.rotation;

        StartCoroutine(EndGameCoroutine());
    }

    private IEnumerator EndGameCoroutine()
    {
        yield return new WaitForSeconds(waitAfterEnding_);

        if (!perpetual_)
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (!Launcher.launcherInstance_.changeMapBetweenRounds_)
                {
                    NextMatchSend();
                }
                else
                {
                    int newLevel = Random.Range(0, Launcher.launcherInstance_.allMaps_.Length);

                    if (Launcher.launcherInstance_.allMaps_[newLevel] == SceneManager.GetActiveScene().name)
                    {
                        NextMatchSend();
                    }
                    else
                    {
                        PhotonNetwork.LoadLevel(Launcher.launcherInstance_.allMaps_[newLevel]);
                    }
                }
            }
        }

        yield break;
    }

    public void NextMatchSend()
    {
        PhotonNetwork.RaiseEvent(
               (byte)EventCodes.NextMatch,
               null,
               new RaiseEventOptions { Receivers = ReceiverGroup.All },
               new SendOptions { Reliability = true }
               );
    }

    public void NextMatchReceive()
    {
        gameState_ = GameState.Playing;

        UI_Controller.uiControllerInstance_.endScreen_.SetActive(false);
        UI_Controller.uiControllerInstance_.leaderBoard_.SetActive(false);

        foreach(PlayerInfo player in allPlayers_)
        {
            player.kills_ = 0;
            player.deaths_ = 0;
        }

        UpdateStatsDisplay();

        PlayerSpawner.playerSpawnerInstance_.CreatePlayer();

        SetupTimer();
    }

    public void SetupTimer()
    {
        if(matchLength_ > 0)
        {
            currMatchTime_ = matchLength_;
            UpdateTimerDisplay();
        }
    }

    public void UpdateTimerDisplay()
    {
        var timeToDisplay = System.TimeSpan.FromSeconds(currMatchTime_);
        UI_Controller.uiControllerInstance_.timerText_.text = timeToDisplay.Minutes.ToString("00") + ":" + timeToDisplay.Seconds.ToString("00");
    }

    public void TimerSend()
    {
        object[] package = new object[] { (int)currMatchTime_, gameState_ };

        PhotonNetwork.RaiseEvent(
               (byte)EventCodes.TimerSync,
               package,
               new RaiseEventOptions { Receivers = ReceiverGroup.All },
               new SendOptions { Reliability = true }
               );
    }

    public void TimerReceive(object[] dataReceived)
    {
        currMatchTime_ = (int)dataReceived[0];
        gameState_ = (GameState)dataReceived[1];

        UpdateTimerDisplay();

        UI_Controller.uiControllerInstance_.timerText_.gameObject.SetActive(true);
    }
}

[System.Serializable]
public class PlayerInfo
{
    public string name_;
    public int actor_;
    public int kills_;
    public int deaths_;

    public PlayerInfo(string name, int actor, int kills, int deaths)
    {
        name_ = name;
        actor_ = actor;
        kills_ = kills;
        deaths_ = deaths;
    }
}