using UnityEngine;

public class TurnModule : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float turnTorque = 180f;
    [SerializeField] private float turnSpeedMax = 10f;
    [SerializeField] private float sideFriction = 5f;
    [SerializeField] private float speedDriftMin = 5f;
    [SerializeField] private AnimationCurve turnTorqueBySpeed;
    [SerializeField] private AnimationCurve driftByTurnInput;

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

        if (Mathf.Abs(TurnInput) >= 0.01f)
        {
            _controller.transform.Rotate(0f, CalculateTurnAngle(deltaTime), 0f, Space.World);
        }

        ApplyVelocity(deltaTime);
    }

    private void FixedUpdate()
    {
        UpdateIndicator();

        /*float deltaTime = Time.fixedDeltaTime;
        Vector3 velocity = _rigidbody.velocity;
        Vector3 velocityHorizontal = GetHorizontalVelocity(); //ball
        Vector3 right = GetRight();                           //skate

        float sideSpeed = Vector3.Dot(velocityHorizontal, right);
        sideSpeed = Mathf.MoveTowards(sideSpeed, 0f, sideFriction * deltaTime);

        Vector3 velocityHorizontalNew = velocity - right * sideSpeed;
        _rigidbody.velocity = new(velocityHorizontalNew.x, velocity.y, velocityHorizontalNew.z);*/
    }

    private float CalculateTurnAngle(float deltaTime)
    {
        Vector3 velocityHorizontal = GetHorizontalVelocity();

        float speed = velocityHorizontal.magnitude;
        float normalizedSpeed = Mathf.Clamp01(speed/turnSpeedMax);
        float multiplier = turnTorqueBySpeed.Evaluate(normalizedSpeed);

        return TurnInput * turnTorque * multiplier * deltaTime;
    }

    /*private void ApplyVelocity(float deltaTime)
    {
        Vector3 velocity = _rigidbody.velocity;
        Vector3 velocityHorizontal = GetHorizontalVelocity(); //ball

        Vector3 forward = GetForward();  //skate
        Vector3 right = GetRight();      //skate

        float forwardSpeed = Vector3.Dot(velocityHorizontal, forward);
        float turnAmount = Mathf.Abs(TurnInput);

        float driftMultiplier = driftByTurnInput.Evaluate(turnAmount);
        float driftAmount = Mathf.Abs(sideSpeed);*
        Debug.Log(forwardSpeed);
        //forwardSpeed = Mathf.Max(0f, forwardSpeed - driftBrake * deltaTime);
        //
        Vector3 velocityHorizontalNew = forward * forwardSpeed;// + right * sideSpeed;
        _rigidbody.velocity = new(velocityHorizontalNew.x, velocity.y, velocityHorizontalNew.z);
    }*/

    
    private void ApplyVelocity(float deltaTime)
    {
        Vector3 velocity = _rigidbody.velocity;
        Vector3 velocityHorizontal = GetHorizontalVelocity(); //ball

        Vector3 forward = GetForward();  //skate
        Vector3 right = GetRight();

        float forwardSpeed = Vector3.Dot(velocityHorizontal, forward);
        float sideSpeed = Vector3.Dot(velocityHorizontal, right);

        sideSpeed = Mathf.MoveTowards(sideSpeed, 0f, sideFriction * deltaTime);

        Vector3 velocityHorizontalNew = forward * forwardSpeed + (forwardSpeed <= speedDriftMin ? 1 : 0) * sideSpeed * right;
        _rigidbody.velocity = new(velocityHorizontalNew.x, velocity.y, velocityHorizontalNew.z);
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