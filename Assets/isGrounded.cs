using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class isGrounded : MonoBehaviour
{
    private PlayerController playerScript;
    // Start is called before the first frame update
    void Start()
    {
        playerScript = gameObject.transform.parent.GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnTriggerEnter2D(Collider2D collider)
    {
        playerScript.isGrounded = true;
    }

    void OnTriggerExit2D(Collider2D collider)
    {
        playerScript.isGrounded = false;
    }
}
