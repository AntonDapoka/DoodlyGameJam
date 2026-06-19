using UnityEngine;

public class PushModule : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float maxSpeed = 12f;
    [SerializeField] private float pushCooldown = 0.2f;

    private Rigidbody _rigidbody;
    private GroundingEvaluator _grounding;

    private bool _requested;
    private float _cooldownTimer;

    public float CurrentSpeed { get; private set; }

    public void Initialize(
        SkateboardMovementInteractorScript controller,
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

        if (!_requested) return;
        _requested = false;

        if (!_grounding.IsGrounded || _cooldownTimer > 0f) return;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 direction = forward;
        if (direction.sqrMagnitude < 0.001f) return;

        ApplyAcceleration(direction);
        _cooldownTimer = pushCooldown;
    }

    private void ApplyAcceleration(Vector3 direction)
    {
        Vector3 velocity = _rigidbody.velocity;
        Vector3 horizontalVelocity = new(velocity.x, 0f, velocity.z);
        float speed = horizontalVelocity.magnitude;

        if (speed >= maxSpeed) return;

        float speedFactor = 1f - (speed / maxSpeed);
        Vector3 force = direction * acceleration * speedFactor;
        _rigidbody.AddForce(force, ForceMode.Acceleration);
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