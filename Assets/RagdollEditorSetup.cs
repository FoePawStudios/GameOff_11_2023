using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.U2D.IK;
using UnityEngine.XR;

[ExecuteAlways]
public class RagdollEditorSetup : MonoBehaviour
{
    public Dictionary<GameObject, GameObject> boneToSkin = new Dictionary<GameObject, GameObject>();
    public Dictionary<GameObject, GameObject> skinToBone = new Dictionary<GameObject, GameObject>();
    
    public Dictionary<GameObject, Vector3>  originalPosition = new Dictionary<GameObject, Vector3>();
    public Dictionary<GameObject, Vector3> originalRotation = new Dictionary<GameObject, Vector3>();

    
    private GameObject rootSkin;

    private Rigidbody2D animationRigidBody;
    private CapsuleCollider2D animationCollider;

    public GameObject rootBone;
    public GameObject neckBone;
    public GameObject shoulderBoneLeft;
    public GameObject shoulderBoneRight;
    public GameObject elbowBoneLeft;
    public GameObject elbowBoneRight;
    public GameObject wristBoneLeft;
    public GameObject wristBoneRight;
    public GameObject hipBoneLeft;
    public GameObject hipBoneRight;
    public GameObject kneeBoneLeft;
    public GameObject kneeBoneRight;
    public GameObject ankleBoneLeft;
    public GameObject ankleBoneRight;


// Start is called before the first frame update
void Start()
    {
        //if (!Application.IsPlaying(gameObject))
        
        // Editor logic
        setupRagdollObjects();
        if (!gameObject.GetComponent<RagdollUpdate>()) gameObject.AddComponent<RagdollUpdate>();
        setupAnimationControls();
    }

    private void setupAnimationControls()
    {
        //add overall capsule collider and rigidbody for animation physics and collisions
        animationRigidBody = gameObject.GetComponent<Rigidbody2D>();
        if (!animationRigidBody) animationRigidBody = gameObject.AddComponent<Rigidbody2D>();

        animationRigidBody.freezeRotation = true;

        animationCollider = gameObject.GetComponent<CapsuleCollider2D>();
        if (!animationCollider) animationCollider = gameObject.AddComponent<CapsuleCollider2D>();

        float yMin = Mathf.Infinity;
        float yMax = -Mathf.Infinity;
        float width = 0;

        //find the y bounding box max and min for all the polygon colliders
        foreach (GameObject bone in boneToSkin.Keys)
        {
            Collider2D skinCollider = bone.GetComponent<Collider2D>();

            if (!skinCollider) continue;

            if (skinCollider.bounds.max.y > yMax) yMax = skinCollider.bounds.max.y;
            if (skinCollider.bounds.min.y < yMin) yMin = skinCollider.bounds.min.y;

            if (skinCollider.bounds.max.x - skinCollider.bounds.min.x > width && !skinCollider.gameObject.name.Contains("Gun"))
            {
                width = skinCollider.bounds.max.x - skinCollider.bounds.min.x;
            }

        }

        float height = yMax - yMin;

        //use root/body sprite collider to place the center of the capsule collider and the width
        Vector2 capOffset = new Vector2(0f, height / 2);
        Vector2 capSize = new Vector2(width, height);

        animationCollider.offset = capOffset;
        animationCollider.size = capSize;
    }

    private void setupRagdollObjects()
    {
        SpriteSkin[] skins = new SpriteSkin[20];
        skins = gameObject.transform.GetComponentsInChildren<SpriteSkin>();

        //set the root bone off the name at the start
        rootBone = gameObject.transform.Find("Root").gameObject;

        float minRootDistance = Mathf.Infinity;
        //first get all the sprites involved
        foreach (SpriteSkin skin in skins)
        {
            //store off the main bone associated with this skin
            if (skin.boneTransforms.Length > 0)
            {
                boneToSkin.Add(skin.boneTransforms[0].gameObject, skin.gameObject);
                skinToBone.Add(skin.gameObject, skin.boneTransforms[0].gameObject);
                originalRotation.Add(skin.gameObject, skin.gameObject.transform.localEulerAngles);
                originalPosition.Add(skin.gameObject, skin.gameObject.transform.localPosition);

                int curRootDist = getRootDistance(skin.boneTransforms[0].gameObject);
                if (curRootDist < minRootDistance)
                {
                    minRootDistance = curRootDist;
                    rootSkin = skin.gameObject;
                }
            }
        }

        setDefaultBonesRecursive(rootBone);
        setupBoneHinges(rootBone);
    }

