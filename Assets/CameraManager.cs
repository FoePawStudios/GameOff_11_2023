using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private GameObject player;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        followPlayer();
    }

    public void followPlayer()
    {
        Vector3 newCameraLocation = player.transform.position;
        newCameraLocation.z = gameObject.transform.position.z;

        gameObject.transform.position = newCameraLocation;
    }

}
