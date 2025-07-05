using UnityEngine;

[ExecuteInEditMode]
public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The transform the camera will follow (usually the player root or hip)")]
    public Transform target;

    [Header("Offset Settings")]
    [Tooltip("Height offset relative to the target's position")]
    public float heightOffset = 2.0f;
    [Tooltip("Default distance from target")]
    public float distance = 5.0f;
    [Tooltip("Minimum zoom distance")]
    public float minDistance = 2.0f;
    [Tooltip("Maximum zoom distance")]
    public float maxDistance = 10.0f;

    [Header("Controls")]
    [Tooltip("Orbit sensitivity on X (horizontal)")]
    public float orbitSensitivityX = 120f;
    [Tooltip("Orbit sensitivity on Y (vertical)")]
    public float orbitSensitivityY = 120f;
    [Tooltip("Scroll wheel zoom speed")]
    public float scrollSensitivity = 2f;

    [Header("Rotation Limits")]
    [Tooltip("Minimum vertical angle (degrees)")]
    public float minYAngle = -20f;
    [Tooltip("Maximum vertical angle (degrees)")]
    public float maxYAngle = 80f;

    [Header("Smoothing")]
    [Tooltip("Use smoothing for position and rotation")]
    public bool enableSmoothing = true;
    [Tooltip("Time (in seconds) to smooth to the target")]
    public float smoothTime = 0.1f;

    float currentDistance;
    float desiredDistance;

    float yaw;
    float pitch;

    Vector3 currentVelocity = Vector3.zero;
    Vector3 desiredPosition;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("ThirdPersonCameraController: No target assigned.");
            enabled = false;
            return;
        }

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        desiredDistance = currentDistance = distance;
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleInput();
        CalculatePosition();
        ApplyPosition();
    }

    void HandleInput()
    {
        // Orbit
        yaw += Input.GetAxis("Mouse X") * orbitSensitivityX * Time.deltaTime;
        pitch -= Input.GetAxis("Mouse Y") * orbitSensitivityY * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minYAngle, maxYAngle);

        // Zoom
        desiredDistance -= Input.GetAxis("Mouse ScrollWheel") * scrollSensitivity;
        desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
    }

    void CalculatePosition()
    {
        // Smooth distance
        if (enableSmoothing)
            currentDistance = Mathf.Lerp(currentDistance, desiredDistance, Time.deltaTime / smoothTime);
        else
            currentDistance = desiredDistance;

        // Desired position based on spherical coordinates
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -currentDistance);
        desiredPosition = target.position + Vector3.up * heightOffset + offset;
    }

    void ApplyPosition()
    {
        if (enableSmoothing)
        {
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
            Quaternion targetRot = Quaternion.LookRotation((target.position + Vector3.up * heightOffset) - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime / smoothTime);
        }
        else
        {
            transform.position = desiredPosition;
            transform.LookAt(target.position + Vector3.up * heightOffset);
        }
    }
}
