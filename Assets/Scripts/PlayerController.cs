using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float groundDrag = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float jumpCooldown = 0.25f;
    [SerializeField] private float airMultiplier = 0.4f;
    [SerializeField] private float acceleration = 20f; // how fast we reach target speed on ground
    [SerializeField] private float deceleration = 25f; // how fast we stop when no input
    
    [Header("Ground Check")]
    [SerializeField] private float playerHeight = 2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private float groundCheckOffset = 0.1f;
    private bool isGrounded;
    
    private float horizontalInput;
    private float verticalInput;
    private bool jumpInput;
    private float jumpCooldownCounter;
    
    private Rigidbody rb;
    private Vector3 moveDirection;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("PlayerController requires a Rigidbody component!");
        }
        // Recommended Rigidbody setup: freeze rotation X/Z in inspector
    }

    private void Update()
    {
        // Robust ground check using CheckSphere at player's feet
        Vector3 checkPos = transform.position + Vector3.down * (playerHeight * 0.5f - groundCheckOffset);
        isGrounded = Physics.CheckSphere(checkPos, groundCheckRadius, groundLayer);

        // Get input
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        jumpInput = Input.GetKeyDown(KeyCode.Space);

        // Handle jumping
        if (jumpInput && isGrounded && jumpCooldownCounter <= 0f)
        {
            Jump();
            jumpCooldownCounter = jumpCooldown;
        }

        // Reduce jump cooldown
        if (jumpCooldownCounter > 0f)
            jumpCooldownCounter -= Time.deltaTime;

        // Apply drag
        ApplyDrag();
    }

    private void FixedUpdate()
    {
        MovePlayer();
        LimitSpeed();
    }

    private void MovePlayer()
    {
        // Calculate move direction relative to where player is looking
        moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;

        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (isGrounded)
        {
            Vector3 targetVel = moveDirection.normalized * moveSpeed;

            // If there's input, accelerate towards target
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Vector3 newFlat = Vector3.Lerp(flatVel, targetVel, Mathf.Clamp01(acceleration * Time.fixedDeltaTime));
                rb.linearVelocity = new Vector3(newFlat.x, rb.linearVelocity.y, newFlat.z);
            }
            else
            {
                // No input: decelerate to zero for snappy stopping
                Vector3 newFlat = Vector3.Lerp(flatVel, Vector3.zero, Mathf.Clamp01(deceleration * Time.fixedDeltaTime));
                rb.linearVelocity = new Vector3(newFlat.x, rb.linearVelocity.y, newFlat.z);
            }
        }
        else
        {
            // In air, keep gentle air control via AddForce
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    private void Jump()
    {
        // Reset Y velocity before jumping
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        // Add jump force
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ApplyDrag()
    {
        if (isGrounded)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0f;
    }

    private void LimitSpeed()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // Limit velocity if needed
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }

    public bool IsGrounded => isGrounded;

    private void OnDrawGizmosSelected()
    {
        // Visualize ground check in editor
        Vector3 checkPos = transform.position + Vector3.down * (playerHeight * 0.5f - groundCheckOffset);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
    }
}
