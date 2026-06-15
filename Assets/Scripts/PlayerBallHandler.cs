using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerBallHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform playerAim;
    [SerializeField] private Transform ballHoldPoint;
    [SerializeField] private Animator anim;

    [Header("Ball Interaction")]
    [SerializeField, Min(0.1f)] private float pickupDistance = 3f;
    [SerializeField, Min(0.01f)] private float pickupRadius = 0.45f;
    [SerializeField] private LayerMask pickupLayers = ~0;
    [SerializeField, Min(0.1f)] private float maxChargeTime = 2f;
    [SerializeField, Min(0f)] private float shootForwardSpeed = 8f;
    [SerializeField, Min(0f)] private float maxShootForwardSpeed = 14f;
    [SerializeField, Min(0f)] private float shootUpwardSpeed = 5f;
    [SerializeField, Min(0f)] private float maxShootUpwardSpeed = 7f;
    [SerializeField, Min(0f)] private float throwReleaseDelay = 0.5f;

    [SerializeField] CinemachineBasicMultiChannelPerlin perlinNoise;

    private Ball heldBall;
    private Ball reachableBall;
    private bool isCharging;
    private bool isThrowing;
    private float chargeStartedAt;

    public bool HasBall => heldBall != null;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInParent<Camera>();
        }

        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        UpdateReachableBall();

        if (isThrowing)
        {
            return;
        }

        if (heldBall != null)
        {
            UpdateThrowInput();
            return;
        }

        if (Input.GetMouseButtonDown(0) && reachableBall != null)
        {
            PickUp(reachableBall);
        }
    }

    private void UpdateThrowInput()
    {
        if (!isThrowing && Input.GetMouseButtonDown(0))
        {
            StartCharging();
        }

        if (isCharging && Input.GetMouseButtonUp(0))
        {
            float chargeTime = Mathf.Min(Time.time - chargeStartedAt, maxChargeTime);
            float chargeAmount = chargeTime / maxChargeTime;
            Shoot(chargeAmount);
        }
    }

    private void StartCharging()
    {
        isCharging = true;
        chargeStartedAt = Time.time;

        if (anim != null)
        {
            anim.SetTrigger("Charge");
        }
    }

    private void UpdateReachableBall()
    {
        Ball nextBall = null;

        if (heldBall == null && playerCamera != null &&
            Physics.SphereCast(
                playerCamera.transform.position,
                pickupRadius,
                playerCamera.transform.forward,
                out RaycastHit hit,
                pickupDistance,
                pickupLayers,
                QueryTriggerInteraction.Ignore))
        {
            nextBall = hit.collider.GetComponentInParent<Ball>();

            if (nextBall != null && nextBall.IsHeld)
            {
                nextBall = null;
            }
        }

        reachableBall = nextBall;

        if (anim != null)
        {
            anim.SetBool("OnReach", reachableBall != null);
        }
    }

    private void PickUp(Ball ball)
    {
        heldBall = ball;
        reachableBall = null;
        heldBall.PickUp(ballHoldPoint);

        if (anim != null)
        {
            anim.SetBool("OnReach", false);
            anim.SetTrigger("Pickup");
        }
    }

    // Shoots/Throws the ball
    private void Shoot(float chargeAmount)
    {
        Ball ballToShoot = heldBall;
        isCharging = false;

        if (anim != null)
        {
            anim.SetTrigger("Shoot");
        }

        perlinNoise.AmplitudeGain = 1 + (chargeAmount * 3);
         
        Vector3 aimDirection = playerAim != null
            ? playerAim.transform.forward
            : transform.forward;

        float forwardSpeed = Mathf.Lerp(
            shootForwardSpeed,
            maxShootForwardSpeed,
            chargeAmount);
        float upwardSpeed = Mathf.Lerp(
            shootUpwardSpeed,
            maxShootUpwardSpeed,
            chargeAmount);
        Vector3 flatDirection = Vector3.ProjectOnPlane(aimDirection, Vector3.up).normalized;
        float cameraPitchInfluence = aimDirection.y * forwardSpeed;
        Vector3 shotVelocity =
            flatDirection * forwardSpeed +
            Vector3.up * (upwardSpeed + cameraPitchInfluence);

        isThrowing = true;
        StartCoroutine(ReleaseBallAfterDelay(ballToShoot, shotVelocity));
    }

    private IEnumerator ReleaseBallAfterDelay(Ball ball, Vector3 velocity)
    {
        yield return new WaitForSeconds(throwReleaseDelay);

        ball.Release(velocity);
        heldBall = null;
        perlinNoise.AmplitudeGain = 0f;
        isThrowing = false;
    }

    private void OnDisable()
    {
        isCharging = false;
        isThrowing = false;
        reachableBall = null;

        if (anim != null)
        {
            anim.SetBool("OnReach", false);
        }
    }
}
