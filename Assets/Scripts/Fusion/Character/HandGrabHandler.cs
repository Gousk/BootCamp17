using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandGrabHandler : MonoBehaviour
{
   [SerializeField] Animator animator;

    //A fixed joint that is created on the fly
    FixedJoint fixedJoint;

    //Our own rigidbody
    Rigidbody rigidbody3D;

    //References
    NetworkPlayer networkPlayer;

    void Awake()
    {
        //Get references
        networkPlayer = transform.root.GetComponent<NetworkPlayer>();
        rigidbody3D = GetComponent<Rigidbody>();

        //Change solver iterations to prevent joint from flexing too much
        rigidbody3D.solverIterations = 255;
    }

    public void UpdateState()
    {
        //Check if grabbing is active
        if (networkPlayer.IsGrabingActive)
        {
            animator.SetBool("isGrabing", true);
        }
        else
        {
            //We are no longer carrying, check if there is a joint to destroy
            if (fixedJoint != null)
            {
                //Give the connected rigidbody a bit of force when we let go
                if (fixedJoint.connectedBody != null)
                {
                    float forceAmountMultiplier = 0.1f;

                    //Get the other player
                    if (fixedJoint.connectedBody.transform.root.TryGetComponent(out NetworkPlayer otherPlayerNetworkPlayer))
                    {
                        //Check the status of the other player
                        if (otherPlayerNetworkPlayer.IsActiveRagdoll)
                            forceAmountMultiplier = 10;
                        else forceAmountMultiplier = 15;
                    }

                    //Toss the object away before we remove the joint
                    fixedJoint.connectedBody.AddForce((networkPlayer.transform.forward + Vector3.up * 0.25f) * forceAmountMultiplier, ForceMode.Impulse);
                }

                Destroy(fixedJoint);
            }

            //Change animation state
            animator.SetBool("isCarrying", false);
            animator.SetBool("isGrabing", false);
        }
    }

    bool TryCarryObject(Collision collision)
    {
        //Check if we are allowed to carry objects, only state authority is allow. 
        if (!networkPlayer.Object.HasStateAuthority)
            return false;
        
        //Check that we are not in active ragdoll mode
        if (!networkPlayer.IsActiveRagdoll)
            return false;

        //Check that we are trying to grab something
        if (!networkPlayer.IsGrabingActive)
            return false;

        //Check if we are already carrying another object
        if(fixedJoint != null)
            return false;

        //Avoid trying to grab yourself
        if (collision.transform.root == networkPlayer.transform)
            return false;

        //Get the other rigidbody if here is one
        if(!collision.collider.TryGetComponent(out Rigidbody otherObjectRigidbody))
            return false;

        //Add a fixed joint
        fixedJoint = transform.gameObject.AddComponent<FixedJoint>();

        //Connect the joint to the other objects rigidbody
        fixedJoint.connectedBody = otherObjectRigidbody;
  
        //We'll take care of the anchor point on our own
        fixedJoint.autoConfigureConnectedAnchor = false;

        //Transform the collision point from world to local space. 
        fixedJoint.connectedAnchor = collision.transform.InverseTransformPoint(collision.GetContact(0).point);

        //Set animator to carrying
        animator.SetBool("isCarrying", true);

        return true;
    }

    void OnCollisionEnter(Collision collision)
    {
        //Attemt to carry the other object
        TryCarryObject(collision);
    }
}
