using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner playerSpawnerInstance_;

    private void Awake()
    {
        if(playerSpawnerInstance_ != null)
        {
            Destroy(this);
        }
        else
        {
            playerSpawnerInstance_ = this;
        }
        
    }

    public PlayerSO playerToSpawn_;
    public Transform[] spawnPoints_;
    public Transform spawnPoint;
    private GameObject player_;
    public GameObject deathEffect_;
    public float respawnTime_ = 5f;

    private void Start()
    {
        foreach(Transform spawn in spawnPoints_) 
        {
            spawn.gameObject.SetActive(false);
        }

        //CreatePlayer();

        if (PhotonNetwork.IsConnected)
        {
            CreatePlayer();
        }

    }

    public void CreatePlayer()
    {
        spawnPoint = GetSpawnPointInRandom();
        //PlayerModel playerModel = new PlayerModel();
        //PlayerController playerController = new PlayerController(playerModel, playerToSpawn_.playerPrefab, this);
        player_ = PhotonNetwork.Instantiate(playerToSpawn_.playerPrefab.gameObject.name, spawnPoint.position, spawnPoint.rotation);
    }

    public Transform GetSpawnPointInRandom()
    {
        return spawnPoints_[Random.Range(0, spawnPoints_.Length)];
    }

    public void Die(string damager)
    {
        UI_Controller.uiControllerInstance_.deathText_.text = "You were killed by " + damager;

        MatchManager.matchMgrInstance_.UpdateStatsSend(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);

        if(player_ != null)
        {
            StartCoroutine(DieCoroutine());
        }
    }

    public IEnumerator DieCoroutine()
    {
        PhotonNetwork.Instantiate(deathEffect_.name, player_.transform.position, Quaternion.identity);
        PhotonNetwork.Destroy(player_);
        player_ = null;
        UI_Controller.uiControllerInstance_.deathScreen_.SetActive(true);

        yield return new WaitForSeconds(respawnTime_);

        UI_Controller.uiControllerInstance_.deathScreen_.SetActive(false);

        if (MatchManager.matchMgrInstance_.gameState_ == MatchManager.GameState.Playing && player_ == null)
        {
            CreatePlayer();
        }

        yield break;
    }
}
