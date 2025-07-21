using Fusion;
using Fusion.Addons.Physics;
using System.Globalization;
using UnityEngine;

public class PlayerControllerFusion : NetworkBehaviour, IPlayerLeft
{
    public static PlayerControllerFusion Local { get; set; }

    [Header("References")]
    public Rigidbody hipRigidbody;    // RagdollRoot/Hips Rigidbody
    public Transform ragdollRoot;     // RagdollRoot for turning the body
    public Transform yawPivot;        // CameraYawPivot
    public Transform pitchPivot;      // CameraPitchPivot
    public Camera playerCamera;       // MainCamera
    public Animator animator;         // Animator with "isMoving" bool parameter
    public RagdollMotionMatcher motionMatcher;
    public NetworkRigidbody3D networkRigidbody3D;

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
    Vector3 movementInput;
    bool isJumpButtonPressed = false;

    // store the joints' starting rotations so we can apply mouse deltas on top
    Quaternion hipInitTargetRot;
    Quaternion stomachInitTargetRot;

    private void Start()
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

        // get movement input
        movementInput.x = Input.GetAxis("Horizontal");
        movementInput.y = Input.GetAxis("Vertical");

        // read raw mouse deltas
        float deltaX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float deltaY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // update our orbit angles
        yaw += deltaX;
        pitch = Mathf.Clamp(pitch - deltaY, minPitch, maxPitch);

        if (Object.HasStateAuthority)
        {
            // drive the pelvis yaw
            if (hipJoint != null)
                hipJoint.targetRotation = hipInitTargetRot *
                    Quaternion.Euler(0f, -deltaX, 0f);

            // drive the chest pitch
            if (stomachJoint != null)
                stomachJoint.targetRotation = stomachInitTargetRot *
                    Quaternion.Euler(-deltaY, 0f, 0f);
        }
    }

    void LateUpdate()
    {
        if (Object.HasStateAuthority)
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
    }

    public override void FixedUpdateNetwork()
    {
        Vector3 localVelocityVsForward = Vector3.zero;
        float localForwardVelocity = 0f;

        if (Object.HasStateAuthority)
        {
            //ground checks here for jump


            localVelocityVsForward = transform.forward * Vector3.Dot(transform.forward, hipRigidbody.linearVelocity);
            localForwardVelocity = localVelocityVsForward.magnitude;
        }

        if (GetInput(out NetworkInputData networkInputData))
        {
            if (networkInputData.movementInput.sqrMagnitude > 1f) networkInputData.movementInput.Normalize();

            // update animator parameter
            bool isMoving = networkInputData.movementInput.sqrMagnitude > 0.01f;

            if (isMoving)
            {
                // camera-relative move dir
                Vector3 forward = yawPivot.forward; forward.y = 0f; forward.Normalize();
                Vector3 right = yawPivot.right; right.y = 0f; right.Normalize();
                Vector3 moveDir = (forward * networkInputData.movementInput.y + right * networkInputData.movementInput.x).normalized;

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

        if (Object.HasStateAuthority)
        {
            Debug.Log(localForwardVelocity);
            if (localForwardVelocity > 1f)
                animator.SetBool("isMoving", true);
            else
                animator.SetBool("isMoving", false);

            motionMatcher.UpdateJoints();

            if (transform.position.y < -10)
            {
                networkRigidbody3D.Teleport(Vector3.zero, Quaternion.identity);
            }
        }
    }

    public NetworkInputData GetNetworkInput()
    {
        NetworkInputData networkInputData = new NetworkInputData();

        networkInputData.movementInput = movementInput;

        if (isJumpButtonPressed) //jump logic will be added later
        {
            networkInputData.isJumpPressed = true;
        }

        isJumpButtonPressed = false;

        return networkInputData;
    }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            Local = this;
            playerCamera.gameObject.SetActive(true);

            Utils.DebugLog("Spawned player with input authority");
        }
        else
        {
            Utils.DebugLog("Spawned player without input authority");
        }

        transform.name = $"P_{Object.Id}";
    }

    public void PlayerLeft(PlayerRef player)
    {

    }
}