using UnityEngine;


[RequireComponent(typeof(PushModule))]
[RequireComponent(typeof(TurnModule))]
[RequireComponent(typeof(JumpModule))]
[RequireComponent(typeof(GrindModule))]
[RequireComponent(typeof(AirControlModule))]
[RequireComponent(typeof(GroundingEvaluator))]
[RequireComponent(typeof(FrictionModule))]
[RequireComponent(typeof(TrickModule))]
public class SkateboardMovementInteractorScript : MonoBehaviour, ISkateboardActor
{
    [SerializeField] private Transform physicsBody;
    [SerializeField] private Camera cameraReference;

    public Transform PhysicsBodyTransform => physicsBody;
    public Rigidbody Rigidbody { get; private set; }
    public SphereCollider Collider { get; private set; }

    private GroundingEvaluator _grounding;
    private PushModule _push;
    private TurnModule _turn;
    private AirControlModule _air;
    private JumpModule _jump;
    private GrindModule _grind;
    private FrictionModule _friction;
    private TrickModule _trick;

    private float _turnInput;
    private bool _reverseHeld;
    private bool _forwardHeld;
    private bool _jumpHeld;

    public bool IsGrounded => _grounding != null && _grounding.IsGrounded;
    public bool IsGrinding => _grind != null && _grind.IsGrinding;
    public float CurrentSpeed => _push != null ? _push.CurrentSpeed : 0f;
    public float MaxSpeed => _push != null ? _push.MaxSpeed : 0f;

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
        _grind = GetComponent<GrindModule>();
        _friction = GetComponent<FrictionModule>();
        _trick = GetComponent<TrickModule>();

        if (cameraReference == null) cameraReference = Camera.main;

        _grounding.Initialize();
        _push.Initialize(Rigidbody, _grounding, cameraReference);
        _turn.Initialize(this, _grounding, Rigidbody, cameraReference);
        _air.Initialize(this, _grounding, Rigidbody, _jump);
        _jump.Initialize(_grounding, Rigidbody, transform);
        _grind.Initialize(Rigidbody, transform, _grounding, _jump);
        _friction.Initialize(this, _grounding, Rigidbody);
        _trick.Initialize(this, _push, _jump, _grounding, Rigidbody, cameraReference);

        physicsBody.gameObject.GetComponent<GrindTriggerRelay>().Initialize(_grind);

        Rigidbody.MoveRotation(transform.rotation);
    }

    private void FixedUpdate()
    {
        if (physicsBody == null || Rigidbody == null) return;

        float deltaTime = Time.fixedDeltaTime;
        float turnThisFrame = _turnInput;
        _turnInput = 0f;
        _grounding.Evaluate(deltaTime);

        if (_grind != null && _grind.IsGrinding)
        {
            _grind.Tick(deltaTime);
            return;
        }

        _jump.Tick(deltaTime);
        _turn.TurnInput = turnThisFrame;
        _turn.Tick(deltaTime);
        _trick.Tick(deltaTime);
        _push.Tick(deltaTime);
        _friction.Tick(deltaTime);
        _air.TurnInput = turnThisFrame;
        _air.ReverseInput = _reverseHeld;
        _air.ForwardInput = _forwardHeld;
        _air.JumpHeld = _jumpHeld;
        _air.Tick(deltaTime);
    }

    public void Push()
    {
        if (IsGrinding) return;
        _push.RequestPush();
    }

    public void PushBackward()
    {
        if (IsGrinding) return;
        _push.RequestPushBackward();
    }

    public void Turn(float direction)
    {
        if (IsGrinding) return;
        _turnInput = Mathf.Clamp(direction, -1f, 1f);
    }

    public void Jump()
    {
        if (_grind != null && _grind.IsGrinding)
        {
            _grind.RequestGrindExit(true);
            return;
        }

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
