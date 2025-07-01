using UnityEngine;

public class CopyMotion : MonoBehaviour
{
    public Transform targetLimb;
    public bool mirror;
    ConfigurableJoint joint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        joint = GetComponent<ConfigurableJoint>();    
    }

    // Update is called once per frame
    void Update()
    {
        if (joint == null) return;

        if(!mirror)
            joint.targetRotation = targetLimb.rotation;
        else
            joint.targetRotation = Quaternion.Inverse(targetLimb.rotation);
    }
}
