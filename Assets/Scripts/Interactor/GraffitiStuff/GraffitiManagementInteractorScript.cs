using System.Collections.Generic;
using UnityEngine;

public class GraffitiManagementInteractorScript : MonoBehaviour
{
    [Header("References:")]
    [SerializeField] private GraffitiScript[] _graffitiSpots;
    [SerializeField] private GraffitiPresenterScript _graffitiPresenter;
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
                if (graffitiSpot == null) continue;

                graffitiSpot.TurnOn();

                _graffitiSpotsValid.Remove(graffitiSpot);
            }
        }
    }

    public void SetRandomGraffitiSpot(GraffitiScript lastSpot)
    {
        UpdateGraffitiSpots();

        GraffitiScript newSpot = _graffitiRandomFinder.GetRandomGraffitiSpotInDistance(_graffitiSpotsValid, lastSpot.transform);

        if (newSpot == null) return;

        _graffitiSpotsActive.Add(newSpot);

        _graffitiSpotsValid.Remove(newSpot);

        newSpot.TurnOn();
        Debug.Log(newSpot.name);
    }

    private void UpdateGraffitiSpots()
    {
        foreach (GraffitiScript graffiti in _graffitiSpots)
        {
            if (!graffiti.GetIsTurnOn() && !_graffitiSpotsValid.Contains(graffiti))
            {
                _graffitiSpotsValid.Add(graffiti);
            }

            if (graffiti.GetIsTurnOn() && !_graffitiSpotsActive.Contains(graffiti))
            {
                _graffitiSpotsActive.Add(graffiti);
            }
        }
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