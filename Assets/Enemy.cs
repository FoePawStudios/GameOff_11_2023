using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

public class Enemy : MonoBehaviour
{
    public float viewDistance = 100f;
    public float viewAngle = 45;
    public float chargeTime = 2f;
    public float shootCooldown = 5f;
    public float defaultLaserAlpha = .1f;

    private RagdollUpdate ragdollUpdateScript;
    private float enlargeForceMagnitude = 300f;
    private float swapToRagdollMagnitude = 20f;

    public GameObject gunBone;
    public GameObject gunExit;
    public GameObject viewCone;
    private GameObject player;
    private LineRenderer laserLine;
    private Vector2 laserAimingAt;

    private bool chargingShot = false;
    private float chargeStart = 0;
    private bool isOnShootCooldown = false;
    private float shootCooldownStart = 0;

    // Start is called before the first frame update
    void Start()
    {
        ragdollUpdateScript = gameObject.GetComponent<RagdollUpdate>();

        gameObject.layer = LayerMask.NameToLayer("Enemy");
        
        player = GameObject.FindGameObjectWithTag("Player");

        //We set these in the editor now instead of dynamically finding them ***********
        //gunExit = gameObject.transform.Find("GunExit").gameObject;
        //gunBone = gunExit.transform.parent.gameObject;
        //viewCone = gameObject.transform.Find("ViewCone").gameObject;

        //set up view cone collider according to view range and view angle
        updateViewCone();

        laserLine = gameObject.GetComponent<LineRenderer>();
        //if(!laserLine) laserLine = gameObject.AddComponent<LineRenderer>();
        //laserLine.enabled = false;

    }

    private void updateViewCone()
    {
        PolygonCollider2D viewConeColl = viewCone.GetComponent<PolygonCollider2D>();

        Assert.IsTrue(viewConeColl.points.Length == 3);

        // Get the current points of the PolygonCollider2D
        Vector2[] currentPoints = viewConeColl.GetPath(0);


        currentPoints[0] = Vector2.zero;

        Vector2 edgeDirection = new Vector2(Mathf.Cos((viewAngle / 2) * Mathf.Deg2Rad), Mathf.Sin((viewAngle / 2) * Mathf.Deg2Rad));

        currentPoints[1] = edgeDirection.normalized * viewDistance;
        
        //flip the y of the direction to get the lower edge of the view cone
        edgeDirection.y = edgeDirection.y * -1;
        currentPoints[2] = edgeDirection.normalized * viewDistance;

        viewConeColl.points = currentPoints;

    }


    // Update is called once per frame
    void Update()
    {
        if(isOnShootCooldown && Time.time - shootCooldownStart > shootCooldown)
        {
            isOnShootCooldown = false;
            setLaserAlpha(defaultLaserAlpha);
        }

        UpdateLaserLine();

        if (chargingShot)
        {
            //If we finished charging a shot, shoot it and enter cooldown
            if(Time.time - chargeStart > chargeTime)
            {
                shootShot();

            }
            //otherwise just indicate we are charging a shot
            else
            {
                //lerp laser color or transparency or luminance or something higher according to charge time
                setLaserAlpha(Mathf.Clamp((Time.time - chargeStart) / chargeTime, defaultLaserAlpha, 1));
            }
        }

        if(playerInLOS())
        {
            aimAtPlayer();

            if(!chargingShot && !isOnShootCooldown) 
            {
                chargingShot = true;
                chargeStart = Time.time;
            }
        }
    }

    private void shootShot()
    {
        chargingShot = false;
        setLaserAlpha(0);

        RaycastHit2D rayHit = ShootRayFromGun();

        //if we hit the player send them back to their last checkpoint
       if(rayHit.collider.gameObject == player || rayHit.collider.gameObject.transform.IsChildOf(player.transform))
        {
            player.GetComponent<PlayerController>().respawnAtCheckpoint();
        }

        isOnShootCooldown = true;
        shootCooldownStart = Time.time;
    }

    private void UpdateLaserLine()
    {
        RaycastHit2D rayHit = ShootRayFromGun();

        //once we have found something that isn't us, draw update the line and return
        laserLine.SetPosition(0, gunExit.transform.position);
        laserLine.SetPosition(1, rayHit.point);

    }

    private RaycastHit2D ShootRayFromGun()
    {
        //shoot ray at targeting point to see where the laser should end
        RaycastHit2D[] rayHits = Physics2D.RaycastAll(gunExit.transform.position, gunExit.transform.position - gunBone.transform.position);

        foreach (RaycastHit2D rayHit in rayHits)
        {
            //don't count anything that's a child of this enemy
            if (rayHit.collider.gameObject.transform.IsChildOf(gameObject.transform)) continue;

            return rayHit;
        }

        return  new RaycastHit2D();
    }

