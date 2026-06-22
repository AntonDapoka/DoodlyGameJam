using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using Unity.Collections;

public class GrindModule : MonoBehaviour
{
    [SerializeField] private bool isGrinding;
    [Header("Settings")]
    [SerializeField] private bool requireAirborneToEnter = true;
    [SerializeField] private float minEntrySpeed = 2f;
    [SerializeField] private float heightOffset = 0f;
    [SerializeField] private float entrySnapStrength = 1f;
    [SerializeField] private float grindReentryCooldown = 0.5f;
    
    [Header("Speed")]
    [SerializeField] private float landingBoost = 3f;
    [SerializeField] private float exitBoost = 4f;
    [SerializeField] private float grindAcceleration = 8f;
    [SerializeField] private float maxGrindSpeed = 20f;
    [SerializeField] private float stopSpeedThreshold = 0.1f;
    [SerializeField] private float uphillResistance = 5f;
    [SerializeField] private float downhillAcceleration = 5f;
    [SerializeField] private float maxSubstepDistance = 0.2f; 
    //Maximum distance integrated in one substep. Lower values improve stability at high speed
    
    private Rigidbody _rigidbody;
    private Transform _controller;
    private GroundingEvaluator _grounding;
    private JumpModule _jumpModule;

    private SplineContainer _activeSplineContainer;
    private Spline _activeSpline;
    private NativeSpline _cachedNativeSpline;
    private bool _hasCachedNativeSpline;

    private float _distanceAlongSpline;
    private float _splineWorldLength;
    private float _grindSpeed;
    private float _directionSign;
    private float _yawOffset;
    private Vector3 _currentWorldTangent;
    private bool _exitRequested;
    private bool _exitWithJump;

    private bool _wasKinematic;
    private bool _wasUsingGravity;
    private float _reentryUnlockTime;

    public bool IsGrinding => isGrinding;
    public float CurrentGrindSpeed => _grindSpeed;
    public float DistanceAlongSpline => _distanceAlongSpline;
    public SplineContainer ActiveSplineContainer => _activeSplineContainer;

    public void Initialize(Rigidbody rigidbody, Transform controller, GroundingEvaluator grounding, JumpModule jumpModule)
    {
        _rigidbody = rigidbody;
        _controller = controller;
        _grounding = grounding;
        _jumpModule = jumpModule;
    }

    private void OnEnable()
    {
        Spline.Changed += OnSplineChanged;
    }

    private void OnDisable()
    {
        Spline.Changed -= OnSplineChanged;
        DisposeCachedNativeSpline();
    }
    
    public void RequestGrindExit(bool withJump = true)
    {
        _exitRequested = true;
        _exitWithJump = withJump;
    }

    public void OnGrindTriggerEntered(GrindableMarker marker)
    {
        if (isGrinding || Time.time < _reentryUnlockTime || marker == null) return;

        if (marker.SplineContainer == null)
            marker.Setup(marker.GetComponentInParent<SplineContainer>(), 0);

        if (marker.SplineContainer == null || (requireAirborneToEnter && _grounding != null && _grounding.IsGrounded))
            return;

        TryStartGrind(marker);
    }

