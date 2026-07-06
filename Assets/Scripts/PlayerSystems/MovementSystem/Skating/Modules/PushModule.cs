using TMPro;
using UnityEngine;

public class PushModule : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float maxSpeed = 16f;
    [SerializeField] private float cruiseSpeed = 12f;
    [SerializeField] private float pushCooldown = 0.2f;
    [SerializeField] private bool isCruise = true;

    [Header("Speed-Based Push Force")]
    [Tooltip("X = normalized speed (0..1 maps to 0..target speed). Y = push force multiplier (0..1).")]
    [SerializeField] private AnimationCurve pushForceBySpeed = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Coasting")]
    [Tooltip("Continuous forward force applied near/above cruise speed to counter drag and maintain speed.")]
    [SerializeField] private float coastForce = 4f;
    [Tooltip("Fraction of cruiseSpeed at which coast force starts being applied.")]
    [SerializeField, Range(0f, 1f)] private float coastThreshold = 0.8f;

    [Header("Loop Assist")]
    [Tooltip("Adds extra forward force on walls/loops if the player drops below minimum speed.")]
    [SerializeField] private bool useLoopAssist = true;
    [SerializeField] private float minLoopSpeed = 6f;
    [SerializeField] private float loopAssistForce = 15f;
    [Tooltip("Loop assist only activates when the board's local up Y is at most this value. Ordinary uphill ramps have positive Y, so they are excluded. Walls have Y ~ 0, ceilings have Y < 0.")]
    [SerializeField, Range(-1f, 1f)] private float loopAssistMaxUpY = 0.05f;

    [SerializeField] private TextMeshProUGUI testSpeed;
    [SerializeField] private TextMeshProUGUI testCruise;

    private Rigidbody _rigidbody;
    private GroundingEvaluator _grounding;
    private Camera _camera;

    private bool _requested;
    private float _cooldownTimer;

    public float CurrentSpeed { get; private set; }

    public void Initialize(
        Rigidbody rigidbody,
        GroundingEvaluator grounding,
        Camera camera)
    {
        _rigidbody = rigidbody;
        _grounding = grounding;
        _camera = camera;
    }

    public void RequestPush()
    {
        _requested = true;
    }

    public void Tick(float deltaTime)
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= deltaTime;

        UpdateSpeed();

        if (_requested)
        {
            _requested = false;

            if (_grounding.IsGrounded && _cooldownTimer <= 0f)
            {
                Vector3 thrustDirection = GetThrustDirection();

                if (thrustDirection.sqrMagnitude >= 0.001f)
                {
                    ApplyAcceleration(thrustDirection);
                    _cooldownTimer = pushCooldown;
                }
            }
        }

        ApplyCoastForce();
        ApplyLoopAssist();
        testSpeed.text = CurrentSpeed.ToString();
        testCruise.text = isCruise ? "Yes" : "No";
    }

    private void ApplyAcceleration(Vector3 direction)
    {
        Vector3 velocity = _rigidbody.velocity;
        Vector3 horizontalVelocity = new(velocity.x, 0f, velocity.z);
        float speed = horizontalVelocity.magnitude;

        float targetSpeed = isCruise ? cruiseSpeed : maxSpeed;
        if (speed >= targetSpeed) return;

        float normalizedSpeed = Mathf.Clamp01(speed / Mathf.Max(targetSpeed, 0.001f));
        float multiplier = pushForceBySpeed.Evaluate(normalizedSpeed);
        Vector3 force = acceleration * multiplier * direction;

        _rigidbody.AddForce(force, ForceMode.Acceleration);
    }

    private void ApplyCoastForce()
    {
        if (!isCruise || _grounding == null || !_grounding.IsGrounded) return;

        Vector3 velocity = _rigidbody.velocity;
        Vector3 horizontalVelocity = new(velocity.x, 0f, velocity.z);
        float speed = horizontalVelocity.magnitude;

        float threshold = cruiseSpeed * coastThreshold;
        if (speed < threshold || speed >= maxSpeed) return;

        Vector3 thrustDirection = GetThrustDirection();
        if (thrustDirection.sqrMagnitude < 0.001f) return;

        _rigidbody.AddForce(coastForce * thrustDirection, ForceMode.Acceleration);
    }

    private void ApplyLoopAssist()
    {
        if (!useLoopAssist || _grounding == null || !_grounding.IsGrounded) return;

        // Ordinary ramps always have transform.up.y > 0. Loop assist should only help when the board
        // is on a wall (up.y ~ 0) or ceiling/overhang (up.y < 0), otherwise it creates an unnatural
        // uphill boost at the start of riding up a slope.
        if (transform.up.y > loopAssistMaxUpY) return;

        Vector3 groundVelocity = Vector3.ProjectOnPlane(_rigidbody.velocity, _grounding.GroundNormal);
        if (groundVelocity.magnitude >= minLoopSpeed) return;

        Vector3 thrustDirection = GetThrustDirection();
        Vector3 groundForward = Vector3.ProjectOnPlane(thrustDirection, _grounding.GroundNormal);
        if (groundForward.sqrMagnitude < 0.001f) return;

        // Don't assist if the player is moving substantially against the thrust direction.
        if (Vector3.Dot(groundVelocity, groundForward) < -0.1f) return;

        _rigidbody.AddForce(loopAssistForce * groundForward.normalized, ForceMode.Acceleration);
    }

    public Vector3 GetForward()
    {
        // Local forward of the tilted board (follows surface slope).
        return transform.forward;
    }

    private Vector3 GetThrustDirection()
    {
        if (_camera == null) return GetForward();

        // Project the camera's look direction onto the board's local plane (spanned by forward/up).
        // This keeps the comparison valid even when the board is vertical or upside-down.
        Vector3 cameraForward = ProjectOnBoardPlane(_camera.transform.forward);

        if (cameraForward.sqrMagnitude < 0.0001f)
            return GetForward();

        float dot = Vector3.Dot(cameraForward.normalized, transform.forward.normalized);
        return dot >= 0f ? transform.forward : -transform.forward;
    }

    private Vector3 ProjectOnBoardPlane(Vector3 vector)
    {
        Vector3 boardRight = transform.right;
        if (boardRight.sqrMagnitude < 0.0001f) return vector;

        Vector3 normal = boardRight.normalized;
        return vector - Vector3.Dot(vector, normal) * normal;
    }

    private void UpdateSpeed()
    {
        Vector3 velocity = _rigidbody.velocity;
        Vector3 horizontal = new(velocity.x, 0f, velocity.z);

        CurrentSpeed = horizontal.magnitude;

        if (CurrentSpeed > maxSpeed)
        {
            Vector3 limited = horizontal.normalized * maxSpeed;
            _rigidbody.velocity = new(limited.x, velocity.y, limited.z);
        }
    }

    private void OnValidate()
    {
        if (maxSpeed < 0f) maxSpeed = 0f;
        if (cruiseSpeed < 0f) cruiseSpeed = 0f;
        if (coastForce < 0f) coastForce = 0f;
        if (pushCooldown < 0f) pushCooldown = 0f;

        if (cruiseSpeed > maxSpeed) cruiseSpeed = maxSpeed;
    }
}
