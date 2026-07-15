using UnityEngine;

public class GraffitiScript : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _objectGraffitiHint;
    [SerializeField] private CapsuleCollider _collider;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Materials")]
    [Tooltip("Material applied when the graffiti belongs to the player.")]
    [SerializeField] private Material _materialPlayer;
    [Tooltip("Material applied when the graffiti belongs to the opponent.")]
    [SerializeField] private Material _materialOpponent;
    [SerializeField] private GameObject plane;

    public event System.Action<GraffitiScript> OnInteractionStarted;
    public event System.Action<GraffitiScript> OnInteractionEnded;
    public event System.Action<GraffitiScript> OnInteractionReset;
    public event System.Action<GraffitiScript> OnProgressChanged;
    public event System.Action<GraffitiScript> OnCompleted;
    public event System.Action<GraffitiScript> OnStateChanged;

    [Header("Graffiti Center")]
    public Transform graffitiCenter;

    [Header("Completion")]
    public float completionCurrent;
    public float completionMax = 100f;

    [Header("Fill Settings")]
    [SerializeField] private float _fillMultiplier = 0.5f;
    [SerializeField] private float _maxFillDistance = 5f;
    [SerializeField] private float _distanceCurvePower = 2f;
    [SerializeField] private float _speedMultiplierMin = 3f;

    [Header("Reset Settings")]
    [SerializeField] private float _resetSpeed = 15f;

    public bool _isTurnOn = false;
   [SerializeField]   private GraffitiType _graffitiType;
    private bool _isCompleted;
    private bool _isPlayerInside;
    private Collider _playerCollider;

    public bool IsCompleted => _isCompleted;

    private void Awake()
    {
        gameObject.SetActive(false);
        plane.SetActive(false);
        if (_objectGraffitiHint != null)
            _objectGraffitiHint.SetActive(false);

        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        _graffitiType = GraffitiType.Opponent;
        completionCurrent = 0f;
        _isCompleted = false;
        _isPlayerInside = false;
        _playerCollider = null;

        if (graffitiCenter == null)
            graffitiCenter = transform;

        if (_collider != null)
            _collider.isTrigger = true;

        RefreshMaterial();

        if (GraffitiPresenterScript.Instance != null)
            GraffitiPresenterScript.Instance.RegisterGraffiti(this);
    }

    private void OnEnable()
    {
        if (GraffitiPresenterScript.Instance != null)
            GraffitiPresenterScript.Instance.RegisterGraffiti(this);
    }

    private void OnDisable()
    {
        if (GraffitiPresenterScript.Instance != null)
            GraffitiPresenterScript.Instance.UnregisterGraffiti(this);
    }

    private void FixedUpdate()
    {
        if (_isCompleted) return;
        if (_graffitiType != GraffitiType.Opponent) return;
        if (!_isPlayerInside || _playerCollider == null) return;

        TryFillProgress();
    }

    private void Update()
    {
        if (_isCompleted) return;
        if (!_isPlayerInside && completionCurrent > 0f)
        {
            completionCurrent -= _resetSpeed * Time.deltaTime;
            if (completionCurrent < 0f)
                completionCurrent = 0f;

            OnProgressChanged?.Invoke(this);

            if (completionCurrent <= 0f)
                OnInteractionReset?.Invoke(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isCompleted) return;
        if (_graffitiType != GraffitiType.Opponent) return;
        if (other.GetComponent<PlayerMarker>() == null) return;

        _isPlayerInside = true;
        _playerCollider = other;
        OnInteractionStarted?.Invoke(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_playerCollider != null && other != _playerCollider) return;
        if (other.GetComponent<PlayerMarker>() == null) return;

        _isPlayerInside = false;
        _playerCollider = null;
        OnInteractionEnded?.Invoke(this);

        if (completionCurrent <= 0f)
            OnInteractionReset?.Invoke(this);
    }

    private void TryFillProgress()
    {
        if (graffitiCenter == null) return;
        if (_playerCollider == null) return;

        if (!HasLineOfSightToCenter()) return;

        float speed = _playerCollider.attachedRigidbody != null
            ? _playerCollider.attachedRigidbody.velocity.magnitude
            : 0f;

        float distance = Vector3.Distance(_playerCollider.bounds.center, graffitiCenter.position);
        float normalizedDistance = Mathf.Clamp01(distance / _maxFillDistance);
        float distanceFactor = 1f - Mathf.Pow(normalizedDistance, _distanceCurvePower);

        float speedMultiplier = Mathf.Max(speed, _speedMultiplierMin);

        completionCurrent += Time.fixedDeltaTime * _fillMultiplier * speedMultiplier * distanceFactor;
        OnProgressChanged?.Invoke(this);

        if (completionCurrent >= completionMax)
        {
            completionCurrent = completionMax;
            _isCompleted = true;
            _isPlayerInside = false;
            _playerCollider = null;
            RedrawGraffitiFromOpponentToPlayer();
        }
    }

    private bool HasLineOfSightToCenter()
    {
        Vector3 origin = _playerCollider.bounds.center;
        Vector3 target = graffitiCenter.position;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (distance <= 0f) return true;

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == _playerCollider) continue;
            if (hit.transform.IsChildOf(transform)) continue;

            return false;
        }

        return true;
    }

    public void TurnOnPlayerGraffiti()
    {
        _isTurnOn = true;
        SetGraffitiType(GraffitiType.Player);
        gameObject.SetActive(true);

        completionCurrent = 0f;
        _isCompleted = false;
        _isPlayerInside = false;
        _playerCollider = null;

        plane.SetActive(false);


        OnStateChanged?.Invoke(this);
    }

    public void TurnOnOpponentGraffiti()
    {
        _isTurnOn = true;
        SetGraffitiType(GraffitiType.Opponent);
        gameObject.SetActive(true);

        completionCurrent = 0f;
        _isCompleted = false;
        _isPlayerInside = false;
        _playerCollider = null;

        plane.SetActive(true);

        if (_objectGraffitiHint != null)
            _objectGraffitiHint.SetActive(true);

        OnStateChanged?.Invoke(this);
    }

    public void RedrawGraffitiFromOpponentToPlayer()
    {
        SetGraffitiType(GraffitiType.Player);
        _isCompleted = true;

        if (_objectGraffitiHint != null)
            _objectGraffitiHint.SetActive(false);

        OnStateChanged?.Invoke(this);
        OnCompleted?.Invoke(this);
    }

    public void RedrawGraffitiFromPlayerToOpponent()
    {
        SetGraffitiType(GraffitiType.Opponent);
        completionCurrent = 0f;
        _isCompleted = false;
        _isPlayerInside = false;
        _playerCollider = null;

        if (_objectGraffitiHint != null)
            _objectGraffitiHint.SetActive(true);

        OnStateChanged?.Invoke(this);
    }

    public void TurnOff()
    {
        _isTurnOn = false;
        gameObject.SetActive(false);

        if (_objectGraffitiHint != null)
            _objectGraffitiHint.SetActive(false);

        completionCurrent = 0f;
        _isCompleted = false;
        _isPlayerInside = false;
        _playerCollider = null;

        OnStateChanged?.Invoke(this);
    }

    public bool GetIsTurnOn()
    {
        return _isTurnOn;
    }

    public GraffitiType GetGraffitiType()
    {
        return _graffitiType;
    }

    public void SetGraffitiType(GraffitiType typeNew)
    {
        if (_graffitiType == typeNew) return;

        _graffitiType = typeNew;
        RefreshMaterial();

        if (_isTurnOn)
            OnStateChanged?.Invoke(this);
    }

    private void RefreshMaterial()
    {
        if (_spriteRenderer == null) return;

        Material material = _graffitiType == GraffitiType.Player ? _materialPlayer : _materialOpponent;
        if (material != null)
            _spriteRenderer.material = material;
    }
}
