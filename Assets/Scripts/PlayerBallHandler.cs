using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private Transform ballStartingPoint;
    [SerializeField] private LineRenderer shotPredictionLine;

    [Header("Ball Interaction")]
    [SerializeField, Min(0.1f)] private float pickupDistance = 3f;
    [SerializeField, Range(0.01f, 0.5f)] private float crosshairTargetRadius = 0.12f;
    [SerializeField] private LayerMask pickupLayers = ~0;
    [SerializeField, Min(0.1f)] private float maxChargeTime = 2f;
    [SerializeField, Min(0f)] private float shootForwardSpeed = 8f;
    [SerializeField, Min(0f)] private float maxShootForwardSpeed = 14f;
    [SerializeField, Min(0f)] private float shootUpwardSpeed = 5f;
    [SerializeField, Min(0f)] private float maxShootUpwardSpeed = 7f;
    [SerializeField, Min(0f)] private float throwReleaseDelay = 0.5f;
    [SerializeField, Range(0f, 1f)] private float playerVelocityTransfer = 0.5f;
    [SerializeField, Min(0f)] private float throwBackspin = 12f;

    [Header("Ball Magnet")]
    [SerializeField, Min(0.1f)] private float magnetDistance = 15f;
    [SerializeField, Min(0f)] private float magnetAcceleration = 25f;
    [SerializeField, Min(0f)] private float magnetMaxSpeed = 10f;

    [Header("Debug")]
    [SerializeField] private bool drawInteractionGizmos = true;

    [Header("Performance")]
    [SerializeField, Range(0.01f, 0.25f)] private float targetingInterval = 0.05f;

    [Header("Charge Camera")]
    [SerializeField, Min(1f)] private float normalFieldOfView = 70f;
    [SerializeField, Min(1f)] private float chargedFieldOfView = 65f;
    [SerializeField, Min(0.01f)] private float fieldOfViewReturnDuration = 0.35f;

    [Header("Shot Prediction")]
    [SerializeField, Min(2)] private int predictionPointCount = 32;
    [SerializeField, Min(0.02f)] private float predictionTimeStep = 0.08f;
    [SerializeField, Min(0.001f)] private float predictionLineWidth = 0.035f;
    [SerializeField] private Color predictionLineColor = new Color(0.2f, 0.9f, 1f, 0.85f);
    [SerializeField, Min(0f)] private float predictionCollisionRadius = 0.12f;
    [SerializeField] private LayerMask predictionCollisionLayers = ~0;

    [SerializeField] CinemachineBasicMultiChannelPerlin perlinNoise;

    private Ball heldBall;
    private Ball reachableBall;
    private Ball targetedBall;
    private bool isCharging;
    private bool isThrowing;
    private float chargeStartedAt;
    private float nextTargetingTime;
    private Coroutine fieldOfViewReturnCoroutine;
    private bool animatorOnReach;
    private bool animatorCalling;

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

        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponentInParent<Rigidbody>();
        }

        ResetAnimatorTriggers();
        SetCameraFieldOfView(normalFieldOfView);
        InitializeShotPredictionLine();
        HideShotPrediction();
    }

    private void Update()
    {
        if (GameplayManager.IsPaused)
        {
            HideShotPrediction();
            return;
        }

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
            UpdateShotPrediction(chargeAmount);
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
        UpdateShotPrediction(0f);

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

        if (!keepMagnetTarget && Time.unscaledTime >= nextTargetingTime)
        {
            nextTargetingTime = Time.unscaledTime + targetingInterval;
            targetedBall = FindLookedAtBall(magnetDistance);
        }

        Ball nextBall = null;

        if (targetedBall != null && IsWithinPickupReach(targetedBall))
        {
            nextBall = targetedBall;
        }

        reachableBall = nextBall;

        SetAnimatorBool("OnReach", reachableBall != null, ref animatorOnReach);
    }

    private Ball FindLookedAtBall(float distance)
    {
        if (heldBall != null || playerCamera == null)
        {
            return null;
        }

        Ball bestBall = null;
        float bestScreenDistance = float.PositiveInfinity;
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);
        float maxDistanceSqr = distance * distance;
        IReadOnlyList<Ball> balls = Ball.ActiveBalls;

        for (int i = 0; i < balls.Count; i++)
        {
            Ball ball = balls[i];

            if (ball == null ||
                ball.IsHeld ||
                (pickupLayers.value & (1 << ball.gameObject.layer)) == 0 ||
                (ball.transform.position - playerCamera.transform.position)
                    .sqrMagnitude > maxDistanceSqr)
            {
                continue;
            }

            Vector3 viewportPoint =
                playerCamera.WorldToViewportPoint(ball.transform.position);

            if (viewportPoint.z <= 0f)
            {
                continue;
            }

            float screenDistance = Vector2.Distance(
                screenCenter,
                new Vector2(viewportPoint.x, viewportPoint.y));

            if (screenDistance <= crosshairTargetRadius &&
                screenDistance < bestScreenDistance)
            {
                bestBall = ball;
                bestScreenDistance = screenDistance;
            }
        }

        return bestBall;
    }

    private Ray GetInteractionRay()
    {
        return playerCamera != null
            ? playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(playerAim.position, playerAim.forward);
    }

    private bool IsWithinPickupReach(Ball ball)
    {
        Transform pickupOrigin =
            ballHoldPoint != null ? ballHoldPoint : transform;
        Vector3 closestPoint = ball.ClosestPoint(pickupOrigin.position);

        return (closestPoint - pickupOrigin.position).sqrMagnitude <=
            pickupDistance * pickupDistance;
    }

    private void UpdateBallMagnet()
    {
        bool isCalling =
            heldBall == null &&
            Input.GetMouseButton(1) &&
            targetedBall != null;

        SetAnimatorBool("Calling", isCalling, ref animatorCalling);

        if (!isCalling)
        {
            return;
        }

        if (IsWithinPickupReach(targetedBall))
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
            SetAnimatorBool("OnReach", false, ref animatorOnReach);
            SetAnimatorBool("Calling", false, ref animatorCalling);
            SetAnimatorTrigger("Pickup");
        }
    }

    // Shoots/Throws the ball
    private void Shoot(float chargeAmount)
    {
        Ball ballToShoot = heldBall;
        isCharging = false;
        HideShotPrediction();
        StartFieldOfViewReturn();

        if (anim != null)
        {
            SetAnimatorTrigger("Shoot");
        }

        //perlinNoise.AmplitudeGain = 1 + (chargeAmount * 2);
         
        isThrowing = true;
        Vector3 shotVelocity = CalculateShotVelocity(chargeAmount);
        Vector3 angularVelocity = transform.right * -throwBackspin;
        StartCoroutine(ReleaseBallAfterDelay(
            ballToShoot,
            shotVelocity,
            angularVelocity));
    }

    private IEnumerator ReleaseBallAfterDelay(
        Ball ball,
        Vector3 velocity,
        Vector3 angularVelocity)
    {
        yield return new WaitForSeconds(throwReleaseDelay);

        if (ball == null)
        {
            heldBall = null;
            isThrowing = false;
            yield break;
        }

        if (ballStartingPoint != null)
        {
            ball.transform.position = ballStartingPoint.position;
        }

        ball.Release(velocity, angularVelocity);
        heldBall = null;

        if (perlinNoise != null)
        {
            perlinNoise.AmplitudeGain = 0f;
        }

        isThrowing = false;
    }

    private Vector3 CalculateShotVelocity(float chargeAmount)
    {
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
        Vector3 flatDirection = Vector3.ProjectOnPlane(
            aimDirection,
            Vector3.up).normalized;
        float cameraPitchInfluence = aimDirection.y * forwardSpeed;
        Vector3 shotVelocity =
            flatDirection * forwardSpeed +
            Vector3.up * (upwardSpeed + cameraPitchInfluence);

        if (playerRigidbody != null)
        {
            shotVelocity +=
                playerRigidbody.linearVelocity * playerVelocityTransfer;
        }

        return shotVelocity;
    }

    private void InitializeShotPredictionLine()
    {
        if (shotPredictionLine == null)
        {
            GameObject predictionObject = new GameObject("Shot Prediction Line");
            predictionObject.transform.SetParent(transform, false);
            shotPredictionLine = predictionObject.AddComponent<LineRenderer>();

            shotPredictionLine.useWorldSpace = true;
            shotPredictionLine.startWidth = predictionLineWidth;
            shotPredictionLine.endWidth = predictionLineWidth;
            shotPredictionLine.numCapVertices = 6;
            shotPredictionLine.numCornerVertices = 6;
            shotPredictionLine.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            shotPredictionLine.receiveShadows = false;

            if (shotPredictionLine.sharedMaterial == null)
            {
                shotPredictionLine.sharedMaterial =
                    new Material(Shader.Find("Sprites/Default"));
            }

            shotPredictionLine.startColor = predictionLineColor;
            shotPredictionLine.endColor = predictionLineColor;
        }

        shotPredictionLine.positionCount = 0;
    }

    private void UpdateShotPrediction(float chargeAmount)
    {
        if (shotPredictionLine == null)
        {
            return;
        }

        Transform startTransform = ballStartingPoint != null
            ? ballStartingPoint
            : ballHoldPoint;

        if (!isCharging || heldBall == null || startTransform == null)
        {
            HideShotPrediction();
            return;
        }

        Vector3 startPosition = startTransform.position;
        Vector3 velocity = CalculateShotVelocity(chargeAmount);
        Vector3 gravity = Physics.gravity;

        shotPredictionLine.enabled = true;
        shotPredictionLine.positionCount = 1;
        shotPredictionLine.SetPosition(0, startPosition);

        Vector3 previousPoint = startPosition;

        for (int i = 1; i < predictionPointCount; i++)
        {
            float time = i * predictionTimeStep;
            Vector3 point =
                startPosition +
                velocity * time +
                0.5f * gravity * time * time;

            Vector3 segment = point - previousPoint;
            float segmentDistance = segment.magnitude;

            if (segmentDistance <= Mathf.Epsilon)
            {
                continue;
            }

            if (CastShotPredictionSegment(
                    previousPoint,
                    segment / segmentDistance,
                    segmentDistance,
                    out RaycastHit hit))
            {
                shotPredictionLine.positionCount = i + 1;
                shotPredictionLine.SetPosition(i, hit.point);
                return;
            }

            shotPredictionLine.positionCount = i + 1;
            shotPredictionLine.SetPosition(i, point);
            previousPoint = point;
        }
    }

    private bool CastShotPredictionSegment(
        Vector3 origin,
        Vector3 direction,
        float distance,
        out RaycastHit hit)
    {
        int collisionMask =
            predictionCollisionLayers.value & ~(1 << gameObject.layer);

        if (predictionCollisionRadius > 0f)
        {
            return Physics.SphereCast(
                origin,
                predictionCollisionRadius,
                direction,
                out hit,
                distance,
                collisionMask,
                QueryTriggerInteraction.Ignore);
        }

        return Physics.Raycast(
            origin,
            direction,
            out hit,
            distance,
            collisionMask,
            QueryTriggerInteraction.Ignore);
    }

    private void HideShotPrediction()
    {
        if (shotPredictionLine == null)
        {
            return;
        }

        shotPredictionLine.enabled = false;
        shotPredictionLine.positionCount = 0;
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

    private void SetAnimatorBool(
        string parameterName,
        bool value,
        ref bool currentValue)
    {
        if (anim == null || currentValue == value)
        {
            return;
        }

        currentValue = value;
        anim.SetBool(parameterName, value);
    }

    private void OnDisable()
    {
        isCharging = false;
        isThrowing = false;
        reachableBall = null;
        targetedBall = null;
        fieldOfViewReturnCoroutine = null;
        HideShotPrediction();
        SetCameraFieldOfView(normalFieldOfView);

        if (anim != null)
        {
            ResetAnimatorTriggers();
            SetAnimatorBool("OnReach", false, ref animatorOnReach);
            SetAnimatorBool("Calling", false, ref animatorCalling);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawInteractionGizmos)
        {
            return;
        }

        if (playerCamera == null && playerAim == null)
        {
            return;
        }

        Ray aimRay = GetInteractionRay();

        DrawCastGizmo(
            aimRay,
            pickupDistance,
            new Color(0.2f, 1f, 0.2f, 0.9f));
        DrawCastGizmo(
            aimRay,
            magnetDistance,
            new Color(0.1f, 0.8f, 1f, 0.55f));
    }

    private void DrawCastGizmo(Ray ray, float distance, Color color)
    {
        Vector3 end = ray.origin + ray.direction * distance;

        Gizmos.color = color;
        Gizmos.DrawLine(ray.origin, end);
    }
}
