using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GunMode
{
    Max_Scale,
    Original_Scale,
    Min_Scale
}

public class PlayerController : MonoBehaviour
{
    public float horizontalSpeed = 5;
    public float jumpSpeed = 8;
    public float jumpCooldown = 1;
    public float scaleRate = 30;
    private float mouseHoldTime = .2f;
    public float dragRange = 2f;
    public float dragStrength = 5f;
    public LayerMask scalableLayer;
    private Vector2 inputVector;
    private Rigidbody2D rigidBody2d;
    private GameObject shoulderPivot;
    private GameObject gunExit;
    private GameObject ShootProjection;
    private GameObject aimingAt;
    public bool isGrounded { get; set; }
    private float shoulderGunYOffset;
    private float lastJump;
    private float oldGravityScale;
    public GunMode gunMode;

    private bool isDragging;
    private float lastClickDown;
    private GameObject draggedObject;
    
    // Start is called before the first frame update
    void Start()
    {
        inputVector = new(0.0f, 0.0f);
        rigidBody2d = gameObject.GetComponent<Rigidbody2D>();
        shoulderPivot = GameObject.FindGameObjectWithTag("ShoulderPivot");
        gunExit = GameObject.FindGameObjectWithTag("gunExit");
        ShootProjection = GameObject.FindGameObjectWithTag("ShootProjection");
        shoulderGunYOffset = Mathf.Abs( shoulderPivot.transform.position.y - gunExit.transform.position.y );
        lastJump = 0;
        isGrounded = false;
        gunMode = GunMode.Max_Scale;

    }

    // Update is called once per frame
    void Update()
    {
        handleMovement();
        pointGunAtMouse();
        handleGunActions();
        moveProjectedShot();
        checkDragging();
    }

    private void FixedUpdate()
    {
        //handle grabbing objects
        moveGrabbedObject();
    }

    public void checkDragging()
    {
        if( Input.GetMouseButtonDown(0) ) 
        {
            lastClickDown = Time.time;
            draggedObject = aimingAt;
        }

        //if we are holding down the mouse long enough and aren't already dragging the item, try to start grabbing it
        if( Input.GetMouseButton(0) && Time.time - lastClickDown > mouseHoldTime && !isDragging)
        {
            if(!draggedObject)
            {
                draggedObject = aimingAt;
            }
            isDragging = tryGrabObject(draggedObject); 
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            stopDragging();
        }
    }

    bool tryGrabObject(GameObject scalableObject)
    {
        //there must be an object to grab
        if (!scalableObject) return false;

        //it has to have the scalable script attached to it
        if (!scalableObject.GetComponent<ScalableObject>()) return false;

        //it can't be a fixed object (either doesn't have a rigidbody or has transform constraints
        if (scalableObject.GetComponent<ScalableObject>().isFixedObject) return false;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float mouseDistance = Vector2.Distance(gunExit.transform.position, mousePos);

        //it has to be within grab distance
        if (mouseDistance > dragRange) return false;


        //We succeeded in grabbing the object
        draggedObject = scalableObject;
        oldGravityScale = draggedObject.GetComponent<Rigidbody2D>().gravityScale;
        draggedObject.GetComponent<Rigidbody2D>().gravityScale = 0;
        draggedObject.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        

        return true;
    }

    private void stopDragging()
    {
        if(draggedObject)
        {
            draggedObject.GetComponent<Rigidbody2D>().gravityScale = oldGravityScale;
        }
        draggedObject = null;
        isDragging = false;
    }

    void handleMovement()
    {
        //Handle Horizontal Input
        if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1) // || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0)
        {
            inputVector.Set(Input.GetAxisRaw("Horizontal") * horizontalSpeed, 0f);
            gameObject.transform.Translate(inputVector * Time.deltaTime);
        }

