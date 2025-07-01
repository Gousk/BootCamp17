using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody hipRigidbody;    // Drag: RagdollRoot/Hips Rigidbody
    public Transform ragdollRoot;     // Drag: RagdollRoot
    public Transform yawPivot;        // Drag: yawPivot
    public Transform pitchPivot;      // Drag: pitchPivot
    public Camera playerCamera;     // Drag: MainCamera

    [Header("Camera Settings")]
    public float mouseSensitivity = 3f;
    public float minPitch = -30f;
    public float maxPitch = 60f;
    public float cameraDistance = 5f;    // local Z of MainCamera
    public float cameraHeight = 2f;    // local Y of pitchPivot
    public float smoothSpeed = 10f;

    float yaw;
    float pitch;

    [Header("Movement Settings")]
    public float moveForce = 200f;   // to hipRigidbody
    public float maxSpeed = 5f;   // horizontal cap
    public float rotationSpeed = 10f;   // how fast ragdollRoot turns

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        yaw = yawPivot.eulerAngles.y;
    }

    void Update()
    {
        // Camera orbit input
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch = Mathf.Clamp(
                    pitch - Input.GetAxis("Mouse Y") * mouseSensitivity,
                    minPitch, maxPitch
                 );
    }

    void LateUpdate()
    {
        // 1) Reposition camera pivots at the hip
        Vector3 hipPos = hipRigidbody.position;
        yawPivot.position = hipPos;
        pitchPivot.position = hipPos + Vector3.up * cameraHeight;

        // 2) Rotate pivots
        yawPivot.rotation = Quaternion.Euler(0f, yaw, 0f);
        pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // 3) Pull camera back & smooth
        Vector3 camLocalOffset = new Vector3(0f, 0f, -cameraDistance);
        Vector3 desiredCamPos = pitchPivot.TransformPoint(camLocalOffset);
        playerCamera.transform.position = Vector3.Lerp(
            playerCamera.transform.position,
            desiredCamPos,
            smoothSpeed * Time.deltaTime
        );

        // 4) Always look at upper body
        playerCamera.transform.LookAt(hipPos + Vector3.up * 1.5f);

        // 5) Snap ragdollRoot position to the hip
        ragdollRoot.position = hipPos;
    }

    void FixedUpdate()
    {
        // Movement input
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 input = new Vector3(h, 0, v);
        if (input.sqrMagnitude > 1f) input.Normalize();

        if (input.sqrMagnitude > 0.01f)
        {
            // a) Compute world move dir from camera yaw
            Vector3 forward = yawPivot.forward; forward.y = 0; forward.Normalize();
            Vector3 right = yawPivot.right; right.y = 0; right.Normalize();
            Vector3 moveDir = (forward * v + right * h).normalized;

            // b) Rotate ragdollRoot toward moveDir
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            ragdollRoot.rotation = Quaternion.Slerp(
                ragdollRoot.rotation,
                targetRot,
                rotationSpeed * Time.fixedDeltaTime
            );

            // c) Apply force to hip
            hipRigidbody.AddForce(
                moveDir * moveForce * Time.fixedDeltaTime,
                ForceMode.VelocityChange
            );
        }

        // Speed cap
        Vector3 vel = hipRigidbody.linearVelocity;
        Vector3 flatV = new Vector3(vel.x, 0, vel.z);
        if (flatV.magnitude > maxSpeed)
        {
            Vector3 clamped = flatV.normalized * maxSpeed;
            hipRigidbody.linearVelocity = new Vector3(clamped.x, vel.y, clamped.z);
        }
    }
}
