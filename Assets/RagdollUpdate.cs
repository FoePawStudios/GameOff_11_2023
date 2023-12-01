using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.U2D.IK;
using UnityEngine.XR;

public class RagdollUpdate : MonoBehaviour
{

    RagdollEditorSetup setupScript;

    //private float lerpTime = 1;

    private bool isLERPing = false;
    private bool _isRagdolled = false;
    //private float lerpStartTime = 0;

    // Start is called before the first frame update
    void Start()
    {
        setupScript = gameObject.GetComponent<RagdollEditorSetup>();
        if(!setupScript) setupScript = gameObject.AddComponent<RagdollEditorSetup>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.T) && !isLERPing)
        {
            if (_isRagdolled)
            {
                swapToAnimation();
            }
            else
            {
                swapToRagdoll();
            }
        }
    }

    public bool isRagdolled()
    {
        return _isRagdolled;
    }


    public void swapToAnimation()
    {
        if (!_isRagdolled || isLERPing) return;

        _isRagdolled = false;

        //turn on IK solver before swapping to animation
        gameObject.GetComponent<IKManager2D>().weight = 1f;
        gameObject.GetComponent<IKManager2D>().enabled = true;
        foreach (LimbSolver2D limbIK in gameObject.GetComponents<LimbSolver2D>())
        {
            limbIK.weight = 1f;
            limbIK.gameObject.SetActive(true);
        }

        foreach (GameObject bone in setupScript.boneToSkin.Keys)
        {
            Rigidbody2D boneRB = bone.GetComponent<Rigidbody2D>();
            if (boneRB)
            {
                bone.GetComponent<Rigidbody2D>().simulated = false;
                bone.GetComponent<Rigidbody2D>().isKinematic = true;
            }
        }

        foreach (GameObject skin in setupScript.skinToBone.Keys)
        {
            //disable sprite skin components
            skin.GetComponent<SpriteSkin>().enabled = true;
        }

    }

    public void swapToRagdoll()
    {
        //don't try and do anything if we are already ragdolled or in the middle of LERPing
        if (_isRagdolled || isLERPing) return;

        _isRagdolled = true;

        //turn off IK solver before swapping to ragdoll
        gameObject.GetComponent<IKManager2D>().weight = 0f;
        gameObject.GetComponent<IKManager2D>().enabled = false;
        foreach (LimbSolver2D limbIK in gameObject.GetComponents<LimbSolver2D>())
        {
            limbIK.weight = 0f;
            limbIK.gameObject.SetActive(false);
        }
        

        /*foreach (GameObject skin in setupScript.skinToBone.Keys)
        {
            //disable sprite skin components
            //skin.GetComponent<SpriteSkin>().enabled = false;
        }*/

        foreach (GameObject bone in setupScript.boneToSkin.Keys)
        {
            //enable rigidbody simulation
            bone.GetComponent<Rigidbody2D>().simulated = true;
            bone.GetComponent<Rigidbody2D>().isKinematic = false;
        }

        //disable the overall animation collider and rigidbody
        gameObject.GetComponent<Collider2D>().enabled = false;

        //disable rigidbody simulation
        gameObject.GetComponent<Rigidbody2D>().simulated = false;
        gameObject.GetComponent<Rigidbody2D>().isKinematic = true;
    }

    public void addForceToRagdoll(Vector2 force)
    {
        if (!_isRagdolled ) return;
        GameObject rootSkin = setupScript.getRootSkin();

        if (!rootSkin || !setupScript.skinToBone.ContainsKey(rootSkin)) return;
        GameObject rootSkinToBone = setupScript.skinToBone[rootSkin];
        rootSkinToBone.GetComponent<Rigidbody2D>().AddRelativeForce(force, ForceMode2D.Impulse);
    }

    public void addForceToRagdoll(Vector2 force, Vector2 position)
    {
        if (!_isRagdolled) return;
        GameObject rootSkin = setupScript.getRootSkin();

        if (!rootSkin || setupScript.skinToBone.ContainsKey(rootSkin)) return;
        GameObject rootSkinToBone = setupScript.skinToBone[rootSkin];
        rootSkinToBone.GetComponent<Rigidbody2D>().AddForceAtPosition(force, position);
    }

}
