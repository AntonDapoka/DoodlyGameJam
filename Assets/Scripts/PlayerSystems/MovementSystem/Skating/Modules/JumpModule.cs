using UnityEngine;

public class JumpModule : MonoBehaviour
{
    [SerializeField] private float jumpForce = 8f;
    [SerializeField, Range(0f, 1f)] private float groundNormalInfluence = 0.35f;

    [Tooltip("If true, vertical velocity is reset before applying jump force for consistent height.")]
    [SerializeField] private bool cancelVerticalVelocityOnJump = true;
    //vertical velocity is reset before applying jump force for consistent height
    [SerializeField] private bool clampUpwardVelocityBeforeJump = true;
    //If true, upward velocity is only cancelled when below Max Pre-Jump Upward Speed.")]

    [SerializeField] private float maxPreJumpUpwardSpeed = 0.5f;
    [SerializeField] private AnimationCurve jumpForceBySpeed = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("Timing")]
    [SerializeField] private float jumpCooldown = 0.18f;
    [SerializeField] private float coyoteTimeWindow = 0.12f;
    [SerializeField] private float jumpBufferWindow = 0.1f;
    [SerializeField, Range(0f, 1f)] private float minGroundNormalDot = 0.65f;
    [SerializeField] private bool allowCoyoteJump = true;

    [Header("Forward Boost")]
    [SerializeField] private bool addForwardBoost = true;
    [SerializeField] private float forwardBoostBase = 1.5f;
    [SerializeField] private float forwardBoostSpeedFactor = 0.15f;
    [SerializeField] private float maxForwardBoost = 5f;

    [Header("Ground Stick")]
    [SerializeField] private bool snapToGroundBeforeJump = true;
    [SerializeField] private float preJumpSnapDistance = 0.1f;

    private GroundingEvaluator _grounding;
    private Rigidbody _rigidbody;
    private Transform _controllerTransform;

    private bool _requested;
    private float _cooldownTimer;
    private float _coyoteTimer;
    private float _bufferTimer;

    public bool IsJumping { get; private set; }
    public bool JumpRequestedThisFrame { get; private set; }
    public float JumpForce => jumpForce;

    public void Initialize(GroundingEvaluator grounding, Rigidbody rigidbody, Transform controllerTransform)
    {
        _grounding = grounding;
        _rigidbody = rigidbody;
        _controllerTransform = controllerTransform;
    }

    public void RequestJump()
    {
        _requested = true;
    }

    public void Tick(float deltaTime)
    {
        JumpRequestedThisFrame = false;

        UpdateTimers(deltaTime);

        if (_requested)
        {
            _bufferTimer = jumpBufferWindow;
            _requested = false;
            JumpRequestedThisFrame = true;
        }

        bool groundSuitable = _grounding != null && _grounding.IsGrounded && _grounding.GroundNormal.y >= minGroundNormalDot;
        bool canUseCoyote = allowCoyoteJump && _coyoteTimer > 0f;
        bool hasBufferedInput = _bufferTimer > 0f;

        if (hasBufferedInput && _cooldownTimer <= 0f && (groundSuitable || canUseCoyote))
        {
            PerformJump();
            _bufferTimer = 0f;
            _cooldownTimer = jumpCooldown;
        }

        if (_grounding != null && _grounding.IsGrounded)
        {
            _coyoteTimer = coyoteTimeWindow;
            IsJumping = false;
        }
    }

    private void UpdateTimers(float deltaTime)
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= deltaTime;
        if (_coyoteTimer > 0f) _coyoteTimer -= deltaTime;
        if (_bufferTimer > 0f) _bufferTimer -= deltaTime;
    }

    private void PerformJump()
    {
        Vector3 velocity = _rigidbody.velocity;

        if (snapToGroundBeforeJump && _grounding != null && _grounding.GroundHit.distance > 0f)
        {
            float snap = Mathf.Min(preJumpSnapDistance, _grounding.GroundHit.distance);
            _rigidbody.MovePosition(_rigidbody.position + Vector3.down * snap);
        }

        if (cancelVerticalVelocityOnJump)
        {
            if (!clampUpwardVelocityBeforeJump || velocity.y < maxPreJumpUpwardSpeed) velocity.y = 0f;
        }

        Vector3 jumpDirection = Vector3.Lerp(Vector3.up, _grounding.GroundNormal, groundNormalInfluence).normalized;
        float force = jumpForce * EvaluateJumpForceCurve();
        velocity += jumpDirection * force;

        if (addForwardBoost)
        {
            Vector3 forward = _controllerTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
            {
                forward.Normalize();
                Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
                float speed = horizontal.magnitude;
                float boost = Mathf.Min(forwardBoostBase + speed * forwardBoostSpeedFactor, maxForwardBoost);
                velocity += forward * boost;
            }
        }

        _rigidbody.velocity = velocity;
        IsJumping = true;
    }

    private float EvaluateJumpForceCurve()
    {
        Vector3 horizontal = new Vector3(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);
        float normalizedSpeed = Mathf.Clamp01(horizontal.magnitude / Mathf.Max(maxForwardBoost, 0.01f));
        return jumpForceBySpeed.Evaluate(normalizedSpeed);
    }

    private void OnValidate()
    {
        if (jumpBufferWindow < 0f) jumpBufferWindow = 0f;
        if (coyoteTimeWindow < 0f) coyoteTimeWindow = 0f;
        if (jumpCooldown < 0f) jumpCooldown = 0f;
        if (minGroundNormalDot < 0f) minGroundNormalDot = 0f;
        if (minGroundNormalDot > 1f) minGroundNormalDot = 1f;
    }
}
