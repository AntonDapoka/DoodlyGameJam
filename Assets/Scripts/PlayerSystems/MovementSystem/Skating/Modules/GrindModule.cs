using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using Unity.Collections;

public class GrindModule : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("If true, grinding can only start while the player is airborne.")]
    [SerializeField] private bool requireAirborneToEnter = true;

    [Tooltip("Minimum horizontal speed required to start grinding.")]
    [SerializeField] private float minEntrySpeed = 2f;

    [Header("Entry")]
    [Tooltip("Instant speed bonus applied when successfully latching onto a rail.")]
    [SerializeField] private float landingBoost = 3f;

    [Tooltip("Vertical offset applied to the player position while grinding.")]
    [SerializeField] private float heightOffset = 0f;

    [Tooltip("How quickly the player position snaps to the rail height on entry.")]
    [SerializeField] private float entrySnapStrength = 1f;

    [Header("Grind Movement")]
    [Tooltip("Constant acceleration applied while grinding.")]
    [SerializeField] private float grindAcceleration = 8f;

    [Tooltip("Maximum speed achievable while grinding.")]
    [SerializeField] private float maxGrindSpeed = 20f;

    [Tooltip("Speed below which the player automatically drops off the rail.")]
    [SerializeField] private float stopSpeedThreshold = 0.1f;

    [Tooltip("Resistance applied when grinding uphill. Scales with slope steepness.")]
    [SerializeField] private float uphillResistance = 5f;

    [Tooltip("Extra acceleration applied when grinding downhill. Scales with slope steepness.")]
    [SerializeField] private float downhillAcceleration = 5f;

    [Tooltip("Maximum distance integrated in one substep. Lower values improve stability at high speed.")]
    [SerializeField] private float maxSubstepDistance = 0.2f;

    [Header("Exit")]
    [Tooltip("Speed bonus preserved into normal movement when exiting a grind.")]
    [SerializeField] private float exitBoost = 4f;

    [Tooltip("If true, exit velocity keeps the last grinding direction. If false, it uses current input/forward.")]
    [SerializeField] private bool preserveExitDirection = true;

    [Header("Re-entry Cooldown")]
    [Tooltip("Delay after leaving a grind before the player can latch onto another rail.")]
    [SerializeField] private float grindReentryCooldown = 0.5f;
    [Header("State")]
    [SerializeField] private bool isGrinding;

    private Rigidbody _rigidbody;
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
    private Vector3 _currentWorldTangent;
    private float _normalizedTime;
    private bool _exitRequested;
    private bool _exitWithJump;

    private bool _wasKinematic;
    private bool _wasUsingGravity;
    private float _reentryUnlockTime;

    public bool IsGrinding => isGrinding;
    public float CurrentGrindSpeed => _grindSpeed;
    public float DistanceAlongSpline => _distanceAlongSpline;
    public SplineContainer ActiveSplineContainer => _activeSplineContainer;

    public void Initialize(
        Rigidbody rigidbody,
        GroundingEvaluator grounding,
        JumpModule jumpModule)
    {
        _rigidbody = rigidbody;
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

    private void OnDestroy()
    {
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
        if (_currentWorldTangent.sqrMagnitude > 0.0001f)
            _currentWorldTangent.Normalize();

        float entrySpeedAlongRail = Vector3.Dot(horizontalVelocity, _currentWorldTangent);
        if (entrySpeedAlongRail < minEntrySpeed)
        {
            ClearGrindState();
            return;
        }

        _splineWorldLength = _cachedNativeSpline.GetLength();
        _distanceAlongSpline = worldDistance;
        _grindSpeed = Mathf.Min(entrySpeedAlongRail + landingBoost, maxGrindSpeed);
        _grindSpeed = Mathf.Max(_grindSpeed, minEntrySpeed);

        isGrinding = true;
        _exitRequested = false;
        _exitWithJump = false;

        _wasKinematic = _rigidbody.isKinematic;
        _wasUsingGravity = _rigidbody.useGravity;
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        Vector3 targetPos = nearestWorldPos + Vector3.up * heightOffset;
        _rigidbody.MovePosition(Vector3.Lerp(_rigidbody.position, targetPos, entrySnapStrength));
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
            if (!EvaluateAtDistance(_distanceAlongSpline, out _, out Vector3 worldTangent, out _))
            {
                ApplyPositionFromDistance();
                ExitGrind(false);
                return;
            }

            Vector3 movementTangent = worldTangent * _directionSign;
            if (movementTangent.sqrMagnitude > 0.0001f)
                movementTangent.Normalize();

            _currentWorldTangent = movementTangent;

            float slopeDot = Vector3.Dot(movementTangent, Vector3.up);
            float acceleration = grindAcceleration;

            if (slopeDot > 0.001f)
                acceleration -= uphillResistance * slopeDot;
            else if (slopeDot < -0.001f)
                acceleration += downhillAcceleration * Mathf.Abs(slopeDot);

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

                _distanceAlongSpline =
                    Mathf.Repeat(_distanceAlongSpline, _splineWorldLength);
            }
            else
            {
                if (_directionSign > 0f)
                {
                    float remainingToEnd = _splineWorldLength - _distanceAlongSpline;

                    if (stepMove >= remainingToEnd)
                    {
                        _distanceAlongSpline = _splineWorldLength;
                        ApplyPositionFromDistance();
                        ExitGrind(false);
                        return;
                    }
                }
                else
                {
                    if (stepMove >= _distanceAlongSpline)
                    {
                        _distanceAlongSpline = 0f;
                        ApplyPositionFromDistance();
                        ExitGrind(false);
                        return;
                    }
                }

                _distanceAlongSpline += stepMove * _directionSign;
            }
        }

        // --- Presentation phase: single physics write per tick ---
        ApplyPositionFromDistance();
    }

    private void ApplyPositionFromDistance()
    {
        if (!_hasCachedNativeSpline)
            return;

        if (_activeSpline != null && _activeSpline.Closed)
        {
            _distanceAlongSpline =
                Mathf.Repeat(_distanceAlongSpline, _splineWorldLength);
        }

        _normalizedTime = _cachedNativeSpline.ConvertIndexUnit(
            _distanceAlongSpline,
            PathIndexUnit.Distance,
            PathIndexUnit.Normalized);

        if (_activeSpline != null && _activeSpline.Closed)
        {
            _normalizedTime = Mathf.Repeat(_normalizedTime, 1f);
        }

        Vector3 worldPos =
            (Vector3)_cachedNativeSpline.EvaluatePosition(_normalizedTime);

        Vector3 worldTangent =
            (Vector3)_cachedNativeSpline.EvaluateTangent(_normalizedTime);

        Vector3 movementTangent = worldTangent * _directionSign;

        if (movementTangent.sqrMagnitude > 0.0001f)
            movementTangent.Normalize();

        _currentWorldTangent = movementTangent;

        Vector3 targetPosition = worldPos + Vector3.up * heightOffset;

        _rigidbody.MovePosition(targetPosition);
    }

    private void ExitGrind(bool withJump)
    {
        if (!isGrinding)
            return;

        Vector3 exitHorizontal = preserveExitDirection
            ? _currentWorldTangent * (_grindSpeed + exitBoost)
            : new Vector3(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z).normalized * (_grindSpeed + exitBoost);

        float verticalVelocity = 0f;
        if (withJump && _jumpModule != null)
            verticalVelocity = _jumpModule.JumpForce;

        _rigidbody.isKinematic = _wasKinematic;
        _rigidbody.useGravity = _wasUsingGravity;
        _rigidbody.velocity = new Vector3(exitHorizontal.x, verticalVelocity, exitHorizontal.z);
        _rigidbody.angularVelocity = Vector3.zero;

        _reentryUnlockTime = Time.time + grindReentryCooldown;
        DisposeCachedNativeSpline();
        ClearGrindState();
    }

    private void StopGrind()
    {
        if (!isGrinding)
            return;

        Vector3 horizontal = new Vector3(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);

        _rigidbody.isKinematic = _wasKinematic;
        _rigidbody.useGravity = _wasUsingGravity;
        _rigidbody.velocity = new Vector3(horizontal.x, 0f, horizontal.z);
        _rigidbody.angularVelocity = Vector3.zero;

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
    }

    private void RebuildNativeSplineCache()
    {
        DisposeCachedNativeSpline();

        if (_activeSpline == null || _activeSplineContainer == null)
            return;

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
        if (!_hasCachedNativeSpline)
            return;

        _cachedNativeSpline.Dispose();
        _hasCachedNativeSpline = false;
    }

    private void OnSplineChanged(Spline spline, int knotIndex, SplineModification modification)
    {
        if (_activeSpline == spline && _hasCachedNativeSpline)
            RebuildNativeSplineCache();
    }

    private bool TryGetNearestPoint(Vector3 worldPosition, out float worldDistance, out Vector3 worldPos, out Vector3 worldTangent)
    {
        worldDistance = 0f;
        worldPos = worldPosition;
        worldTangent = Vector3.forward;

        if (!_hasCachedNativeSpline) return false;

        float distance = SplineUtility.GetNearestPoint(_cachedNativeSpline, worldPosition, out float3 nearestWorld, out float t);

        if (distance == float.PositiveInfinity) return false;

        worldPos = nearestWorld;
        worldTangent = (Vector3)_cachedNativeSpline.EvaluateTangent(t);
        if (worldTangent.sqrMagnitude > 0.0001f)
            worldTangent.Normalize();

        worldDistance = _cachedNativeSpline.ConvertIndexUnit(t, PathIndexUnit.Normalized, PathIndexUnit.Distance);
        return true;
    }

    private bool EvaluateAtDistance(float worldDistance, out Vector3 worldPos, out Vector3 worldTangent, out float normalizedTime)
    {
        worldPos = Vector3.zero;
        worldTangent = Vector3.forward;
        normalizedTime = 0f;

        if (!_hasCachedNativeSpline)
            return false;

        normalizedTime = _cachedNativeSpline.ConvertIndexUnit(worldDistance, PathIndexUnit.Distance, PathIndexUnit.Normalized);
        worldPos = (Vector3)_cachedNativeSpline.EvaluatePosition(normalizedTime);
        worldTangent = (Vector3)_cachedNativeSpline.EvaluateTangent(normalizedTime);

        if (normalizedTime < 0 || normalizedTime > 1)
        {
            Debug.LogWarning(
                $"Grind invalid t={normalizedTime}, dist={worldDistance}, len={_splineWorldLength}");
        }

        if (worldTangent.sqrMagnitude > 0.0001f)
            worldTangent.Normalize();

        return true;
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
