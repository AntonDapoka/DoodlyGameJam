using UnityEngine;

public class GroundingEvaluator : MonoBehaviour
{
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundDistance = 0.3f;
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

        if (IsGrounded)
        {
            Vector3 origin = groundCheck.position;
            Vector3 boardDown = -transform.up;
            float maxDistance = groundDistance + 0.25f;

            // Prefer the board-relative down direction so tilted surfaces are detected correctly.
            // Fall back to world down in case the board-relative ray misses (thin colliders, edges, etc.).
            if (TryGetGroundHit(origin, boardDown, maxDistance, mask, out RaycastHit hit) ||
                TryGetGroundHit(origin, Vector3.down, maxDistance, mask, out hit))
            {
                GroundHit = hit;
                GroundNormal = hit.normal;
                GroundAngle = Vector3.Angle(Vector3.up, hit.normal);
                return;
            }
        }

        ResetState();
    }

    private bool TryGetGroundHit(Vector3 origin, Vector3 direction, float maxDistance, LayerMask mask, out RaycastHit hit)
    {
        if (Physics.Raycast(origin, direction, out hit, maxDistance, mask, QueryTriggerInteraction.Ignore))
            return true;

        return false;
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

        Vector3 origin = groundCheck.position;
        Vector3 boardDown = -transform.up;
        float maxDistance = groundDistance + 0.25f;

        // Sphere check
        Gizmos.color = IsGrounded ? new Color(0f, 1f, 0f, 0.75f) : new Color(1f, 0f, 0f, 0.75f);
        Gizmos.DrawWireSphere(origin, groundDistance);

        // Board-relative ray
        Gizmos.color = IsGrounded ? Color.cyan : Color.yellow;
        Gizmos.DrawRay(origin, boardDown * maxDistance);

        // World down fallback ray
        Gizmos.color = IsGrounded ? Color.green : Color.magenta;
        Gizmos.DrawRay(origin, Vector3.down * maxDistance);

        // Hit normal and point
        if (IsGrounded && GroundHit.collider != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(GroundHit.point, 0.04f);
            Gizmos.DrawLine(GroundHit.point, GroundHit.point + GroundHit.normal * 0.4f);
        }
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
