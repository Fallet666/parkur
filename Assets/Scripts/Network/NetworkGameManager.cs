using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[DisallowMultipleComponent]
public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager instance;

    [SerializeField] private float respawnTime = 10f;

    [SerializeField] private List<PlayerNetwork> players = new List<PlayerNetwork>();

    private void Awake()
    {
        if(instance == null)
            instance = this;
    }
    public static void InitializePlayer(PlayerNetwork _player) {
        if (instance == null || instance.players.Contains(_player))
            return;

        instance.players.Add(_player);
        _player.OnPlayerHealthEnded += instance.StartRespawnScenario;
    }
    public static void DeinitializePlayer(PlayerNetwork _player)
    {
        if (instance == null || !instance.players.Contains(_player))
            return;

        instance.players.Remove(_player);
    }

    #region Spawn points
    [SerializeField] private Transform[] spawnPoints;
    public static GameObjectsTransforms GetAvailabeSpawnPoint()
    {
        if (instance == null)
            return new GameObjectsTransforms();
        if (instance.spawnPoints.Length <= 0)
            return new GameObjectsTransforms();

        Transform sp = instance.spawnPoints[Random.Range(0, instance.spawnPoints.Length)];
        return new GameObjectsTransforms(sp);
    }
    #endregion


    #region Respawn
    private void StartRespawnScenario(PlayerNetwork _player)
    {
        StartCoroutine(RespawnEnumaretor(_player, respawnTime));
    }
    private IEnumerator RespawnEnumaretor(PlayerNetwork _player, float _respawnTime)
    {
        float targetTime = NetworkManager.ServerTime.TimeAsFloat + respawnTime;
        _player.SetRestartTime(targetTime);
        while (NetworkManager.ServerTime.TimeAsFloat < targetTime)
        {
            if (_player.LiveState == LivingState.alive)
                yield break;
            yield return null;
        }
        _player.Regenerate();
    }
    #endregion
}
