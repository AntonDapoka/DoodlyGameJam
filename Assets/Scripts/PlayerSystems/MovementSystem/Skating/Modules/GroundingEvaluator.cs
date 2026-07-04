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
    }

    public void Evaluate(float deltaTime)
    {
        if (groundCheck == null)
        {
            ResetState();
            return;
        }

        LayerMask mask = groundMask;

        IsGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            mask,
            QueryTriggerInteraction.Ignore);

        if (!IsGrounded)
        {
            ResetState();
            return;
        }

        float maxDistance = groundDistance + raycastExtraDistance;

        // Average normals from multiple probes so seams and sharp transitions don't break grounding.
        Vector3 normalSum = Vector3.zero;
        Vector3 pointSum = Vector3.zero;
        int hitCount = 0;
        RaycastHit closestHit = default;
        float closestDistance = float.MaxValue;

        TryProbe(groundCheck, maxDistance, mask, ref normalSum, ref pointSum, ref hitCount, ref closestHit, ref closestDistance);
        TryProbe(frontGroundCheck, maxDistance, mask, ref normalSum, ref pointSum, ref hitCount, ref closestHit, ref closestDistance);
        TryProbe(backGroundCheck, maxDistance, mask, ref normalSum, ref pointSum, ref hitCount, ref closestHit, ref closestDistance);

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
        ref Vector3 pointSum,
        ref int hitCount,
        ref RaycastHit closestHit,
        ref float closestDistance)
    {
        if (probe == null) return;

        Vector3 origin = probe.position;

        // Try board-relative down first (correct when upside-down), then world-down fallback.
        if (TryRaycast(origin, -transform.up, maxDistance, mask, out RaycastHit hit) ||
            TryRaycast(origin, Vector3.down, maxDistance, mask, out hit))
        {
            normalSum += hit.normal;
            pointSum += hit.point;
            hitCount++;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
            }
        }
    }

    private bool TryRaycast(Vector3 origin, Vector3 direction, float maxDistance, LayerMask mask, out RaycastHit hit)
    {
        return Physics.Raycast(origin, direction, out hit, maxDistance, mask, QueryTriggerInteraction.Ignore);
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
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        if (isMain)
            Gizmos.DrawWireSphere(origin, groundDistance);

        Gizmos.color = IsGrounded ? Color.cyan : Color.yellow;
        Gizmos.DrawRay(origin, -transform.up * maxDistance);

        Gizmos.color = IsGrounded ? Color.green : Color.magenta;
        Gizmos.DrawRay(origin, Vector3.down * maxDistance);
    }

    private void OnValidate()
    {
        if (groundMask == 0)
        {
            Debug.LogWarning(
                $"[{nameof(GroundingEvaluator)}] groundMask is not set on '{gameObject.name}'. Ground detection will not work.",
                this);
        }
    }
}
