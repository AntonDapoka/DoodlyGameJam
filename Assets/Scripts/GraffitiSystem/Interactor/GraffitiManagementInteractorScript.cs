using System.Collections.Generic;
using UnityEngine;

public class GraffitiManagementInteractorScript : MonoBehaviour
{
    [Header("References:")]
    [SerializeField] private GraffitiScript[] _graffitiSpots;
    [SerializeField] private GraffitiPresenterScript _graffitiPresenter;
    [SerializeField] private GraffitiJarvisAlgorithmFinderScript _graffitiJarvisAlgorithmFinder;
    [SerializeField] private GraffitiRandomFinderScript _graffitiRandomFinder;

    public List<GraffitiScript> _graffitiSpotsValid = new();
    public List<GraffitiScript> _graffitiSpotsActive = new();

    private void Start()
    {
        foreach (var spot in _graffitiSpots)
        {
            if (spot != null) _graffitiSpotsValid.Add(spot);
        }
    }

    public void SetRandomInitialOpponentGraffitiSpots(int amount, float maxPerimeter, float minPerimeter)
    {
        _graffitiSpotsActive = _graffitiJarvisAlgorithmFinder.GetMultipleRandomGraffitiSpots(_graffitiSpotsValid, amount, maxPerimeter, minPerimeter);
       
        if (_graffitiSpotsActive != null)
        {
            foreach (GraffitiScript graffitiSpot in _graffitiSpotsActive)
            {
                if (graffitiSpot == null) continue;

                graffitiSpot.TurnOnOpponentGraffiti();

                _graffitiSpotsValid.Remove(graffitiSpot);
            }
        }

        UpdateGraffitiSpots();
    }

    public GraffitiScript SetRandomOpponentGraffitiSpot(Transform lastSpotTransform)
    {
        UpdateGraffitiSpots();

        GraffitiScript newSpot = _graffitiRandomFinder.GetRandomGraffitiSpotInDistance(_graffitiSpotsValid, lastSpotTransform);

        if (newSpot == null) return null;
        else return newSpot; 
    }

    public void UpdateRandomOpponentGraffitiSpot(GraffitiScript newSpot)
    {
        UpdateGraffitiSpots();

        if (newSpot == null) return;

        _graffitiSpotsActive.Add(newSpot);

        _graffitiSpotsValid.Remove(newSpot);

        if (newSpot.GetIsTurnOn() && newSpot.GetIsGraffitiPlayer())
            newSpot.RedrawGraffitiFromPlayerToOpponent();
        else if (!newSpot.GetIsTurnOn())
            newSpot.TurnOnOpponentGraffiti();
        else
            Debug.Log("SOME BUG IDK");

        UpdateGraffitiSpots();
    }

    private void Update()
    {
        //UpdateGraffitiSpots();
    }

    private void UpdateGraffitiSpots()
    {
        foreach (GraffitiScript graffiti in _graffitiSpots)
        {

            bool isTurnedOn = graffiti.GetIsTurnOn();
            bool isPlayer = graffiti.GetIsGraffitiPlayer();

            if ((!isTurnedOn || isPlayer))
            {
                if (!_graffitiSpotsValid.Contains(graffiti))
                    _graffitiSpotsValid.Add(graffiti);
            }
            else _graffitiSpotsValid.Remove(graffiti);

            if (isTurnedOn && !isPlayer)
            {
                if (!_graffitiSpotsActive.Contains(graffiti))
                    _graffitiSpotsActive.Add(graffiti);
            }
            else _graffitiSpotsActive.Remove(graffiti);
        }
    }

    public GraffitiScript[] GetGraffitiSpots()
    {
        return _graffitiSpots;
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