    private void TryStartGrind(GrindableMarker marker)
    {
        Vector3 velocity = _rigidbody.velocity;
        Vector3 horizontalVelocity = new(velocity.x, 0f, velocity.z);
        float horizontalSpeed = horizontalVelocity.magnitude;

        if (horizontalSpeed < minEntrySpeed) return;

        _activeSplineContainer = marker.SplineContainer;
        _activeSpline = _activeSplineContainer[marker.SplineIndex];

        if (_activeSpline == null || _activeSpline.Count < 1)
        {
            ClearGrindState();
            return;
        }

        RebuildNativeSplineCache();

        if (!_hasCachedNativeSpline || !TryGetNearestPoint(_rigidbody.position, out float worldDistance, out Vector3 nearestWorldPos, out Vector3 worldTangent))
        {
            ClearGrindState();
            return;
        }

        float directionDot = Vector3.Dot(horizontalVelocity.normalized, worldTangent);
        _directionSign = directionDot >= 0f ? 1f : -1f;
        _currentWorldTangent = worldTangent * _directionSign;
        if (_currentWorldTangent.sqrMagnitude > 0.0001f) _currentWorldTangent.Normalize();

        float entrySpeedAlongRail = Vector3.Dot(horizontalVelocity, _currentWorldTangent);
        if (entrySpeedAlongRail < minEntrySpeed)
        {
            ClearGrindState();
            return;
        }

        _distanceAlongSpline = worldDistance;
        _grindSpeed = Mathf.Min(entrySpeedAlongRail + landingBoost, maxGrindSpeed);
        _grindSpeed = Mathf.Max(_grindSpeed, minEntrySpeed);

        CaptureYawOffset(_currentWorldTangent);

        isGrinding = true;
        _exitRequested = false;
        _exitWithJump = false;

        _wasKinematic = _rigidbody.isKinematic;
        _wasUsingGravity = _rigidbody.useGravity;

        // Zero velocity before making the body kinematic; assigning velocity/angularVelocity
        // on a kinematic body is not supported and logs a warning.
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;

        Vector3 targetPos = nearestWorldPos + Vector3.up * heightOffset;
        _rigidbody.MovePosition(Vector3.Lerp(_rigidbody.position, targetPos, entrySnapStrength));
        ApplyRotationFromSpline(_currentWorldTangent);
    }

