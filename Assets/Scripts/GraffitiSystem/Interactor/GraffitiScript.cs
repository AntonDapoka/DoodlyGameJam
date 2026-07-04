using UnityEngine;

public class GraffitiScript : MonoBehaviour, IInteractable
{
    [SerializeField] private GraffitiManagementInteractorScript _graffitiManagementInteractor;
    [SerializeField] private GraffitiPresenterScript _graffitiPresenter;
    [SerializeField] private GameObject _objectGraffitiHint;
    public bool _isTurnOn = false;
    public GraffitiType _graffitiType; // true = Player, false = Opponent

    private void Awake()
    {
        gameObject.SetActive(false);
        _objectGraffitiHint.SetActive(false);
        _graffitiType = GraffitiType.Opponent;
    }

    public void Interact()
    {
        if (_graffitiType == GraffitiType.Opponent)
            RedrawGraffitiFromOpponentToPlayer();
    }

    public void TurnOnPlayerGraffiti()
    {
        _isTurnOn = true;
        _graffitiType = GraffitiType.Player;
        gameObject.SetActive(true);

        _graffitiPresenter.ManageGraffitiSprite(this, true);
    }
    
    public void TurnOnOpponentGraffiti()
    {
        _isTurnOn = true;
        _graffitiType = GraffitiType.Opponent;
        gameObject.SetActive(true);
        _objectGraffitiHint.SetActive(true);

        _graffitiPresenter.ManageGraffitiSprite(this, false);
    }

    public void RedrawGraffitiFromOpponentToPlayer()
    {
        _graffitiType = GraffitiType.Player;
        _objectGraffitiHint.SetActive(false);
        _graffitiPresenter.ManageGraffitiSound();
        _graffitiPresenter.ManageGraffitiSprite(this, true);

        //_graffitiManagementInteractor.SetRandomOpponentGraffitiSpot(this);
    }

    public void RedrawGraffitiFromPlayerToOpponent()
    {
        _graffitiType = GraffitiType.Opponent;
        _objectGraffitiHint.SetActive(true);
        _graffitiPresenter.ManageGraffitiSprite(this, false);
    }

    public void TurnOff()
    {
        _isTurnOn = false;
        gameObject.SetActive(false);
        _objectGraffitiHint.SetActive(false);
    }

    public bool GetIsTurnOn()
    {
        return _isTurnOn;
    }

    public GraffitiType GetGraffitiType()
    {
        return _graffitiType;
    }
}
