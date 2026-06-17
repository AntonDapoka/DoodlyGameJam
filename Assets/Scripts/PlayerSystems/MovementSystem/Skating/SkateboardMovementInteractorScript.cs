using UnityEngine;

[RequireComponent(typeof(GroundingEvaluator))]
[RequireComponent(typeof(PushModule))]
[RequireComponent(typeof(TurnModule))]
[RequireComponent(typeof(AirControlModule))]
[RequireComponent(typeof(JumpModule))]
public class SkateboardMovementInteractorScript : MonoBehaviour, ISkateboardActor
{
    [SerializeField] private Transform physicsBody;

    public Transform PhysicsBodyTransform => physicsBody;
    public Rigidbody Rigidbody { get; private set; }
    public SphereCollider Collider { get; private set; }

    private GroundingEvaluator _grounding;
    private PushModule _push;
    private TurnModule _turn;
    private AirControlModule _air;
    private JumpModule _jump;

    private float _turnInput;
    private bool _reverseHeld;
    private bool _forwardHeld;
    private bool _jumpHeld;

    public bool IsGrounded => _grounding != null && _grounding.IsGrounded;
    public bool IsGrinding => false;
    public float CurrentSpeed => _push != null ? _push.CurrentSpeed : 0f;

    private void Awake()
    {
        physicsBody.localPosition = Vector3.zero;

        Rigidbody = physicsBody.GetComponent<Rigidbody>();
        Collider = physicsBody.GetComponent<SphereCollider>();

        _grounding = GetComponent<GroundingEvaluator>();
        _push = GetComponent<PushModule>();
        _turn = GetComponent<TurnModule>();
        _air = GetComponent<AirControlModule>();
        _jump = GetComponent<JumpModule>();

        _grounding.Initialize();
        _push.Initialize(this, Rigidbody, _grounding, Collider);
        _turn.Initialize(this, _grounding, Rigidbody);
        _air.Initialize(this, _grounding, Rigidbody, _jump);
        _jump.Initialize(_grounding, Rigidbody, transform);

        Rigidbody.MoveRotation(transform.rotation);
    }

    private void FixedUpdate()
    {
        if (physicsBody == null || Rigidbody == null) return;

        float deltaTime = Time.fixedDeltaTime;

        float turnThisFrame = _turnInput;
        _turnInput = 0f;

        _grounding.Evaluate(deltaTime);
        _push.Tick(deltaTime);
        _jump.Tick(deltaTime);
        _turn.TurnInput = turnThisFrame;
        _turn.Tick(deltaTime);
        _air.TurnInput = turnThisFrame;
        _air.ReverseInput = _reverseHeld;
        _air.ForwardInput = _forwardHeld;
        _air.JumpHeld = _jumpHeld;
        _air.Tick(deltaTime);
    }

    public void Push()
    {
        _push.RequestPush();
    }

    public void Turn(float direction)
    {
        _turnInput = Mathf.Clamp(direction, -1f, 1f);
    }

    public void Jump()
    {
        _jump.RequestJump();
    }

    public void SetForwardHeld(bool held)
    {
        _forwardHeld = held;
    }

    public void SetReverseHeld(bool held)
    {
        _reverseHeld = held;
    }

    public void SetJumpHeld(bool held)
    {
        _jumpHeld = held;
    }
}
