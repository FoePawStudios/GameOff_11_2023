using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class collisionTracker : MonoBehaviour
{
    public List<Collider2D> activelyCollidingList = new List<Collider2D>(); 
    public bool includeTrigger=false;
    public bool includeCollider=true;
    public List<GameObject> includeObjectList = new List<GameObject>();
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }

    public bool isCollidingWith(Collider2D collider)
    {
        return activelyCollidingList.Contains(collider);
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (includeTrigger)
        {
            if(includeObjectList.Count > 0)
            {
                if( includeObjectList.Contains( collider.gameObject ) )
                {
                    activelyCollidingList.Add(collider);
                }
            }
            else
            {
                activelyCollidingList.Add(collider);
            }
        }
        
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (includeCollider)
        {
            if (includeObjectList.Count > 0)
            {
                if (includeObjectList.Contains(collision.collider.gameObject))
                {
                    activelyCollidingList.Add(collision.collider);
                }
            }
            else
            {
                activelyCollidingList.Add(collision.collider);
            }
        }
    }


    void OnTriggerExit2D(Collider2D collider)
    {
        if (includeTrigger)
        {
            if (includeObjectList.Count > 0)
            {
                if (includeObjectList.Contains(collider.gameObject))
                {
                    activelyCollidingList.Remove(collider);
                }
            }
            else
            {
                activelyCollidingList.Remove(collider);
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (includeCollider)
        {
            if (includeObjectList.Count > 0)
            {
                if (includeObjectList.Contains(collision.collider.gameObject))
                {
                    activelyCollidingList.Remove(collision.collider);
                }
            }
            else
            {
                activelyCollidingList.Remove(collision.collider);
            }
        }
    }
}
