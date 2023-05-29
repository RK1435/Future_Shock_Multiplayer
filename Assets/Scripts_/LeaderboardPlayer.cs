using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LeaderboardPlayer : MonoBehaviour
{
    public TMP_Text playerNameText_;
    public TMP_Text killsText_;
    public TMP_Text deathsText_;

    public void SetDetails(string name, int kills, int deaths)
    {
        playerNameText_.text = name;
        killsText_.text = kills.ToString();
        deathsText_.text = deaths.ToString();
    }
}
