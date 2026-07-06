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
    [Tooltip("Adds extra forward force on steep slopes/loops if the player drops below minimum speed.")]
    [SerializeField] private bool useLoopAssist = true;
    [SerializeField] private float loopAssistAngleThreshold = 45f;
    [SerializeField] private float minLoopSpeed = 6f;
    [SerializeField] private float loopAssistForce = 15f;
    [Tooltip("Only apply loop assist when the ground normal's Y component is at most this value (0 = vertical wall, 1 = flat). Prevents an artificial boost on ordinary ramps.")]
    [SerializeField, Range(0f, 1f)] private float loopAssistMaxGroundNormalY = 0.5f;

    [SerializeField] private TextMeshProUGUI testSpeed;
    [SerializeField] private TextMeshProUGUI testCruise;

    private Rigidbody _rigidbody;
    private GroundingEvaluator _grounding;

    private bool _requested;
    private float _cooldownTimer;

    public float CurrentSpeed { get; private set; }

    public void Initialize(
        Rigidbody rigidbody,
        GroundingEvaluator grounding)
    {
        _rigidbody = rigidbody;
        _grounding = grounding;
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
                Vector3 forward = GetForward();

                if (forward.sqrMagnitude >= 0.001f)
                {
                    ApplyAcceleration(forward);
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

        Vector3 forward = GetForward();
        if (forward.sqrMagnitude < 0.001f) return;

        _rigidbody.AddForce(coastForce * forward, ForceMode.Acceleration);
    }

    private void ApplyLoopAssist()
    {
        if (!useLoopAssist || _grounding == null || !_grounding.IsGrounded) return;
        if (_grounding.GroundAngle < loopAssistAngleThreshold) return;

        // Avoid giving an unnatural boost on ordinary uphill ramps. Loop assist should only
        // help on near-vertical walls / loops where the surface normal is mostly horizontal or points down.
        if (_grounding.GroundNormal.y > loopAssistMaxGroundNormalY) return;

        Vector3 velocity = _rigidbody.velocity;
        Vector3 groundVelocity = Vector3.ProjectOnPlane(velocity, _grounding.GroundNormal);
        float speed = groundVelocity.magnitude;

        if (speed >= minLoopSpeed) return;

        Vector3 forward = GetForward();
        Vector3 groundForward = Vector3.ProjectOnPlane(forward, _grounding.GroundNormal);
        if (groundForward.sqrMagnitude < 0.001f) return;

        _rigidbody.AddForce(loopAssistForce * groundForward.normalized, ForceMode.Acceleration);
    }

    public Vector3 GetForward()
    {
        // Local forward of the tilted board (follows surface slope).
        return transform.forward;
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
