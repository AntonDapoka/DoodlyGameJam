using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SprayPaintUIScript : MonoBehaviour
{
    [SerializeField] private Image imageSprayPaint;
    [SerializeField] private Image imageMask;
    [SerializeField] private Image imageBack;

    [Header("Sprites")]
    [SerializeField] private Sprite[] spritesBasic;
    [SerializeField] private Sprite[] spritesSpraying;
    [SerializeField] private Sprite[] spritesSuper;
    [SerializeField] private Sprite[] spritesMask;
    [SerializeField] private Sprite[] spritesBack;

    [Header("Settings")]
    [SerializeField] private float changeInterval = 0.2f;

    [Header("Back Movement")]
    [SerializeField] private Vector2 backMinPosition;
    [SerializeField] private Vector2 backMaxPosition;
    [SerializeField] private float minMoveSpeed = 200f;
    [SerializeField] private float maxMoveSpeed = 800f;

    private float backTargetPercent;
    private RectTransform backRect;

    private Sprite[] currentSet;
    private int lastSpriteIndex = -1;
    private int lastSpriteBackIndex = -1;
    private Coroutine switchCoroutine;

    private void Start()
    {
        backRect = imageBack.rectTransform;
        backRect.anchoredPosition = backMinPosition;
        SetSpritesBasic();
        StartCoroutine(SwitchSpriteMaskRoutine());
    }
    private IEnumerator SwitchSpriteRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(changeInterval);
            ChangeSprite();
        }
    }

    private IEnumerator SwitchSpriteMaskRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(changeInterval);
            ChangeSpriteBack();
        }
    }

    private void ChangeSprite()
    {
        if (currentSet == null || currentSet.Length == 0 ||
            imageSprayPaint == null || imageMask == null)
            return;

        int newIndex = GetRandomIndex(currentSet.Length, ref lastSpriteIndex);

        imageSprayPaint.sprite = currentSet[newIndex];
        imageMask.sprite = spritesMask[newIndex];
    }

    private void ChangeSpriteBack()
    {
        if (spritesBack == null || spritesBack.Length == 0 || imageBack == null)
            return;

        int newIndex = GetRandomIndex(spritesBack.Length, ref lastSpriteBackIndex);
        imageBack.sprite = spritesBack[newIndex];
    }

    private int GetRandomIndex(int length, ref int lastIndex)
    {
        int newIndex;

        if (length == 1)
        {
            newIndex = 0;
        }
        else
        {
            do newIndex = Random.Range(0, length);
            while (newIndex == lastIndex);
        }

        lastIndex = newIndex;
        return newIndex;
    }

    private void SetCurrentSprites(Sprite[] newSprites)
    {
        currentSet = newSprites;
        lastSpriteIndex = -1;

        if (switchCoroutine != null)
            StopCoroutine(switchCoroutine);

        switchCoroutine = StartCoroutine(SwitchSpriteRoutine());
        ChangeSprite();
    }

    public void SetSpritesBasic()
    {
        SetCurrentSprites(spritesBasic);
    }

    public void SetSpritesSpraying()
    {
        SetCurrentSprites(spritesSpraying);
    }

    public void SetSpritesSuper()
    {
        SetCurrentSprites(spritesSuper);
    }

    public void SetBackPositionPercent(float percent)
    {
        backTargetPercent = Mathf.Clamp(percent, 0f, 100f);
    }

    private void Update()
    {
        UpdateBackPosition();
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

}