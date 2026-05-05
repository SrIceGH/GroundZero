using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerBasicMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody rb;
    private Vector3 inputDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S

        Vector3 input = new Vector3(h, 0f, v);

        
        inputDirection = input.sqrMagnitude > 0.01f ? input.normalized : Vector3.zero;
    }

    void FixedUpdate()
    {
        if (inputDirection != Vector3.zero)
        {
            Vector3 move = inputDirection * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + move);
        }
    }
}
