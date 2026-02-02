using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraffitiLocationManagementScript : MonoBehaviour
{
    [SerializeField] private GraffitiSpot[] graffitiSpots;
    [SerializeField] private CompassUIScript compass;

    private void Start()
    {
        compass.AddWorldTarget(graffitiSpots[0].graffity.GetComponent<Transform>());
    }
}

[System.Serializable]
public struct GraffitiSpot
{
    public GameObject graffity;
    public GameObject icon;
}