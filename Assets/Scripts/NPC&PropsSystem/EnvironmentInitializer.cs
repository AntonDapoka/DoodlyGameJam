using System.Collections.Generic;
using UnityEngine;

public class EnvironmentInitializer : MonoBehaviour
{
    public static EnvironmentInitializer Instance { get; private set; }

    [SerializeField] private GameObject player;
    private List<GameObject> environment = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddAnProp(GameObject prop)
    {
        environment.Add(prop);
    }

    public GameObject GetPlayer()
    {
        return player;
    }
}