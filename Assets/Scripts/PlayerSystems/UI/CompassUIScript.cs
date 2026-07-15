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
    [SerializeField] private float refreshInterval = 0.5f;

    [Header("Static Marks")]
    [SerializeField] private List<CompassMark> marks = new();

    [Header("World Targets")]
    [SerializeField] private RectTransform worldIconPrefab;
    [SerializeField] private Sprite graffitiSprite;
    [SerializeField] private Sprite opponentSprite;

    private readonly List<WorldCompassTarget> worldTargets = new();
    private float _refreshTimer;

    private void Start()
    {
        RefreshWorldTargets();
        UpdateCompass();
    }

    private void Update()
    {
        _refreshTimer += Time.deltaTime;
        if (_refreshTimer >= refreshInterval)
        {
            _refreshTimer = 0f;
            RefreshWorldTargets();
        }

        UpdateCompass();
    }

    public void RefreshWorldTargets()
    {
        // Remove destroyed, missing or no-longer-visible markers.
        for (int i = worldTargets.Count - 1; i >= 0; i--)
        {
            WorldCompassTarget target = worldTargets[i];
            if (target.Target == null)
            {
                RemoveTargetAt(i);
                continue;
            }

            GraffitiMarker graffitiMarker = target.Target.GetComponent<GraffitiMarker>();
            if (graffitiMarker != null && !IsMarkerVisible(graffitiMarker))
            {
                RemoveTargetAt(i);
            }
        }

        // Find all active graffiti markers that belong to the opponent.
        GraffitiMarker[] graffitiMarkers = FindObjectsOfType<GraffitiMarker>();
        foreach (GraffitiMarker marker in graffitiMarkers)
        {
            if (marker == null) continue;
            if (!IsMarkerVisible(marker)) continue;

            if (!IsTracked(marker.transform))
            {
                AddWorldTarget(marker.transform, graffitiSprite);
            }
        }

        // Find all active opponent markers.
        OpponentMarker[] opponentMarkers = FindObjectsOfType<OpponentMarker>();
        foreach (OpponentMarker marker in opponentMarkers)
        {
            if (marker == null) continue;
            if (!marker.gameObject.activeInHierarchy) continue;

            if (!IsTracked(marker.transform))
            {
                AddWorldTarget(marker.transform, opponentSprite);
            }
        }

        // Hide icons for targets that became inactive.
        foreach (WorldCompassTarget target in worldTargets)
        {
            if (target.Target != null)
            {
                target.Rect.gameObject.SetActive(target.Target.gameObject.activeInHierarchy);
            }
        }
    }

    public void AddWorldTarget(Transform target, Sprite sprite)
    {
        if (target == null || worldIconPrefab == null || compassRoot == null) return;

        RectTransform icon = Instantiate(worldIconPrefab, compassRoot);
        icon.name = $"CompassIcon_{target.gameObject.name}";

        Image image = icon.GetComponent<Image>();
        if (image != null && sprite != null)
            image.sprite = sprite;

        worldTargets.Add(new WorldCompassTarget
        {
            Target = target,
            Rect = icon,
            Icon = image
        });
    }

    private void RemoveTargetAt(int index)
    {
        if (index < 0 || index >= worldTargets.Count) return;

        WorldCompassTarget target = worldTargets[index];
        if (target.Rect != null)
            Destroy(target.Rect.gameObject);

        worldTargets.RemoveAt(index);
    }

    private bool IsTracked(Transform target)
    {
        foreach (WorldCompassTarget worldTarget in worldTargets)
        {
            if (worldTarget.Target == target)
                return true;
        }

        return false;
    }

    private bool IsMarkerVisible(GraffitiMarker marker)
    {
        if (!marker.gameObject.activeInHierarchy) return false;

        GraffitiScript graffiti = marker.GetComponent<GraffitiScript>();
        if (graffiti != null)
        {
            if (!graffiti.GetIsTurnOn()) return false;
            if (graffiti.GetGraffitiType() != GraffitiType.Opponent) return false;
        }

        return true;
    }

    private void UpdateCompass()
    {
        if (player == null) return;

        float playerYaw = player.eulerAngles.y;

        foreach (var mark in marks)
        {
            if (mark.Rect == null) continue;
            UpdateElement(mark.WorldAngle, mark.Rect, playerYaw);
        }

        foreach (var target in worldTargets)
        {
            if (!target.Target) continue;
            if (!target.Target.gameObject.activeInHierarchy) continue;

            Vector3 dir = target.Target.position - player.position;
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.0001f) continue;

            float worldAngle = Quaternion.LookRotation(dir).eulerAngles.y;
            UpdateElement(worldAngle, target.Rect, playerYaw, true);
        }
    }

    private void UpdateElement(float worldAngle, RectTransform rect, float playerYaw, bool alwaysShowEdge = false)
    {
        if (rect == null) return;

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
