using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void PlayerTriggerEventHadler(PlayerNetwork _player);
public class PlayerTrigger : MonoBehaviour
{
    public PlayerTriggerEventHadler OnPlayerTriggerEnter;
    public PlayerTriggerEventHadler OnPlayerTriggerStay;
    public PlayerTriggerEventHadler OnPlayerTriggerExit;
    public List<PlayerNetwork> PlayersInTrigger { get => players; }


    private List<PlayerNetwork> players = new List<PlayerNetwork>();
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out PlayerNetwork _player))
        {
            players.Add(_player);
            OnPlayerTriggerEnter?.Invoke(_player);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        foreach (var _player in players)
        {
            OnPlayerTriggerStay?.Invoke(_player);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent(out PlayerNetwork _player))
        {
            players.Remove(_player);
            OnPlayerTriggerExit?.Invoke(_player);
        }
    }
}
