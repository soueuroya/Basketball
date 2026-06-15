using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HoopPassTrigger : MonoBehaviour
{
    public enum Zone
    {
        Top,
        Bottom
    }

    [SerializeField] private HoopPassChecker checker;
    [SerializeField] private Zone zone;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
        checker = GetComponentInParent<HoopPassChecker>();
    }

    private void Awake()
    {
        if (checker == null)
        {
            checker = GetComponentInParent<HoopPassChecker>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Ball ball = other.GetComponentInParent<Ball>();

        if (ball != null)
        {
            checker?.RegisterPass(ball, zone);
        }
    }
}
