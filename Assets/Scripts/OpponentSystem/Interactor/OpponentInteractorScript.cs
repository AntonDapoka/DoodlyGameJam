using UnityEngine;

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

    private OpponentNavigator _navigator;
    private GraffitiScript _currentTarget;
    private float _paintTimer;
    private float _delayTimer;
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
            return;
        }

        if (_isPainting)
        {
            UpdatePainting();
            return;
        }

        if (_currentTarget == null)
        {
            PickNewTarget();
            return;
        }

        if (_navigator.HasReachedTarget ||
            Vector3.Distance(transform.position, _currentTarget.transform.position) <= _interactionReach)
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

        _currentTarget = _graffitiInteractor.SetRandomOpponentGraffitiSpot(transform);

        if (_currentTarget != null)
        {
            _navigator.SetTarget(_currentTarget.transform);
        }
        else
        {
            _navigator.ClearTarget();
        }
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
