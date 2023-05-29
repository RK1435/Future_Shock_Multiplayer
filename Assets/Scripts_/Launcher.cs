using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    #region Singleton
    public static Launcher launcherInstance_;
    
    private void Awake()
    {
        if(launcherInstance_ != null)
        {
            Destroy(this);
        }
        else
        {
            launcherInstance_ = this;
        }
    }
    #endregion

    public GameObject loadingScreen_;
    public TMP_Text loadingText_;

    public GameObject menuButtons_;

    public GameObject createRoomScreen_;
    public TMP_InputField roomNameInput_;

    public GameObject roomScreen_;
    public TMP_Text roomNameText_;
    public TMP_Text playerNameLabel_;
    private List<TMP_Text> playerNameList_ = new List<TMP_Text>();

    public GameObject errorScreen_;
    public TMP_Text errorText_;

    public GameObject roomBrowserScreen_;
    public RoomButton roomButton_;
    private List<RoomButton> roomButtonList_ = new List<RoomButton>();

    public GameObject nameInputScreen_;
    public TMP_InputField nameInput_;
    public static bool hasSetNickName;

    public string levelToPlay_;
    public GameObject startButton_;

    public GameObject roomTestButton_;

    public string[] allMaps_;
    public bool changeMapBetweenRounds_ = true;

    // Start is called before the first frame update
    void Start()
    {
        CloseMenus();

        loadingScreen_.SetActive(true);
        loadingText_.text = "Connecting to Network...";

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        #if UNITY_EDITOR
        roomTestButton_.SetActive(true);
        #endif

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseMenus()
    {
        loadingScreen_.SetActive(false);
        menuButtons_.SetActive(false);
        createRoomScreen_.SetActive(false);
        roomScreen_.SetActive(false);
        errorScreen_.SetActive(false);
        roomBrowserScreen_ .SetActive(false);
        nameInputScreen_.SetActive(false); 
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
        loadingText_.text = "Joining Lobby...";
    }

    public override void OnJoinedLobby()
    {
        CloseMenus();
        menuButtons_.SetActive(true);

        PhotonNetwork.NickName = Random.Range(0, 1000).ToString();

        if(!hasSetNickName)
        {
            CloseMenus();
            nameInputScreen_.SetActive(true);

            if(PlayerPrefs.HasKey("playerName"))
            {
                nameInput_.text = PlayerPrefs.GetString("playerName");
            }
        }
        else
        {
            PhotonNetwork.NickName = PlayerPrefs.GetString("playerName");
        }
    }

    public void OpenRoomCreate()
    {
        CloseMenus();
        createRoomScreen_.SetActive(true);
    }

    public void CreateRoom()
    {
        if(!string.IsNullOrEmpty(roomNameInput_.text))
        {
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = 8;

            PhotonNetwork.CreateRoom(roomNameInput_.text, roomOptions);
            CloseMenus();
            loadingText_.text = "Creating Room...";
            loadingScreen_.SetActive(true);
        }
    }

    public override void OnJoinedRoom()
    {
        CloseMenus();
        roomScreen_.SetActive(true);

        roomNameText_.text = PhotonNetwork.CurrentRoom.Name;
        ListAllPlayers();

        if(PhotonNetwork.IsMasterClient)
        {
           startButton_.SetActive(true);
        }
        else 
        { 
            startButton_.SetActive(false); 
        }
    }

    private void ListAllPlayers()
    {
        foreach (TMP_Text player in playerNameList_)
        {
            Destroy(player.gameObject);
        }

        playerNameList_.Clear();

        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < players.Length; i++) 
        {
            TMP_Text newPlayerLabel = Instantiate(playerNameLabel_, playerNameLabel_.transform.parent);
            newPlayerLabel.text = players[i].NickName;
            newPlayerLabel.gameObject.SetActive(true);

            playerNameList_.Add(newPlayerLabel);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TMP_Text newPlayerLabel = Instantiate(playerNameLabel_, playerNameLabel_.transform.parent);
        newPlayerLabel.text = newPlayer.NickName;
        newPlayerLabel.gameObject.SetActive(true);

        playerNameList_.Add(newPlayerLabel);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ListAllPlayers();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText_.text = "Failed to Create Room: " + message;
        CloseMenus();
        errorScreen_.SetActive(true);
    }

    public void CloseErrorScreen()
    {
        CloseMenus();
        menuButtons_.SetActive(true);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        CloseMenus();
        loadingText_.text = "Leaving Room....";
        loadingScreen_.SetActive(true);
    }

    public override void OnLeftRoom()
    {
        CloseMenus();
        menuButtons_.SetActive(true);
    }

    public void OpenRoomBrowser()
    {
        CloseMenus();
        roomBrowserScreen_.SetActive(true);
    }

    public void CloseRoomBrowser()
    {
        CloseMenus();
        menuButtons_.SetActive(true);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach(RoomButton rmBtn in roomButtonList_) 
        {
            Destroy(rmBtn.gameObject);
        }

        roomButtonList_.Clear();

        roomButton_.gameObject.SetActive(false);

        for(int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].PlayerCount != roomList[i].MaxPlayers && !roomList[i].RemovedFromList)
            {
                RoomButton newRoomButton = Instantiate(roomButton_, roomButton_.transform.parent);
                newRoomButton.SetButtonDetails(roomList[i]);
                newRoomButton.gameObject.SetActive(true);

                roomButtonList_.Add(newRoomButton);
            }
        }
    }

    public void JoinRoom(RoomInfo inputInfo)
    {
        PhotonNetwork.JoinRoom(inputInfo.Name);
        CloseMenus();
        loadingText_.text = "Joining Room";
        loadingScreen_.SetActive(true);
    }

    public void SetNickName()
    {
        if(!string.IsNullOrEmpty(nameInput_.text))
        {
            PhotonNetwork.NickName = nameInput_.text;
            PlayerPrefs.SetString("playerName", nameInput_.text);

            CloseMenus();
            menuButtons_.SetActive(true);
            hasSetNickName = true;
        }
    }

    public void StartGame()
    {
        //PhotonNetwork.LoadLevel(levelToPlay_);
        PhotonNetwork.LoadLevel(allMaps_[Random.Range(0, allMaps_.Length)]);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            startButton_.SetActive(true);
        }
        else
        {
            startButton_.SetActive(false);
        }
    }

    public void QuickJoin()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 8;

        PhotonNetwork.CreateRoom("Test", roomOptions);
        CloseMenus();
        loadingText_.text = "Creating Room";
        loadingScreen_.SetActive(true);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
        
        Application.Quit();
    }
}
