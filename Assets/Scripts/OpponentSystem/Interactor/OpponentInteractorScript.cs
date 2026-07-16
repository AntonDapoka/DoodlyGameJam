using UnityEngine;
using UnityEngine.AI;

public class OpponentInteractorScript : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GraffitiManagementInteractorScript _graffitiInteractor;

    [Header("Interaction")]
    [Tooltip("Distance at which the opponent can start painting a graffiti spot.")]
    [SerializeField] private float _interactionReach = 0.6f;
    [Tooltip("Time the opponent spends painting one spot before claiming it.")]
    [SerializeField] private float _paintDuration = 1f;
    [Tooltip("Delay before picking the next target after finishing a paint action.")]
    [SerializeField] private float _nextTargetDelay = 0.5f;

    [Header("Fallback")]
    [Tooltip("How often to run an aggressive target search when the opponent has no target.")]
    [SerializeField] private float _noTargetRetryInterval = 1f;

    [Header("NavMesh Projection")]
    [Tooltip("Max distance to search for a NavMesh position under/around a graffiti target.")]
    [SerializeField] private float _navMeshSampleDistance = 50f;
    [Tooltip("How long the opponent can ignore walls when trying to reach an elevated graffiti.")]
    [SerializeField] private float _wallIgnoreDuration = 2f;

    private OpponentNavigator _navigator;
    private GraffitiScript _currentTarget;
    private Vector3 _currentTargetPosition;
    private float _paintTimer;
    private float _delayTimer;
    private float _noTargetRetryTimer;
    private bool _isPainting;
    private bool _isWaitingForNextTarget;

    private void Awake()
    {
        _navigator = GetComponent<OpponentNavigator>();
        if (_navigator == null)
        {
            _navigator = gameObject.AddComponent<OpponentNavigator>();
        }
    }

    private void Start()
    {
        PickNewTarget();
    }

    private void Update()
    {
        if (_isWaitingForNextTarget)
        {
            _delayTimer += Time.deltaTime;
            if (_delayTimer >= _nextTargetDelay)
            {
                _isWaitingForNextTarget = false;
                _delayTimer = 0f;
                PickNewTarget();
            }

            TryFindTargetFallback();
            return;
        }

        if (_isPainting)
        {
            UpdatePainting();
            return;
        }

        if (_currentTarget == null)
        {
            TryFindTargetFallback();
            return;
        }

        _noTargetRetryTimer = 0f;

        if (_navigator.HasReachedTarget ||
            Vector3.Distance(transform.position, _currentTargetPosition) <= _interactionReach)
        {
            StartPainting();
        }
    }

    private void PickNewTarget()
    {
        if (_graffitiInteractor == null)
        {
            _currentTarget = null;
            _navigator.ClearTarget();
            return;
        }

        GraffitiScript newTarget = _graffitiInteractor.SetRandomOpponentGraffitiSpot(transform);
        if (!TrySetTarget(newTarget))
        {
            _currentTarget = null;
            _navigator.ClearTarget();
        }
    }

    /// <summary>
    /// Aggressively tries to find a new graffiti target when the opponent currently has none.
    /// Called on a timer so the AI does not get permanently stuck if the distance-based finder fails.
    /// Projects elevated graffiti onto the NavMesh and temporarily ignores walls if the path is blocked.
    /// </summary>
    private void TryFindTargetFallback()
    {
        if (_graffitiInteractor == null) return;

        _noTargetRetryTimer += Time.deltaTime;
        if (_noTargetRetryTimer < _noTargetRetryInterval) return;

        _noTargetRetryTimer = 0f;

        // If the navigator is stuck, briefly ignore obstacles to get out of walls.
        if (_navigator != null && _navigator.IsStuck)
        {
            _navigator.IgnoreObstacles(_wallIgnoreDuration);
        }

        GraffitiScript fallbackTarget = _graffitiInteractor.GetFallbackOpponentGraffitiSpot();
        if (fallbackTarget == null) return;

        if (TrySetTarget(fallbackTarget) && IsPathBlocked(transform.position, _currentTargetPosition))
        {
            _navigator.IgnoreObstacles(_wallIgnoreDuration);
        }
    }

    /// <summary>
    /// Projects a graffiti target onto the NavMesh and tells the navigator to go to the projected point.
    /// Returns false if no NavMesh point could be found near the graffiti.
    /// </summary>
    private bool TrySetTarget(GraffitiScript graffiti)
    {
        if (graffiti == null || _navigator == null)
        {
            _currentTarget = null;
            _navigator?.ClearTarget();
            return false;
        }

        Vector3 graffitiPosition = graffiti.transform.position;
        if (NavMesh.SamplePosition(graffitiPosition, out NavMeshHit hit, _navMeshSampleDistance, NavMesh.AllAreas))
        {
            _isWaitingForNextTarget = false;
            _delayTimer = 0f;
            _currentTarget = graffiti;
            _currentTargetPosition = hit.position;
            _navigator.SetTarget(_currentTargetPosition);
            return true;
        }

        _currentTarget = null;
        _navigator.ClearTarget();
        return false;
    }

    /// <summary>
    /// Returns true if the NavMesh path from <paramref name="from"/> to <paramref name="to"/> is not complete.
    /// </summary>
    private bool IsPathBlocked(Vector3 from, Vector3 to)
    {
        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path))
            return path.status != NavMeshPathStatus.PathComplete;
        return true;
    }

    private void StartPainting()
    {
        if (_currentTarget == null) return;

        _isPainting = true;
        _paintTimer = 0f;
        _navigator.ClearTarget();
    }

    private void UpdatePainting()
    {
        _paintTimer += Time.deltaTime;
        if (_paintTimer >= _paintDuration)
        {
            FinishPainting();
        }
    }

    private void FinishPainting()
    {
        if (_currentTarget != null && _graffitiInteractor != null)
        {
            _graffitiInteractor.UpdateRandomOpponentGraffitiSpot(_currentTarget);
        }

        _isPainting = false;
        _paintTimer = 0f;
        _currentTarget = null;
        _isWaitingForNextTarget = true;
        _delayTimer = 0f;
    }
}
