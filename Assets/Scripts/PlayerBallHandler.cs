using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerBallHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
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

    [Header("Ball Magnet")]
    [SerializeField, Min(0.1f)] private float magnetDistance = 15f;
    [SerializeField, Min(0f)] private float magnetAcceleration = 25f;
    [SerializeField, Min(0f)] private float magnetMaxSpeed = 10f;

    [Header("Charge Camera")]
    [SerializeField, Min(1f)] private float normalFieldOfView = 70f;
    [SerializeField, Min(1f)] private float chargedFieldOfView = 65f;
    [SerializeField, Min(0.01f)] private float fieldOfViewReturnDuration = 0.35f;

    [SerializeField] CinemachineBasicMultiChannelPerlin perlinNoise;

    private Ball heldBall;
    private Ball reachableBall;
    private Ball targetedBall;
    private bool isCharging;
    private bool isThrowing;
    private float chargeStartedAt;
    private Coroutine fieldOfViewReturnCoroutine;

    public bool HasBall => heldBall != null;
    public bool IsThrowing => isThrowing;

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

        ResetAnimatorTriggers();
        SetCameraFieldOfView(normalFieldOfView);
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

        UpdateBallMagnet();

        if (Input.GetMouseButtonDown(0) && reachableBall != null)
        {
            PickUp(reachableBall);
        }
    }

    private void FixedUpdate()
    {
        if (heldBall == null &&
            targetedBall != null &&
            Input.GetMouseButton(1))
        {
            Transform target = ballHoldPoint != null ? ballHoldPoint : transform;
            targetedBall.AttractTowards(
                target.position,
                magnetAcceleration,
                magnetMaxSpeed);
        }
    }

    private void UpdateThrowInput()
    {
        if (isCharging)
        {
            float chargeAmount = Mathf.Clamp01(
                (Time.time - chargeStartedAt) / maxChargeTime);
            SetCameraFieldOfView(Mathf.Lerp(
                normalFieldOfView,
                chargedFieldOfView,
                chargeAmount));
        }

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
        if (fieldOfViewReturnCoroutine != null)
        {
            StopCoroutine(fieldOfViewReturnCoroutine);
            fieldOfViewReturnCoroutine = null;
        }

        isCharging = true;
        chargeStartedAt = Time.time;

        if (anim != null)
        {
            SetAnimatorTrigger("Charge");
        }
    }

    private void UpdateReachableBall()
    {
        bool keepMagnetTarget =
            Input.GetMouseButton(1) &&
            targetedBall != null &&
            !targetedBall.IsHeld;

        if (!keepMagnetTarget)
        {
            targetedBall = FindLookedAtBall(magnetDistance);
        }

        Ball nextBall = null;

        if (targetedBall != null &&
            Vector3.Distance(transform.position, targetedBall.transform.position) <=
            pickupDistance)
        {
            nextBall = targetedBall;
        }

        reachableBall = nextBall;

        if (anim != null)
        {
            anim.SetBool("OnReach", reachableBall != null);
        }
    }

    private Ball FindLookedAtBall(float distance)
    {
        if (heldBall != null || (playerAim == null && playerCamera == null))
        {
            return null;
        }

        Ray aimRay = playerCamera != null
            ? playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(playerAim.position, playerAim.forward);

        if (!Physics.SphereCast(
                aimRay.origin,
                pickupRadius,
                aimRay.direction,
                out RaycastHit hit,
                distance,
                pickupLayers,
                QueryTriggerInteraction.Ignore))
        {
            return null;
        }

        Ball ball = hit.collider.GetComponentInParent<Ball>();
        return ball != null && !ball.IsHeld ? ball : null;
    }

    private void UpdateBallMagnet()
    {
        bool isCalling =
            heldBall == null &&
            Input.GetMouseButton(1) &&
            targetedBall != null;

        if (anim != null)
        {
            anim.SetBool("Calling", isCalling);
        }

        if (!isCalling)
        {
            return;
        }

        Transform target = ballHoldPoint != null ? ballHoldPoint : transform;
        float pickupSqrDistance = pickupDistance * pickupDistance;

        if ((targetedBall.transform.position - target.position).sqrMagnitude <=
            pickupSqrDistance)
        {
            PickUp(targetedBall);
            targetedBall = null;
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
            anim.SetBool("Calling", false);
            SetAnimatorTrigger("Pickup");
        }
    }

    // Shoots/Throws the ball
    private void Shoot(float chargeAmount)
    {
        Ball ballToShoot = heldBall;
        isCharging = false;
        StartFieldOfViewReturn();

        if (anim != null)
        {
            SetAnimatorTrigger("Shoot");
        }

        //perlinNoise.AmplitudeGain = 1 + (chargeAmount * 2);
         
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

    private void SetCameraFieldOfView(float fieldOfView)
    {
        if (cinemachineCamera == null)
        {
            return;
        }

        LensSettings lens = cinemachineCamera.Lens;
        lens.FieldOfView = fieldOfView;
        cinemachineCamera.Lens = lens;
    }

    private void StartFieldOfViewReturn()
    {
        if (cinemachineCamera == null)
        {
            return;
        }

        if (fieldOfViewReturnCoroutine != null)
        {
            StopCoroutine(fieldOfViewReturnCoroutine);
        }

        fieldOfViewReturnCoroutine = StartCoroutine(ReturnFieldOfView());
    }

    private IEnumerator ReturnFieldOfView()
    {
        float startingFieldOfView = cinemachineCamera.Lens.FieldOfView;
        float elapsed = 0f;

        while (elapsed < fieldOfViewReturnDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fieldOfViewReturnDuration);
            SetCameraFieldOfView(Mathf.Lerp(
                startingFieldOfView,
                normalFieldOfView,
                progress));
            yield return null;
        }

        SetCameraFieldOfView(normalFieldOfView);
        fieldOfViewReturnCoroutine = null;
    }

    private void ResetAnimatorTriggers()
    {
        if (anim == null)
        {
            return;
        }

        anim.ResetTrigger("Pickup");
        anim.ResetTrigger("Charge");
        anim.ResetTrigger("Shoot");
    }

    private void SetAnimatorTrigger(string triggerName)
    {
        ResetAnimatorTriggers();
        anim.SetTrigger(triggerName);
    }

    private void OnDisable()
    {
        isCharging = false;
        isThrowing = false;
        reachableBall = null;
        targetedBall = null;
        fieldOfViewReturnCoroutine = null;
        SetCameraFieldOfView(normalFieldOfView);

        if (anim != null)
        {
            ResetAnimatorTriggers();
            anim.SetBool("OnReach", false);
            anim.SetBool("Calling", false);
        }
    }
}
