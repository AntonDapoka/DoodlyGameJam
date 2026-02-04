using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GraffitiManagementInteractorScript : MonoBehaviour
{
    [SerializeField] private GraffitiSpot[] _graffitiSpots;
    private List<GraffitiSpot> _graffitiSpotsValid = new();
    private List<GraffitiSpot> _graffitiSpotsActive = new();
    [SerializeField] private GraffitiPresenterScript _presenterGraffity;

    private void Start()
    {
        foreach (var spot in _graffitiSpots)
        {
            if (spot._objectGraffiti != null) _graffitiSpotsValid.Add(spot);
        }
    }

    public void SetRandomInitialGraffitiSpots(int amount, float maxPerimeter, float minPerimeter)
    {
        _graffitiSpotsActive = GetMultipleRandomGraffitiSpots(amount, maxPerimeter, minPerimeter);

        if (_graffitiSpotsActive != null)
        {
            foreach (GraffitiSpot graffitiSpot in _graffitiSpotsActive)
            {
                Debug.Log(graffitiSpot._objectGraffiti.name);
            }
        }
    }

    private List<GraffitiSpot> GetMultipleRandomGraffitiSpots(int amount, float maxPerimeter, float minPerimeter, int maxAttempts = 1000)
    {
        if (amount < 1 || amount > _graffitiSpotsValid.Count)
        {
            Debug.Log("Something isn't right");
            return null;
        }

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var subset = GetRandomSubset(_graffitiSpotsValid, amount);

            var positions = subset
                .Select(s => s._objectGraffiti.transform.position)
                .ToList();

            var hull = ConvexHullXZ(positions);
            float perimeter = CalculatePerimeter(hull);

            if (perimeter <= maxPerimeter && perimeter >= minPerimeter)
            {
                Debug.Log(perimeter);
                return subset;
            }
        }
        Debug.Log("WHAT THE FUCK IS A PEREMITAAAAAAAAAAAR *gunshots* *gunshots* *gunshots* *eagle*");
        return null;
    }


    private List<Vector3> ConvexHullXZ(List<Vector3> points)
    {
        if (points.Count < 3)
            return new List<Vector3>(points);

        List<Vector3> hull = new();

        Vector3 start = points[0];
        foreach (var p in points) if (p.x < start.x) start = p;

        Vector3 current = start;

        while (true)
        {
            hull.Add(current);
            Vector3 next = points[0];

            foreach (var candidate in points)
            {
                if (candidate == current) continue;

                float cross = Cross(current, next, candidate);

                if (next == current || cross < 0 || (Mathf.Approximately(cross, 0) &&
                    Vector3.Distance(current, candidate) > Vector3.Distance(current, next)))
                {
                    next = candidate;
                }
            }

            current = next;
            if (current == start)
                break;
        }

        return hull;
    }

    private float Cross(Vector3 o, Vector3 a, Vector3 b)
    {
        return (a.x - o.x) * (b.z - o.z) - (a.z - o.z) * (b.x - o.x);
    }

    private float CalculatePerimeter(List<Vector3> hull)
    {
        float perimeter = 0f;

        for (int i = 0; i < hull.Count; i++)
        {
            Vector3 a = hull[i];
            Vector3 b = hull[(i + 1) % hull.Count];
            perimeter += Vector3.Distance(a, b);
        }

        return perimeter;
    }

    private List<GraffitiSpot> GetRandomSubset(List<GraffitiSpot> source, int count)
    {
        List<GraffitiSpot> temp = new(source);
        List<GraffitiSpot> result = new();

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, temp.Count);
            result.Add(temp[index]);
            temp.RemoveAt(index);
        }

        return result;
    }

}

[System.Serializable]
public struct GraffitiSpot
{
    public GameObject _objectGraffiti;
    public GameObject _objectGraffitiHint;
}