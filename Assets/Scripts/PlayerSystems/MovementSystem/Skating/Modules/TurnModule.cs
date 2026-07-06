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
    [SerializeField] private float tiltSmoothSpeed = 360f;
    [Tooltip("Multiplier for tilt smoothing speed based on normalized horizontal speed.")]
    [SerializeField] private AnimationCurve tiltBySpeed = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("Ground Normal Smoothing")]
    [Tooltip("If true, the ground normal is smoothed before the board is tilted. Reduces jitter on uneven surfaces.")]
    [SerializeField] private bool smoothGroundNormal = true;
    [SerializeField] private float groundNormalSmoothSpeed = 12f;

    private SkateboardMovementInteractorScript _controller;
    private GroundingEvaluator _grounding;
    private Rigidbody _rigidbody;
    private Vector3 _smoothedGroundNormal;

    public float TurnInput { private get; set; }

    [Header("Indicator")]
    [SerializeField] private Transform indicator;
    [SerializeField] private float radius;
    private float angleCurrent = 0f;

    public void Initialize(
        SkateboardMovementInteractorScript controller,
        GroundingEvaluator grounding,
        Rigidbody rigidbody)
    {
        _controller = controller;
        _grounding = grounding;
        _rigidbody = rigidbody;
        _smoothedGroundNormal = Vector3.up;
    }

    public void Tick(float deltaTime)
    {
        if (!_grounding.IsGrounded) return;

        float yawDelta = 0f;
        if (Mathf.Abs(TurnInput) >= 0.01f) yawDelta = CalculateTurnAngle(deltaTime);

        ApplyTilt(yawDelta, deltaTime);
        ApplyVelocity(deltaTime);
    }

    private void FixedUpdate()
    {
        UpdateIndicator();
    }

    
    private float CalculateTurnAngle(float deltaTime)
    {
        Vector3 velocityHorizontal = GetHorizontalVelocity();

        float speed = velocityHorizontal.magnitude;
        float normalizedSpeed = Mathf.Clamp01(speed / turnSpeedMax);
        float multiplier = turnTorqueBySpeed.Evaluate(normalizedSpeed);

        return TurnInput * turnTorque * multiplier * deltaTime;
    }

    private void ApplyTilt(float yawDelta, float deltaTime)
    {
        Quaternion currentRotation = _controller.transform.rotation;

        if (Mathf.Abs(yawDelta) >= 0.001f)
            currentRotation = Quaternion.AngleAxis(yawDelta, currentRotation * Vector3.up) * currentRotation;

        Vector3 targetNormal = _grounding.GroundNormal;
        if (smoothGroundNormal)
        {
            _smoothedGroundNormal = Vector3.Slerp(_smoothedGroundNormal, targetNormal, groundNormalSmoothSpeed * deltaTime);
            targetNormal = _smoothedGroundNormal;
        }

        Vector3 desiredForward = currentRotation * Vector3.forward;
        Vector3 projectedForward = Vector3.ProjectOnPlane(desiredForward, targetNormal);

        if (projectedForward.sqrMagnitude < 0.0001f) projectedForward = desiredForward;

        projectedForward.Normalize();
        Quaternion targetRotation = Quaternion.LookRotation(projectedForward, targetNormal);

        float speed = GetHorizontalVelocity().magnitude;
        float normalizedSpeed = Mathf.Clamp01(speed / Mathf.Max(turnSpeedMax, 0.01f));
        float tiltMultiplier = tiltBySpeed.Evaluate(normalizedSpeed);

        _controller.transform.rotation = Quaternion.RotateTowards(currentRotation, targetRotation, tiltSmoothSpeed * tiltMultiplier * deltaTime);
     
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
        velocity.y = 0f; //?
        return velocity;
    }

    private Vector3 GetRight()
    {
        Vector3 right = _controller.transform.right;
        //right.y = 0f; //?
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
