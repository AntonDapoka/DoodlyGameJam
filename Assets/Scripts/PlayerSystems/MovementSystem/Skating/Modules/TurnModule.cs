using UnityEngine;

public class TurnModule : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float turnTorque = 180f;
    [SerializeField] private AnimationCurve turnTorqueBySpeed;

    [Header("Surface Tilt")]
    [SerializeField] private float groundNormalSmoothSpeed = 12f;
    [SerializeField] private float tiltSmoothSpeed = 360f;
    [SerializeField] private AnimationCurve tiltBySpeed = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("Indicators")]
    [SerializeField] private Transform indicatorMoving;
    [SerializeField] private float radiusIndicatorMoving = 1f;
    private float angleCurrent = 0f;
    [SerializeField] private Transform indicatorFacing;
    [SerializeField] private float radiusIndicatorMovingFacing = 1.1f;

    private SkateboardMovementInteractorScript _controller;
    private GroundingEvaluator _grounding;
    private Rigidbody _rigidbody;
    private Camera _camera;
    private Vector3 _smoothedGroundNormal;
    private const float EPSILON = 0.001f;

    public float TurnInput { private get; set; }

    public void Initialize(
        SkateboardMovementInteractorScript controller,
        GroundingEvaluator grounding,
        Rigidbody rigidbody,
        Camera camera)
    {
        _controller = controller;
        _grounding = grounding;
        _rigidbody = rigidbody;
        _camera = camera;
        _smoothedGroundNormal = Vector3.up;
    }

    public void Tick(float deltaTime)
    {
        if (!_grounding.IsGrounded) return;

        Vector3 planarVelocity = GetPlanarVelocity();
        float yawDelta = CalculateTurnAngle(planarVelocity, deltaTime);

        ApplyTilt(yawDelta, planarVelocity, deltaTime);
    }

    private void LateUpdate()
    {
        UpdateMovingIndicator();
        UpdateFacingIndicator();
    }

    private float CalculateTurnAngle(Vector3 planarVelocity, float deltaTime)
    {
        float speed = Mathf.Clamp01(planarVelocity.magnitude / _controller.MaxSpeed);
        return TurnInput * turnTorque * turnTorqueBySpeed.Evaluate(speed) * deltaTime;
    }

    private void ApplyTilt(float yawDelta, Vector3 planarVelocity, float deltaTime)
    {
        Quaternion rotation = ApplyYaw(_controller.transform.rotation, yawDelta);
        Vector3 normal = SmoothGroundNormal(deltaTime);
        Quaternion targetRotation = BuildGroundRotation(rotation, normal);
        RotateTowards(planarVelocity,rotation, targetRotation, deltaTime);
    }

    private Quaternion ApplyYaw(Quaternion rotation, float yawDelta)
    {
        if (Mathf.Abs(yawDelta) < EPSILON) return rotation;
        return Quaternion.AngleAxis(yawDelta, rotation * Vector3.up) * rotation;
    }

    private Vector3 SmoothGroundNormal(float deltaTime)
    {
        float t = 1f - Mathf.Exp(-groundNormalSmoothSpeed * deltaTime);
        _smoothedGroundNormal = Vector3.Slerp(_smoothedGroundNormal, _grounding.GroundNormal, t);
        return _smoothedGroundNormal;
    }

    private Quaternion BuildGroundRotation(Quaternion rotation, Vector3 normal)
    {
        Vector3 desiredForward = rotation * Vector3.forward;
        Vector3 projectedForward = Vector3.ProjectOnPlane(desiredForward, normal);

        if (projectedForward.sqrMagnitude < EPSILON) projectedForward = desiredForward;

        projectedForward.Normalize();
        return Quaternion.LookRotation(projectedForward, normal);
    }

    private void RotateTowards(Vector3 planarVelocity, Quaternion currentRotation, Quaternion targetRotation, float deltaTime)
    {
        float speed = planarVelocity.magnitude;
        float normalizedSpeed = Mathf.Clamp01(speed / Mathf.Max(_controller.MaxSpeed, EPSILON));
        float tiltMultiplier = tiltBySpeed.Evaluate(normalizedSpeed);

        _controller.transform.rotation = Quaternion.RotateTowards(currentRotation, targetRotation, tiltSmoothSpeed * tiltMultiplier * deltaTime);
    }

    private Vector3 GetPlanarVelocity()
    {
        return Vector3.ProjectOnPlane(_rigidbody.velocity, _grounding.GroundNormal);
    }

    private void UpdateMovingIndicator()
    {
        if (indicatorMoving == null) return;

        Vector3 velocity = GetPlanarVelocity();

        if (velocity.sqrMagnitude < EPSILON) return;

        float angleNew = Mathf.Atan2(velocity.z, velocity.x) * Mathf.Rad2Deg;
        angleCurrent = Mathf.LerpAngle(angleCurrent, angleNew, Time.deltaTime * 10f);
        Vector3 positionNew = new Vector3(Mathf.Cos(angleCurrent * Mathf.Deg2Rad), 0f, Mathf.Sin(angleCurrent * Mathf.Deg2Rad)) * radiusIndicatorMoving;

        indicatorMoving.position = transform.position + positionNew;
    }

    private void UpdateFacingIndicator()
    {
        if (indicatorFacing == null || _camera == null) return;

        Vector3 cameraForward = ProjectOnBoardPlane(_camera.transform.forward);

        if (cameraForward.sqrMagnitude >= EPSILON)
        {
            Vector3 forward = transform.forward;
            indicatorFacing.position = transform.position + Mathf.Sign(Vector3.Dot(cameraForward, forward)) * radiusIndicatorMovingFacing * forward;
        }
    }

    private Vector3 ProjectOnBoardPlane(Vector3 vector)
    {
        Vector3 boardRight = transform.right;
        if (boardRight.sqrMagnitude < EPSILON) return vector;

        Vector3 normal = boardRight.normalized;
        return vector - Vector3.Dot(vector, normal) * normal;
    }
}