    void setupBoneHinges(GameObject rootBone)
    {
        setupBoneHingeRecursive(rootBone, null);
    }

    void setupBoneHingeRecursive(GameObject bone, Rigidbody2D colliderParentRB)
    {
        //if we have a rigidBody, grab that now
        Rigidbody2D selfRB = bone.GetComponent<Rigidbody2D>();


        //if there is a corresponding skin for this bone with a collider, copy and paste the polygon collider from it to this one
        if ( boneToSkin.ContainsKey( bone ) )
        {
            GameObject skin = boneToSkin[ bone ];

            //add collider to skin so it gets the points from the skin physics shape
            PolygonCollider2D skinCol = skin.GetComponent<PolygonCollider2D>();
            if (!skinCol) skinCol = skin.gameObject.AddComponent<PolygonCollider2D>();

            if(skinCol.points.Length > 0)
            {
                PolygonCollider2D selfCollider = bone.GetComponent<PolygonCollider2D>();

                //get rid of an existing collider and copy it anew
                if (selfCollider) DestroyImmediate(selfCollider);

                selfCollider = bone.GetComponent<PolygonCollider2D>();
                if(!selfCollider) selfCollider = bone.AddComponent<PolygonCollider2D>();
                
                selfCollider.points = skinCol.points;

                DestroyImmediate(skinCol);

                //go through points and relocate their space to bone local space
                Vector2[] relocatedPoints = new Vector2[selfCollider.points.Length];
                for (int i = 0; i < selfCollider.points.Length; i++)
                {
                    Vector3 worldPoint = skin.transform.TransformPoint(selfCollider.points[i]);
                    relocatedPoints[i] = bone.transform.InverseTransformPoint(worldPoint);
                }
                selfCollider.points = relocatedPoints;
            }

            //if we don't have a RB to go with the collider, add it now and set it up
            if (!selfRB) selfRB = bone.AddComponent<Rigidbody2D>();

            //set simulated to false to start out
            selfRB.simulated = false;

            //change it to start asleep 
            selfRB.sleepMode = RigidbodySleepMode2D.StartAsleep;
            bone.layer = LayerMask.NameToLayer("RagdollCollider");
        }

        //if we have a rigidbody/collider and we have a passed down rigidbody/collider, create a joint between them 
        if(colliderParentRB && selfRB)
        {
            //get and/or add hinge
            HingeJoint2D boneHinge = bone.GetComponent<HingeJoint2D>();
            if (!boneHinge) boneHinge = bone.AddComponent<HingeJoint2D>();

            //add rigidbody of parent skin to the hinge 
            boneHinge.connectedBody = colliderParentRB;

            //don't overwrite angle restrictions if they already exist (in case user refined them)
            if (!boneHinge.useLimits)//Application.IsPlaying(gameObject)
            {
                //set the starting angles restrictions to +-30 in the direction of the bone
                boneHinge.useLimits = true;
                JointAngleLimits2D angleLimits = boneHinge.limits;

                float lowerAngle = 0;
                float upperAngle = 0;
                getAngles(bone.name + " " + bone.name, out lowerAngle, out upperAngle);

                angleLimits.min = lowerAngle;
                angleLimits.max = upperAngle;
                boneHinge.limits = angleLimits;
            }
        }

        //if we don't have a rigidbody/collider, pass down the parent ones for the children to use
        if(!selfRB)
        {
            selfRB = colliderParentRB;
        }

        foreach (Transform child in bone.transform) setupBoneHingeRecursive(child.gameObject, selfRB);
    }

