using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkateBoardSpriteManager : MonoBehaviour
{
    [SerializeField] private GameObject skateboard;

    [SerializeField] private Sprite spriteSkateboardIdle;
    [SerializeField] private Sprite spriteSkateboardLeft;
    [SerializeField] private Sprite spriteSkateboardRight;

    [SerializeField] private Sprite[] animationSprites; 
    [SerializeField] private float animationFrameTime = 0.1f;

    [SerializeField] private KeyCode keyRight;
    [SerializeField] private KeyCode keyLeft;
    [SerializeField] private KeyCode animationKey = KeyCode.Space;

    private SpriteRenderer spriteRenderer;

    private bool pressedLeft;
    private bool pressedRight;
    private bool isAnimating = false;

    private void Awake()
    {
        spriteRenderer = skateboard.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = spriteSkateboardIdle;
    }

    private void Update()
    {
        if (isAnimating)
        {
            return;
        }

        if (Input.GetKeyDown(animationKey) && animationSprites != null && animationSprites.Length > 0)
        {
            StartCoroutine(PlayAnimation());
            return;
        }

        pressedLeft = Input.GetKey(keyLeft);
        pressedRight = Input.GetKey(keyRight);

        if (pressedLeft == pressedRight)
        {
            spriteRenderer.sprite = spriteSkateboardIdle;
        }
        else if (pressedLeft)
        {
            spriteRenderer.sprite = spriteSkateboardLeft;
        }
        else if (pressedRight)
        {
            spriteRenderer.sprite = spriteSkateboardRight;
        }
    }

    private IEnumerator PlayAnimation()
    {
        isAnimating = true; 

        foreach (Sprite frame in animationSprites)
        {
            spriteRenderer.sprite = frame;
            yield return new WaitForSeconds(animationFrameTime);
        }

        spriteRenderer.sprite = spriteSkateboardIdle;
        isAnimating = false;
    }
}