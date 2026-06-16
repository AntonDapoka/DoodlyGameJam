using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GraffitiScript : MonoBehaviour, IInteractable
{
    [SerializeField] private GraffitiManagementInteractorScript _graffitiManagementInteractor;
    [SerializeField] private GraffitiPresenterScript _graffitiPresenter;
    [SerializeField] private GameObject _objectGraffitiHint;
    public bool _isTurnOn = false;
    public bool _isGraffitiPlayer = false; // true = Player, false = Opponent

    private void Awake()
    {
        gameObject.SetActive(false);
        _objectGraffitiHint.SetActive(false);
    }

    public void Interact()
    {
        if (!_isGraffitiPlayer)
        {
            RedrawGraffitiFromOpponentToPlayer();
        }
    }

    public void TurnOnPlayerGraffiti()
    {
        _isTurnOn = true;
        _isGraffitiPlayer = true;
        gameObject.SetActive(true);

        _graffitiPresenter.ManageGraffitiSprite(this, true);
    }
    
    public void TurnOnOpponentGraffiti()
    {
        _isTurnOn = true;
        _isGraffitiPlayer = false;
        gameObject.SetActive(true);
        _objectGraffitiHint.SetActive(true);

        _graffitiPresenter.ManageGraffitiSprite(this, false);
    }

    public void RedrawGraffitiFromOpponentToPlayer()
    {
        _isGraffitiPlayer = true;
        _objectGraffitiHint.SetActive(false);
        _graffitiPresenter.ManageGraffitiSound();
        _graffitiPresenter.ManageGraffitiSprite(this, true);

        //_graffitiManagementInteractor.SetRandomOpponentGraffitiSpot(this);
    }

    public void RedrawGraffitiFromPlayerToOpponent()
    {
        _isGraffitiPlayer = false;
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

    public bool GetIsGraffitiPlayer()
    {
        return _isGraffitiPlayer;
    }
}
