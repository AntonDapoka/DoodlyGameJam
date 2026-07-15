using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WinLoseManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional TextMeshPro text for win/lose messages. If unassigned, a UI text will be created automatically.")]
    [SerializeField] private TextMeshProUGUI _resultText;

    [Header("Messages")]
    [SerializeField] private string _winMessage = "You Won";
    [SerializeField] private string _loseMessage = "You Lose";

    [Header("Timing")]
    [Tooltip("Delay in seconds before the first win/lose check is performed. Prevents instant win/lose during level initialization.")]
    [SerializeField] private float _startDelay = 1f;

    [Header("Opponent")]
    [Tooltip("Optional reference to the opponent object. If unassigned, the manager will try to find an object with OpponentMarker.")]
    [SerializeField] private GameObject _opponentObject;

    private bool _gameFinished;
    private float _elapsedTime;

    private void Awake()
    {
        if (_opponentObject == null)
        {
            OpponentMarker marker = FindObjectOfType<OpponentMarker>();
            if (marker != null) _opponentObject = marker.gameObject;
        }

        if (_resultText != null) _resultText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (GraffitiScoreCounter.Instance != null) GraffitiScoreCounter.Instance.OnScoresChanged += HandleScoresChanged;
    }

    private void OnDisable()
    {
        if (GraffitiScoreCounter.Instance != null)  GraffitiScoreCounter.Instance.OnScoresChanged -= HandleScoresChanged;
    }

    private void Update()
    {
        if (_gameFinished) return;

        _elapsedTime += Time.deltaTime;
        if (_elapsedTime >= _startDelay)
        {
            HandleScoresChanged();
        }
    }

    private void HandleScoresChanged()
    {
        if (_gameFinished) return;
        if (GraffitiScoreCounter.Instance == null) return;

        int playerScore = GraffitiScoreCounter.Instance.GetScore(GraffitiType.Player);
        int opponentScore = GraffitiScoreCounter.Instance.GetScore(GraffitiType.Opponent);

        // Do not trigger win/lose before the level has finished initializing.
        if (playerScore + opponentScore <= 0)
            return;

        if (playerScore <= 0)
        {
            EndGame(false);
        }
        else if (opponentScore <= 0)
        {
            EndGame(true);
        }
    }

    private void EndGame(bool playerWon)
    {
        _gameFinished = true;

        if (_resultText != null)
        {
            _resultText.gameObject.SetActive(true);
            _resultText.text = playerWon ? _winMessage : _loseMessage;
        }

        if (_opponentObject != null)
        {
            _opponentObject.SetActive(false);
        }

        Debug.Log($"[WinLoseManager] Game ended. Player won: {playerWon}");
    }
}
