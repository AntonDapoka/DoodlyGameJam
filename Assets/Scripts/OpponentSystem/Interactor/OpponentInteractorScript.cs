using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpponentInteractorScript : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GraffitiManagementInteractorScript _graffityInteractor;
    private GraffitiScript[] _graffitiSpots;
    private Transform[] _graffitiTransform;
    private GraffitiScript currentTarget;

    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float reachDistance = 0.1f;

    private void Start()
    {
        _graffitiSpots = _graffityInteractor.GetGraffitiSpots();

        _graffitiTransform = new Transform[_graffitiSpots.Length];
        for (int i = 0; i < _graffitiSpots.Length; i++)
        {
            if (_graffitiSpots[i] != null)
            {
                _graffitiTransform[i] = _graffitiSpots[i].transform;
            }
        }

        PickNewTarget();
    }

    private void Update()
    {
        if (currentTarget == null || _graffitiTransform.Length == 0) return;

        MoveToTarget();

        if (Vector3.Distance(transform.position, currentTarget.transform.position) <= reachDistance)
        {
            _graffityInteractor.UpdateRandomOpponentGraffitiSpot(currentTarget);
            PickNewTarget();

        }
    }

    private void MoveToTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, currentTarget.transform.position, speed * Time.deltaTime);
    }

    private void PickNewTarget()
    {
        if (_graffitiTransform.Length == 0) return;

        currentTarget = _graffityInteractor.SetRandomOpponentGraffitiSpot(gameObject.transform);
    }
}

