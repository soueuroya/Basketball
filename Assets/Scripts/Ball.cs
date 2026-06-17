using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Ball : MonoBehaviour
{
    private static readonly List<Ball> activeBalls = new();

    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField, Min(0f)] private float trailSpeedThreshold = 8f;

    [Header("Collision Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField, MinMaxSlider(0f, 1f)]
    private Vector2 randomVolumeModifier = new Vector2(0.85f, 1f);
    [SerializeField, MinMaxSlider(0.5f, 1.5f)]
    private Vector2 randomPitch = new Vector2(0.92f, 1.08f);
    [SerializeField, MinMaxSlider(0f, 0.2f)]
    private Vector2 randomStartTime = new Vector2(0f, 0.03f);
    [SerializeField, Min(0f)] private float minimumImpactSpeed = 0.5f;
    [SerializeField, Min(0.01f)] private float fullVolumeImpactSpeed = 8f;
    [SerializeField, Min(0f)] private float impactSoundCooldown = 0.06f;

    private Rigidbody ballRigidbody;
    private Collider[] ballColliders;
    private Transform holdPoint;
    private float nextImpactSoundTime;
    private bool isTrailEmitting;

    public bool IsHeld { get; private set; }
    public static IReadOnlyList<Ball> ActiveBalls => activeBalls;

    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody>();
        ballColliders = GetComponentsInChildren<Collider>();

        if (trailRenderer == null)
        {
            trailRenderer = GetComponentInChildren<TrailRenderer>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        SetTrailEmission(false, true, true);
    }

    private void OnEnable()
    {
        if (!activeBalls.Contains(this))
        {
            activeBalls.Add(this);
        }
    }

    private void OnDisable()
    {
        activeBalls.Remove(this);
    }

    private void Update()
    {
        if (IsHeld)
        {
            SetTrailEmission(false, true, true);
            return;
        }

        bool shouldEmit =
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
        SetTrailEmission(false, true, true);

        transform.SetParent(holdPoint, false);
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public void Release(Vector3 velocity, Vector3 angularVelocity)
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
        ballRigidbody.angularVelocity = angularVelocity;
        SetTrailEmission(false, true);
    }

    public void AttractTowards(Vector3 target, float acceleration, float maxSpeed)
    {
        if (IsHeld || ballRigidbody.isKinematic)
        {
            return;
        }

        Vector3 direction = target - transform.position;

        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        ballRigidbody.AddForce(
            direction.normalized * acceleration,
            ForceMode.Acceleration);
        ballRigidbody.linearVelocity = Vector3.ClampMagnitude(
            ballRigidbody.linearVelocity,
            maxSpeed);
    }

    public Vector3 ClosestPoint(Vector3 position)
    {
        return ballColliders.Length > 0
            ? ballColliders[0].ClosestPoint(position)
            : transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsHeld ||
            audioSource == null ||
            audioSource.clip == null ||
            Time.unscaledTime < nextImpactSoundTime ||
            ShouldIgnoreCollisionAudio(collision.collider))
        {
            return;
        }

        float impactSpeed = collision.relativeVelocity.magnitude;

        if (impactSpeed < minimumImpactSpeed)
        {
            return;
        }

        float collisionVolume = Mathf.InverseLerp(
            minimumImpactSpeed,
            fullVolumeImpactSpeed,
            impactSpeed);
        float randomModifier = Random.Range(
            randomVolumeModifier.x,
            randomVolumeModifier.y);

        nextImpactSoundTime = Time.unscaledTime + impactSoundCooldown;
        audioSource.pitch = Random.Range(randomPitch.x, randomPitch.y);
        audioSource.volume = collisionVolume * randomModifier;
        audioSource.time = Mathf.Min(
            Random.Range(randomStartTime.x, randomStartTime.y),
            Mathf.Max(0f, audioSource.clip.length - 0.01f));
        audioSource.Play();
    }

    private bool ShouldIgnoreCollisionAudio(Collider other)
    {
        if (other.GetComponentInParent<Ball>() != null)
        {
            return true;
        }

        return other.GetComponentInParent<FirstPersonPlayerController>() != null;
    }

    private void SetTrailEmission(
        bool shouldEmit,
        bool clear = false,
        bool hideRenderer = false)
    {
        if (trailRenderer == null)
        {
            return;
        }

        if (!hideRenderer && !trailRenderer.enabled)
        {
            trailRenderer.enabled = true;
        }

        if (isTrailEmitting != shouldEmit)
        {
            isTrailEmitting = shouldEmit;
            trailRenderer.emitting = shouldEmit;
        }

        if (clear)
        {
            trailRenderer.Clear();
        }

        if (hideRenderer && trailRenderer.enabled)
        {
            trailRenderer.enabled = false;
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
