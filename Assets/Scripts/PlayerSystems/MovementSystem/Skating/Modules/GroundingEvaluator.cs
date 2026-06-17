using UnityEngine;

public class GroundingEvaluator : MonoBehaviour
{
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundDistance = 0.3f;

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

        IsGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundMask,
            QueryTriggerInteraction.Ignore);

        if (IsGrounded)
        {
            if (Physics.Raycast(
                transform.position,
                Vector3.down,
                out RaycastHit hit,
                groundDistance + 0.1f,
                groundMask))
            {
                GroundHit = hit;
                GroundNormal = hit.normal;
                GroundAngle = Vector3.Angle(Vector3.up, hit.normal);
                return;
            }
        }

        ResetState();
    }

    private void ResetState()
    {
        IsGrounded = false;
        GroundNormal = Vector3.up;
        GroundAngle = 0f;
        GroundHit = default;
    }
}
