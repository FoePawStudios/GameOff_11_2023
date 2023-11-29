using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms;

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
    //public AnimationCurve jumpCurve = new AnimationCurve();
    public float horizontalSpeed = 5;
    public float scaleRate = 30;
    public float dragRange = 2f;
    public float dragStrength = 5f;
    public LayerMask scalableLayer;
    private float mouseHoldTime = .1f;

    //Jump Variables
    public float jumpSpeed = 8;
    public float maxJumpHeight = 3;
    public float minJumpHeight = 2;

    public float jumpFloatTime = 1;
    public float fallSpeed = 8;
    private JumpStateInfo jumpState = new JumpStateInfo();
    
    private Vector2 inputVector;
    private Rigidbody2D rigidBody2d;
    private GameObject gunObject;
    private GameObject gunExit;
    private GameObject ShootProjection;
    private GameObject aimingAt;

    private collisionTracker bodyCollisionTracker;
    private collisionTracker headCollisionTracker;
    private collisionTracker feetCollisionTracker;
    private collisionTracker frontCollisionTracker;
    private collisionTracker backCollisionTracker;

    Dictionary<Collider2D, bool> groundCollisions = new Dictionary<Collider2D, bool>();
    Dictionary<Collider2D, bool> headCollisions = new Dictionary<Collider2D, bool>();

    //private List<GameObject> groundCollisions = new List<GameObject>();
    //private List<GameObject> headCollisions = new List<GameObject>();

    private float shoulderGunYOffset;
    
    private float oldGravityScale;
    //public int oldLayer;
    public GunMode gunMode;

    private bool usingGun = false;
    private bool isDragging;
    private float lastClickDown;
    private GameObject draggedObject;

    private Vector2 checkPoint;
    
    // Start is called before the first frame update
    void Start()
    {
        inputVector = new(0.0f, 0.0f);
        rigidBody2d = gameObject.GetComponent<Rigidbody2D>();
        gunObject = GameObject.FindGameObjectWithTag("Gun");
        gunExit = GameObject.FindGameObjectWithTag("gunExit");
        ShootProjection = GameObject.FindGameObjectWithTag("ShootProjection");
        shoulderGunYOffset = Mathf.Abs( gunObject.transform.position.y - gunExit.transform.position.y );
        gunMode = GunMode.Max_Scale;
        checkPoint = gameObject.transform.position;

        headCollisionTracker = GameObject.FindGameObjectWithTag("Head").GetComponent<collisionTracker>();
        feetCollisionTracker = GameObject.FindGameObjectWithTag("Feet").GetComponent<collisionTracker>();
        bodyCollisionTracker = gameObject.GetComponent<collisionTracker>();
        frontCollisionTracker = GameObject.FindGameObjectWithTag("FrontSide").GetComponent<collisionTracker>();
        backCollisionTracker = GameObject.FindGameObjectWithTag("BackSide").GetComponent<collisionTracker>();
    }

    // Update is called once per frame
    void Update()
    {
        //By default set usingGun to false and update as we evaluate
        usingGun=false;

        handleMovement();
        pointGunAtMouse();
        handleGunActions();
        moveProjectedShot();
        checkDragging();
        handleGunAnim();

    }

    private void FixedUpdate()
    {
        //handle grabbing objects
        handleJump();
        moveGrabbedObject();
    }

    
    void handleGunAnim()
    {
        if(isDragging)
        {
            usingGun = true;
        }
        gunObject.GetComponent<Animator>().SetBool("isUsing", usingGun);
    }


    public void checkDragging()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastClickDown = Time.time;
            draggedObject = aimingAt;
        }

        //if we are holding down the mouse long enough and aren't already dragging the item, try to start grabbing it
        if (Input.GetMouseButton(0) && Time.time - lastClickDown > mouseHoldTime && !isDragging)
        {
            if (!draggedObject)
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

        ScalableObject scalableScript = scalableObject.GetComponent<ScalableObject>();

        //it has to have the scalable script attached to it
        if (!scalableScript) return false;

        //it can't be a fixed object (either doesn't have a rigidbody or has transform constraints
        if (scalableScript.isFixedObject()) return false;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float mouseDistance = Vector2.Distance(gunExit.transform.position, mousePos);

        //it has to be within grab distance
        if (mouseDistance > (dragRange*1.75)) return false;


        //We succeeded in grabbing the object
        scalableScript.unfreezeObject();
        draggedObject = scalableObject;
        
        oldGravityScale = draggedObject.GetComponent<Rigidbody2D>().gravityScale;
        //oldLayer = draggedObject.layer;
        
        draggedObject.GetComponent<Rigidbody2D>().gravityScale = 0;
        draggedObject.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        draggedObject.layer = LayerMask.NameToLayer("DraggedObject");

        //draggedObject.GetComponent<Collider2D>().excludeLayers = oldColliderFilters & LayerMask.NameToLayer("Player");


        return true;
    }

    public void stopDragging()
    {
        if(draggedObject)
        {
            draggedObject.GetComponent<Rigidbody2D>().gravityScale = oldGravityScale;
            draggedObject.layer = LayerMask.NameToLayer("Scalable");

        }
        draggedObject = null;
        isDragging = false;
    }

    void handleMovement()
    {
        if (Input.GetAxisRaw("Horizontal") > 0.1 && !isFrontBlocked())
        {
            inputVector.Set(Input.GetAxisRaw("Horizontal") * horizontalSpeed, 0f);
            gameObject.transform.Translate(inputVector * Time.deltaTime);
        }

        if (Input.GetAxisRaw("Horizontal") < -0.1 && !isBackBlocked())
        {
            inputVector.Set(Input.GetAxisRaw("Horizontal") * horizontalSpeed, 0f);
            gameObject.transform.Translate(inputVector * Time.deltaTime);
        }

        if ( Input.GetAxisRaw("Vertical") > 0.1 || Input.GetKeyDown(KeyCode.Space) )
        {
             if (jumpState.isGrounded())
            {
                jumpState.setJumping();
            }
        }

    }

    void handleJump()
    {
        //if we are grounded, and we didn't just start a jump, set the state to grounded
        if (isGrounded() && !jumpState.isGrounded())
        {
            if(!jumpState.isJumping() || Time.time - jumpState.stateStart >= .5 ) 
            {
                jumpState.setGrounded();
            }
        }

        if(jumpState.isGrounded() && !isGrounded())
        {
            jumpState.setFalling();
        }

        //If we are jumping, check if we should keep upward velocity or transition into hanging or falling
        if (jumpState.isJumping())
        {
            //if we are no longer holding the jump button, AND we are past the minimum jump time, transition to falling state
            if(Input.GetAxisRaw("Vertical") < 0.1 && !Input.GetKey(KeyCode.Space) && Time.time - jumpState.stateStart > (minJumpHeight / jumpSpeed))
            {
                jumpState.setFalling();
            }
            //first check if we hit our head, and if so move to the falling state
            else if( isHeadColliding() )
            {
                jumpState.setFalling();
            }
            //next check if we exceeded jump time limit and if so transition to the hang time state
            else if (Time.time - jumpState.stateStart > (maxJumpHeight / jumpSpeed))
            {
                //When we transition to hanging, reduce the vertical velocity by a fraction
                /*Vector2 rbVel = rigidBody2d.velocity;
                rbVel.y =  rbVel.y*.5f;
                rigidBody2d.velocity = rbVel;*/

                jumpState.setHanging();
;           }
            //otherwise we can keep out velocity moving upwards 
            else
            {
                setYVelocity(jumpSpeed);
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
            //handle floating velocity
            else if(jumpFloatTime > 0)
            {
                //set the y velocity as a LERP val between jump and fall speeds
                float lerpPercent = (Time.time - jumpState.stateStart) / jumpFloatTime;
                float yVel = Mathf.Lerp(jumpSpeed, -fallSpeed, lerpPercent);

                setYVelocity(yVel);
            }
        }

        if( jumpState.isFalling() ) 
        {
            //if we are grounded, update the state and we are all good
            if (isGrounded())
            {
                jumpState.setGrounded();
            }
            //otherwise set vertical velocity
            else
            {
                setYVelocity(-fallSpeed);
            }
        }
    }

    void setYVelocity(float yVel)
    {
        Vector2 rbVel = rigidBody2d.velocity;
        rbVel.y = yVel;
        rigidBody2d.velocity = rbVel;
    }

    void setXVelocity(float xVel)
    {
        Vector2 rbVel = rigidBody2d.velocity;
        rbVel.x = xVel;
        rigidBody2d.velocity = rbVel;
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
        Vector2 shootDir = gunExit.transform.right;
        if ( !isFacingRight() ) shootDir *= -1;


        RaycastHit2D rayHit = Physics2D.Raycast(gunExit.transform.position, shootDir, Mathf.Infinity, scalableLayer.value );

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
            usingGun = true;
        }

        //Pressed Q
        if (Input.GetKeyUp( KeyCode.Q ))
        {
            aimingAt.GetComponent<ScalableObject>().scaleToMin();
            usingGun = true;
        }

        //Pressed E
        if (Input.GetKeyUp(KeyCode.E))
        {
            aimingAt.GetComponent<ScalableObject>().scaleToMax();
            usingGun = true;
        }

        //middle mouse click
        if (Input.GetMouseButtonDown(2))
        {
            aimingAt.GetComponent<ScalableObject>().scaleToStart();
            usingGun = true;
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

            //get object dimensions and add half of the larger to dimension to the drag range
            float largerDimensionSize = Mathf.Max( draggedObject.GetComponent<Renderer>().bounds.size.x, draggedObject.GetComponent<Renderer>().bounds.size.y)/2;

            //If the mouse is too far away, just have the object move to max range
            if ( mouseDistance > dragRange) 
            {
                targetDrag = (Vector2)gunExit.transform.position + mouseDirection * (dragRange + largerDimensionSize);
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

    public void activateCheckpoint()
    {
        checkPoint = gameObject.transform.position;
    }

    public void respawnAtCheckpoint()
    {
        gameObject.transform.position = checkPoint;
    }

    public bool isActivelyColliding(collisionTracker colTracker)
    {
        foreach (Collider2D collider in colTracker.activelyCollidingList)
        {
            if (bodyCollisionTracker.isCollidingWith(collider)) return true;
        }
        return false;
    }


    public bool isFrontBlocked()
    {
        if(isFacingRight()) return isActivelyColliding(frontCollisionTracker);
        else return isActivelyColliding(backCollisionTracker);
    }

    public bool isBackBlocked()
    {
        if (isFacingRight()) return isActivelyColliding(backCollisionTracker);
        else return isActivelyColliding(frontCollisionTracker);
    }

    public bool isGrounded()
    {
        return isActivelyColliding(feetCollisionTracker);
    }
    public bool isHeadColliding()
    {
        return isActivelyColliding(headCollisionTracker);
    }
}
