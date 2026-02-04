using System;
using UnityEngine;

public class EnvironmentInitializer : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject[] environment;

    private void Awake()
    {
        if (player == null) return;

        foreach (var obj in environment)
        {
            if (obj != null && obj.TryGetComponent(out LookAtPlayerBase lookAtPlayer))
            {
                lookAtPlayer.SetPlayer(player.transform);
            }
        }
    }

    private void Update()
    {
        
    }
}