        //Handle Jump Input
        if( isGrounded )
        {
            if (Input.GetAxisRaw("Vertical") > 0.1 || Input.GetKeyDown(KeyCode.Space)) // || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0)
            {
                //If we are off jump cooldown
                if (Time.time - lastJump > jumpCooldown)
                { 
                    lastJump = Time.time;
                    rigidBody2d.velocity = Vector2.up * jumpSpeed;
                }
            }
        }
        
    }

    void pointGunAtMouse()
    {
        //Get the mouse position
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //First get the amount to rotate the shoulder to look at the mouse point
        Vector3 gunPointVector = new Vector3(mousePos.x - shoulderPivot.transform.position.x, mousePos.y - shoulderPivot.transform.position.y);
        gunPointVector = gunPointVector.normalized;

        float absoluteRotation = Mathf.Rad2Deg * Mathf.Atan2(gunPointVector.y, gunPointVector.x);

        float rotateDistance = absoluteRotation - shoulderPivot.transform.localEulerAngles.z;

        //Then calculate how much extra we need to rotate to have the actual gun barrel aim at the mouse point
        float shoulderToMouseDist = Vector2.Distance(shoulderPivot.transform.position, mousePos);
        float hypotenuseDistance = Mathf.Sqrt(Mathf.Pow(shoulderToMouseDist, 2) + Mathf.Pow(shoulderGunYOffset, 2));
        rotateDistance = rotateDistance + Mathf.Rad2Deg * Mathf.Asin(shoulderGunYOffset / hypotenuseDistance);

        shoulderPivot.transform.Rotate(0f, 0f, rotateDistance);
    }

    void moveProjectedShot()
    {
        //Shoot ray from gun exit to find anything on the scalable layer that it would collide with
        RaycastHit2D rayHit = Physics2D.Raycast(gunExit.transform.position, gunExit.transform.up, Mathf.Infinity, scalableLayer.value );

        if (rayHit.transform == null)
        {
            ShootProjection.SetActive(false);
            aimingAt = null;
            return;
        }

        ShootProjection.SetActive(true);
        ShootProjection.transform.position = rayHit.point;

        aimingAt = rayHit.transform.gameObject;

    }

    void handleGunActions()
    {
        if (aimingAt == null) return;

        if( Input.mouseScrollDelta.y != 0)
        {
            aimingAt.GetComponent<ScalableObject>().changeScale(Input.mouseScrollDelta.y * Time.fixedDeltaTime * scaleRate);
        }

        //left click
        if (Input.GetMouseButtonDown(0))
        {
            aimingAt.GetComponent<ScalableObject>().scaleToMin();
        }

        //right click
        if ( Input.GetMouseButtonDown(1) )
        {
            aimingAt.GetComponent<ScalableObject>().scaleToMax();
        }

        //middle mouse click
        if (Input.GetMouseButtonDown(2))
        {
            aimingAt.GetComponent<ScalableObject>().scaleToStart();
        }

    }

    void moveGrabbedObject()
    {
        if(isDragging) 
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float mouseDistance = Vector2.Distance( gunExit.transform.position,  mousePos);

            Vector2 targetDrag = new Vector2(mousePos.x, mousePos.y);
            Vector2 mouseDirection = (mousePos - (Vector2)gunExit.transform.position).normalized;

            //If the mouse is too far away, just have the object move to max range
            if ( mouseDistance > dragRange) 
            {
                targetDrag = (Vector2)gunExit.transform.position + mouseDirection * dragRange;
            }

            Rigidbody2D draggedRB = draggedObject.GetComponent<Rigidbody2D>();

            Vector2 targetDirection = targetDrag - (Vector2)draggedObject.transform.position;

            //accelerate the object towards the target drag location
            draggedRB.AddForce(targetDirection * dragStrength , ForceMode2D.Impulse);

            //the velocity direction should be altered to face the target point harder and harder as it gets closer
            //float velocityMag = draggedRB.velocity.magnitude;
            Vector2 currentVel = draggedRB.velocity;

            //If the velocity is moving in the opposite direction of the target location, dampen it so it doesn't orbit the target
            if (currentVel.x * targetDirection.x < 0)
            {
                currentVel.x = currentVel.x * .7f;
            }
            if (currentVel.y * targetDirection.y < 0)
            {
                currentVel.y = currentVel.y * .7f;
            }

            draggedRB.velocity = currentVel;

        }
    }

    void nextGunMode()
    {
        int gunModeInt = (int)gunMode;
        gunModeInt++;

        if (gunModeInt > Enum.GetValues(typeof(GunMode)).Length-1 ) 
        {
            gunModeInt = 0;
        }

        gunMode = (GunMode)gunModeInt;

    }

    void previousGunMode()
    {
        int gunModeInt = (int)gunMode;
        gunModeInt--;

        if (gunModeInt < 0)
        {
            gunModeInt = Enum.GetValues(typeof(GunMode)).Length - 1;
        }

        gunMode = (GunMode)gunModeInt;
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        isGrounded = true;
    }

    void OnTriggerExit2D(Collider2D collider)
    {
        isGrounded = false;
    }

}
