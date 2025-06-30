using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public delegate void PlayerNetworkEventHandler(PlayerNetwork _player);
public struct PlayerGameData : INetworkSerializable
{
    public float respawnTime;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref respawnTime);
    }
}

public enum LivingState
{
    alive,
    dead
}

[DisallowMultipleComponent]
public class PlayerNetwork : NetworkBehaviour
{
    [Header("Live")]
    [SerializeField] private LivingState livingState = LivingState.alive;
    [SerializeField] private Health health;
    [SerializeField] private GameObject mainCameraObject;

    [Header("Camera")]
    [SerializeField] private float minDistanceToSyncRotations = 1f;
    [SerializeField] private float maxInterpolationTime = 0.1f;

    [Header("Components")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private CameraLooking cameraLooking;
    [SerializeField] private PlayerUI playerUI;
    [SerializeField] private ThirdPersonCharacterController thirdPersonCharacter;
    [SerializeField] private CustomNetworkTransform networkTransform;

    [Header("Destroy if isn't local player")]
    [SerializeField] private GameObject[] toDestroy;

    public PlayerMovement Movement { get => movement; }
    public LivingState LiveState { get => livingState; }

    //sync values
    private NetworkVariable<Vector3> syncingCameraRotation = new NetworkVariable<Vector3>(
        Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<PlayerGameData> syncingPlayerGameData = new NetworkVariable<PlayerGameData>();

    //local values
    private TimedVector oldRotation = new TimedVector();
    private TimedVector newRotation = new TimedVector();

    private PlayerInputActions inputActions;

    public Vector3 CameraRotation { get => syncingCameraRotation.Value; }

    //events
    public PlayerNetworkEventHandler OnPlayerHealthEnded;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkGameManager.InitializePlayer(this);
        }
        else
        {
            syncingPlayerGameData.OnValueChanged += OnPlayerGameDataChanged;
        }


        if (health != null)
        {
            health.OnHealthChanged += OnHealthPointsChanged;
            OnHealthPointsChanged(health.HealthPoints, health.HealthPoints);
        }

        if (IsOwner)
        {
            inputActions = new PlayerInputActions();
            inputActions.Player.Enable();

            if (playerUI != null)
                PlayerUI.SetSigletone(playerUI);

            TeleportToSpawnPoint();
            //Invoke("TeleportToSpawnPoint", 0.1f);
        }
        else
        {
            RequestCameraRotationServerRPC(NetworkManager.LocalClientId);
            foreach(GameObject _obj in toDestroy)
            {
                Destroy(_obj);
            }
        }

        //apply singletone for camera
        if (cameraLooking != null)
        {
            if (IsOwner)
            {
                CameraLooking.instance = cameraLooking;
                cameraLooking.IsOwner = true;

            }
            else
            {
                cameraLooking.IsOwner = false;
                syncingCameraRotation.OnValueChanged += ChangeCameraRotations;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkGameManager.DeinitializePlayer(this);
        }
    }

    private void Update()
    {      
        SyncCamera();
    }

    # region Player Game Data
    public void SetGameData(PlayerGameData _data)
    {
        if (!IsServer) return;

        syncingPlayerGameData.Value = _data;
    }
    public void SetRestartTime(float _time)
    {
        PlayerGameData _data = syncingPlayerGameData.Value;
        _data.respawnTime = _time;

        OnPlayerGameDataChanged(syncingPlayerGameData.Value, _data);
        syncingPlayerGameData.Value = _data;
    }
    private void OnPlayerGameDataChanged(PlayerGameData oldData, PlayerGameData newData)
    {
        PlayerUI.SetRespawnTime(newData.respawnTime);
    }
    #endregion
    #region Camera
    private void SyncCamera()
    {
        if (!IsOwner)
        {
            cameraLooking.Rotation = VectorMathf.LerpVectors(oldRotation, newRotation, true);
        }
        else
        {
            float _dist = Vector3.Distance(cameraLooking.Rotation, syncingCameraRotation.Value);
            if (_dist > minDistanceToSyncRotations)
            {
                syncingCameraRotation.Value = cameraLooking.Rotation;
            }
        }
    }
    private void ChangeCameraRotations(Vector3 _oldValue, Vector3 _newValue)
    {
        oldRotation = newRotation;
        newRotation = new TimedVector(_newValue, Time.time);

        if (newRotation.time - oldRotation.time > maxInterpolationTime)
            oldRotation.time = Time.time - maxInterpolationTime;
    }
    [ServerRpc(RequireOwnership = false)] private void RequestCameraRotationServerRPC(ulong clientID)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams()
        {
            Send = new ClientRpcSendParams()
            {
                TargetClientIds = new ulong[] { clientID }
            }
        };
        SendCameraRotationClientRPC(CameraRotation, clientRpcParams);
    }
    [ClientRpc] private void SendCameraRotationClientRPC(Vector3 _cameraRotation, ClientRpcParams clientRpcParams = default)
    {
        newRotation = new TimedVector(_cameraRotation, 0f);
    }
    #endregion
    #region Health
    private void TeleportToSpawnPoint()
    {
        GameObjectsTransforms spawnPoint = NetworkGameManager.GetAvailabeSpawnPoint();

        movement.Teleport(spawnPoint.position, spawnPoint.rotation);

        if (networkTransform != null)
            networkTransform.Teleport(spawnPoint.position, spawnPoint.rotation);
    }
    private void OnHealthPointsChanged(float oldValue, float newValue)
    {
        LivingState newLivingState = LivingState.dead;
        if (newValue <= 0f)
            newLivingState = LivingState.dead;
        else
            newLivingState = LivingState.alive;
        if(newLivingState != livingState)
            OnLivingStateChanged(newLivingState);
    }
    private void OnLivingStateChanged(LivingState newState)
    {
        livingState = newState;
        if (livingState == LivingState.alive)
            Revive();
        else if (livingState == LivingState.dead)
            Die();
    }
    private void Die()
    {
        OnPlayerHealthEnded?.Invoke(this);

        if (thirdPersonCharacter == null)
            return;

        if (!IsOwner)
        {
            thirdPersonCharacter.ActivateRagdoll();
            PlayerMovement.instance.DisableAllMovement();
            WeaponsController.LockWeapons = true;
        }
        else
        {
            thirdPersonCharacter.ResetLocal();
            thirdPersonCharacter.ActivateRagdoll();
            thirdPersonCharacter.SetHeadCameraActive(true);
            mainCameraObject.SetActive(false);
            PlayerUI.ActivateDeadUI();
        }
    }
    private void Revive()
    {
        if (thirdPersonCharacter == null)
            return;

        if (!IsOwner)
        {
            thirdPersonCharacter.DisableRagdoll();
            PlayerMovement.instance.EnableAllMovement();
            WeaponsController.LockWeapons = false;
        }
        else
        {
            thirdPersonCharacter.SetLocal();
            thirdPersonCharacter.DisableRagdoll();
            thirdPersonCharacter.SetHeadCameraActive(false);
            mainCameraObject.SetActive(true);
            PlayerUI.ActivateAliveUI();

            TeleportToSpawnPoint();
        }
    }
    public void Regenerate()
    {
        if(IsServer)
            health.ChangeHealthPointsServerRPC(100f);
    }
    #endregion
}
