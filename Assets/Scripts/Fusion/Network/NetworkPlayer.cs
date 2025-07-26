using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using Fusion.Addons.Physics;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    public static NetworkPlayer Local { get; set; }

    [SerializeField] Rigidbody rigidbody3D;
    [SerializeField] NetworkRigidbody3D networkRigidbody3D;
    [SerializeField] ConfigurableJoint mainJoint;
    [SerializeField] Animator animator;

    [SerializeField] Transform playerRoot; // Root transform reference (set in inspector)

    [Header("Settings")]
    [SerializeField] float maxSpeed = 3;
    [SerializeField] float rotationSensitivity = 500f; // **NEW**: Rotation speed in degrees/sec

    //Input
    Vector2 moveInputVector = Vector2.zero;
    bool isJumpButtonPressed = false;
    bool isRevivedButtonPressed = false;
    bool isGrabButtonPressed = false;

    //States
    bool isGrounded = false;
    bool isActiveRagdoll = true;
    public bool IsActiveRagdoll => isActiveRagdoll;
    bool isGrabingActive = false;
    public bool IsGrabingActive => isGrabingActive;

    //Raycasts
    RaycastHit[] raycastHits = new RaycastHit[10];

    //Syncing of physics objects
    SyncPhysicsObject[] syncPhysicsObjects;

    //Cinemachine
    CinemachineVirtualCamera cinemachineVirtualCamera;
    CinemachineBrain cinemachineBrain;

    //Syncing clients ragdolls
    [Networked, Capacity(10)] public NetworkArray<Quaternion> networkPhysicsSyncedRotations { get; }

    //Store original values
    float startSlerpPositionSpring = 0.0f;

    //Timing
    float lastTimeBecameRagdoll = 0;

    //Grabhandler
    HandGrabHandler[] handGrabHandlers;

    void Awake()
    {
        syncPhysicsObjects = GetComponentsInChildren<SyncPhysicsObject>();
        handGrabHandlers = GetComponentsInChildren<HandGrabHandler>();
    }

    void Start()
    {
        startSlerpPositionSpring = mainJoint.slerpDrive.positionSpring;
    }

    void Update()
    {
        // Capture inputs
        moveInputVector.x = Input.GetAxis("Horizontal");
        moveInputVector.y = Input.GetAxis("Vertical");

        if (Input.GetKeyDown(KeyCode.Space))
            isJumpButtonPressed = true;

        if (Input.GetKeyDown(KeyCode.R))
            isRevivedButtonPressed = true;

        isGrabButtonPressed = Input.GetKey(KeyCode.G);
    }

    public override void FixedUpdateNetwork()
    {
        Vector3 localVelocifyVsForward = Vector3.zero;
        float localForwardVelocity = 0;

        if (Object.HasStateAuthority)
        {
            isGrounded = false;

            // Ground check
            int numberOfHits = Physics.SphereCastNonAlloc(rigidbody3D.position, 0.1f, Vector3.down, raycastHits, 0.5f);
            for (int i = 0; i < numberOfHits; i++)
            {
                if (raycastHits[i].transform.root == transform)
                    continue;

                isGrounded = true;
                break;
            }

            if (!isGrounded)
                rigidbody3D.AddForce(Vector3.down * 10);

            localVelocifyVsForward = playerRoot.forward * Vector3.Dot(playerRoot.forward, rigidbody3D.linearVelocity);
            localForwardVelocity = localVelocifyVsForward.magnitude;
        }

        if (GetInput(out NetworkInputData networkInputData))
        {
            float inputMagnitude = networkInputData.movementInput.magnitude;
            isGrabingActive = networkInputData.isGrabPressed;

            if (isActiveRagdoll)
            {
                if (inputMagnitude > 0.01f)
                {
                    // Input relative to playerRoot forward/right
                    Vector3 moveDirection = (playerRoot.forward * networkInputData.movementInput.y) +
                                            (playerRoot.right * networkInputData.movementInput.x);
                    moveDirection.Normalize();

                    // Rotate the playerRoot to face the movement direction using sensitivity
                    Quaternion desiredRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                    playerRoot.rotation = Quaternion.RotateTowards(
                        playerRoot.rotation,
                        desiredRotation,
                        Runner.DeltaTime * rotationSensitivity   // **Uses sensitivity**
                    );

                    // Sync mainJoint rotation to root
                    mainJoint.targetRotation = Quaternion.Inverse(playerRoot.localRotation);

                    // Apply movement relative to facing direction
                    if (localForwardVelocity < maxSpeed)
                    {
                        rigidbody3D.AddForce(moveDirection * 30);
                    }
                }

                if (isGrounded && networkInputData.isJumpPressed)
                {
                    rigidbody3D.AddForce(Vector3.up * 20, ForceMode.Impulse);
                    isJumpButtonPressed = false;
                }
            }
            else
            {
                if (networkInputData.isRevivePressed && Runner.SimulationTime - lastTimeBecameRagdoll > 3)
                    MakeActiveRagdoll();
            }
        }

        if (Object.HasStateAuthority)
        {
            animator.SetFloat("movementSpeed", localForwardVelocity * 0.4f);

            for (int i = 0; i < syncPhysicsObjects.Length; i++)
            {
                if (isActiveRagdoll)
                    syncPhysicsObjects[i].UpdateJointFromAnimation();

                networkPhysicsSyncedRotations.Set(i, syncPhysicsObjects[i].transform.localRotation);
            }

            if (transform.position.y < -10)
            {
                networkRigidbody3D.Teleport(Vector3.zero, Quaternion.identity);
                MakeActiveRagdoll();
            }

            foreach (HandGrabHandler handGrabHandler in handGrabHandlers)
            {
                handGrabHandler.UpdateState();
            }
        }
    }

    public override void Render()
    {
        if (!Object.HasStateAuthority)
        {
            var interpolated = new NetworkBehaviourBufferInterpolator(this);

            for (int i = 0; i < syncPhysicsObjects.Length; i++)
            {
                syncPhysicsObjects[i].transform.localRotation = Quaternion.Slerp(
                    syncPhysicsObjects[i].transform.localRotation,
                    networkPhysicsSyncedRotations.Get(i),
                    interpolated.Alpha
                );
            }
        }

        if (Object.HasInputAuthority)
        {
            cinemachineBrain.ManualUpdate();
            cinemachineVirtualCamera.UpdateCameraState(Vector3.up, Runner.LocalAlpha);
        }
    }

    public NetworkInputData GetNetworkInput()
    {
        NetworkInputData networkInputData = new NetworkInputData();

        networkInputData.movementInput = moveInputVector;

        if (isJumpButtonPressed)
            networkInputData.isJumpPressed = true;

        if (isRevivedButtonPressed)
            networkInputData.isRevivePressed = true;

        if (isGrabButtonPressed)
            networkInputData.isGrabPressed = true;

        isJumpButtonPressed = false;
        isRevivedButtonPressed = false;

        return networkInputData;
    }

    public void OnPlayerBodyPartHit()
    {
        if (!IsActiveRagdoll)
            return;

        MakeRagdoll();
    }

    void MakeRagdoll()
    {
        if (!Object.HasStateAuthority)
            return;

        JointDrive jointDrive = mainJoint.slerpDrive;
        jointDrive.positionSpring = 0;
        mainJoint.slerpDrive = jointDrive;

        for (int i = 0; i < syncPhysicsObjects.Length; i++)
        {
            syncPhysicsObjects[i].MakeRagdoll();
        }

        lastTimeBecameRagdoll = Runner.SimulationTime;
        isActiveRagdoll = false;
        isGrabingActive = false;
    }

    void MakeActiveRagdoll()
    {
        if (!Object.HasStateAuthority)
            return;

        JointDrive jointDrive = mainJoint.slerpDrive;
        jointDrive.positionSpring = startSlerpPositionSpring;
        mainJoint.slerpDrive = jointDrive;

        for (int i = 0; i < syncPhysicsObjects.Length; i++)
        {
            syncPhysicsObjects[i].MakeActiveRagdoll();
        }

        isActiveRagdoll = true;
        isGrabingActive = false;
    }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            Local = this;

            cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            cinemachineBrain = FindObjectOfType<CinemachineBrain>();

            cinemachineVirtualCamera.m_Follow = transform;
            cinemachineVirtualCamera.m_LookAt = transform;

            Utils.DebugLog("Spawned player with input authority");
        }
        else Utils.DebugLog("Spawned player without input authority");

        transform.name = $"P_{Object.Id}";

        if (!Object.HasStateAuthority)
        {
            rigidbody3D.isKinematic = true;
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (Object.InputAuthority == player)
            Runner.Despawn(Object);
    }
}