    private int getRootDistance(GameObject bone)
    {
        if (bone.name.ToLower() == "root") return 0;

        return getRootDistance(bone.transform.parent.gameObject) + 1;


    }

    //https://www.verywellhealth.com/what-is-normal-range-of-motion-in-a-joint-3120361
    private void getAngles(string jointName, out float lowerAngle, out float upperAngle)
    {
        getAngles(jointName, 0, out lowerAngle, out upperAngle);
    }

    private void getAngles(string jointName, float startAngle, out float lowerAngle, out float upperAngle)
    {
        startAngle = startAngle % 360;

        lowerAngle = startAngle - 15;
        upperAngle = startAngle + 15;

        jointName = jointName.ToLower();

        if (jointName.Contains("head") || jointName.Contains("neck"))
        {
            upperAngle = startAngle + 45;
            lowerAngle = startAngle - 45;
        }

        if ( jointName.Contains("should") ) 
        {
            upperAngle = startAngle + 150;
            lowerAngle = startAngle - 50;
        }

        if (jointName.Contains("elbow") || jointName.Contains("forearm"))
        {
            if (jointName.Contains("r_"))
            {
                upperAngle = startAngle - 150;
                lowerAngle = startAngle + 6;
            }
            else
            {
                upperAngle = startAngle + 150;
                lowerAngle = startAngle - 6;
            }
        }

        if (jointName.Contains("hand") || jointName.Contains("wrist"))
        {
            upperAngle = startAngle + 60;
            lowerAngle = startAngle - 60;
        }

        if (jointName.Contains("hip") || jointName.Contains("thigh"))
        {
            upperAngle = startAngle - 100;
            lowerAngle = startAngle + 30;
        }

        if (jointName.Contains("knee") || jointName.Contains("calf"))
        {
            upperAngle = startAngle + 150;
            lowerAngle = startAngle - 2;
        }

        if (jointName.Contains("ankle") || jointName.Contains("foot"))
        {
            upperAngle = startAngle + 40;
            lowerAngle = startAngle - 10;
        }
    }

    private void setDefaultBonesRecursive(GameObject bone)
    {
        foreach (Transform child in bone.transform) setDefaultBonesRecursive(child.gameObject);

        string boneName = bone.name.ToLower();

        if (boneName.Contains("root")) if (!rootBone) rootBone = bone;
        if (boneName.Contains("head") || boneName.Contains("neck")) if (!neckBone) neckBone = bone;
        if (boneName.Contains("shoulder"))
        {
            if (boneName.Contains("r_") && !shoulderBoneRight) shoulderBoneRight = bone;
            else if (!shoulderBoneLeft) shoulderBoneLeft = bone;
        }

        if (boneName.Contains("elbow") || boneName.Contains("forearm"))
        {
            if (boneName.Contains("r_") && !elbowBoneRight) elbowBoneRight = bone;
            else if (!elbowBoneLeft) elbowBoneLeft = bone;
        }

        if (boneName.Contains("hand") || boneName.Contains("wrist"))
        {
            if (boneName.Contains("r_") && !wristBoneRight) wristBoneRight = bone;
            else if (!wristBoneLeft) wristBoneLeft = bone;
        }
        if (boneName.Contains("hip") || boneName.Contains("thigh"))
        {
            if (boneName.Contains("r_") && !hipBoneRight) hipBoneRight = bone;
            else if (!hipBoneLeft) hipBoneLeft = bone;
        }

        if (boneName.Contains("knee") || boneName.Contains("calf"))
        {
            if (boneName.Contains("r_") && !kneeBoneRight) kneeBoneRight = bone;
            else if (!kneeBoneLeft) kneeBoneLeft = bone;
        }

        if (boneName.Contains("ankle") || boneName.Contains("foot"))
        {
            if (boneName.Contains("r_") && !ankleBoneRight) ankleBoneRight = bone;
            else if (!ankleBoneLeft) ankleBoneLeft = bone;
        }
    }

    public GameObject getRootBone()
    {
        return rootBone;
    }
    public GameObject getRootSkin()
    {
        return rootSkin;
    }

}
