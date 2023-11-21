using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScalableObject : MonoBehaviour
{
    private float lerpSpeed;// = 2.0f;
    public float maxScale = 10f;
    public bool fixedUntilInteractedWith = false;
    private bool isStillFixed = true;

    private Vector2 originalScale;
    private Vector2 minScale;
    private Vector2 maxDefaultScale;
    private Vector3 targetScale;

    private Texture spriteTexture;
    private GameObject playerObject;
    private Rigidbody2D rigidBody;
    
    private float throwablePercentOfPlayer = .35f; //capsule height 512, circle 256
    private bool _isFixedObject;

    // Start is called before the first frame update
    void Start()
    {
        originalScale = gameObject.transform.localScale;
        targetScale = originalScale;
        maxDefaultScale = originalScale * 10;

        if ( !gameObject.GetComponent<SpriteRenderer>() )
        {
            return;
        }

        spriteTexture = gameObject.GetComponent<SpriteRenderer>().sprite.texture;
        playerObject = GameObject.FindGameObjectWithTag("Player");
        rigidBody = gameObject.GetComponent<Rigidbody2D>();

        calcThrowableScale();
        calcFixedPhysics();
        lerpSpeed = 20f;

        if(fixedUntilInteractedWith)
        {
            setRBConstraints(true, true, true);
        }


    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        if (gameObject.transform.localScale.x != targetScale.x || gameObject.transform.localScale.y != targetScale.y)
        {
            gameObject.transform.localScale = Vector3.Lerp(gameObject.transform.localScale, targetScale, lerpSpeed * Time.fixedDeltaTime);
        }
    }

    public void scaleToMax()
    {
        unfreezeObject();
        targetScale = .9f * getMaxScale();
    }
    public void scaleToMin()
    {
        unfreezeObject();
        targetScale = minScale;
    }
    public void scaleToStart()
    {
        unfreezeObject();
        targetScale = originalScale;
    }

    public void changeScale(float changeAmount)
    {
        unfreezeObject();

        targetScale = gameObject.transform.localScale * (1 + changeAmount);

        //make sure we don't go smaller than minScale
        if (targetScale.y - minScale.y < 0 ) 
        {
            targetScale = minScale;
        }

        //make sure we don't go larger than maxScale
        Vector3 maxScaleVec = getMaxScale();

        if(targetScale.y - maxScaleVec.y > 0)
        {
            targetScale = maxScaleVec;
        }
    }

    public void calcThrowableScale() 
    {
        //Our basis for throwable scale will be a percentage of the height of the main character
        float playerHeightRealPx = playerObject.GetComponent<SpriteRenderer>().sprite.texture.height * playerObject.transform.localScale.y;
        float targetHeightRealPx = throwablePercentOfPlayer * playerHeightRealPx;

        float startWidth = spriteTexture.width * originalScale.x;
        float startHeight = spriteTexture.height * originalScale.y;

        //get cross section for current longest amount
        float startRadiusRealPx = Mathf.Sqrt(Mathf.Pow(startWidth, 2) + Mathf.Pow(startHeight, 2));  //Mathf.Max(spriteRenderer.sprite.texture.height * startScale.y, spriteRenderer.sprite.texture.width * startScale.x);

        minScale = originalScale * (targetHeightRealPx / startRadiusRealPx);
    }

    private void calcFixedPhysics()
    {
        if(!rigidBody)
        {
            _isFixedObject = true;
            return;
        }

        if(fixedUntilInteractedWith)
        {
            _isFixedObject = false;
            return;
        }

        bool isXPositionFrozen = (rigidBody.constraints & RigidbodyConstraints2D.FreezePositionX) != 0;
        bool isYPositionFrozen = (rigidBody.constraints & RigidbodyConstraints2D.FreezePositionY) != 0;

        if( isXPositionFrozen || isYPositionFrozen)
        {
            _isFixedObject = true;
            return;
        }

        _isFixedObject = false;
    }

    public Vector3 getMaxScale()
    {
        //check 8 directions for raycast distance 
        Vector2[] directions = new Vector2[] { Vector2.up, Vector2.right, (Vector2.up + Vector2.right), (Vector2.up + Vector2.left) };

        float maxScale = Mathf.Infinity;

        foreach (Vector2 direction in directions)
        {
            float tempScale = getScaleMaxOnAxis(direction);
            if(tempScale < maxScale)
            {
                maxScale = tempScale;
            }
        }

        if(maxScale == Mathf.Infinity)
        {
            return maxDefaultScale;
        }

        return maxScale * gameObject.transform.localScale;
    }

    public float getScaleMaxOnAxis(Vector2 direction)
    {
        float directionMaxDist = 0f;
        float directionCurrentDist = 0f;
        float directionRatio = getScaleMaxInDirection(direction, out directionMaxDist, out directionCurrentDist);

        //If this isn't a fixed object and the first ray didn't hit anything we can return early
        if(!_isFixedObject && directionRatio == Mathf.Infinity)
        {
            return directionMaxDist;
        }

        float oppositeMaxDist = 0f;
        float oppositeCurrentDist = 0f;
        float oppositeRatio = getScaleMaxInDirection(-direction, out oppositeMaxDist, out oppositeCurrentDist);

        //If this is a fixed object return the smaller of the two directions it can scale on the axis
        if(_isFixedObject)
        {
            return Mathf.Min(directionRatio, oppositeRatio);
        }
        
        //if this isn't a fixed object and the second measurement we took was infinity, just return infinity
        if(oppositeRatio == Mathf.Infinity)
        {
            return oppositeRatio;
        }


        return (directionMaxDist + oppositeMaxDist) / (directionCurrentDist + oppositeCurrentDist);
    }

    public float getScaleMaxInDirection(Vector2 direction, out float maxDistance, out float currentDistance )
    {
        maxDistance = Mathf.Infinity;
        currentDistance = Mathf.Infinity;
        Collider2D collider = gameObject.GetComponent<Collider2D>();

        if (!collider)
        {
            return Mathf.Infinity;
        }

        //Shoot ray from collider to another object
        RaycastHit2D directionFarHit = shootColliderRay(direction);

        //if we don't hit something, we can scale as much as we want
        if (!directionFarHit.collider)
        {
            return Mathf.Infinity;
        }

        //Calculate the max distance our object can scale to
        maxDistance = Vector2.Distance(directionFarHit.point, collider.transform.position);

        //shoot a ray from the point we hit back at ourselves for a more exact distance
        //take a 10% step towards original collider to avoid self-collision, and make end-estimate not actually hit walls 
        float rayDistance = Vector2.Distance(collider.transform.position, directionFarHit.point);
        Vector2 smallStep = new Vector2(directionFarHit.point.x, directionFarHit.point.y);
        smallStep = smallStep + (-direction * .1f * rayDistance);

        RaycastHit2D directionColliderHit = Physics2D.Raycast(smallStep, -direction);//shootColliderRay(-direction, directionFarHit.collider);//Physics2D.Raycast(directionFarHit.point, -direction);

        if (!directionColliderHit.collider)
        {
            return Mathf.Infinity;
        }

        currentDistance = Vector2.Distance(directionColliderHit.point, collider.transform.position);

        return maxDistance / currentDistance;
    }

        public RaycastHit2D shootColliderRay(Vector2 direction)
    {
        Collider2D collider = gameObject.GetComponent<Collider2D>();
        //set up hit array for output
        RaycastHit2D[] hitResults = new RaycastHit2D[1];
        int numHits = collider.Raycast(direction, hitResults);

        if (numHits == 0 || hitResults[0].point == null)
        {
            return new RaycastHit2D();
        }

        return hitResults[0];
    }

    public RaycastHit2D shootColliderRay(Vector2 direction, Collider2D collider)
    {
        //set up hit array for output
        RaycastHit2D[] hitResults = new RaycastHit2D[1];
        int numHits = collider.Raycast(direction, hitResults);

        if (numHits == 0 || hitResults[0].point == null)
        {
            return new RaycastHit2D();
        }

        return hitResults[0];
    }


    public RaycastHit2D shootRay(Vector2 startPoint, Vector2 direction, float maxDistance)
    {
        RaycastHit2D rayHit = Physics2D.Raycast(startPoint, direction, maxDistance);

        if (rayHit.transform != null)
        {
            Debug.DrawLine(startPoint, rayHit.point, Color.red);// new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));
        }
        else
        {
            Debug.DrawRay(startPoint, direction, Color.blue);//new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));
        }
        
        //rayHit.collider.Raycast

        //if we hit something and it was ourselves, shoot another ray
        if ( rayHit.transform != null && rayHit.transform.gameObject == gameObject )
        {
            float distanceTraveled = Vector2.Distance( rayHit.point, startPoint);
            rayHit = shootRay(rayHit.point, direction, maxDistance - distanceTraveled);
        }

        return rayHit;
    }

    public bool isFixedObject()
    {
        return _isFixedObject;
    }

    public void unfreezeObject()
    {
        if(fixedUntilInteractedWith && isStillFixed)
        {
            isStillFixed = false;
            setRBConstraints(false, false, false);
        }
    }

    private void setRBConstraints(bool freezeX, bool freezeY, bool freezeRotation )
    {
        RigidbodyConstraints2D rbc2d = new RigidbodyConstraints2D();

        if (freezeX) rbc2d = rbc2d | RigidbodyConstraints2D.FreezePositionX;
        if (freezeY) rbc2d = rbc2d | RigidbodyConstraints2D.FreezePositionY;
        if (freezeRotation) rbc2d = rbc2d | RigidbodyConstraints2D.FreezeRotation;

        rigidBody.constraints = rbc2d;
    }

}
