using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class BoundSkin
{
    public GameObject skin;
    public GameObject bone;

    public Vector3 initialRelativePosition;
    public Quaternion initialRelativeRotation;
    public Vector3 relativeSkinStartRot;
    public Vector3 lerpStartPosition;
    public Quaternion lerpStartRotation;
    public BoundSkin(GameObject skinObj, GameObject boneObj)
    {
        skin = skinObj;
        bone = boneObj;
        initialRelativePosition = skin.transform.position - bone.transform.position;
        initialRelativeRotation = Quaternion.Inverse(bone.transform.rotation) * skin.transform.rotation;
    }

    public void moveSkinRelativeToBone(float lerpFraction) 
    {
        skin.transform.position = Vector3.Lerp(lerpStartPosition, bone.transform.position + initialRelativePosition, lerpFraction);
        skin.transform.rotation = Quaternion.Lerp(lerpStartRotation, bone.transform.rotation * initialRelativeRotation, lerpFraction);
    }

    public void recordStartVals()
    {
        lerpStartPosition = skin.transform.position;
        lerpStartRotation = skin.transform.rotation;
    }

}

public class RagdollUpdate : MonoBehaviour
{

    RagdollEditorSetup setupScript;

    List<BoundSkin> boundSkins = new List<BoundSkin>();

    private float lerpTime = 1;

    private bool isLERPing = false;
    private bool isRagdolled = false;
    private float lerpStartTime = 0;

    private GameObject rootBone;
    private GameObject rootSkin;

    // Start is called before the first frame update
    void Start()
    {
        setupScript = gameObject.GetComponent<RagdollEditorSetup>();

        if(!setupScript) setupScript = gameObject.AddComponent<RagdollEditorSetup>();

        if (setupScript.skinToBone == null) return;

        foreach (GameObject skin in setupScript.skinToBone.Keys)
        {
            boundSkins.Add(new BoundSkin(skin, setupScript.skinToBone[skin]));
        }

        rootBone = setupScript.getRootBone();
        rootSkin = setupScript.getRootSkin();

    }

    // Update is called once per frame
    void Update()
    {
        //if we didn't retrieve the bones successfully, try again
        if (boundSkins.Count == 0) Start();

        if (Input.GetKeyUp(KeyCode.T) && !isLERPing)
        {
            if (isRagdolled)
            {
                swapToAnimation();
            }
            else
            {
                swapToRagdoll();
            }
        }

        if (isLERPing) lerpSkins();
    }

    void lerpSkins()
    {
        if (!isLERPing) return;

        float lerpFraction = (Time.time - lerpStartTime) / lerpTime;

        foreach (BoundSkin boundSkin in boundSkins)
        {
            boundSkin.moveSkinRelativeToBone(lerpFraction);
            if (lerpFraction >= 1) boundSkin.skin.GetComponent<SpriteSkin>().enabled = true;
        }

        if (lerpFraction >= 1)
        {
            isLERPing = false;
            lerpStartTime = 0;
            isRagdolled = false;

            //enable the overall animation collider and rigidbody
            gameObject.GetComponent<Collider2D>().enabled = true;

            //enable rigidbody simulation
            gameObject.GetComponent<Rigidbody2D>().simulated = true;
            gameObject.GetComponent<Rigidbody2D>().isKinematic = false;

        }
    }

    public void swapToAnimation()
    {
        if (!isRagdolled || isLERPing) return;

        isRagdolled = false;

        //move root bone below body
        Vector3 newRootPos = findFloorBeneathSkins();
        //rootLERPMovement = newRootPos - rootBone.transform.position;
        rootBone.transform.position = newRootPos;


        foreach (BoundSkin boundSkin in boundSkins)
        {
            boundSkin.recordStartVals();

            //disable rigidbody simulation
            boundSkin.skin.GetComponent<Rigidbody2D>().simulated = false;
            boundSkin.skin.GetComponent<Rigidbody2D>().isKinematic = true;
        }

        //tell the update function to work on LERPing the body parts to the animation location
        isLERPing = true;
        lerpStartTime = Time.time;
    }

    Vector3 findFloorBeneathSkins()
    {
        //shoot a ray downwards from root skin collider
        RaycastHit2D[] rayHits = new RaycastHit2D[50];
        int rayHitNum = rootSkin.GetComponent<Collider2D>().Raycast(Vector2.down, rayHits);

        Vector3 newRootPos = Vector3.zero;

        //go through hits and fine one that isn't part of the ragdoll skins
        for (int i = 0; i < rayHitNum; i++)
        {
            if (!setupScript.skinToBone.ContainsKey(rayHits[i].collider.gameObject))
            {
                newRootPos = rayHits[i].point;
                return newRootPos;
            }
        }
        return newRootPos;
    }

    public void swapToRagdoll()
    {
        //don't try and do anything if we are already ragdolled or in the middle of LERPing
        if (isRagdolled || isLERPing) return;

        isRagdolled = true;

        foreach (GameObject skin in setupScript.skinToBone.Keys)
        {
            //disable sprite skin components
            skin.GetComponent<SpriteSkin>().enabled = false;

            //enable rigidbody simulation
            skin.GetComponent<Rigidbody2D>().simulated = true;
            skin.GetComponent<Rigidbody2D>().isKinematic = false;
        }

        //disable the overall animation collider and rigidbody
        gameObject.GetComponent<Collider2D>().enabled = false;

        //disable rigidbody simulation
        gameObject.GetComponent<Rigidbody2D>().simulated = false;
        gameObject.GetComponent<Rigidbody2D>().isKinematic = true;
    }

    public void addForceToRagdoll(Vector2 force)
    {
        if (!isRagdolled) return;

        //rootSkin.GetComponent<Rigidbody2D>().velocity = force;
        rootSkin.GetComponent<Rigidbody2D>().AddRelativeForce(force, ForceMode2D.Impulse);
        //addForceToRagdoll(force, rootSkin.GetComponent<Rigidbody2D>().transform.localPosition);
    }

    public void addForceToRagdoll(Vector2 force, Vector2 position)
    {
        if(!isRagdolled) return;

        rootSkin.GetComponent<Rigidbody2D>().AddForceAtPosition(force, position);

    }

}