    void setLaserAlpha(float alpha)
    {
        Color laserRed = Color.red;
        laserRed.a = alpha;
        laserLine.startColor = laserRed;
        laserLine.endColor = laserRed;
    }

    bool playerInLOS()
    {
        //Make sure the enemy is facing the correct direction first
        if (player.transform.position.x < gameObject.transform.position.x && facingRight())
        {
            Vector3 tempScale = gameObject.transform.localScale;
            tempScale.x *= -1;
            gameObject.transform.localScale = tempScale;
        }
        else if (player.transform.position.x > gameObject.transform.position.x && !facingRight())
        {
            Vector3 tempScale = gameObject.transform.localScale;
            tempScale.x *= -1;
            gameObject.transform.localScale = tempScale;
        }

        //if player is in view cone AND is in LOS, aim sniper at them
        if (isPlayerInViewCone())
        {
            Collider2D playerCollider = player.GetComponent<Collider2D>();
            Collider2D viewConeCollider = viewCone.GetComponent<Collider2D>();

            //start shooting some rays at the player collider, starting with center, then top and bottom
            Vector2 rayDirection = playerCollider.bounds.center - viewCone.transform.position;

            RaycastHit2D[] hits = new RaycastHit2D[10];
            viewConeCollider.Raycast(rayDirection, hits);
            //Debug.DrawRay(viewCone.transform.position, rayDirection, Color.red);
            foreach (RaycastHit2D hit in hits)
            {
                //if we hit a child collider of ours, just skip it
                if(hit.collider.gameObject.transform.IsChildOf(gameObject.transform))
                {
                    continue;
                }

                //if this isn't a child collider, and it's the player, we are good
                if (hit.collider.gameObject == player || hit.collider.gameObject.transform.IsChildOf(player.transform))
                {
                    laserAimingAt = hit.point;
                    return true;
                }

                //otherwise we hit something that isn't the player, we can stop
                break;
            }


            //now shoot one at the minimum
            rayDirection = playerCollider.bounds.min - viewCone.transform.position;

            hits = new RaycastHit2D[10];
            viewConeCollider.Raycast(rayDirection, hits);
            //Debug.DrawRay(viewCone.transform.position, rayDirection, Color.blue);
            foreach (RaycastHit2D hit in hits)
            {
                //if we hit a child collider of ours, just skip it
                if (hit.collider.gameObject.transform.IsChildOf(gameObject.transform))
                {
                    continue;
                }

                //if this isn't a child collider, and it's the player, we are good
                if (hit.collider.gameObject == player || hit.collider.gameObject.transform.IsChildOf(player.transform))
                {
                    laserAimingAt = hit.point;
                    return true;
                }

                //otherwise we hit something that isn't the player, we can stop
                break;
            }

            //now shoot one at the max
            rayDirection = playerCollider.bounds.max - viewCone.transform.position;

            hits = new RaycastHit2D[10];
            viewConeCollider.Raycast(rayDirection, hits);
            //Debug.DrawRay(viewCone.transform.position, rayDirection, Color.green);
            foreach (RaycastHit2D hit in hits)
            {
                //if we hit a child collider of ours, just skip it
                if (hit.collider.gameObject.transform.IsChildOf(gameObject.transform))
                {
                    continue;
                }

                //if this isn't a child collider, and it's the player, we are good
                if (hit.collider.gameObject == player || hit.collider.gameObject.transform.IsChildOf(player.transform) )
                {
                    laserAimingAt = hit.point;
                    return true;
                }

                //otherwise we hit something that isn't the player, we can stop
                break;
            }
        }

        return false;
    }

    void aimAtPlayer()
    {
        Vector2 gunDirection = player.GetComponent<Collider2D>().bounds.center - gunBone.transform.position;

        //Debug.DrawRay(gunBone.transform.position, gunDirection);

        //If we are flipped, flip the vector to match
        if (!facingRight())
        {
            gunDirection.x *= -1;
            gunDirection.y *= -1;
        }

        Vector3 gunRotation = Vector3.zero;
        gunRotation.z = Mathf.Rad2Deg * Mathf.Atan2(gunDirection.y, gunDirection.x);
        gunBone.transform.eulerAngles = gunRotation;
    }

    private bool facingRight()
    {
        return gameObject.transform.localScale.x > 0;
    }

    private void handleCollisionRagdoll(Collision2D collision)
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
            else if (collision.relativeVelocity.magnitude > swapToRagdollMagnitude)
            {
                ragdollUpdateScript.swapToRagdoll();
            }
        }
    }

    bool isPlayerInViewCone()
    {
        List<Collider2D> activeTriggers = viewCone.GetComponent<collisionTracker>().activelyCollidingList;

        foreach( Collider2D trigger in activeTriggers ) 
        {
            if (trigger.gameObject == player || trigger.gameObject.transform.IsChildOf(player.transform))
            {
                return true;
            }
        }

        return false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        handleCollisionRagdoll(collision);
    }
}
