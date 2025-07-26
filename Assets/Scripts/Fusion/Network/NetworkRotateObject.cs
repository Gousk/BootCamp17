using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class NetworkRotateObject : NetworkBehaviour
{
    [SerializeField]
    Rigidbody rigidbody3D;

    [SerializeField]
    Vector3 rotationAmount;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            Vector3 rotateBy = transform.rotation.eulerAngles + rotationAmount * Runner.DeltaTime;

            if (rigidbody3D != null)
                rigidbody3D.MoveRotation(Quaternion.Euler(rotateBy));
            else transform.rotation = Quaternion.Euler(rotateBy);
        }
    }
}
