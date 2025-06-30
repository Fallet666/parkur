using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowGroundNormalTrigger : PlayerTrigger
{
    private void OnEnable()
    {
        OnPlayerTriggerStay += OnPlayerStay;
        OnPlayerTriggerExit += OnPlayerExit;
    }
    private void OnDisable()
    {
        OnPlayerTriggerStay -= OnPlayerStay;
        OnPlayerTriggerExit -= OnPlayerExit;
    }
    private void OnPlayerStay(PlayerNetwork _player)
    {
        if (!_player.IsOwner) return;

        Walker _walker = _player.Movement.GetWalker;
        _walker.FollowGroundNormal();
    }
    private void OnPlayerExit(PlayerNetwork _player)
    {
        if (!_player.IsOwner) return;

        Walker _walker = _player.Movement.GetWalker;
        _walker.DontFollowGroundNormal();
    }
}
