using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SprayPaintUIScript : MonoBehaviour
{
    [Header("UI Root (optional)")]
    [SerializeField] private RectTransform _uiRoot;

    [Header("Group Movement")]
    [SerializeField] private Vector2 _offScreenPosition = new Vector2(-330f, -725f);
    [SerializeField] private Vector2 _onScreenPosition = new Vector2(150f, 175f);
    [SerializeField] private float _moveDuration = 0.35f;
    [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _finalDelay = 1f;

    [Header("Images")]
    [SerializeField] private Image imageSprayPaint;
    [SerializeField] private Image imageMask;
    [SerializeField] private Image imageBack;

    [Header("Sprites")]
    [SerializeField] private Sprite[] spritesBasic;
    [SerializeField] private Sprite[] spritesSpraying;
    [SerializeField] private Sprite[] spritesSuper;
    [SerializeField] private Sprite[] spritesMask;
    [SerializeField] private Sprite[] spritesBack;

    [Header("Super Animation")]
    [SerializeField] private float _superAnimationInterval = 0.1f;

    [Header("Back Movement")]
    [SerializeField] private Vector2 backMinPosition;
    [SerializeField] private Vector2 backMaxPosition;
    [SerializeField] private float minMoveSpeed = 200f;
    [SerializeField] private float maxMoveSpeed = 800f;

    private float backTargetPercent;
    private RectTransform backRect;
    private int _superSpriteIndex;
    private Coroutine _superAnimationCoroutine;

    private GraffitiScript _currentGraffiti;
    private bool _isVisible;
    private bool _isCompleted;
    private Coroutine _animationCoroutine;

    private readonly List<RectTransform> _movingRects = new();
    private readonly List<Vector2> _movingOriginalAnchoredPositions = new();
    private RectTransform _leadRect;

    private void Start()
    {
        ResolveMovingRects();
        CacheOriginalPositions();
        SetLeadPosition(_offScreenPosition);

        backRect = imageBack != null ? imageBack.rectTransform : null;
        ResetBackToMin();

        _isVisible = false;
        _isCompleted = false;
        _currentGraffiti = null;
    }

    private void Update()
    {
        UpdateBackPosition();
    }

    private void ResolveMovingRects()
    {
        _movingRects.Clear();

        if (_uiRoot != null)
        {
            _movingRects.Add(_uiRoot);
            _leadRect = _uiRoot;
            return;
        }

        if (imageSprayPaint != null)
        {
            _movingRects.Add(imageSprayPaint.rectTransform);
            _leadRect = imageSprayPaint.rectTransform;
        }

        if (imageMask != null)
            _movingRects.Add(imageMask.rectTransform);

        if (imageBack != null && imageMask != null && imageBack.transform.parent != imageMask.transform)
            _movingRects.Add(imageBack.rectTransform);
    }

    private void CacheOriginalPositions()
    {
        _movingOriginalAnchoredPositions.Clear();

        foreach (RectTransform rect in _movingRects)
            _movingOriginalAnchoredPositions.Add(rect.anchoredPosition);
    }

    private void SetLeadPosition(Vector2 leadPosition)
    {
        if (_leadRect == null)
            return;

        Vector2 offset = leadPosition - _movingOriginalAnchoredPositions[0];

        for (int i = 0; i < _movingRects.Count; i++)
            _movingRects[i].anchoredPosition = _movingOriginalAnchoredPositions[i] + offset;
    }

    public void Show(GraffitiScript graffiti)
    {
        if (graffiti == null)
            return;

        StopAnimation();

        bool wasVisible = _isVisible;
        _currentGraffiti = graffiti;
        _isCompleted = false;

        RefreshSprites(graffiti);

        if (wasVisible)
        {
            _isVisible = true;
            SetLeadPosition(_onScreenPosition);
            return;
        }

        _isVisible = true;
        SetLeadPosition(_offScreenPosition);
        ResetBackToMin();
        _animationCoroutine = StartCoroutine(AnimateTo(_onScreenPosition));
    }

    public void Hide(GraffitiScript graffiti = null)
    {
        if (!_isVisible)
            return;

        if (graffiti != null && _currentGraffiti != graffiti)
            return;

        StopAnimation();
        _isVisible = false;
        _animationCoroutine = StartCoroutine(AnimateOut());
    }

    public void SetProgress(GraffitiScript graffiti, float percent)
    {
        if (_currentGraffiti != graffiti)
            return;

        if (_isCompleted)
            return;

        percent = Mathf.Clamp(percent, 0f, 100f);
        _isCompleted = percent >= 100f;
        backTargetPercent = percent;

        RefreshSprites(percent);
    }

    public void OnCompleted(GraffitiScript graffiti)
    {
        if (_currentGraffiti != graffiti)
            return;

        StopAnimation();
        _isCompleted = true;
        backTargetPercent = 100f;
        RefreshSprites(100f);
        _animationCoroutine = StartCoroutine(CompletedRoutine());
    }

    private IEnumerator CompletedRoutine()
    {
        yield return new WaitForSeconds(_finalDelay);
        Hide(_currentGraffiti);
    }

    private void RefreshSprites(GraffitiScript graffiti)
    {
        float percent = graffiti.completionMax > 0f
            ? graffiti.completionCurrent / graffiti.completionMax * 100f
            : 0f;

        RefreshSprites(percent);
    }

    private void RefreshSprites(float percent)
    {
        backTargetPercent = percent;

        int basicCount = spritesBasic?.Length ?? 0;
        int sprayingCount = spritesSpraying?.Length ?? 0;
        int superCount = spritesSuper?.Length ?? 0;

        Sprite mainSprite = null;
        int stageIndex = 0;

        if (percent >= 100f && superCount > 0)
        {
            stageIndex = 2;

            if (_superAnimationCoroutine == null)
            {
                _superSpriteIndex = 0;
                mainSprite = spritesSuper[0];
                _superAnimationCoroutine = StartCoroutine(AnimateSuperSprites());
            }
        }
        else if (percent < 50f && basicCount > 0)
        {
            stageIndex = 0;
            float t = percent / 50f;
            int index = Mathf.Clamp(Mathf.FloorToInt(t * basicCount), 0, basicCount - 1);
            mainSprite = spritesBasic[index];
        }
        else if (percent >= 50f && sprayingCount > 0)
        {
            stageIndex = 1;
            float t = (percent - 50f) / 50f;
            int index = Mathf.Clamp(Mathf.FloorToInt(t * sprayingCount), 0, sprayingCount - 1);
            mainSprite = spritesSpraying[index];
        }

        if (imageSprayPaint != null)
            imageSprayPaint.sprite = mainSprite;

        if (imageMask != null && spritesMask != null && spritesMask.Length > 0)
        {
            int maskIndex = Mathf.Clamp(stageIndex, 0, spritesMask.Length - 1);
            imageMask.sprite = spritesMask[maskIndex];
        }

        if (imageBack != null && spritesBack != null && spritesBack.Length > 0)
        {
            int backIndex = Mathf.Clamp(Mathf.FloorToInt(percent / 100f * spritesBack.Length), 0, spritesBack.Length - 1);
            imageBack.sprite = spritesBack[backIndex];
        }

        if (percent < 100f)
            StopSuperAnimation();
    }

    private IEnumerator AnimateSuperSprites()
    {
        if (spritesSuper == null || spritesSuper.Length == 0)
            yield break;

        while (_isCompleted && _currentGraffiti != null)
        {
            yield return new WaitForSeconds(_superAnimationInterval);

            if (!_isCompleted || _currentGraffiti == null)
                yield break;

            _superSpriteIndex = (_superSpriteIndex + 1) % spritesSuper.Length;

            if (imageSprayPaint != null)
                imageSprayPaint.sprite = spritesSuper[_superSpriteIndex];
        }
    }

    private void UpdateBackPosition()
    {
        if (imageBack == null || backRect == null)
            return;

        float t = backTargetPercent / 100f;
        Vector2 targetPosition = Vector2.Lerp(backMinPosition, backMaxPosition, t);
        float speed = Mathf.Lerp(minMoveSpeed, maxMoveSpeed, t);

        backRect.anchoredPosition = Vector2.MoveTowards(backRect.anchoredPosition, targetPosition, speed * Time.deltaTime);
    }

    private IEnumerator AnimateTo(Vector2 targetLeadPosition)
    {
        if (_leadRect == null)
            yield break;

        Vector2[] startPositions = new Vector2[_movingRects.Count];
        Vector2[] targetPositions = new Vector2[_movingRects.Count];

        Vector2 leadOffset = targetLeadPosition - _movingOriginalAnchoredPositions[0];

        for (int i = 0; i < _movingRects.Count; i++)
        {
            startPositions[i] = _movingRects[i].anchoredPosition;
            targetPositions[i] = _movingOriginalAnchoredPositions[i] + leadOffset;
        }

        float elapsed = 0f;

        while (elapsed < _moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = _moveCurve.Evaluate(Mathf.Clamp01(elapsed / _moveDuration));

            for (int i = 0; i < _movingRects.Count; i++)
                _movingRects[i].anchoredPosition = Vector2.Lerp(startPositions[i], targetPositions[i], t);

            yield return null;
        }

        for (int i = 0; i < _movingRects.Count; i++)
            _movingRects[i].anchoredPosition = targetPositions[i];
    }

    private IEnumerator AnimateOut()
    {
        yield return StartCoroutine(AnimateTo(_offScreenPosition));

        ResetBackToMin();
        _currentGraffiti = null;
        _isCompleted = false;
    }

    private void ResetBackToMin()
    {
        backTargetPercent = 0f;
        if (backRect != null)
            backRect.anchoredPosition = backMinPosition;
    }

    private void StopAnimation()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }

        StopSuperAnimation();
        StopAllCoroutines();
    }

    private void StopSuperAnimation()
    {
        if (_superAnimationCoroutine == null)
            return;

        StopCoroutine(_superAnimationCoroutine);
        _superAnimationCoroutine = null;
        _superSpriteIndex = 0;
    }
}
