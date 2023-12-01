using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathPoint : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        PlayerController playerScript = collider.gameObject.GetComponent<PlayerController>();
        if (playerScript) playerScript.respawnAtCheckpoint();

        ScalableObject scalableObjectScript = collider.gameObject.GetComponent<ScalableObject>();
        if (scalableObjectScript) scalableObjectScript.respawn();
    }
}
