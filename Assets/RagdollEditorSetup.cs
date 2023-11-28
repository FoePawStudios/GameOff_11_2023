using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.U2D.IK;

[ExecuteAlways]
public class RagdollEditorSetup : MonoBehaviour
{
    Dictionary<GameObject, GameObject> boneToSkin = new Dictionary<GameObject, GameObject>();
    public Dictionary<GameObject, GameObject> skinToBone = new Dictionary<GameObject, GameObject>();
    
    public Dictionary<GameObject, Vector3>  originalPosition = new Dictionary<GameObject, Vector3>();
    public Dictionary<GameObject, Vector3> originalRotation = new Dictionary<GameObject, Vector3>();

    
    private GameObject rootSkin;

    private Rigidbody2D animationRigidBody;
    private CapsuleCollider2D animationCollider;

    private IKManager2D ikManager;

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
        foreach (GameObject skin in skinToBone.Keys)
        {
            Collider2D skinCollider = skin.GetComponent<Collider2D>();
            if (skinCollider.bounds.max.y > yMax) yMax = skinCollider.bounds.max.y;
            if (skinCollider.bounds.min.y < yMin) yMin = skinCollider.bounds.min.y;

            if (skin == rootSkin)
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

        //ikManager = gameObject.GetComponent<IKManager2D>();
        //if (!ikManager) ikManager = gameObject.AddComponent<IKManager2D>();

        //add IKs if they haven't already been set up
        /*if(ikManager.solvers.Count == 0) 
        {

            LimbSolver2D rightArm = new LimbSolver2D();
            rightArm.GetChain(0)

            IKChain2D rightArm = new IKChain2D();
            rightArm.effector = wristBoneRight.transform;
            rightArm.target = shoulderBoneRight.transform;

            //right arm
            Limb rightArm = Limb();
            LimbSolver2D rightArm = new LimbSolver2D();
            ikManager.AddSolver(rightArm);
            //rightArm.
            //left arm
        }*/


    }

    //private IKChain2D getLimbChain( Transform effectorTransform, Transform targetTransform )

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

            //add rigidbody2d
            Rigidbody2D skinRB = skin.gameObject.GetComponent<Rigidbody2D>();
            if (!skinRB) skinRB = skin.gameObject.AddComponent<Rigidbody2D>();

            //set simulated to false to start out
            skinRB.simulated = false;

            //change it to start asleep 
            skin.gameObject.GetComponent<Rigidbody2D>().sleepMode = RigidbodySleepMode2D.StartAsleep;

            //add polygoncollider2d
            if (!skin.gameObject.GetComponent<PolygonCollider2D>()) skin.gameObject.AddComponent<PolygonCollider2D>();

            skin.gameObject.layer = LayerMask.NameToLayer("RagdollCollider");

        }

        setDefaultBonesRecursive(rootBone);

        //Go through skins and figure out where to place hinge joints
        foreach (GameObject bone in boneToSkin.Keys)
        {
            GameObject skin = boneToSkin[bone];

            //Find the parent that will have the rigidbody we want to slot in
            Rigidbody2D parentRB = findHingeConnectedRigidbody(bone, skin);

            //if we didn't find anything suitable, continue on to next bone
            if (!parentRB) continue;

            //if there is an existing hinge joint, grab that
            HingeJoint2D hingeJoint = skin.gameObject.GetComponent<HingeJoint2D>();

            //add a hinge2d to the sprite at the transform location of the associated bone
            if (!hingeJoint) hingeJoint = skin.gameObject.AddComponent<HingeJoint2D>();

            //add rigidbody of parent skin to the hinge 
            hingeJoint.connectedBody = parentRB;
            //hingeJoint.autoConfigureConnectedAnchor = false;

            //find bone location to use as hinge position that overlaps with both the current skin collider and the parent skin collider
            Vector3 bestBonePos;
            if (!findHingePosition(skin, parentRB.gameObject, out bestBonePos)) continue;

            //find the difference in skin position to the bone position we found to get a local transform to apply
            Vector2 anchorLocalPos = bestBonePos - skin.transform.position;

            //set the hinge location to the best position
            hingeJoint.anchor = anchorLocalPos;


            //don't overwrite angle restrictions if they already exist (in case user refined them)
            if (!hingeJoint.useLimits)//Application.IsPlaying(gameObject)
            { 
                //set the starting angles restrictions to +-30 in the direction of the bone
                hingeJoint.useLimits = true;
                JointAngleLimits2D angleLimits = hingeJoint.limits;
                float startAngle = bone.transform.eulerAngles.z;
                //translate CCW angle to Clockwise
                startAngle = 360 - startAngle;

                float lowerAngle = 0;
                float upperAngle = 0;
                getAngles(skin.name + " " + bone.name, startAngle, out lowerAngle, out upperAngle);

                angleLimits.min = lowerAngle;
                angleLimits.max = upperAngle;
                hingeJoint.limits = angleLimits;
            }
        }
    }

    private int getRootDistance(GameObject bone)
    {
        if (bone.name.ToLower() == "root") return 0;

        return getRootDistance(bone.transform.parent.gameObject) + 1;


    }
    private bool findHingePosition( GameObject skin, GameObject skinParent, out Vector3 position )
    {
        position = Vector3.zero;
        GameObject bone;
        if (!skinToBone.TryGetValue(skin, out bone)) return false;

        
        Collider2D skinCollider = skin.GetComponent<Collider2D>();
        Collider2D skinParentCollider = skinParent.GetComponent<Collider2D>();

        if (!skinCollider || !skinParentCollider) return false;

        Transform parentBoneTransform = bone.transform;

        do
        {
            bool selfOverlap = skinCollider.OverlapPoint(parentBoneTransform.position);
            bool parentOverlap = skinParentCollider.OverlapPoint(parentBoneTransform.position);

            if ( selfOverlap && parentOverlap )
            {
                position = parentBoneTransform.position;
                return true;
            }

            //get the next parent
            parentBoneTransform = parentBoneTransform.parent;
        }
        while (parentBoneTransform);


        return false;
    }

    //https://www.verywellhealth.com/what-is-normal-range-of-motion-in-a-joint-3120361
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

    private Rigidbody2D findHingeConnectedRigidbody(GameObject bone, GameObject skin)
    {
        //get the rigidbody from the from the parent bone and slot it into the connected rigid body slot
        Transform parentBoneTransform = bone.transform.parent;

        //make sure this bone has an intersecting parent we can put a hinge on
        while (parentBoneTransform)
        {
            //first get the skin the rigidbody will be attached to
            GameObject parentSkin;
            if (!boneToSkin.TryGetValue(parentBoneTransform.gameObject, out parentSkin))
            { 
                parentBoneTransform = parentBoneTransform.parent;
                continue;
            }

            //make sure it has a rigidbody and collider
            if(!parentSkin.GetComponent<Rigidbody2D>() || !parentSkin.GetComponent<Collider2D>())
            {
                parentBoneTransform = parentBoneTransform.parent;
                continue;
            }

            return parentSkin.GetComponent<Rigidbody2D>();
        }

        return null;
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
