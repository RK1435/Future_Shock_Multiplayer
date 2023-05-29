using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.UI;
using Photon.Pun;

public class UI_Controller : MonoBehaviour
{
    #region Singleton
    public static UI_Controller uiControllerInstance_;

    void Awake()
    {
        if (uiControllerInstance_ != null)
        { 
            Destroy(this);
        }
        else
        {
            uiControllerInstance_ = this;
        }
    }
    #endregion

    public TMP_Text overHeatedMessage_;
    public Slider weaponTempSlider_;

    public GameObject deathScreen_;
    public TMP_Text deathText_;

    public Slider healthSlider_;

    public TMP_Text killsText_;
    public TMP_Text deathsText_;

    public GameObject leaderBoard_;
    public LeaderboardPlayer leaderBoardPlayerDisplay_;

    public GameObject endScreen_;

    public TMP_Text timerText_;

    public GameObject optionsScreen_;

    private void Start()
    {
        
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            ShowHideOptions();
        }

        if(optionsScreen_.activeInHierarchy && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ShowHideOptions()
    {
        if(!optionsScreen_.activeInHierarchy)
        {
            optionsScreen_.SetActive(true);
        }
        else
        {
            optionsScreen_.SetActive(false);
        }
    }

    public void ReturnToMainMenu()
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LeaveRoom();
    }

    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
