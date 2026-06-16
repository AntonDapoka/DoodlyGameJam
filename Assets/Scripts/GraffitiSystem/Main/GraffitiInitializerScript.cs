using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraffitiInitializerScript : MonoBehaviour
{
    [Header("References:")]
    [SerializeField] private GraffitiManagementInteractorScript _graffityInteractor;
    [SerializeField] private GraffitiPresenterScript _graffityPresenter;
    [SerializeField] private int _graffitiAmountInitial = 3;
    [SerializeField] private float _graffitiPerimeterMin = 30f;
    [SerializeField] private float _graffitiPerimeterMax = 90f;

    private void Start()
    {

        if (_graffityInteractor != null && _graffitiAmountInitial >= 0)
        {
            _graffityInteractor.SetRandomInitialOpponentGraffitiSpots(_graffitiAmountInitial, _graffitiPerimeterMax, _graffitiPerimeterMin);
        }
    }
}
