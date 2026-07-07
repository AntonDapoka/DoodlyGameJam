using TMPro;
using UnityEngine;

public class PushModule : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float maxSpeed = 16f;
    [SerializeField] private float pushCooldown = 0.2f;
    [SerializeField] private AnimationCurve pushForceBySpeed = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    //X = normalized speed (0..1 maps to 0..target speed). Y = push force multiplier (0..1).

    [Header("Debug")]
    [SerializeField] private TextMeshProUGUI testSpeed;

    private Rigidbody _rigidbody;
    private GroundingEvaluator _grounding;
    private Camera _camera;
    private bool _requested;
    private float _cooldownTimer;

    public float CurrentSpeed { get; private set; }
    public float MaxSpeed => maxSpeed;

    private const float EPSILON = 0.0001f;

    public void Initialize( Rigidbody rigidbody, GroundingEvaluator grounding, Camera camera)
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
        #if UNITY_EDITOR
        testSpeed.text = CurrentSpeed.ToString("F2");
        #endif

        if (_cooldownTimer > 0f) _cooldownTimer -= deltaTime;

        UpdateSpeed();

        if (!_requested) return;

        _requested = false;

        if (!_grounding.IsGrounded || _cooldownTimer > 0f) return;

        Vector3 direction = GetThrustDirection();

        if (direction.sqrMagnitude < EPSILON) return;

        ApplyAcceleration(direction);
        _cooldownTimer = pushCooldown;
    }

    private void ApplyAcceleration(Vector3 direction)
    {
        float speed = CurrentSpeed;
        float targetSpeed = maxSpeed;
        if (speed >= targetSpeed) return;

        float normalizedSpeed = Mathf.Clamp01(speed / Mathf.Max(targetSpeed, EPSILON));
        float multiplier = pushForceBySpeed.Evaluate(normalizedSpeed);

        _rigidbody.AddForce(acceleration * multiplier * direction, ForceMode.Acceleration);
    }

    private Vector3 GetThrustDirection()
    {
        Vector3 cameraForward = ProjectOnBoardPlane(_camera.transform.forward);

        if (cameraForward.sqrMagnitude < EPSILON) return transform.forward;
        cameraForward.Normalize();
        return Vector3.Dot(cameraForward, transform.forward) >= 0f ? transform.forward : -transform.forward;
    }

    private Vector3 ProjectOnBoardPlane(Vector3 vector)
    {
        Vector3 boardRight = transform.right;
        if (boardRight.sqrMagnitude < EPSILON) return vector;

        Vector3 normal = boardRight.normalized;
        return Vector3.ProjectOnPlane(vector, normal);
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
}
