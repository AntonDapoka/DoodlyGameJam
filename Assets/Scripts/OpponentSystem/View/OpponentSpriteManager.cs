using UnityEngine;

public class OpponentSpriteManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Transform of the player. If unassigned, the manager will try to find an object tagged 'Player'.")]
    [SerializeField] private Transform _playerTransform;
    [Tooltip("SpriteRenderer used to display the opponent. If unassigned, the manager will search in children.")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Sprites")]
    [Tooltip("8 sprites arranged clockwise around the opponent. Index 0 = front (player is ahead).")]
    [SerializeField] private Sprite[] _sprites = new Sprite[8];

    [Header("Update")]
    [Tooltip("How often the sprite is refreshed (in seconds).")]
    [SerializeField] private float _updateInterval = 0.05f;

    private float _timer;
    private int _currentIndex = -1;

    private void Awake()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (_playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _playerTransform = player.transform;
        }

        if (_sprites.Length != 8)
        {
            Debug.LogWarning($"[OpponentSpriteManager] Expected exactly 8 sprites, but found {_sprites.Length} on '{gameObject.name}'.", this);
        }
    }

    private void Update()
    {
        if (_spriteRenderer == null || _playerTransform == null || _sprites.Length != 8)
            return;

        _timer += Time.deltaTime;
        if (_timer < _updateInterval)
            return;

        _timer = 0f;
        RefreshSprite();
    }

    private void RefreshSprite()
    {
        Vector3 toPlayer = _playerTransform.position - transform.position;
        toPlayer.y = 0f;

        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (toPlayer.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f)
            return;

        float angle = Vector3.SignedAngle(forward, toPlayer, Vector3.up);

        // Shift by half a sector so the selected sprite matches the center of each 45-degree slice.
        float shifted = angle + 22.5f;
        if (shifted < 0f)
            shifted += 360f;

        int index = Mathf.FloorToInt(shifted / 45f) % 8;

        if (index == _currentIndex)
            return;

        _currentIndex = index;
        _spriteRenderer.sprite = _sprites[index];
    }

    private void OnValidate()
    {
        if (_sprites.Length != 8)
        {
            System.Array.Resize(ref _sprites, 8);
        }
    }
}
