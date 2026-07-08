using UnityEngine;

public class GraffitiViewScript : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSourceSFXTagging;

    public void SetGraffitiSprite(GraffitiScript graffiti, Sprite sprite)
    {
        if (graffiti == null)
            return;

        SpriteRenderer renderer = graffiti.gameObject.GetComponent<SpriteRenderer>();
        if (renderer == null)
            return;

        renderer.sprite = sprite;
    }

    public void PlayCompletionSound(AudioClip audioClip)
    {
        if (_audioSourceSFXTagging == null || audioClip == null)
            return;

        _audioSourceSFXTagging.clip = audioClip;
        _audioSourceSFXTagging.Play();
    }
}
