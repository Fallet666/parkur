using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public delegate void OnFloatValueChanged(float oldValue, float newValue);
public class Health : NetworkBehaviour
{

    public event VoidEventHandler OnHealthEnded;
    public event OnFloatValueChanged OnHealthChanged;
    public UnityEvent OnHealthChangedUnityEvent;
    public UnityEvent OnHealthEndedUnityEvent;

    public float HealthPoints { get => healthPoints; }
    public bool Damageable { get => damageable; set { damageable = value; } }

    private NetworkVariable<float> syncingHealthPoints = new NetworkVariable<float>(100f, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private float healthPoints = 100f;

    [SerializeField] private ThirdPersonCharacterController thirdPersonController;
    [SerializeField] private bool damageable = true;

    private void OnEnable()
    {
        if (thirdPersonController != null)
            thirdPersonController.SetupHealthColliders(this);
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            syncingHealthPoints.Value = healthPoints;
        }
        else
        {
            syncingHealthPoints.OnValueChanged += OnHealthPointsChanged;
            RequestHealthStateServerRPC(NetworkManager.LocalClientId);
        }
        OnHealthPointsChanged(syncingHealthPoints.Value, healthPoints);
    }
    private void OnDisable()
    {
        syncingHealthPoints.OnValueChanged -= OnHealthPointsChanged;
    }
    [ServerRpc(RequireOwnership = false)] public void DamageServerRPC(float _damageValue)
    {
        ChangeHealthPointsServerRPC(healthPoints - _damageValue);
    }
    [ServerRpc(RequireOwnership = false)] public void ChangeHealthPointsServerRPC(float _newHealth)
    {
        if (damageable == false)
            return;
        if (_newHealth < 0f)
            _newHealth = 0f;
        _newHealth = Mathf.Round(_newHealth);
        syncingHealthPoints.Value = _newHealth;
        OnHealthPointsChanged(healthPoints, _newHealth);
    }
    [ServerRpc(RequireOwnership = false)] private void RequestHealthStateServerRPC(ulong clientID)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientID }
            }
        };
        SendHealthStateClientRPC(healthPoints, clientRpcParams);
    }
    [ClientRpc] private void SendHealthStateClientRPC(float _healthPoints, ClientRpcParams clientRpcParams = default)
    {
        OnHealthPointsChanged(healthPoints, _healthPoints);
    }

    private void OnHealthPointsChanged(float oldHealth, float newHealth)
    {
        healthPoints = newHealth;
        if(oldHealth != newHealth)
        {
            OnHealthChanged?.Invoke(oldHealth, newHealth);
            OnHealthChangedUnityEvent?.Invoke();
            if (newHealth <= 0f)
            {
                OnHealthEnded?.Invoke();
                OnHealthEndedUnityEvent?.Invoke();
            }
        }
    }
}
