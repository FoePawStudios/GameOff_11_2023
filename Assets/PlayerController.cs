using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float horizontalSpeed = 5;
    public float jumpSpeed = 8;
    public float jumpCooldown = 1;
    public float scaleRate = 30;
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

    // Start is called before the first frame update
    void Start()
    {
        inputVector = new(0.0f, 0.0f);
        rigidBody2d = gameObject.GetComponent<Rigidbody2D>();
        shoulderPivot = GameObject.FindGameObjectWithTag("ShoulderPivot");
        gunExit = GameObject.FindGameObjectWithTag("gunExit");
        ShootProjection = GameObject.FindGameObjectWithTag("ShootProjection");
        //TO DO: maybe replace this with actual distance between two lines instead of depending on the lines starting parallel pointing forwards
        shoulderGunYOffset = Mathf.Abs( shoulderPivot.transform.position.y - gunExit.transform.position.y );
        lastJump = 0;
        isGrounded = false;
    }

    // Update is called once per frame
    void Update()
    {
        handleMovement();
        pointGunAtMouse();
    }

    private void FixedUpdate()
    {
        moveProjectedShot();
        handleScalingAtDistance();
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

    void handleScalingAtDistance()
    {
        if( aimingAt != null && Input.mouseScrollDelta.y != 0)
        {
            aimingAt.GetComponent<ScalableObject>().changeScale(Input.mouseScrollDelta.y * Time.fixedDeltaTime * scaleRate);
        }
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
