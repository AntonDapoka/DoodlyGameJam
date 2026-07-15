using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GraffitiScoreCounter : MonoBehaviour
{
    public static GraffitiScoreCounter Instance { get; private set; }

    public event Action OnScoresChanged;

    [Header("UI")]
    [Tooltip("Optional TextMeshPro text. If unassigned, a UI text will be created automatically.")]
    [SerializeField] private TextMeshProUGUI _scoreText;

    [Header("Format")]
    [SerializeField] private string _playerLabel = "Player";
    [SerializeField] private string _opponentLabel = "Opponent";
    [SerializeField] private string _format = "{0}: {1}  |  {2}: {3}";

    private readonly Dictionary<GraffitiType, int> _scores = new();

    public IReadOnlyDictionary<GraffitiType, int> Scores => _scores;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        EnsureScoreText();
        InitializeDictionary();
        SubscribeToAllGraffiti();
    }

    private void Start()
    {
        RecalculateScores();
    }

    private void OnDestroy()
    {
        UnsubscribeFromAllGraffiti();

        if (Instance == this)
            Instance = null;
    }

    public int GetScore(GraffitiType type)
    {
        return _scores.TryGetValue(type, out int value) ? value : 0;
    }

    private void InitializeDictionary()
    {
        _scores.Clear();
        foreach (GraffitiType type in Enum.GetValues(typeof(GraffitiType)))
        {
            _scores[type] = 0;
        }
    }

    private void SubscribeToAllGraffiti()
    {
        GraffitiScript[] allGraffiti = FindObjectsOfType<GraffitiScript>();
        foreach (GraffitiScript graffiti in allGraffiti)
        {
            if (graffiti != null)
                graffiti.OnStateChanged += HandleStateChanged;
        }
    }

    private void UnsubscribeFromAllGraffiti()
    {
        GraffitiScript[] allGraffiti = FindObjectsOfType<GraffitiScript>();
        foreach (GraffitiScript graffiti in allGraffiti)
        {
            if (graffiti != null)
                graffiti.OnStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(GraffitiScript graffiti)
    {
        RecalculateScores();
    }

    private void RecalculateScores()
    {
        InitializeDictionary();

        GraffitiScript[] allGraffiti = FindObjectsOfType<GraffitiScript>();
        foreach (GraffitiScript graffiti in allGraffiti)
        {
            if (graffiti == null) continue;
            if (!graffiti.GetIsTurnOn()) continue;

            GraffitiType type = graffiti.GetGraffitiType();
            _scores[type]++;
        }

        RefreshUI();
        OnScoresChanged?.Invoke();
    }

    private void RefreshUI()
    {
        if (_scoreText == null) return;

        _scoreText.text = string.Format(
            _format,
            _playerLabel,
            GetScore(GraffitiType.Player),
            _opponentLabel,
            GetScore(GraffitiType.Opponent));
    }

    private void EnsureScoreText()
    {
        if (_scoreText != null) return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        GameObject textGO = new GameObject("GraffitiScoreText");
        textGO.transform.SetParent(canvas.transform, false);
        _scoreText = textGO.AddComponent<TextMeshProUGUI>();
        _scoreText.font = TMP_Settings.defaultFontAsset;
        _scoreText.fontSize = 36;
        _scoreText.alignment = TextAlignmentOptions.TopRight;

        RectTransform rectTransform = _scoreText.rectTransform;
        rectTransform.anchorMin = Vector2.one;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = Vector2.one;
        rectTransform.anchoredPosition = new Vector2(-20f, -20f);
        rectTransform.sizeDelta = new Vector2(400f, 60f);
    }
}
