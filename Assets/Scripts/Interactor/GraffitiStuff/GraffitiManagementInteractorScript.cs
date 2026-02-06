using System.Collections.Generic;
using UnityEngine;

public class GraffitiManagementInteractorScript : MonoBehaviour
{
    [Header("References:")]
    [SerializeField] private GraffitiScript[] _graffitiSpots;
    [SerializeField] private GraffitiPresenterScript _presenterGraffity;
    [SerializeField] private GraffitiJarvisAlgorithmFinderScript _graffitiJarvisAlgorithmFinder;
    [SerializeField] private GraffitiRandomFinderScript _graffitiRandomFinder;

    private readonly List<GraffitiScript> _graffitiSpotsValid = new();
    private List<GraffitiScript> _graffitiSpotsActive = new();

    private void Start()
    {
        foreach (var spot in _graffitiSpots)
        {
            if (spot != null) _graffitiSpotsValid.Add(spot);
        }
    }

    public void SetRandomInitialGraffitiSpots(int amount, float maxPerimeter, float minPerimeter)
    {
        _graffitiSpotsActive = _graffitiJarvisAlgorithmFinder.GetMultipleRandomGraffitiSpots(_graffitiSpotsValid, amount, maxPerimeter, minPerimeter);

        if (_graffitiSpotsActive != null)
        {
            foreach (GraffitiScript graffitiSpot in _graffitiSpotsActive)
            {
                graffitiSpot.TurnOn();
            }
        }
    }

    public void SetRandomGraffitiSpot(GraffitiScript lastSpot)
    {
        GraffitiScript newSpot = _graffitiRandomFinder.GetRandomGraffitiSpotInDistance(_graffitiSpotsValid, lastSpot.transform);
        _graffitiSpotsActive.Add(newSpot);
        newSpot.TurnOn();
        Debug.Log(newSpot.name);
    }
    /*
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            int rand = Random.Range(0, _graffitiSpotsValid.Count);
            SetRandomGraffitiSpot(_graffitiSpotsValid[rand]);
        }
    }*/
}