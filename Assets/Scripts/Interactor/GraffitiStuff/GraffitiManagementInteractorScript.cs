using System.Collections.Generic;
using UnityEngine;

public class GraffitiManagementInteractorScript : MonoBehaviour
{
    [Header("References:")]
    [SerializeField] private GraffitiSpot[] _graffitiSpots;
    [SerializeField] private GraffitiPresenterScript _presenterGraffity;
    [SerializeField] private GraffitiJarvisAlgorithmFinderScript _graffitiJarvisAlgorithmFinder;
    [SerializeField] private GraffitiRandomFinderScript _graffitiRandomFinder;

    private List<GraffitiSpot> _graffitiSpotsValid = new();
    private List<GraffitiSpot> _graffitiSpotsActive = new();

    private void Start()
    {
        foreach (var spot in _graffitiSpots)
        {
            if (spot._objectGraffiti != null) _graffitiSpotsValid.Add(spot);
        }
    }

    public void SetRandomInitialGraffitiSpots(int amount, float maxPerimeter, float minPerimeter)
    {
        _graffitiSpotsActive = _graffitiJarvisAlgorithmFinder.GetMultipleRandomGraffitiSpots(_graffitiSpotsValid, amount, maxPerimeter, minPerimeter);

        if (_graffitiSpotsActive != null)
        {
            foreach (GraffitiSpot graffitiSpot in _graffitiSpotsActive)
            {
                Debug.Log(graffitiSpot._objectGraffiti.name);
            }
        }
    }

    public void SetRandomGraffitiSpot(GraffitiSpot lastSpot)
    {
        _graffitiSpotsActive.Add(_graffitiRandomFinder.GetRandomGraffitiSpotInDistance(_graffitiSpotsValid, lastSpot._objectGraffiti.transform));
        Debug.Log(_graffitiSpotsActive[_graffitiSpotsActive.Count-1]._objectGraffiti.name);
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

[System.Serializable]
public struct GraffitiSpot
{
    public GameObject _objectGraffiti;
    public GameObject _objectGraffitiHint;
}