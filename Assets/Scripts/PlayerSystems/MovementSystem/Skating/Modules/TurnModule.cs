using UnityEngine;

public class TurnModule : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float turnTorque = 180f;
    [SerializeField] private float turnSpeedMax = 10f;
    [SerializeField] private float sideFriction = 5f;
    [SerializeField] private AnimationCurve turnTorqueBySpeed;
    [SerializeField] private AnimationCurve driftByTurnInput;

    [Header("Surface Tilt")]
    [Tooltip("Visually align the board with the ground surface.")]
    [SerializeField] private bool alignToSurface = true;
    [Tooltip("Max tilt speed in degrees per second when following the surface.")]
    [SerializeField] private float tiltSmoothSpeed = 360f;

    private SkateboardMovementInteractorScript _controller;
    private GroundingEvaluator _grounding;
    private Rigidbody _rigidbody;

    public float TurnInput { private get; set; }

    [Header("Delete me")]
    [SerializeField] private Transform indicator;
    [SerializeField] private float radius;
    [SerializeField] private float angleCurrent = 0f;

    public void Initialize(
        SkateboardMovementInteractorScript controller,
        GroundingEvaluator grounding,
        Rigidbody rigidbody)
    {
        _controller = controller;
        _grounding = grounding;
        _rigidbody = rigidbody;
    }

    public void Tick(float deltaTime)
    {
        if (!_grounding.IsGrounded) return;

        float yawDelta = 0f;
        if (Mathf.Abs(TurnInput) >= 0.01f)
            yawDelta = CalculateTurnAngle(deltaTime);

        ApplyTilt(yawDelta, deltaTime);
        ApplyVelocity(deltaTime);
    }

    private void FixedUpdate()
    {
        UpdateIndicator();
    }

    private void ApplyTilt(float yawDelta, float deltaTime)
    {
        Quaternion currentRotation = _controller.transform.rotation;

        // Apply yaw around the board's local up axis so turning works on walls/ceilings too.
        if (Mathf.Abs(yawDelta) >= 0.001f)
        {
            currentRotation = Quaternion.AngleAxis(yawDelta, currentRotation * Vector3.up) * currentRotation;
        }

        if (!alignToSurface)
        {
            _controller.transform.rotation = currentRotation;
            return;
        }

        // Build a rotation whose up axis matches the surface normal while preserving yaw.
        Vector3 desiredForward = currentRotation * Vector3.forward;
        Vector3 projectedForward = Vector3.ProjectOnPlane(desiredForward, _grounding.GroundNormal);

        if (projectedForward.sqrMagnitude < 0.0001f)
            projectedForward = desiredForward;

        projectedForward.Normalize();

        Quaternion targetRotation = Quaternion.LookRotation(projectedForward, _grounding.GroundNormal);

        _controller.transform.rotation = Quaternion.RotateTowards(
            currentRotation,
            targetRotation,
            tiltSmoothSpeed * deltaTime);
        
    }

    private float CalculateTurnAngle(float deltaTime)
    {
        Vector3 velocityHorizontal = GetHorizontalVelocity();

        float speed = velocityHorizontal.magnitude;
        float normalizedSpeed = Mathf.Clamp01(speed / turnSpeedMax);
        float multiplier = turnTorqueBySpeed.Evaluate(normalizedSpeed);

        return TurnInput * turnTorque * multiplier * deltaTime;
    }

    private void ApplyVelocity(float deltaTime)
    {
        Vector3 velocity = _rigidbody.velocity;
        Vector3 sideVelocity = Vector3.Project(GetHorizontalVelocity(), GetRight());

        velocity -= deltaTime * sideFriction * sideVelocity;
        _rigidbody.velocity = velocity;
    }

    private Vector3 GetHorizontalVelocity()
    {
        Vector3 velocity = _rigidbody.velocity;
        //velocity.y = 0f;
        return velocity;
    }

    private Vector3 GetRight()
    {
        Vector3 right = _controller.transform.right;
        //right.y = 0f;
        return right.normalized;
    }

    private void UpdateIndicator()
    {
        Vector3 velocityHorizontal = GetHorizontalVelocity();
        velocityHorizontal.Normalize();

        float angleNew = Mathf.Atan2(velocityHorizontal.z, velocityHorizontal.x) * Mathf.Rad2Deg;
        angleCurrent = Mathf.LerpAngle(angleCurrent, angleNew, Time.deltaTime * 10f);
        Vector3 positionNew = new Vector3(Mathf.Cos(angleCurrent * Mathf.Deg2Rad), 0f, Mathf.Sin(angleCurrent * Mathf.Deg2Rad)) * radius;

        indicator.position = transform.position + positionNew;
    }
}