    public void Tick(float deltaTime)
    {
        if (!isGrinding) return;

        if (_exitRequested)
        {
            ExitGrind(_exitWithJump);
            return;
        }

        ValidateNativeSplineCache();

        if (!_hasCachedNativeSpline)
        {
            ExitGrind(false);
            return;
        }

        float totalMove = _grindSpeed * deltaTime;
        int substeps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(totalMove) / Mathf.Max(maxSubstepDistance, 0.001f)));
        float substepDeltaTime = deltaTime / substeps;

        for (int i = 0; i < substeps; i++)
        {
            if (!EvaluateAtDistance(_distanceAlongSpline, out Vector3 worldTangent))
            {
                ExitAtCurrentPosition();
                return;
            }

            Vector3 movementTangent = worldTangent * _directionSign;
            if (movementTangent.sqrMagnitude > 0.0001f) movementTangent.Normalize();

            _currentWorldTangent = movementTangent;

            float slopeDot = Vector3.Dot(movementTangent, Vector3.up);
            float acceleration = grindAcceleration;

            if (slopeDot > 0.001f) acceleration -= uphillResistance * slopeDot;
            else if (slopeDot < -0.001f) acceleration += downhillAcceleration * Mathf.Abs(slopeDot);

            _grindSpeed += acceleration * substepDeltaTime;
            _grindSpeed = Mathf.Clamp(_grindSpeed, 0f, maxGrindSpeed);

            if (_grindSpeed <= stopSpeedThreshold)
            {
                ApplyPositionFromDistance();
                StopGrind();
                return;
            }

            float stepMove = _grindSpeed * substepDeltaTime;

            if (_activeSpline != null && _activeSpline.Closed)
            {
                _distanceAlongSpline += stepMove * _directionSign;
                _distanceAlongSpline = Mathf.Repeat(_distanceAlongSpline, _splineWorldLength);
            }
            else
            {
                if (_directionSign > 0f)
                {
                    float remainingToEnd = _splineWorldLength - _distanceAlongSpline;

                    if (stepMove >= remainingToEnd)
                    {
                        _distanceAlongSpline = _splineWorldLength;
                        ExitAtCurrentPosition();
                        return;
                    }
                }
                else
                {
                    if (stepMove >= _distanceAlongSpline)
                    {
                        _distanceAlongSpline = 0f;
                        ExitAtCurrentPosition();
                        return;
                    }
                }

                _distanceAlongSpline += stepMove * _directionSign;
            }
        }

        ApplyPositionFromDistance();
    }

    private void ApplyPositionFromDistance()
    {
        if (!_hasCachedNativeSpline) return;

        if (_activeSpline != null && _activeSpline.Closed)
            _distanceAlongSpline = Mathf.Repeat(_distanceAlongSpline, _splineWorldLength);

        float _normalizedTime = _cachedNativeSpline.ConvertIndexUnit(_distanceAlongSpline, PathIndexUnit.Distance, PathIndexUnit.Normalized);

        if (_activeSpline != null && _activeSpline.Closed) _normalizedTime = Mathf.Repeat(_normalizedTime, 1f);

        Vector3 worldPos = (Vector3)_cachedNativeSpline.EvaluatePosition(_normalizedTime);
        Vector3 worldTangent = (Vector3)_cachedNativeSpline.EvaluateTangent(_normalizedTime);
        Vector3 movementTangent = worldTangent * _directionSign;

        if (movementTangent.sqrMagnitude > 0.0001f) movementTangent.Normalize();

        _currentWorldTangent = movementTangent;

        Vector3 targetPosition = worldPos + Vector3.up * heightOffset;

        _rigidbody.MovePosition(targetPosition);
        ApplyRotationFromSpline(movementTangent);
    }

    private void CaptureYawOffset(Vector3 splineTangent)
    {
        _yawOffset = 0f;
        if (_controller == null) return;

        Vector3 horizontalTangent = Vector3.ProjectOnPlane(splineTangent, Vector3.up);
        Vector3 controllerForward = Vector3.ProjectOnPlane(_controller.forward, Vector3.up);

        if (horizontalTangent.sqrMagnitude < 0.0001f || controllerForward.sqrMagnitude < 0.0001f)
            return;

        _yawOffset = Vector3.SignedAngle(horizontalTangent.normalized, controllerForward.normalized, Vector3.up);
    }

    private void ApplyRotationFromSpline(Vector3 splineTangent)
    {
        if (_controller == null) return;

        Vector3 horizontalTangent = Vector3.ProjectOnPlane(splineTangent, Vector3.up);
        if (horizontalTangent.sqrMagnitude < 0.0001f) return;

        horizontalTangent.Normalize();
        Quaternion offset = Quaternion.AngleAxis(_yawOffset, Vector3.up);
        Vector3 targetForward = offset * horizontalTangent;

        if (targetForward.sqrMagnitude > 0.0001f)
            _controller.rotation = Quaternion.LookRotation(targetForward, Vector3.up);
    }

    private void ExitGrind(bool withJump)
    {
        if (!isGrinding) return;

        Vector3 exitHorizontal = _currentWorldTangent * (_grindSpeed + exitBoost);
        float verticalVelocity = 0f;
        if (withJump && _jumpModule != null) verticalVelocity = _jumpModule.JumpForce;

        _rigidbody.isKinematic = _wasKinematic;
        _rigidbody.useGravity = _wasUsingGravity;

        if (!_rigidbody.isKinematic)
        {
            _rigidbody.velocity = new(exitHorizontal.x, verticalVelocity, exitHorizontal.z);
            _rigidbody.angularVelocity = Vector3.zero;
        }

        _reentryUnlockTime = Time.time + grindReentryCooldown;
        DisposeCachedNativeSpline();
        ClearGrindState();
    }

    private void StopGrind()
    {
        if (!isGrinding) return;

        Vector3 horizontal = new(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);

        _rigidbody.isKinematic = _wasKinematic;
        _rigidbody.useGravity = _wasUsingGravity;

        if (!_rigidbody.isKinematic)
        {
            _rigidbody.velocity = new(horizontal.x, 0f, horizontal.z);
            _rigidbody.angularVelocity = Vector3.zero;
        }

        _reentryUnlockTime = Time.time + grindReentryCooldown;
        DisposeCachedNativeSpline();
        ClearGrindState();
    }

    private void ClearGrindState()
    {
        isGrinding = false;
        _activeSplineContainer = null;
        _activeSpline = null;
        _exitRequested = false;
        _exitWithJump = false;
        _yawOffset = 0f;
    }

    private void RebuildNativeSplineCache()
    {
        DisposeCachedNativeSpline();

        if (_activeSpline == null || _activeSplineContainer == null)  return;

        _cachedNativeSpline = new NativeSpline(_activeSpline, _activeSplineContainer.transform.localToWorldMatrix, Allocator.Persistent);
        _hasCachedNativeSpline = true;
        _splineWorldLength = _cachedNativeSpline.GetLength();

        if (_activeSplineContainer.transform.hasChanged)
            _activeSplineContainer.transform.hasChanged = false;
    }

    private void ValidateNativeSplineCache()
    {
        if (!_hasCachedNativeSpline || _activeSpline == null || _activeSplineContainer == null)
        {
            RebuildNativeSplineCache();
            return;
        }

        if (_activeSplineContainer.transform.hasChanged)
        {
            _activeSplineContainer.transform.hasChanged = false;
            RebuildNativeSplineCache();
        }
    }

    private void DisposeCachedNativeSpline()
    {
        if (!_hasCachedNativeSpline) return;

        _cachedNativeSpline.Dispose();
        _hasCachedNativeSpline = false;
    }

    private void OnSplineChanged(Spline spline, int knotIndex, SplineModification modification)
    {
        if (_activeSpline == spline && _hasCachedNativeSpline) RebuildNativeSplineCache();
    }

    private bool TryGetNearestPoint(Vector3 worldPosition, out float worldDistance, out Vector3 worldPos, out Vector3 worldTangent)
    {
        worldDistance = 0f;
        worldPos = default;
        worldTangent = Vector3.forward;

        if (!_hasCachedNativeSpline) return false;

        float distance = SplineUtility.GetNearestPoint(_cachedNativeSpline, worldPosition, out float3 nearestWorld, out float t);

        if (distance == float.PositiveInfinity) return false;

        worldPos = nearestWorld;
        worldTangent = (Vector3)_cachedNativeSpline.EvaluateTangent(t);
        if (worldTangent.sqrMagnitude > 0.0001f) worldTangent.Normalize();

        worldDistance = _cachedNativeSpline.ConvertIndexUnit(t, PathIndexUnit.Normalized, PathIndexUnit.Distance);
        return true;
    }

    private bool EvaluateAtDistance(float worldDistance, out Vector3 worldTangent)
    {
        worldTangent = Vector3.forward;

        if (!_hasCachedNativeSpline) return false;

        float normalizedTime = _cachedNativeSpline.ConvertIndexUnit(worldDistance, PathIndexUnit.Distance, PathIndexUnit.Normalized);
        worldTangent = (Vector3)_cachedNativeSpline.EvaluateTangent(normalizedTime);

        if (normalizedTime < 0 || normalizedTime > 1)
            Debug.LogWarning(
                $"Grind invalid t={normalizedTime}, dist={worldDistance}, len={_splineWorldLength}");

        if (worldTangent.sqrMagnitude > 0.0001f) worldTangent.Normalize();

        return true;
    }

    private void ExitAtCurrentPosition()
    {
        ApplyPositionFromDistance();
        ExitGrind(false);
    }

    private void OnValidate()
    {
        if (minEntrySpeed < 0f) minEntrySpeed = 0f;
        if (maxGrindSpeed < 0f) maxGrindSpeed = 0f;
        if (stopSpeedThreshold < 0f) stopSpeedThreshold = 0f;
        if (landingBoost < 0f) landingBoost = 0f;
        if (exitBoost < 0f) exitBoost = 0f;
        if (grindReentryCooldown < 0f) grindReentryCooldown = 0f;
        if (maxSubstepDistance < 0.001f) maxSubstepDistance = 0.001f;
    }
}
