using System.Collections.Generic;
using UnityEngine;

public class GraffitiPresenterScript : MonoBehaviour
{
    public static GraffitiPresenterScript Instance { get; private set; }

    [Header("World View")]
    [SerializeField] private GraffitiViewScript _graffitiView;

    [Header("UI View")]
    [SerializeField] private SprayPaintUIScript _sprayPaintUI;

    [Header("Assets")]
    [SerializeField] private Sprite[] _graffitiSpritesPlayer;
    [SerializeField] private Sprite[] _graffitiSpritesOpponent;
    [SerializeField] private AudioClip _graffitiJingleSound;

    private readonly HashSet<GraffitiScript> _registeredGraffiti = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RegisterGraffiti(GraffitiScript graffiti)
    {
        if (graffiti == null || _registeredGraffiti.Contains(graffiti))
            return;

        _registeredGraffiti.Add(graffiti);

        graffiti.OnInteractionStarted += HandleInteractionStarted;
        graffiti.OnInteractionEnded += HandleInteractionEnded;
        graffiti.OnInteractionReset += HandleInteractionReset;
        graffiti.OnProgressChanged += HandleProgressChanged;
        graffiti.OnCompleted += HandleCompleted;
        graffiti.OnStateChanged += HandleStateChanged;

        RefreshWorldVisual(graffiti);
    }

    public void UnregisterGraffiti(GraffitiScript graffiti)
    {
        if (graffiti == null || !_registeredGraffiti.Contains(graffiti))
            return;

        graffiti.OnInteractionStarted -= HandleInteractionStarted;
        graffiti.OnInteractionEnded -= HandleInteractionEnded;
        graffiti.OnInteractionReset -= HandleInteractionReset;
        graffiti.OnProgressChanged -= HandleProgressChanged;
        graffiti.OnCompleted -= HandleCompleted;
        graffiti.OnStateChanged -= HandleStateChanged;

        _registeredGraffiti.Remove(graffiti);
    }

    private void HandleInteractionStarted(GraffitiScript graffiti)
    {
        _sprayPaintUI?.Show(graffiti);
    }

    private void HandleInteractionEnded(GraffitiScript graffiti)
    {
    }

    private void HandleInteractionReset(GraffitiScript graffiti)
    {
        _sprayPaintUI?.Hide(graffiti);
    }

    private void HandleProgressChanged(GraffitiScript graffiti)
    {
        if (_sprayPaintUI == null)
            return;

        float percent = graffiti.completionMax > 0f
            ? graffiti.completionCurrent / graffiti.completionMax * 100f
            : 0f;

        _sprayPaintUI.SetProgress(graffiti, percent);
    }

    private void HandleCompleted(GraffitiScript graffiti)
    {
        RefreshWorldVisual(graffiti);
        _graffitiView?.PlayCompletionSound(_graffitiJingleSound);
        _sprayPaintUI?.OnCompleted(graffiti);
    }

    private void HandleStateChanged(GraffitiScript graffiti)
    {
        RefreshWorldVisual(graffiti);
    }

    private void RefreshWorldVisual(GraffitiScript graffiti)
    {
        if (_graffitiView == null)
            return;

        GraffitiType type = graffiti.GetGraffitiType();
        Sprite[] pool = type == GraffitiType.Player ? _graffitiSpritesPlayer : _graffitiSpritesOpponent;

        if (pool == null || pool.Length == 0)
            return;

        Sprite sprite = pool[Random.Range(0, pool.Length)];
        _graffitiView.SetGraffitiSprite(graffiti, sprite);
    }
}
