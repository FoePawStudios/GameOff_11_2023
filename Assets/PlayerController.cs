using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float xSpeed = 3;
    public float jumpVel = 20;
    public float scaleRate = 50;
    public LayerMask scalableLayer;
    private Vector2 inputVector;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rigidBody2d;
    private Vector2 facingDir;
    private BoxCollider2D groundCollider;
    private GameObject shoulderPivot;
    private GameObject gun;
    private GameObject gunExit;
    private GameObject ShootProjection;
    private GameObject aimingAt;
    private bool isMoving;
    public bool isGrounded { get; set; }
    private bool jumpLeftGround = true;
    private float shoulderGunYOffset;

    // Start is called before the first frame update
    void Start()
    {
        inputVector = new(0.0f, 0.0f);
        rigidBody2d = gameObject.GetComponent<Rigidbody2D>();
        shoulderPivot = GameObject.FindGameObjectWithTag("ShoulderPivot");
        gun = GameObject.FindGameObjectWithTag("Gun");
        gunExit = GameObject.FindGameObjectWithTag("gunExit");
        ShootProjection = GameObject.FindGameObjectWithTag("ShootProjection");
        //TO DO: maybe replace this with actual distance between two lines instead of depending on the lines starting parallel pointing forwards
        shoulderGunYOffset = Mathf.Abs( shoulderPivot.transform.position.y - gunExit.transform.position.y );
    }

    // Update is called once per frame
    void Update()
    {
        handleMovement();
        handleJump();
        pointGunAtMouse();
    }

    private void FixedUpdate()
    {
        moveProjectedShot();
        handleScalingAtDistance();
    }

    void handleMovement()
    {
        //Handle Movement Input
        if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1) // || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0)
        {
            inputVector.Set(Input.GetAxisRaw("Horizontal") * xSpeed, 0f);
            gameObject.transform.Translate(inputVector * Time.deltaTime);
        }
    }

    void handleJump()
    {
        if (!jumpLeftGround && !isGrounded)
        {
            jumpLeftGround = true;
        }

        if (jumpLeftGround && isGrounded && (Input.GetAxisRaw("Vertical") > 0.1 || Input.GetKeyDown(KeyCode.Space))) // || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0)
        {
            jumpLeftGround = false;

            rigidBody2d.velocity = Vector2.up * jumpVel;
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

        if( aimingAt != null )
        {
            if (Input.mouseScrollDelta.y != 0)
            {
                float minScale = aimingAt.GetComponent<ScalableObject>().minScale;
                float maxScale = aimingAt.GetComponent<ScalableObject>().maxScale;

                float scaleChange = (Input.mouseScrollDelta.y * Time.fixedDeltaTime * scaleRate);
                Vector3 tempScale = aimingAt.transform.localScale;
                tempScale.x = tempScale.x + scaleChange;
                tempScale.y = tempScale.y + scaleChange;

                //If shrinking would cause object to be smaller than minimum, just set to minimum
                if (tempScale.x < minScale)
                {
                    float overshoot = minScale - tempScale.x;
                    tempScale.x = tempScale.x + overshoot;
                    tempScale.y = tempScale.y + overshoot;
                }
                else if (tempScale.x > maxScale)
                {
                    float overshoot = tempScale.x - maxScale;
                    tempScale.x = tempScale.x - overshoot;
                    tempScale.y = tempScale.y - overshoot;
                }

                if (tempScale.y < minScale)
                {
                    float overshoot = minScale - tempScale.y;
                    tempScale.x = tempScale.x + overshoot;
                    tempScale.y = tempScale.y + overshoot;
                }
                else if (tempScale.y > maxScale)
                {
                    float overshoot = tempScale.y - maxScale;
                    tempScale.x = tempScale.x - overshoot;
                    tempScale.y = tempScale.y - overshoot;
                }

                aimingAt.transform.localScale = tempScale;
            }
        }
    }


}
