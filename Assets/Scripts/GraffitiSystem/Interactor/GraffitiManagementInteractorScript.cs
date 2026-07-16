using System.Collections.Generic;
using UnityEngine;

public class GraffitiManagementInteractorScript : MonoBehaviour
{
    [Header("References:")]
    [SerializeField] private GraffitiScript[] _graffitiSpots;
    [SerializeField] private GraffitiJarvisAlgorithmFinderScript _graffitiJarvisAlgorithmFinder;
    [SerializeField] private GraffitiRandomFinderScript _graffitiRandomFinder;

    [HideInInspector] private List<GraffitiScript> _graffitiSpotsValid = new();
    [HideInInspector] private List<GraffitiScript> _graffitiSpotsActive = new();

    private void Awake()
    {
        if (_graffitiSpotsValid == null)
            _graffitiSpotsValid = new List<GraffitiScript>();
        if (_graffitiSpotsActive == null)
            _graffitiSpotsActive = new List<GraffitiScript>();

        foreach (var spot in _graffitiSpots)
        {
            if (spot != null)
                _graffitiSpotsValid.Add(spot);
        }
    }

    public void SetRandomInitialOpponentGraffitiSpots(int amount, float maxPerimeter, float minPerimeter)
    {
        if (_graffitiSpotsValid == null)
            _graffitiSpotsValid = new List<GraffitiScript>();
        if (_graffitiSpotsActive == null)
            _graffitiSpotsActive = new List<GraffitiScript>();

        if (amount <= 0)
            return;

        UpdateGraffitiSpots();

        // Opponent initial spots should be chosen from inactive graffiti only,
        // so they do not overwrite player spots seeded just before this call.
        List<GraffitiScript> candidateSpots = new();
        foreach (GraffitiScript spot in _graffitiSpotsValid)
        {
            if (spot == null) continue;
            if (!spot.GetIsTurnOn())
                candidateSpots.Add(spot);
        }

        if (amount > candidateSpots.Count)
        {
            Debug.LogWarning($"[GraffitiManagementInteractorScript] Requested {amount} opponent spots, but only {candidateSpots.Count} inactive spots are available. Clamping.");
            amount = candidateSpots.Count;
        }

        if (amount <= 0)
            return;

        List<GraffitiScript> opponentSpots = _graffitiJarvisAlgorithmFinder.GetMultipleRandomGraffitiSpots(candidateSpots, amount, maxPerimeter, minPerimeter);

        if (opponentSpots == null)
        {
            Debug.LogWarning($"[GraffitiManagementInteractorScript] Could not find opponent spots matching perimeter constraints ({minPerimeter}-{maxPerimeter}). Falling back to random selection.");
            opponentSpots = GetRandomSubset(candidateSpots, amount);
        }

        foreach (GraffitiScript graffitiSpot in opponentSpots)
        {
            if (graffitiSpot == null) continue;

            graffitiSpot.TurnOnOpponentGraffiti();
            _graffitiSpotsValid.Remove(graffitiSpot);
        }

        UpdateGraffitiSpots();
    }

    public void SetRandomInitialPlayerGraffitiSpots(int amount)
    {
        if (amount <= 0) return;

        List<GraffitiScript> playerSpots = new();
        int attempts = 0;

        while (playerSpots.Count < amount && attempts < amount * 10 && _graffitiSpotsValid.Count > 0)
        {
            attempts++;
            int index = Random.Range(0, _graffitiSpotsValid.Count);
            GraffitiScript spot = _graffitiSpotsValid[index];

            if (spot == null || playerSpots.Contains(spot)) continue;

            playerSpots.Add(spot);
        }

        foreach (GraffitiScript graffitiSpot in playerSpots)
        {
            if (graffitiSpot == null) continue;

            graffitiSpot.TurnOnPlayerGraffiti();
            _graffitiSpotsValid.Remove(graffitiSpot);
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

    /// <summary>
    /// Returns a random valid graffiti spot that is not already owned by the opponent.
    /// Used as a fallback when the distance-based finder cannot find a target.
    /// </summary>
    public GraffitiScript GetFallbackOpponentGraffitiSpot()
    {
        UpdateGraffitiSpots();

        if (_graffitiSpotsValid == null || _graffitiSpotsValid.Count == 0)
            return null;

        List<GraffitiScript> candidates = new();
        foreach (GraffitiScript spot in _graffitiSpotsValid)
        {
            if (spot == null) continue;
            if (!spot.GetIsTurnOn() || spot.GetGraffitiType() != GraffitiType.Opponent)
                candidates.Add(spot);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    public void UpdateRandomOpponentGraffitiSpot(GraffitiScript newSpot)
    {
        UpdateGraffitiSpots();

        if (newSpot == null) return;

        _graffitiSpotsActive.Add(newSpot);

        _graffitiSpotsValid.Remove(newSpot);

        if (newSpot.GetIsTurnOn() && newSpot.GetGraffitiType() == GraffitiType.Player)
            newSpot.RedrawGraffitiFromPlayerToOpponent();
        else if (!newSpot.GetIsTurnOn())
            newSpot.TurnOnOpponentGraffiti();
        else
            Debug.Log("SOME BUG IDK");

        UpdateGraffitiSpots();
    }
    private void UpdateGraffitiSpots()
    {
        if (_graffitiSpotsValid == null)
            _graffitiSpotsValid = new List<GraffitiScript>();
        if (_graffitiSpotsActive == null)
            _graffitiSpotsActive = new List<GraffitiScript>();

        foreach (GraffitiScript graffiti in _graffitiSpots)
        {
            if (graffiti == null) continue;

            bool isTurnedOn = graffiti.GetIsTurnOn();
            GraffitiType graffitiType = graffiti.GetGraffitiType();

            if (!isTurnedOn || graffitiType == GraffitiType.Player)
            {
                if (!_graffitiSpotsValid.Contains(graffiti))
                    _graffitiSpotsValid.Add(graffiti);
            }
            else _graffitiSpotsValid.Remove(graffiti);

            if (isTurnedOn && graffitiType == GraffitiType.Opponent)
            {
                if (!_graffitiSpotsActive.Contains(graffiti))
                    _graffitiSpotsActive.Add(graffiti);
            }
            else _graffitiSpotsActive.Remove(graffiti);
        }
    }

    private List<GraffitiScript> GetRandomSubset(List<GraffitiScript> source, int count)
    {
        List<GraffitiScript> temp = new(source);
        List<GraffitiScript> result = new();

        for (int i = 0; i < count && temp.Count > 0; i++)
        {
            int index = Random.Range(0, temp.Count);
            result.Add(temp[index]);
            temp.RemoveAt(index);
        }

        return result;
    }

    public GraffitiScript[] GetGraffitiSpots()
    {
        return _graffitiSpots;
    }
}