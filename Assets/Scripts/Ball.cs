using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Ball : MonoBehaviour
{
    private Rigidbody ballRigidbody;
    private Collider[] ballColliders;
    private Transform holdPoint;

    public bool IsHeld { get; private set; }

    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody>();
        ballColliders = GetComponentsInChildren<Collider>();
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

    private void SetCollidersEnabled(bool enabled)
    {
        foreach (Collider ballCollider in ballColliders)
        {
            ballCollider.enabled = enabled;
        }
    }
}
