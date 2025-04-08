using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 10f;
    //public float flightSpeed = 10f;
    public float turnSpeed = 360f;

    [Header("Flight Mode Settings")]
    public bool isFlightMode = false;

    private Rigidbody rb;
    private Vector3 movement;

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 100f;
    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Prevent rigidbody rotation
        Cursor.lockState = CursorLockMode.Locked; // Lock the cursor for mouse look

        if (isFlightMode)
        {
            rb.useGravity = false;
        }
    }

    void Update()
    {
        HandleMovementInput();
        HandleMouseLook();

        /* if (Input.GetKeyDown(KeyCode.Space))
         {
             ToggleFlightMode();
         }*/
    }

    void HandleMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal") + (Input.GetKey(KeyCode.LeftArrow) ? -1 : 0) + (Input.GetKey(KeyCode.RightArrow) ? 1 : 0);
        float vertical = Input.GetAxisRaw("Vertical") + (Input.GetKey(KeyCode.UpArrow) ? 1 : 0) + (Input.GetKey(KeyCode.DownArrow) ? -1 : 0);

        // Normalize movement direction
        movement = new Vector3(horizontal, 0f, vertical).normalized;

        if (isFlightMode)
        {
            // Allow vertical movement in flight mode
            if (Input.GetKey(KeyCode.E)) // Ascend
            {
                movement.y = 1;
            }
            else if (Input.GetKey(KeyCode.Q)) // Descend
            {
                movement.y = -1;
            }
        }
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        // Rotate the parent for yaw (this keeps the collider aligned with the ground)
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // Rotate only the camera for pitch (local rotation only)
        Camera.main.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }


    void FixedUpdate()
    {
        if (isFlightMode)
        {
            FlyMovement();
        }
        else
        {
            WalkMovement();
        }
    }

    void WalkMovement()
    {
        Vector3 moveDirection = transform.forward * movement.z + transform.right * movement.x;
        if (movement.magnitude > 0.1f)
        {
            rb.MovePosition(rb.position + moveDirection * walkSpeed * Time.fixedDeltaTime);
        }
    }

    void FlyMovement()
    {
        // Vector3 flyDirection = transform.forward * movement.z + transform.right * movement.x + transform.up * movement.y;
        // rb.MovePosition(rb.position + flyDirection * flightSpeed * Time.fixedDeltaTime);
    }

    void ToggleFlightMode()
    {
        isFlightMode = !isFlightMode;

        if (isFlightMode)
        {
            rb.useGravity = false;
        }
        else
        {
            rb.useGravity = true;
        }
    }
}
