using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CompassUIScript : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private RectTransform compassRoot;

    [Header("Settings")]
    [SerializeField] private float compassWidth = 800f;
    [SerializeField] private float visibleAngle = 90f;

    [Header("Static Marks")]
    [SerializeField] private List<CompassMark> marks = new();

    [Header("World Targets")]
    [SerializeField] private RectTransform worldIconPrefab;
    [SerializeField] private Sprite graffitiSprite;
    [SerializeField] private Sprite opponentSprite;

    private readonly List<WorldCompassTarget> worldTargets = new();

    private void Awake()
    {
        UpdateCompass();
    }

    private void Update()
    {
        UpdateCompass();
    }

    public void AddWorldTarget(Transform target)
    {
        Debug.Log("ddsfs");
        RectTransform icon = Instantiate(worldIconPrefab, compassRoot);
        Image image = icon.GetComponent<Image>();

        if (target.GetComponent<GraffitiMarker>())
            image.sprite = graffitiSprite;
        else if (target.GetComponent<OpponentMarker>())
            image.sprite = opponentSprite;

        worldTargets.Add(new WorldCompassTarget
        {
            Target = target,
            Rect = icon,
            Icon = image
        });
    }

    private void UpdateCompass()
    {
        float playerYaw = player.eulerAngles.y;

        foreach (var mark in marks)
        {
            UpdateElement(mark.WorldAngle, mark.Rect, playerYaw);
        }

        foreach (var target in worldTargets)
        {
            if (!target.Target) continue;

            Vector3 dir = target.Target.position - player.position;
            dir.y = 0f;

            float worldAngle = Quaternion.LookRotation(dir).eulerAngles.y;

            UpdateElement(worldAngle, target.Rect, player.eulerAngles.y, true);
        }
    }

    private void UpdateElement(float worldAngle, RectTransform rect, float playerYaw, bool alwaysShowEdge = false)
    {
        float deltaAngle = Mathf.DeltaAngle(playerYaw, worldAngle);
        bool visible = Mathf.Abs(deltaAngle) <= visibleAngle;

        if (visible)
        {
            rect.gameObject.SetActive(true);
            float normalized = deltaAngle / visibleAngle;
            float xPos = normalized * (compassWidth * 0.5f);
            Vector2 pos = rect.anchoredPosition;
            pos.x = xPos;
            rect.anchoredPosition = pos;
        }
        else if (alwaysShowEdge)
        {
            rect.gameObject.SetActive(true);
            float clampedDelta = Mathf.Sign(deltaAngle) * visibleAngle;
            float normalized = clampedDelta / visibleAngle;
            float xPos = normalized * (compassWidth * 0.5f);
            Vector2 pos = rect.anchoredPosition;
            pos.x = xPos;
            rect.anchoredPosition = pos;
        }
        else
        {
            rect.gameObject.SetActive(false);
        }
    }
}

[System.Serializable]
public class CompassMark
{
    [Range(0f, 360f)]
    public float WorldAngle;

    public RectTransform Rect;
}

[System.Serializable]
public class WorldCompassTarget
{
    public Transform Target;
    public RectTransform Rect;
    public Image Icon;
}
