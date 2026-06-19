using UnityEngine;
using UnityEngine.Splines;

public class GrindableMarker : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private int splineIndex;

    public SplineContainer SplineContainer => splineContainer;
    public int SplineIndex => splineIndex;

    public void Setup(SplineContainer container, int index)
    {
        splineContainer = container;
        splineIndex = index;
    }
}
