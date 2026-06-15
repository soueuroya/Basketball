using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Ball : MonoBehaviour
{
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField, Min(0f)] private float trailSpeedThreshold = 8f;

    private Rigidbody ballRigidbody;
    private Collider[] ballColliders;
    private Transform holdPoint;

    public bool IsHeld { get; private set; }

    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody>();
        ballColliders = GetComponentsInChildren<Collider>();

        if (trailRenderer == null)
        {
            trailRenderer = GetComponentInChildren<TrailRenderer>();
        }

        SetTrailEmission(false, true);
    }

    private void Update()
    {
        bool shouldEmit =
            !IsHeld &&
            !ballRigidbody.isKinematic &&
            ballRigidbody.linearVelocity.sqrMagnitude >=
            trailSpeedThreshold * trailSpeedThreshold;

        SetTrailEmission(shouldEmit);
    }

    private void LateUpdate()
    {
        if (IsHeld && holdPoint != null)
        {
            transform.SetPositionAndRotation(holdPoint.position, holdPoint.rotation);
        }
    }

    public void PickUp(Transform holdPoint)
    {
        if (IsHeld || holdPoint == null)
        {
            return;
        }

        IsHeld = true;
        this.holdPoint = holdPoint;
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
        ballRigidbody.isKinematic = true;
        ballRigidbody.useGravity = false;
        SetCollidersEnabled(false);
        SetTrailEmission(false, true);

        transform.SetParent(holdPoint, false);
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public void Release(Vector3 velocity)
    {
        if (!IsHeld)
        {
            return;
        }

        IsHeld = false;
        holdPoint = null;
        transform.SetParent(null, true);
        SetCollidersEnabled(true);
        ballRigidbody.isKinematic = false;
        ballRigidbody.useGravity = true;
        ballRigidbody.linearVelocity = velocity;
    }

    private void SetTrailEmission(bool shouldEmit, bool clear = false)
    {
        if (trailRenderer == null)
        {
            return;
        }

        trailRenderer.emitting = shouldEmit;

        if (clear)
        {
            trailRenderer.Clear();
        }
    }

    private void SetCollidersEnabled(bool enabled)
    {
        foreach (Collider ballCollider in ballColliders)
        {
            ballCollider.enabled = enabled;
        }
    }
}
