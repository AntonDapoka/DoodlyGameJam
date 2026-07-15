using System.Collections.Generic;
using UnityEngine;

public class TrickModule : MonoBehaviour
{
    [Header("Definitions")]
    [SerializeField] private List<TrickDefinition> trickDefinitions;

    [Header("Visuals")]
    [SerializeField] private SkateBoardSpriteManager spriteManager;

    private Rigidbody _rigidbody;
    private PushModule _push;
    private JumpModule _jump;
    private GroundingEvaluator _grounding;
    private Camera _camera;
    private Transform _controllerTransform;

    private Vector3 _boardForwardAtAirStart;
    private Vector3 _cameraForwardAtAirStart;
    private float _previousYaw;
    private float _cumulativeYaw;
    private bool _wasAirborne;
    private readonly HashSet<TrickType> _executedBaseThisJump = new();
    private readonly HashSet<TrickType> _executedInAirThisAirTime = new();
    private readonly HashSet<TrickType> _executedOnLandingThisAirTime = new();

    public float CumulativeYaw => _cumulativeYaw;
    public SkateBoardSpriteManager SpriteManager => spriteManager;
    public Transform ControllerTransform => _controllerTransform;

    public event System.Action<TrickDefinition> OnTrickExecuted;

    public void Initialize(
        SkateboardMovementInteractorScript controller,
        PushModule push,
        JumpModule jump,
        GroundingEvaluator grounding,
        Rigidbody rigidbody,
        Camera camera)
    {
        _controllerTransform = controller.transform;
        _push = push;
        _jump = jump;
        _grounding = grounding;
        _rigidbody = rigidbody;
        _camera = camera;

        if (spriteManager == null)
            spriteManager = GetComponentInChildren<SkateBoardSpriteManager>();
    }

    public void Tick(float deltaTime)
    {
        bool isAirborne = _grounding != null && !_grounding.IsGrounded;

        if (isAirborne && !_wasAirborne)
            BeginAirTime();

        bool jumpRequested = _jump != null && _jump.JumpRequestedThisFrame;
        if (jumpRequested)
            _executedBaseThisJump.Clear();

        TrickContext baseContext = BuildContext(isAirborne);
        Evaluate(baseContext, ExecutionPhase.Base);

        if (isAirborne)
        {
            UpdateCumulativeYaw();
            TrickContext airContext = BuildContext(isAirborne);
            Evaluate(airContext, ExecutionPhase.InAir);
        }

        if (!isAirborne && _wasAirborne)
        {
            TrickContext landingContext = BuildContext(isAirborne);
            Evaluate(landingContext, ExecutionPhase.Landing);
            EndAirTime();
        }

        _wasAirborne = isAirborne;
    }

    private void BeginAirTime()
    {
        _boardForwardAtAirStart = _controllerTransform.forward;
        _cameraForwardAtAirStart = _camera != null
            ? _camera.transform.forward
            : _controllerTransform.forward;

        _cumulativeYaw = 0f;
        _previousYaw = _controllerTransform.eulerAngles.y;
        _executedInAirThisAirTime.Clear();
        _executedOnLandingThisAirTime.Clear();
    }

    private void EndAirTime()
    {
        _cumulativeYaw = 0f;
        _executedInAirThisAirTime.Clear();
        _executedOnLandingThisAirTime.Clear();
    }

    private void UpdateCumulativeYaw()
    {
        float currentYaw = _controllerTransform.eulerAngles.y;
        float delta = Mathf.DeltaAngle(_previousYaw, currentYaw);
        _cumulativeYaw += delta;
        _previousYaw = currentYaw;
    }

    private TrickContext BuildContext(bool isAirborne)
    {
        Vector3 boardForward = _push != null
            ? _push.GetCurrentFacingDirection()
            : _controllerTransform.forward;

        Vector3 velocity = _rigidbody.velocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;
        Vector3 currentVelocityDirection = currentSpeed > 0.01f
            ? horizontalVelocity.normalized
            : Vector3.zero;

        return new TrickContext(
            _rigidbody,
            _controllerTransform,
            _camera,
            boardForward,
            _controllerTransform.forward,
            _boardForwardAtAirStart,
            _cameraForwardAtAirStart,
            currentVelocityDirection,
            currentSpeed,
            _cumulativeYaw,
            _grounding != null && _grounding.IsGrounded,
            isAirborne,
            _jump != null && _jump.IsJumping,
            _jump != null && _jump.JumpRequestedThisFrame);
    }

    private void Evaluate(TrickContext context, ExecutionPhase phase)
    {
        HashSet<TrickType> executedSet = GetExecutedSet(phase);
        foreach (TrickDefinition definition in trickDefinitions)
        {
            if (executedSet.Contains(definition.TrickType)) continue;
            if (!definition.CanExecute(context, phase, this)) continue;

            definition.OnExecuted(this, phase);
            NotifyTrickExecuted(definition, phase);
        }
    }

    private HashSet<TrickType> GetExecutedSet(ExecutionPhase phase)
    {
        return phase switch
        {
            ExecutionPhase.InAir => _executedInAirThisAirTime,
            ExecutionPhase.Landing => _executedOnLandingThisAirTime,
            _ => _executedBaseThisJump
        };
    }

    public void ApplyImpulse(float force)
    {
        if (_push == null) return;
        _push.RequestTrickImpulse(force);
    }

    public void NotifyTrickExecuted(TrickDefinition definition, ExecutionPhase phase)
    {
        GetExecutedSet(phase).Add(definition.TrickType);
        OnTrickExecuted?.Invoke(definition);
    }
}
