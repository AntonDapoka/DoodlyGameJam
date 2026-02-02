using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkateBoardSpriteManager : MonoBehaviour
{
    [SerializeField] private GameObject skateboard;

    [SerializeField] private Sprite spriteSkateboardIdle;
    [SerializeField] private Sprite spriteSkateboardLeft;
    [SerializeField] private Sprite spriteSkateboardRight;

    [SerializeField] private KeyCode keyRight;
    [SerializeField] private KeyCode keyLeft;

    private SpriteRenderer spriteRenderer;

    private bool pressedLeft;
    private bool pressedRight;

    private void Awake()
    {
        spriteRenderer = skateboard.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = spriteSkateboardIdle;
    }

    private void Update()
    {
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
}
