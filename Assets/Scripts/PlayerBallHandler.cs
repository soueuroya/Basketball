using UnityEngine;

public class PlayerBallHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform ballHoldPoint;
    [SerializeField] private Animator anim;

    [Header("Ball Interaction")]
    [SerializeField, Min(0.1f)] private float pickupDistance = 3f;
    [SerializeField, Min(0.01f)] private float pickupRadius = 0.45f;
    [SerializeField] private LayerMask pickupLayers = ~0;
    [SerializeField, Min(0f)] private float shootForwardSpeed = 8f;
    [SerializeField, Min(0f)] private float shootUpwardSpeed = 5f;

    private Ball heldBall;
    private Ball reachableBall;

    public bool HasBall => heldBall != null;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInParent<Camera>();
        }

        if (ballHoldPoint == null)
        {
            Transform anchor = transform.Find("BasketballAnchor");
            ballHoldPoint = anchor != null ? anchor : transform;
        }

        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        UpdateReachableBall();

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (heldBall != null)
        {
            Shoot();
        }
        else if (reachableBall != null)
        {
            PickUp(reachableBall);
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

    private void Shoot()
    {
        Ball ballToShoot = heldBall;
        heldBall = null;

        if (anim != null)
        {
            anim.SetTrigger("Shoot");
        }

        Vector3 aimDirection = playerCamera != null
            ? playerCamera.transform.forward
            : transform.forward;

        Vector3 flatDirection = Vector3.ProjectOnPlane(aimDirection, Vector3.up).normalized;
        float cameraPitchInfluence = aimDirection.y * shootForwardSpeed;
        Vector3 shotVelocity =
            flatDirection * shootForwardSpeed +
            Vector3.up * (shootUpwardSpeed + cameraPitchInfluence);

        ballToShoot.Release(shotVelocity);
    }

    private void OnDisable()
    {
        reachableBall = null;

        if (anim != null)
        {
            anim.SetBool("OnReach", false);
        }
    }
}
