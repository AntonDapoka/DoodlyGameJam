using UnityEngine;
using UnityEngine.AI;

public class OpponentNavigator : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _speed = 6f;
    [SerializeField] private float _acceleration = 12f;
    [SerializeField] private float _angularSpeed = 360f;
    [SerializeField] private float _stoppingDistance = 0.5f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private float _obstacleAvoidanceRadius = 0.5f;
    [SerializeField] private int _obstacleAvoidancePriority = 50;

    [Header("Anti-Stuck")]
    [Tooltip("How often we check whether the opponent has moved enough.")]
    [SerializeField] private float _stuckCheckInterval = 1f;
    [Tooltip("Fraction of expected movement that counts as 'stuck'.")]
    [SerializeField] private float _stuckMovementFraction = 0.05f;
    [Tooltip("How long the opponent may stay stuck before switching to direct (wall-passing) movement.")]
    [SerializeField] private float _stuckTimeout = 2f;
    [Tooltip("How long direct movement stays active once triggered.")]
    [SerializeField] private float _directMoveDuration = 3f;
    [Tooltip("Layers treated as obstacles that can be passed through when stuck.")]
    [SerializeField] private LayerMask _passThroughObstacles = ~0;

    private NavMeshAgent _agent;
    private Transform _target;
    private Vector3 _targetPosition;
    private bool _hasTargetPosition;
    private bool _hasReachedTarget;

    private float _stuckCheckTimer;
    private float _stuckDuration;
    private Vector3 _lastCheckPosition;
    private bool _isDirectMoving;
    private float _directMoveTimer;
    private float _ignoreObstaclesTimer;
    private float _forceDirectMovementTimer;

    public Transform Target => _target;
    public bool HasReachedTarget => _hasReachedTarget;
    public bool IsDirectMoving => _isDirectMoving;
    public bool IsStuck => _stuckDuration >= _stuckTimeout;
    public float RemainingDistance => _agent != null && _agent.isOnNavMesh ? _agent.remainingDistance : float.PositiveInfinity;

    private Vector3 TargetPosition => _hasTargetPosition ? _targetPosition : (_target != null ? _target.position : transform.position);

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
        {
            _agent = gameObject.AddComponent<NavMeshAgent>();
        }

        ConfigureAgent();
        _lastCheckPosition = transform.position;
    }

    private void Update()
    {
        if (!_hasTargetPosition && _target == null)
        {
            _hasReachedTarget = false;
            return;
        }

        if (_forceDirectMovementTimer > 0f)
        {
            _forceDirectMovementTimer = Mathf.Max(0f, _forceDirectMovementTimer - Time.deltaTime);
            if (!_isDirectMoving)
                EnterDirectMovement();
            UpdateDirectMovement(Time.deltaTime);
            return;
        }

        if (_isDirectMoving)
        {
            if (_agent.isOnNavMesh)
            {
                ExitDirectMovement();
            }
            else
            {
                UpdateDirectMovement(Time.deltaTime);
                return;
            }
        }

        if (!_agent.isOnNavMesh || !NavMesh.SamplePosition(transform.position, out _, 2f, NavMesh.AllAreas))
        {
            EnterDirectMovement();
            return;
        }

        EnsureDestination(TargetPosition);
        CheckStuck(Time.deltaTime);

        if (_agent.hasPath && _agent.remainingDistance <= _stoppingDistance)
        {
            _hasReachedTarget = true;
        }
    }

    private void ConfigureAgent()
    {
        if (_agent == null) return;

        _agent.speed = _speed;
        _agent.acceleration = _acceleration;
        _agent.angularSpeed = _angularSpeed;
        _agent.stoppingDistance = _stoppingDistance;
        _agent.radius = _obstacleAvoidanceRadius;
        _agent.avoidancePriority = _obstacleAvoidancePriority;
        _agent.autoBraking = true;
        _agent.autoRepath = true;
        _agent.updateRotation = true;
        _agent.updateUpAxis = true;
    }

    public void SetTarget(Transform target)
    {
        _target = target;
        _hasTargetPosition = false;
        _hasReachedTarget = false;
        ResetStuckState();

        if (_target == null)
        {
            if (_agent != null && _agent.isOnNavMesh)
                _agent.ResetPath();
            return;
        }

        if (_agent != null && _agent.isOnNavMesh)
        {
            SetDestination(TargetPosition);
        }
    }

    public void SetTarget(Vector3 position)
    {
        _target = null;
        _targetPosition = position;
        _hasTargetPosition = true;
        _hasReachedTarget = false;
        ResetStuckState();

        if (_agent != null && _agent.isOnNavMesh)
        {
            SetDestination(_targetPosition);
        }
    }

    public void ClearTarget()
    {
        _target = null;
        _targetPosition = transform.position;
        _hasTargetPosition = false;
        _hasReachedTarget = false;
        _forceDirectMovementTimer = 0f;
        _ignoreObstaclesTimer = 0f;

        if (_agent != null && _agent.isOnNavMesh)
            _agent.ResetPath();
    }

    /// <summary>
    /// Forces direct movement that ignores colliders for the specified duration.
    /// Useful for getting unstuck from walls or reaching elevated graffiti.
    /// </summary>
    public void IgnoreObstacles(float duration)
    {
        _ignoreObstaclesTimer = duration;
        _forceDirectMovementTimer = duration;
        EnterDirectMovement();
    }

    private void EnsureDestination(Vector3 destination)
    {
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh) return;

        bool needsNewPath = !_agent.hasPath
            || _agent.pathStatus == NavMeshPathStatus.PathInvalid
            || (_agent.pathPending == false && _agent.remainingDistance <= 0f);

        if (needsNewPath || (_agent.pathPending == false && Vector3.Distance(_agent.destination, destination) > 0.5f))
        {
            SetDestination(destination);
        }
    }

    private void SetDestination(Vector3 destination)
    {
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh) return;

        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path)
            && path.status != NavMeshPathStatus.PathInvalid
            && path.corners.Length > 0)
        {
            _agent.SetPath(path);
        }
        else
        {
            _agent.SetDestination(destination);
        }
    }

    private void CheckStuck(float deltaTime)
    {
        _stuckCheckTimer += deltaTime;
        if (_stuckCheckTimer < _stuckCheckInterval) return;

        _stuckCheckTimer = 0f;
        float moved = Vector3.Distance(transform.position, _lastCheckPosition);
        _lastCheckPosition = transform.position;

        float expected = _speed * _stuckCheckInterval;
        if (moved < expected * _stuckMovementFraction && _agent.remainingDistance > _stoppingDistance)
        {
            _stuckDuration += _stuckCheckInterval;
            if (_stuckDuration >= _stuckTimeout)
            {
                EnterDirectMovement();
            }
        }
        else
        {
            _stuckDuration = 0f;
        }
    }

    private void EnterDirectMovement()
    {
        _isDirectMoving = true;
        _directMoveTimer = 0f;
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }
    }

    private void UpdateDirectMovement(float deltaTime)
    {
        if (!_hasTargetPosition && _target == null)
        {
            _isDirectMoving = false;
            _hasReachedTarget = false;
            return;
        }

        Vector3 destination = TargetPosition;
        Vector3 toTarget = destination - transform.position;
        float distance = toTarget.magnitude;
        if (distance <= _stoppingDistance)
        {
            _hasReachedTarget = true;
            _isDirectMoving = false;
            return;
        }

        Vector3 direction = toTarget / distance;
        Vector3 step = direction * _speed * deltaTime;
        if (step.sqrMagnitude > distance * distance)
            step = direction * distance;

        Vector3 from = transform.position;
        Vector3 to = from + step;

        if (_ignoreObstaclesTimer > 0f)
        {
            _ignoreObstaclesTimer = Mathf.Max(0f, _ignoreObstaclesTimer - deltaTime);
            transform.position = to;
        }
        else if (Physics.Linecast(from, to, out RaycastHit hit, _passThroughObstacles))
        {
            transform.position = hit.point + direction * 0.05f;
        }
        else
        {
            transform.position = to;
        }

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, _angularSpeed * deltaTime);
        }

        _directMoveTimer += deltaTime;
        if (_directMoveTimer >= _directMoveDuration)
        {
            _isDirectMoving = false;
            _directMoveTimer = 0f;
            ResetStuckState();
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                SetDestination(destination);
            }
        }
    }

    private void ExitDirectMovement()
    {
        _isDirectMoving = false;
        _directMoveTimer = 0f;
        ResetStuckState();
        if (_agent.isOnNavMesh)
            SetDestination(TargetPosition);
    }

    private void ResetStuckState()
    {
        _stuckCheckTimer = 0f;
        _stuckDuration = 0f;
        _lastCheckPosition = transform.position;
        if (_agent != null && _agent.isOnNavMesh)
            _agent.isStopped = false;
    }

    private void OnValidate()
    {
        ConfigureAgent();
    }
}
