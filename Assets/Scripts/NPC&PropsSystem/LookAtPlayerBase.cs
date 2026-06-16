using UnityEngine;

public abstract class LookAtPlayerBase : MonoBehaviour
{
    [SerializeField] protected bool lockY = true;
    [SerializeField] protected Transform playerTransform;

    public void SetPlayer(Transform player)
    {
        playerTransform = player;
    }

    protected virtual void LateUpdate()
    {
        LookAtPlayer(playerTransform);
    }

    public virtual void LookAtPlayer(Transform player)
    {
        if (player == null) return;

        Vector3 dir = transform.position - player.position;

        if (lockY)
            dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(dir);
    }
}
