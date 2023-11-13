using System.Collections;
using System.Collections.Generic;

using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

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
        //Check for user wanting to quit
        if (Input.GetKeyUp( KeyCode.Escape ) ) 
        {
            Application.Quit();
        }

        //Check for user wanting to restart
        if (Input.GetKeyUp(KeyCode.R))
        {
            RestartScene();
        }
        
    }

    void RestartScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    
}
