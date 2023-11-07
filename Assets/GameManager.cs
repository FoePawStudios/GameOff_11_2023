using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    public ScalableObject scalableScript;
    public LayerMask scalableLayer;

    // Start is called before the first frame update
    void Start()
    {
        Assert.IsNotNull(scalableScript);

        //Go through scalable objects and attach scalable script and tag and layer
        foreach (GameObject scalableObject in GameObject.FindGameObjectsWithTag("ScalableObject"))
        {
            //if a scalable object doesn't have the scalable script attached, attach it
            if (scalableObject.GetComponent( scalableScript.GetType() ) == null )
            {
                scalableObject.AddComponent(scalableScript.GetType());
            }
            
            //add the object to the scalable layer
            scalableObject.layer = 6;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /*void FixedUpdate()
    {
        //Check for mouse-over on scalable objects using raycast
        Vector2 mousePosV3 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        foreach( GameObject scalableObject in GameObject.FindGameObjectsWithTag("Scalable") )
        {
            if( scalableObject.GetComponent<Collider2D>().OverlapPoint( new Vector2(mousePosV3.x, mousePosV3.y) ) )
            {
                if (Input.mouseScrollDelta.y != 0)
                {
                    float scaleChange = (Input.mouseScrollDelta.y * Time.fixedDeltaTime * scaleRate);
                    Vector3 startScale = scalableObject.transform.localScale;
                    startScale.x = startScale.x + scaleChange;
                    startScale.y = startScale.y + scaleChange;
                    
                    //If shrinking would cause object to be smaller than minimum, just set to minimum
                    if(startScale.x < defaultMinScale)
                    {
                        float overshoot = defaultMinScale - startScale.x;
                        startScale.x = startScale.x + overshoot;
                        startScale.y = startScale.y + overshoot;
                    }
                    else if(startScale.x > defaultMaxScale)
                    {
                        float overshoot = startScale.x - defaultMaxScale;
                        startScale.x = startScale.x - overshoot;
                        startScale.y = startScale.y - overshoot;
                    }

                    if (startScale.y < defaultMinScale)
                    {
                        float overshoot = defaultMinScale - startScale.y;
                        startScale.x = startScale.x + overshoot;
                        startScale.y = startScale.y + overshoot;
                    }
                    else if (startScale.y > defaultMaxScale)
                    {
                        float overshoot = startScale.y - defaultMaxScale;
                        startScale.x = startScale.x - overshoot;
                        startScale.y = startScale.y - overshoot;
                    }

                    scalableObject.transform.localScale = startScale;
                }
            }
        }
    }*/
}
