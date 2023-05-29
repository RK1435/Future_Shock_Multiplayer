using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Realtime;

public class RoomButton : MonoBehaviour
{
    public TMP_Text buttonText_;
    private RoomInfo roomInfo_;

    public void SetButtonDetails(RoomInfo inputInfo)
    {
        roomInfo_ = inputInfo;
        buttonText_.text = roomInfo_.Name;
    }

    public void OpenRoom()
    {
        Launcher.launcherInstance_.JoinRoom(roomInfo_);
    }
}
