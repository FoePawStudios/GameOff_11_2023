using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private RagdollUpdate ragdollUpdateScript;
    private float enlargeForceMagnitude = 300f;
    private float swapToRagdollMagnitude = 20f;

    // Start is called before the first frame update
    void Start()
    {
        ragdollUpdateScript = gameObject.GetComponent<RagdollUpdate>();

        gameObject.layer = LayerMask.NameToLayer("Enemy");

    }

    // Update is called once per frame
    void Update()
    {
        //if an object hits us hard enough, turn on ragdoll
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject scalableObject = collision.collider.gameObject;
        ScalableObject scalableScript = null;
        if (scalableObject) scalableScript = scalableObject.GetComponent<ScalableObject>();
        if (scalableScript)
        {
            //if the object is currently scaling artificially increase the force to make it cooler
            if (scalableScript.isEnlarging())
            {
                ragdollUpdateScript.swapToRagdoll();

                Vector2 forceDirection = Vector2.zero;

                foreach (ContactPoint2D contact in collision.contacts)
                {
                    forceDirection = forceDirection + contact.normal;
                    //ragdollUpdateScript.addForceToRagdoll(contact.normal * (enlargeForceMagnitude/collision.contacts.Length)); //, contact.point
                }

                ragdollUpdateScript.addForceToRagdoll(forceDirection.normalized * enlargeForceMagnitude);

            }
            //if the object is moving fast enough, turn it to ragdoll anyways
            else if(collision.relativeVelocity.magnitude > swapToRagdollMagnitude)
            {
                Debug.Log(collision.relativeVelocity.magnitude);
                ragdollUpdateScript.swapToRagdoll();
            }
        }
    }
}
