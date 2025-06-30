using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetControlPointTrigger : PlayerTrigger
{
    [SerializeField] private Transform _controlPoint;

    private void Awake()
    {
        OnPlayerTriggerEnter += SetControlPoint;
    }
    private void OnDestroy()
    {
        OnPlayerTriggerEnter -= SetControlPoint;
    }

    private void SetControlPoint(PlayerNetwork player)
    {
        NetworkGameManager.OverrideSpawnPoint(_controlPoint);
    }
}
