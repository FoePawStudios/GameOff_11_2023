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

public enum JumpState
{
    Jumping,
    Hanging,
    Falling,
    Grounded
}

public class JumpStateInfo
{
    public float stateStart;
    private JumpState _state;

    public JumpState state
    {
        get { return _state; }
        set
        {
            _state = value;
            stateStart = Time.time;
        }
    }

    public JumpStateInfo(JumpState startState)
    {
        _state = startState;
    }

    public JumpStateInfo()
    {
        _state = JumpState.Falling;
    }

    public void setGrounded()
    {
        state = JumpState.Grounded;
    }

    public void setJumping()
    {
        state = JumpState.Jumping;
    }

    public void setHanging()
    {
        state = JumpState.Hanging;
    }

    public void setFalling()
    {
        state = JumpState.Falling;
    }

    public bool isJumping()
    {
        return _state == JumpState.Jumping;
    }

    public bool isHanging()
    {
        return _state == JumpState.Hanging;
    }

    public bool isGrounded()
    {
        return _state == JumpState.Grounded;
    }

    public bool isFalling()
    {
        return _state == JumpState.Falling;
    }
}


public class PlayerController : MonoBehaviour
{
    public AnimationCurve jumpCurve = new AnimationCurve();
    public float horizontalSpeed = 5;
    public float scaleRate = 30;
    private float mouseHoldTime = .2f;
    public float dragRange = 2f;
    public float dragStrength = 5f;
    public LayerMask scalableLayer;


    //Jump Variables
    public float jumpSpeed = 8;
    public float jumpHeightLimit = 3;

    private float jumpMaxTime = 0;
    public float jumpFloatTime = 1;
    public float fallSpeed = 8;
    private JumpStateInfo jumpState = new JumpStateInfo();
    
    private Vector2 inputVector;
    private Rigidbody2D rigidBody2d;
    private GameObject gunObject;
    private GameObject gunExit;
    private GameObject ShootProjection;
    private GameObject aimingAt;
    public bool isGrounded { get; set; }
    public bool isHeadColliding { get; set; }
    private float shoulderGunYOffset;
    
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
        gunObject = GameObject.FindGameObjectWithTag("Gun");
        gunExit = GameObject.FindGameObjectWithTag("gunExit");
        ShootProjection = GameObject.FindGameObjectWithTag("ShootProjection");
        shoulderGunYOffset = Mathf.Abs( gunObject.transform.position.y - gunExit.transform.position.y );
        isGrounded = false;
        isHeadColliding = false;
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
        jumpMaxTime = jumpHeightLimit / jumpSpeed;
    }

    private void FixedUpdate()
    {
        //handle grabbing objects
        handleJump();
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

        if ( Input.GetAxisRaw("Vertical") > 0.1 || Input.GetKeyDown(KeyCode.Space) )
        {
            if(jumpState.isGrounded())
            {
                jumpState.setJumping();
            }
        }

    }

    void handleJump()
    {
        Debug.Log(jumpState.state);
        //if we are grounded, and we didn't just start a jump, set the state to grounded
        if (isGrounded && !jumpState.isGrounded())
        {
            if(!jumpState.isJumping() || Time.time - jumpState.stateStart >= .5 ) 
            {
                jumpState.setGrounded();
            }
        }

        if(jumpState.isGrounded() && !isGrounded)
        {
            jumpState.setFalling();
        }

        //If we are jumping, check if we should keep upward velocity or transition into hanging or falling
        if (jumpState.isJumping())
        {
            //if we are no longer holding the jump button, transition to falling state
            if(Input.GetAxisRaw("Vertical") < 0.1 && !Input.GetKey(KeyCode.Space) )
            {
                jumpState.setFalling();
            }
            //first check if we hit our head, and if so move to the falling state
            else if( isHeadColliding )
            {
                jumpState.setFalling();
            }
            //next check if we exceeded jump time limit and if so transition to the hang time state
            else if (Time.time - jumpState.stateStart > jumpMaxTime)
            {
                jumpState.setHanging();
;           }
            //otherwise we can keep out velocity moving upwards 
            else
            {
                Vector2 rbVel = rigidBody2d.velocity;
                rbVel.y = jumpSpeed;
                rigidBody2d.velocity = rbVel;
            }
        }

        if (jumpState.isHanging())
        {
            //if we are no longer holding the jump button, transition to falling state
            if (Input.GetAxisRaw("Vertical") < 0.1 && !Input.GetKey(KeyCode.Space))
            {
                jumpState.setFalling();
            }
            //if we have hung for long enough, start falling
            else if (Time.time - jumpState.stateStart > jumpFloatTime)
            {
                jumpState.setFalling();
            }
        }

        if( jumpState.isFalling() ) 
        {
            //if we are grounded, update the state and we are all good
            if (isGrounded)
            {
                jumpState.setGrounded();
            }
            //otherwise set vertical velocity
            else
            {
                Vector2 rbVel = rigidBody2d.velocity;
                rbVel.y = -jumpSpeed;
                rigidBody2d.velocity = rbVel;
            }
        }
    }

    void pointGunAtMouse()
    {
        //Get the mouse position
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //if the mouse position is behind the character, flip the character
        if (mousePos.x < gameObject.transform.position.x ) 
        {
            if (isFacingRight())
            {
                flipPlayerHorizontal();
            }
        }
        else
        {
            if (!isFacingRight())
            {
                flipPlayerHorizontal();
            }
        }

        

        //First get the amount to rotate the shoulder to look at the mouse point
        Vector3 gunPointVector = new Vector3(mousePos.x - gunObject.transform.position.x, mousePos.y - gunObject.transform.position.y);

        gunPointVector = gunPointVector.normalized;

        //If we are flipped, flip the vector to match
        if(!isFacingRight())
        {
            gunPointVector.x *= -1;
        }


        float absoluteRotation = Mathf.Rad2Deg * Mathf.Atan2(gunPointVector.y, gunPointVector.x);
        float rotateDistance = absoluteRotation - gunObject.transform.localEulerAngles.z;

        //Then calculate how much extra we need to rotate to have the actual gun barrel aim at the mouse point
        float shoulderToMouseDist = Vector2.Distance(gunObject.transform.position, mousePos);
        float hypotenuseDistance = Mathf.Sqrt(Mathf.Pow(shoulderToMouseDist, 2) + Mathf.Pow(shoulderGunYOffset, 2));
        rotateDistance = rotateDistance + Mathf.Rad2Deg * Mathf.Asin(shoulderGunYOffset / hypotenuseDistance);

        gunObject.transform.Rotate(0f, 0f, rotateDistance);
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

    bool isFacingRight()
    {
        return gameObject.transform.localScale.x > 0;
    }

    void flipPlayerHorizontal()
    {
        Vector3 localScaleTemp = gameObject.transform.localScale;
        localScaleTemp.x *= -1;
        gameObject.transform.localScale = localScaleTemp;
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
}
