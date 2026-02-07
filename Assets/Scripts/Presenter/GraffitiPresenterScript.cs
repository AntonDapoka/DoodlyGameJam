using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraffitiPresenterScript : MonoBehaviour
{
    [SerializeField] private Sprite[] _graffitiSpritesPlayer;
    [SerializeField] private Sprite[] _graffitiSpritesOpponent;
    [SerializeField] private GraffitiViewScript _graffitiView;
    [SerializeField] private AudioClip _graffitiJingleSound;

    public void ManageGraffitiSound() 
    {
        _graffitiView.PlayGraffitiJingleSound(_graffitiJingleSound);
        
    }

    public void ManageGraffitiSprite(GraffitiScript graffiti, bool isPlayerGraffiti) //isPlayerGraffiti == false, then it is Opponent's
    {
        _graffitiView.SetSprite(graffiti, isPlayerGraffiti ? _graffitiSpritesPlayer[0] : _graffitiSpritesOpponent[0]);
    }
}
