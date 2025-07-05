using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody hipRigidbody;    // RagdollRoot/Hips Rigidbody
    public Transform ragdollRoot;     // RagdollRoot for turning the body
    public Transform yawPivot;        // CameraYawPivot
    public Transform pitchPivot;      // CameraPitchPivot
    public Camera playerCamera;       // MainCamera
    public Animator animator;         // Animator with "isMoving" bool parameter

    [Header("Joints")]
    public ConfigurableJoint hipJoint;        // pelvis joint drive
    public ConfigurableJoint stomachJoint;    // chest/spine joint drive

    [Header("Camera Settings")]
    public float mouseSensitivity = 3f;
    public float minPitch = -30f;
    public float maxPitch = 60f;
    public float cameraDistance = 5f;
    public float cameraHeight = 2f;
    public float smoothSpeed = 10f;

    [Header("Movement Settings")]
    public float moveForce = 200f;
    public float maxSpeed = 5f;
    public float rotationSpeed = 10f;

    float yaw;
    float pitch;

    // store the joints' starting rotations so we can apply mouse deltas on top
    Quaternion hipInitTargetRot;
    Quaternion stomachInitTargetRot;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        // align our yaw accumulator
        yaw = yawPivot.eulerAngles.y;

        // cache the joints' initial targetRotation
        if (hipJoint != null) hipInitTargetRot = hipJoint.targetRotation;
        if (stomachJoint != null) stomachInitTargetRot = stomachJoint.targetRotation;
    }

    void Update()
    {
        // read raw mouse deltas
        float deltaX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float deltaY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // update our orbit angles
        yaw += deltaX;
        pitch = Mathf.Clamp(pitch - deltaY, minPitch, maxPitch);

        // drive the pelvis yaw
        if (hipJoint != null)
            hipJoint.targetRotation = hipInitTargetRot *
                Quaternion.Euler(0f, -deltaX, 0f);

        // drive the chest pitch
        if (stomachJoint != null)
            stomachJoint.targetRotation = stomachInitTargetRot *
                Quaternion.Euler(-deltaY, 0f, 0f);
    }

    void LateUpdate()
    {
        // follow the hip position
        Vector3 hipPos = hipRigidbody.position;
        yawPivot.position = hipPos;
        pitchPivot.position = hipPos + Vector3.up * cameraHeight;

        // orbit pivots
        yawPivot.rotation = Quaternion.Euler(0f, yaw, 0f);
        pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // smooth camera pull‐back
        Vector3 desiredCamPos = pitchPivot.TransformPoint(Vector3.back * cameraDistance);
        playerCamera.transform.position = Vector3.Lerp(
            playerCamera.transform.position,
            desiredCamPos,
            smoothSpeed * Time.deltaTime
        );

        // look at the upper body
        playerCamera.transform.LookAt(hipPos + Vector3.up * 1.5f);

        // snap ragdollRoot for movement rotation
        ragdollRoot.position = hipPos;
    }

    void FixedUpdate()
    {
        // get movement input
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f) input.Normalize();

        // update animator parameter
        bool isMoving = input.sqrMagnitude > 0.01f;
        animator.SetBool("isMoving", isMoving);

        if (isMoving)
        {
            // camera-relative move dir
            Vector3 forward = yawPivot.forward; forward.y = 0f; forward.Normalize();
            Vector3 right = yawPivot.right; right.y = 0f; right.Normalize();
            Vector3 moveDir = (forward * v + right * h).normalized;

            // face movement direction
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            ragdollRoot.rotation = Quaternion.Slerp(
                ragdollRoot.rotation,
                targetRot,
                rotationSpeed * Time.fixedDeltaTime
            );

            // apply physics force
            hipRigidbody.AddForce(
                moveDir * moveForce * Time.fixedDeltaTime,
                ForceMode.VelocityChange
            );
        }

        // clamp horizontal speed
        Vector3 vel = hipRigidbody.linearVelocity;
        Vector3 flat = new Vector3(vel.x, 0f, vel.z);
        if (flat.magnitude > maxSpeed)
        {
            Vector3 clamped = flat.normalized * maxSpeed;
            hipRigidbody.linearVelocity = new Vector3(clamped.x, vel.y, clamped.z);
        }
    }
}
