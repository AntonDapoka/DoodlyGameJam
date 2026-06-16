using System.Collections.Generic;
using UnityEngine;

public class GraffitiRandomFinderScript : MonoBehaviour
{
    [Header("Minimum distance range")]
    [SerializeField] private float _distanceMinMin;
    [SerializeField] private float _distanceMinMax;

    [Header("Maximum distance range")]
    [SerializeField] private float _distanceMaxMin;
    [SerializeField] private float _distanceMaxMax;

    [Header("Fallback")]
    [SerializeField] private float _distanceSafeMin;


    public GraffitiScript GetRandomGraffitiSpotInDistance(List<GraffitiScript> graffitiSpots, Transform pointStart)
    {
        float minDistance = Random.Range(_distanceMinMin, _distanceMinMax);
        float maxDistance = Random.Range(_distanceMaxMin, _distanceMaxMax);

        if (minDistance > maxDistance)
            (maxDistance, minDistance) = (minDistance, maxDistance);

        float minSqr = minDistance * minDistance;
        float maxSqr = maxDistance * maxDistance;
        float safeDistanceSqr = _distanceSafeMin * _distanceSafeMin;

        List<GraffitiScript> candidates = new();

        foreach (var spot in graffitiSpots)
        {
            if (spot == null) continue;

            float sqrDist = (spot.transform.position - pointStart.position).sqrMagnitude;

            if (sqrDist >= minSqr && sqrDist <= maxSqr)
            {
                candidates.Add(spot);
            }
        }

        if (candidates.Count == 0)
        {
            Debug.Log("Plan B");

            foreach (var spot in graffitiSpots)
            {
                if (spot == null) continue;

                float sqrDist = (spot.transform.position - pointStart.position).sqrMagnitude;

                if (sqrDist > safeDistanceSqr)
                    candidates.Add(spot);
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                int rand = Random.Range(i, candidates.Count);
                (candidates[rand], candidates[i]) = (candidates[i], candidates[rand]);
            }
        }

        if (candidates.Count == 0) return default;

        return candidates[Random.Range(0, candidates.Count)];
    }
}