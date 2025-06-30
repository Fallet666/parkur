using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LadderTrigger : MonoBehaviour
{
    private List<PlayerMovement> players = new List<PlayerMovement>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerMovement _mov))
            players.Add(_mov);
    }
    private void OnTriggerStay(Collider other)
    {
        foreach(var _pl in players)
        {
            _pl.MoveOnLadder();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerMovement _mov))
            players.Remove(_mov);
    }
}
