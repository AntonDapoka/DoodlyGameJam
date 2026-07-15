using UnityEngine;

public abstract class TrickDefinition : ScriptableObject
{
    [SerializeField] private TrickType trickType;
    [SerializeField] private float minSpeed;
    [SerializeField] private float maxSpeed = 1000f;
    [SerializeField] private float impulseForce;
    [SerializeField] private Sprite[] animationFrames;
    [SerializeField] private float animationFrameTime = 0.1f;
    [SerializeField] private AudioClip sfx;

    public TrickType TrickType => trickType;
    public float MinSpeed => minSpeed;
    public float MaxSpeed => maxSpeed;
    public float ImpulseForce => impulseForce;
    public Sprite[] AnimationFrames => animationFrames;
    public float AnimationFrameTime => animationFrameTime;
    public AudioClip Sfx => sfx;

    public abstract bool CanExecute(in TrickContext context, ExecutionPhase phase, TrickModule module);

    public virtual void OnExecuted(TrickModule module, ExecutionPhase phase)
    {
        if (animationFrames != null && animationFrames.Length > 0 && module.SpriteManager != null)
            module.SpriteManager.PlayTrickAnimation(animationFrames, animationFrameTime);

        if (sfx != null && module.ControllerTransform != null)
            AudioSource.PlayClipAtPoint(sfx, module.ControllerTransform.position);

        module.ApplyImpulse(impulseForce);
    }
}
