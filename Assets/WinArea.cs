using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WinArea : MonoBehaviour
{
    GameObject player;
    GameObject winText;
    collisionTracker collTracker;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        winText = GameObject.FindGameObjectWithTag("WinText");
        collTracker = gameObject.GetComponent<collisionTracker>();
    }

    // Update is called once per frame
    void Update()
    {
        if(collTracker.activelyCollidingList.Count > 0 )
        {
            winText.GetComponent<TextMeshProUGUI>().enabled = true;
        }
        else
        {
            winText.GetComponent<TextMeshProUGUI>().enabled = false;
        }
    }
}
