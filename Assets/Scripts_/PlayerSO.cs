using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Player", menuName = "Create Player")]
public class PlayerSO : ScriptableObject
{
    [Header("Player Name and Discription")]
    public string playerName;
    public string discriptionOfPlayer;

    [Header("Player Prefab")]
    public PlayerController_02 playerPrefab;
}
