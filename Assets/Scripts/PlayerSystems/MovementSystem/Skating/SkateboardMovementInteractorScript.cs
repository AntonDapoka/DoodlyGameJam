using UnityEngine;

[RequireComponent(typeof(GroundingEvaluator))]
[RequireComponent(typeof(VelocityHandler))]
[RequireComponent(typeof(PushModule))]
[RequireComponent(typeof(TurnModule))]
[RequireComponent(typeof(DragModule))]
[RequireComponent(typeof(AirControlModule))]
[RequireComponent(typeof(JumpModule))]
public class SkateboardMovementInteractorScript : MonoBehaviour, ISkateboardActor
{
    [SerializeField] private Transform physicsBody;

    public Transform PhysicsBodyTransform => physicsBody;
    public Rigidbody Rigidbody { get; private set; }
    public SphereCollider Collider { get; private set; }

    private GroundingEvaluator _grounding;
    private VelocityHandler _velocity;
    private PushModule _push;
    private TurnModule _turn;
    private DragModule _drag;
    private AirControlModule _air;
    private JumpModule _jump;

    private float _turnInput;
    private bool _reverseHeld;

    public bool IsGrounded => _grounding != null && _grounding.IsGrounded;

   public bool IsGrinding => throw new System.NotImplementedException();

   public float CurrentSpeed => throw new System.NotImplementedException();

   private void Reset()
    {
    
    }

    private void Awake()
    {
        physicsBody.localPosition = Vector3.zero;

        Rigidbody = physicsBody.GetComponent<Rigidbody>();
        Collider = physicsBody.GetComponent<SphereCollider>();

        _grounding = GetComponent<GroundingEvaluator>();
        _velocity = GetComponent<VelocityHandler>();
        _push = GetComponent<PushModule>();
        _turn = GetComponent<TurnModule>();
        _drag = GetComponent<DragModule>();
        //_air = GetComponent<AirControlModule>();
        _jump = GetComponent<JumpModule>();


        _grounding.Initialize();
        _push.Initialize(this, Rigidbody, _grounding, Collider);
        _turn.Initialize(this, _grounding, Rigidbody);
        //_drag.Initialize(this, _velocity, _grounding);
        //_air.Initialize(this, config, _grounding);
        //_jump.Initialize(, _grounding);

        Rigidbody.MoveRotation(transform.rotation);
    }

    private void FixedUpdate()
    {
        if (physicsBody == null || Rigidbody == null)
            return;

        float deltaTime = Time.fixedDeltaTime;

        float turnThisFrame = _turnInput;
        bool reverseThisFrame = _reverseHeld;

        _turnInput = 0f;
        _reverseHeld = false;

        _grounding.Evaluate(deltaTime);

        _push.Tick(deltaTime);
        _jump.Tick(deltaTime);

        _turn.TurnInput = turnThisFrame;
        _turn.Tick(deltaTime);
/*
        _air.TurnInput = turnThisFrame;
        _air.ReverseInput = reverseThisFrame;
        _air.Tick(deltaTime);*/
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
}
