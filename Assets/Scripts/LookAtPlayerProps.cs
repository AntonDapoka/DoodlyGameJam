using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtPlayerProps : LookAtPlayerBase
{
    private void Start()
    {
        lockY = true;
    }
    public override void LookAtPlayer(Transform player)
    {
        base.LookAtPlayer(player);
    }
}
