using UnityEngine;

public class TurnModule : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float turnTorque = 180f;
    [SerializeField] private float turnSpeedMax = 10f;
    [SerializeField] private float sideFriction = 5f;
    [SerializeField] private float driftBrake = 5f;

    [SerializeField] private AnimationCurve turnTorqueBySpeed;
    [SerializeField] private AnimationCurve driftByTurnInput;

    private SkateboardMovementInteractorScript _controller;
    private GroundingEvaluator _grounding;
    private Rigidbody _rigidbody;

    public float TurnInput { private get; set; }

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

        if (Mathf.Abs(TurnInput) >= 0.01f)
            _controller.transform.Rotate(0f, CalculateTurnAngle(deltaTime), 0f, Space.World);

        ApplyVelocity(deltaTime);
        ClampRoll();
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
        Vector3 velocityHorizontal = GetHorizontalVelocity();

        Vector3 forward = GetForward();
        Vector3 right = GetRight();

        float forwardSpeed = Vector3.Dot(velocityHorizontal, forward);
        float sideSpeed = Vector3.Dot(velocityHorizontal, right);

        float turnAmount = Mathf.Abs(TurnInput);

        float driftMultiplier = driftByTurnInput.Evaluate(turnAmount);
        float driftAmount = Mathf.Abs(sideSpeed);

        forwardSpeed = Mathf.Max(0f, forwardSpeed - driftAmount * driftBrake * driftMultiplier * deltaTime);
        sideSpeed = Mathf.MoveTowards(sideSpeed, 0f,sideFriction * driftMultiplier * deltaTime);

        Vector3 velocityHorizontalNew = forward * forwardSpeed + right * sideSpeed;
        _rigidbody.velocity = new(velocityHorizontalNew.x, velocity.y, velocityHorizontalNew.z);
    }

    private void ClampRoll()
    {
        Vector3 euler = _controller.transform.localEulerAngles;

        if (euler.z > 180f) euler.z -= 360f;
        euler.z = Mathf.Clamp(euler.z, -15f, 15f);
        _controller.transform.localEulerAngles = new(euler.x, euler.y, euler.z);
    }

    private Vector3 GetHorizontalVelocity()
    {
        Vector3 velocity = _rigidbody.velocity;
        velocity.y = 0f;
        return velocity;
    }

    private Vector3 GetForward()
    {
        Vector3 forward = _controller.transform.forward;
        forward.y = 0f;
        return forward.normalized;
    }

    private Vector3 GetRight()
    {
        Vector3 right = _controller.transform.right;
        right.y = 0f;
        return right.normalized;
    }
}