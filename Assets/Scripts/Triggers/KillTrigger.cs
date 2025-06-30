using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillTrigger : PlayerTrigger
{
    private void Awake()
    {
        OnPlayerTriggerEnter += KillPlayer;
    }
    private void OnDestroy()
    {
        OnPlayerTriggerEnter -= KillPlayer;
    }

    private void KillPlayer(PlayerNetwork player)
    {
        var health = player.GetComponent<Health>();

        if (health == null)
            return;

        health.ChangeHealthPointsServerRPC(0f);
    }
}
