using UnityEngine;
using UnityEngine.Splines;

public class SplineGrindRailSetup : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private int splineIndex;

    private void Awake()
    {
        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();

        if (splineContainer == null)
        {
            Debug.LogError($"[{nameof(SplineGrindRailSetup)}] SplineContainer is missing on {gameObject.name}.", this);
            enabled = false;
            return;
        }

        foreach (var marker in GetComponentsInChildren<GrindableMarker>(true))
        {
            marker.Setup(splineContainer, splineIndex);
        }
    }

    private void OnValidate()
    {
        if (splineIndex < 0)
            splineIndex = 0;

        if (splineContainer != null && splineIndex >= splineContainer.Splines.Count)
            splineIndex = Mathf.Max(0, splineContainer.Splines.Count - 1);
    }
}
