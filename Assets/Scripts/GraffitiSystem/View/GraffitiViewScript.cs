using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraffitiViewScript : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSourceSFXTagging;

    public void PlayGraffitiJingleSound(AudioClip audioClip)
    {
        _audioSourceSFXTagging.clip = audioClip;
        _audioSourceSFXTagging.Play();
    }

    public void SetSprite(GraffitiScript graffiti, Sprite sprite)
    {
        graffiti.gameObject.GetComponent<SpriteRenderer>().sprite = sprite;
    }
}
