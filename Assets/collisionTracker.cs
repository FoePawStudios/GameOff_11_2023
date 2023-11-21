using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class collisionTracker : MonoBehaviour
{
    public List<Collider2D> activelyCollidingList = new List<Collider2D>(); 
    private bool includeTrigger=false;
    private bool includeCollider=true;
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
            activelyCollidingList.Add(collider);
        }
        
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (includeCollider)
        {
            activelyCollidingList.Add(collision.collider);
        }
        
    }


    void OnTriggerExit2D(Collider2D collider)
    {
        if (includeTrigger)
        {
            activelyCollidingList.Remove(collider);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (includeCollider)
        {
            activelyCollidingList.Remove(collision.collider);
        }
    }
}
