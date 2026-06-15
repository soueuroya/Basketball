using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class FirstPersonPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 6f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float groundCheckDistance = 0.15f;

    private Rigidbody body;
    private CapsuleCollider capsule;
    private Vector2 moveInput;
    private bool jumpRequested;
    private bool isGrounded;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void Update()
    {
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        if (Input.GetButtonDown("Jump"))
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        CheckGround();
        Move();
        Jump();
    }

    private void Move()
    {
        Vector3 direction =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        Vector3 velocity = body.linearVelocity;
        velocity.x = direction.x * moveSpeed;
        velocity.z = direction.z * moveSpeed;

        body.linearVelocity = velocity;
    }

    private void Jump()
    {
        if (jumpRequested && isGrounded)
        {
            Vector3 velocity = body.linearVelocity;
            velocity.y = 0f;
            body.linearVelocity = velocity;

            body.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }

        jumpRequested = false;
    }

    private void CheckGround()
    {
        Vector3 center = transform.TransformPoint(capsule.center);
        float radius = capsule.radius * 0.9f;
        float halfHeight = Mathf.Max(capsule.height * 0.5f, radius);
        Vector3 origin = center + Vector3.down * (halfHeight - radius);

        isGrounded = Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out _,
            groundCheckDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );
    }
}