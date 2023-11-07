using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScalableObject : MonoBehaviour
{
    private Vector3 startScale;
    private float currentScale = 1f;
    public float minScale = 1f;
    public float maxScale = 10f;

    // Start is called before the first frame update
    void Start()
    {
        startScale = gameObject.transform.localScale;

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
    }

    public void changeScale(float changeAmount)
    {
        currentScale = currentScale + changeAmount;

        if (currentScale < minScale) 
        {
            currentScale = minScale;
        }
        if (currentScale > maxScale)
        {
            currentScale = maxScale;
        }

        gameObject.transform.localScale = startScale * currentScale;


    }

}
