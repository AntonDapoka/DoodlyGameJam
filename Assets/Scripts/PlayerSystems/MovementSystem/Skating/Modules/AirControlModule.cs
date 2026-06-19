using UnityEngine;

public class AirControlModule : MonoBehaviour
{
    [Header("Input Forces")]
    [SerializeField] private float airSideForce = 14f;
    [SerializeField] private float airBackForce = 8f;
    [SerializeField] private float airForwardForce = 6f;
    [SerializeField] private float maxAirHorizontalSpeed = 16f;

    [Header("Turning")]
    [SerializeField] private float airTurnTorque = 140f;
    [SerializeField] private float airTurnMaxSpeed = 8f;

    [Header("Air Physics")]
    [SerializeField] private float fallMultiplier = 2.2f;
    [SerializeField] private float lowJumpMultiplier = 1.8f; //variable jump height
    [SerializeField] private float maxDownwardSpeed = 40f;
    [SerializeField] private float airDrag = 0.5f;

    [Tooltip("If true, manual gravity enhancement is applied. Disable if using Rigidbody gravity only.")]
    [SerializeField] private bool applyEnhancedGravity = true;

    private SkateboardMovementInteractorScript _controller;
    private GroundingEvaluator _grounding;
    private Rigidbody _rigidbody;
    private JumpModule _jumpModule;

    public float TurnInput { private get; set; }
    public bool ReverseInput { private get; set; }
    public bool ForwardInput { private get; set; }
    public bool JumpHeld { private get; set; }

    public float AirTime { get; private set; }
    public bool IsAirborne => _grounding != null && !_grounding.IsGrounded;

    public void Initialize(
        SkateboardMovementInteractorScript controller,
        GroundingEvaluator grounding,
        Rigidbody rigidbody,
        JumpModule jumpModule)
    {
        _controller = controller;
        _grounding = grounding;
        _rigidbody = rigidbody;
        _jumpModule = jumpModule;
    }

    public void Tick(float deltaTime)
    {
        if (_grounding == null || _grounding.IsGrounded)
        {
            AirTime = 0f;
            return;
        }

        AirTime += deltaTime;

        ApplyAirControl(deltaTime);
        ApplyAirTurn(deltaTime);

        if (applyEnhancedGravity)
            ApplyEnhancedGravity(deltaTime);

        ApplyAirDrag(deltaTime);
    }

    private void ApplyAirControl(float deltaTime)
    {
        float turn = Mathf.Clamp(TurnInput, -1f, 1f);
        float reverse = ReverseInput ? -1f : 0f;
        float forward = ForwardInput ? 1f : 0f;

        if (Mathf.Abs(turn) < 0.01f && Mathf.Abs(reverse) < 0.01f && Mathf.Abs(forward) < 0.01f)
            return;

        Vector3 boardForward = _controller.transform.forward;
        Vector3 boardRight = _controller.transform.right;
        boardForward.y = 0f;
        boardRight.y = 0f;

        if (boardForward.sqrMagnitude > 0.0001f)
            boardForward.Normalize();
        else
            boardForward = Vector3.forward;

        if (boardRight.sqrMagnitude > 0.0001f)
            boardRight.Normalize();
        else
            boardRight = Vector3.right;

        Vector3 worldForward = Vector3.forward;
        Vector3 worldRight = Vector3.right;

        Vector3 inputForward = Vector3.Lerp(worldForward, boardForward, 0).normalized;
        Vector3 inputRight = Vector3.Lerp(worldRight, boardRight, 0).normalized;

        Vector3 force = inputRight * turn * airSideForce
                      + inputForward * reverse * airBackForce
                      + inputForward * forward * airForwardForce;

        _rigidbody.AddForce(force, ForceMode.Acceleration);

        Vector3 horizontal = new(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);
        if (horizontal.magnitude > maxAirHorizontalSpeed)
        {
            horizontal = horizontal.normalized * maxAirHorizontalSpeed;
            _rigidbody.velocity = new(horizontal.x, _rigidbody.velocity.y, horizontal.z);
        }
    }

    private void ApplyAirTurn(float deltaTime)
    {
        if (Mathf.Abs(TurnInput) < 0.01f)
            return;

        Vector3 horizontal = new(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);
        float speed = horizontal.magnitude;
        float normalizedSpeed = Mathf.Clamp01(speed / Mathf.Max(airTurnMaxSpeed, 0.01f));
        float speedFactor = Mathf.Lerp(1f, 1f, normalizedSpeed);

        _controller.transform.Rotate(0f, TurnInput * airTurnTorque * speedFactor * deltaTime, 0f, Space.World);
        //_rigidbody.transform.Rotate(0f, TurnInput * airTurnTorque * speedFactor * deltaTime, 0f, Space.World);
    }

    private void ApplyEnhancedGravity(float deltaTime)
    {
        if (_rigidbody.velocity.y < 0f)
        {
            _rigidbody.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1f) * deltaTime;
        }
        else if (_rigidbody.velocity.y > 0.01f)
        {
            bool jumpHeld = _jumpModule != null && _jumpModule.IsJumping && JumpHeld;
            if (!jumpHeld)
            {
                _rigidbody.velocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1f) * deltaTime;
            }
        }

        if (_rigidbody.velocity.y < -maxDownwardSpeed)
        {
            Vector3 velocity = _rigidbody.velocity;
            velocity.y = -maxDownwardSpeed;
            _rigidbody.velocity = velocity;
        }
    }

    private void ApplyAirDrag(float deltaTime)
    {
        Vector3 horizontal = new Vector3(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);
        horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, airDrag * deltaTime);
        _rigidbody.velocity = new Vector3(horizontal.x, _rigidbody.velocity.y, horizontal.z);
    }
    private void OnValidate()
    {
        if (fallMultiplier < 0f) fallMultiplier = 0f;
        if (lowJumpMultiplier < 0f) lowJumpMultiplier = 0f;
        if (maxDownwardSpeed < 0f) maxDownwardSpeed = 0f;
        if (airDrag < 0f) airDrag = 0f;
        if (maxAirHorizontalSpeed < 0f) maxAirHorizontalSpeed = 0f;
    }
}
