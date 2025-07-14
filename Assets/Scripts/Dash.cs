using UnityEngine;

public class SpeedBoostPlayer : MonoBehaviour
{
    public float normalSpeed = 5f;
    public float boostedSpeed = 50f;
    private float currentSpeed;

    private Rigidbody rb;
    private Vector3 inputDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody bulunamadý!");
        }

        currentSpeed = normalSpeed;
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        inputDirection = new Vector3(h, 0, v);
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.MovePosition(rb.position + inputDirection.normalized * currentSpeed * Time.fixedDeltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SpeedZone"))
        {
            Debug.Log("Dash alanýna girdi!");
            currentSpeed = boostedSpeed;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("SpeedZone"))
        {
            Debug.Log("Dash alanýndan çýktý.");
            currentSpeed = normalSpeed;
        }
    }
}
