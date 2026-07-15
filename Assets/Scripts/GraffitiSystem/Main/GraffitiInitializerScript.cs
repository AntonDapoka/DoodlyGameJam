using UnityEngine;
using UnityEngine.Serialization;

public class GraffitiInitializerScript : MonoBehaviour
{
    [Header("References:")]
    [SerializeField] private GraffitiManagementInteractorScript _graffityInteractor;
    [SerializeField] private GraffitiPresenterScript _graffityPresenter;

    [Header("Opponent Initial Graffiti")]
    [FormerlySerializedAs("_graffitiAmountInitial")]
    [SerializeField] private int _opponentInitialAmount = 3;
    [FormerlySerializedAs("_graffitiPerimeterMin")]
    [SerializeField] private float _opponentPerimeterMin = 30f;
    [FormerlySerializedAs("_graffitiPerimeterMax")]
    [SerializeField] private float _opponentPerimeterMax = 90f;

    [Header("Player Initial Graffiti")]
    [SerializeField] private int _playerInitialAmount = 3;

    private void Start()
    {
        if (_graffityInteractor == null) return;

        if (_playerInitialAmount >= 0)
            _graffityInteractor.SetRandomInitialPlayerGraffitiSpots(_playerInitialAmount);

        if (_opponentInitialAmount >= 0)
            _graffityInteractor.SetRandomInitialOpponentGraffitiSpots(_opponentInitialAmount, _opponentPerimeterMax, _opponentPerimeterMin);
    }
}
