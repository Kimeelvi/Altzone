using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ExitRaid : MonoBehaviour
{
    public bool raidEnded = false;
    [SerializeField, Header("Reference scripts")] private Raid_References raid_References;
   
    public void EndRaid()
    {
        raidEnded = true;

        raid_References.RedScreen.SetActive(true);
        raid_References.EndMenu.SetActive(true);

        if(!raid_References.OutOfSpace.enabled && !raid_References.OutOfTime.enabled)
            raid_References.RaidEndedText.enabled = true;


    }
}