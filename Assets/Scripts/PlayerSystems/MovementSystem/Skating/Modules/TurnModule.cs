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
    [Tooltip("Visually align the board with the ground surface while keeping control over limits.")]
    [SerializeField] private bool alignToSurface = true;
    [SerializeField] private float maxPitch = 35f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxRoll = 35f;
    [SerializeField] private float minRoll = -35f;
    [SerializeField, Range(0f, 2f)] private float pitchSensitivity = 1f;
    [SerializeField, Range(0f, 2f)] private float rollSensitivity = 1f;
    [SerializeField] private float tiltSmoothSpeed = 8f;

    private SkateboardMovementInteractorScript _controller;
    private GroundingEvaluator _grounding;
    private Rigidbody _rigidbody;

    public float TurnInput { private get; set; }

    private float _currentPitch;
    private float _currentRoll;

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

        _currentPitch = Mathf.DeltaAngle(0f, _controller.transform.eulerAngles.x);
        _currentRoll = Mathf.DeltaAngle(0f, _controller.transform.eulerAngles.z);
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
        float currentYaw = _controller.transform.eulerAngles.y;
        float newYaw = currentYaw + yawDelta;

        float targetPitch = 0f;
        float targetRoll = 0f;

        if (alignToSurface)
        {
            // Build a forward vector that lies on the ground plane and preserves yaw.
            Vector3 desiredForward = Quaternion.Euler(0f, newYaw, 0f) * Vector3.forward;
            Vector3 projectedForward = Vector3.ProjectOnPlane(desiredForward, _grounding.GroundNormal);

            if (projectedForward.sqrMagnitude < 0.0001f)
                projectedForward = desiredForward;

            projectedForward.Normalize();

            // This rotation has the board's up axis aligned with the surface normal.
            Quaternion targetRotation = Quaternion.LookRotation(projectedForward, _grounding.GroundNormal);
            Vector3 targetEuler = targetRotation.eulerAngles;

            targetPitch = Mathf.DeltaAngle(0f, targetEuler.x) * pitchSensitivity;
            targetRoll = Mathf.DeltaAngle(0f, targetEuler.z) * rollSensitivity;

            // Clamp to designer-defined limits so the board never clips or over-rotates.
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
            targetRoll = Mathf.Clamp(targetRoll, minRoll, maxRoll);
        }

        _currentPitch = Mathf.MoveTowardsAngle(_currentPitch, targetPitch, tiltSmoothSpeed * deltaTime);
        _currentRoll = Mathf.MoveTowardsAngle(_currentRoll, targetRoll, tiltSmoothSpeed * deltaTime);

        // Compose yaw (existing steering), pitch (surface slope forward/back)
        // and roll (surface slope left/right).
        _controller.transform.rotation = Quaternion.Euler(_currentPitch, newYaw, _currentRoll);
    }

    private float CalculateTurnAngle(float deltaTime)
    {
        Vector3 velocityHorizontal = GetHorizontalVelocity();

        float speed = velocityHorizontal.magnitude;
        float normalizedSpeed = Mathf.Clamp01(speed/turnSpeedMax);
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
