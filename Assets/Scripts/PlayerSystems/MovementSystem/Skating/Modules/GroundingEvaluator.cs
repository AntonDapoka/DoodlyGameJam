using UnityEngine;

public class GroundingEvaluator : MonoBehaviour
{
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform frontGroundCheck;
    [SerializeField] private Transform backGroundCheck;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundDistance = 0.3f;
    [SerializeField] private float raycastExtraDistance = 0.3f;
    [SerializeField] private bool showGizmos = true;

    public bool IsGrounded;
    public Vector3 GroundNormal { get; private set; }
    public float GroundAngle { get; private set; }
    public RaycastHit GroundHit { get; private set; }

    public void Initialize()
    {
        GroundNormal = Vector3.up;
        ResetState();
    }

    public void Evaluate(float deltaTime)
    {
        if (groundCheck == null)
        {
            ResetState();
            return;
        }

        IsGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);

        if (!IsGrounded)
        {
            ResetState();
            return;
        }

        float maxDistance = groundDistance + raycastExtraDistance;

        Vector3 normalSum = Vector3.zero;
        int hitCount = 0;
        RaycastHit closestHit = default;
        float closestDistance = float.MaxValue;

        TryProbe(groundCheck, maxDistance, groundMask, ref normalSum, ref hitCount, ref closestHit, ref closestDistance);
        TryProbe(frontGroundCheck, maxDistance, groundMask, ref normalSum, ref hitCount, ref closestHit, ref closestDistance);
        TryProbe(backGroundCheck, maxDistance, groundMask, ref normalSum, ref hitCount, ref closestHit, ref closestDistance);

        if (hitCount > 0)
        {
            GroundNormal = normalSum.normalized;
            GroundAngle = Vector3.Angle(Vector3.up, GroundNormal);
            GroundHit = closestHit;
            return;
        }

        ResetState();
    }

    private void TryProbe(
        Transform probe,
        float maxDistance,
        LayerMask mask,
        ref Vector3 normalSum,
        ref int hitCount,
        ref RaycastHit closestHit,
        ref float closestDistance)
    {
        if (probe == null) return;

        Vector3 origin = probe.position;

        if (Physics.Raycast(origin, -transform.up, out RaycastHit hit, maxDistance, mask, QueryTriggerInteraction.Ignore) ||
            Physics.Raycast(origin, Vector3.down, out hit, maxDistance, mask, QueryTriggerInteraction.Ignore))
        {
            normalSum += hit.normal;
            hitCount++;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
            }
        }
    }

    private void ResetState()
    {
        IsGrounded = false;
        GroundNormal = Vector3.up;
        GroundAngle = 0f;
        GroundHit = default;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || groundCheck == null) return;

        float maxDistance = groundDistance + raycastExtraDistance;

        DrawProbeGizmos(groundCheck, maxDistance);
        DrawProbeGizmos(frontGroundCheck, maxDistance);
        DrawProbeGizmos(backGroundCheck, maxDistance);

        if (IsGrounded && GroundHit.collider != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(GroundHit.point, 0.04f);
            Gizmos.DrawLine(GroundHit.point, GroundHit.point + GroundNormal * 0.4f);
        }
    }

    private void DrawProbeGizmos(Transform probe, float maxDistance)
    {
        if (probe == null) return;

        Vector3 origin = probe.position;

        bool isMain = probe == groundCheck;

        Color rayColor = IsGrounded ? Color.cyan : Color.yellow;
        Color localColor = IsGrounded ? Color.green : Color.magenta;
        
        Gizmos.color = localColor;
        if (isMain) Gizmos.DrawWireSphere(origin, groundDistance);

        Gizmos.color = rayColor;
        Gizmos.DrawRay(origin, -transform.up * maxDistance);

        Gizmos.color = localColor;
        Gizmos.DrawRay(origin, Vector3.down * maxDistance);
    }
}
