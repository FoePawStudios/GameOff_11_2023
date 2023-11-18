using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class HUD : MonoBehaviour
{
    private GameObject player;
    private GameObject HUDObject;
    private GameObject GunSelectUI;
    //private int lastScreenWidth = 0;
    //private int lastScreenHeight = 0;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        HUDObject = GameObject.FindGameObjectWithTag("HUD");
        GunSelectUI = GameObject.Find("SelectedGun");
        GunSelectUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        updateHUD();
    }

    void updateHUD()
    {
        //updateGunSelect();
    }

    void updateGunSelect()
    {//"SelectedGun (TMPro.TextMeshProUGUI)"
        string currentGunSelection = player.GetComponent<PlayerController>().gunMode.ToString().Replace("_", " ");
        TextMeshProUGUI displayGunMode = GunSelectUI.GetComponent<TextMeshProUGUI>();
        if (displayGunMode.text != currentGunSelection)
        {
            displayGunMode.text = currentGunSelection;
        }
    }
}
