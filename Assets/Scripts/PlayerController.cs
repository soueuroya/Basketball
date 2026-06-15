using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class FirstPersonPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.15f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField, Min(0f)] private float jumpUngroundedTime = 0.1f;

    [Header("Air Time Slow Motion")]
    [SerializeField, Range(0.05f, 1f)] private float airborneTimeScale = 0.65f;
    [SerializeField, Min(0.01f)] private float timeScaleTransitionSpeed = 3f;

    [Header("Mid-Air Throw")]
    [SerializeField] private PlayerBallHandler ballHandler;
    [SerializeField, Range(0f, 1f)] private float throwGravityMultiplier = 0.2f;

    private Rigidbody body;
    private CapsuleCollider capsule;
    private Vector2 moveInput;
    private float jumpBufferCounter;
    private bool isGrounded;
    private float defaultFixedDeltaTime;
    private float ignoreGroundUntil;
    private readonly RaycastHit[] groundHits = new RaycastHit[8];

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        if (ballHandler == null)
        {
            ballHandler = GetComponentInChildren<PlayerBallHandler>();
        }

        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void Update()
    {
        if (GameplayManager.IsPaused)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter = Mathf.Max(
                0f,
                jumpBufferCounter - Time.unscaledDeltaTime);
        }

        UpdateTimeScale();
    }

    private void FixedUpdate()
    {
        CheckGround();
        Move();
        Jump();
        ApplyThrowAirHang();
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
        if (jumpBufferCounter > 0f && isGrounded)
        {
            Vector3 velocity = body.linearVelocity;
            velocity.y = 0f;
            body.linearVelocity = velocity;

            body.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            jumpBufferCounter = 0f;
            isGrounded = false;
            ignoreGroundUntil = Time.unscaledTime + jumpUngroundedTime;
        }
    }

    private void CheckGround()
    {
        if (Time.unscaledTime < ignoreGroundUntil)
        {
            isGrounded = false;
            return;
        }

        Vector3 center = transform.TransformPoint(capsule.center);
        float radius = capsule.radius * 0.9f;
        float halfHeight = Mathf.Max(capsule.height * 0.5f, radius);
        Vector3 origin = center + Vector3.down * (halfHeight - radius);

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            radius,
            Vector3.down,
            groundHits,
            groundCheckDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );

        isGrounded = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = groundHits[i];

            if (hit.collider.transform.root == transform.root)
            {
                continue;
            }

            isGrounded = true;
            break;
        }
    }

    private void ApplyThrowAirHang()
    {
        if (isGrounded ||
            ballHandler == null ||
            !ballHandler.IsThrowing ||
            body.linearVelocity.y >= 0f)
        {
            return;
        }

        Vector3 counterGravity =
            -Physics.gravity * (1f - throwGravityMultiplier);
        body.AddForce(counterGravity, ForceMode.Acceleration);
    }

    private void UpdateTimeScale()
    {
        bool shouldSlowTime = !isGrounded && Input.GetButton("Jump");
        float targetTimeScale = shouldSlowTime ? airborneTimeScale : 1f;

        Time.timeScale = Mathf.MoveTowards(
            Time.timeScale,
            targetTimeScale,
            timeScaleTransitionSpeed * Time.unscaledDeltaTime);
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
    }
}
