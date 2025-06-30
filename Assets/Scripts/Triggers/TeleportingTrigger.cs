using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportingTrigger : PlayerTrigger
{
    public Transform pointOnEnter;
    public Transform pointOnExit;

    private void Awake()
    {
        if(pointOnEnter != null)
            OnPlayerTriggerEnter += (PlayerNetwork _player) => TeleportToPoint(_player, pointOnEnter);
        if (pointOnExit != null)
            OnPlayerTriggerExit += (PlayerNetwork _player) => TeleportToPoint(_player, pointOnExit);
    }

    private void TeleportToPoint(PlayerNetwork _player, Transform _point)
    {
        _player.transform.position = _point.position;
        _player.Movement.GetWalker.SetTargetRotation(_point.up);
    }
}
