using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Collections;

[System.Serializable]
public struct WeaponContainerData : INetworkSerializable
{
    public FixedString32Bytes weaponName;
    public WeaponAmmunitionData ammunitionData;
    public bool onlyAmmo;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref weaponName);
        serializer.SerializeValue(ref ammunitionData);
        serializer.SerializeValue(ref onlyAmmo);
    }

    public WeaponContainerData(Weapon _wep, bool _onlyAmmo = false)
    {
        weaponName = _wep.Resources.weaponName;
        ammunitionData = _wep.AmmunitionData;
        onlyAmmo = _onlyAmmo;
    }
}

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(Outline))]
public class WorldWeaponContainer : NetworkBehaviour, IWeaponContainable
{
    public bool applyStartData = false;
    [SerializeField] private WeaponResources startResources;
    [SerializeField] private WeaponAmmunitionData startAmmoData;
    [SerializeField] private bool startOnlyAmmo = false;

    private Rigidbody rb;
    private NetworkTransform netTr;
    private Outline outline;

    private NetworkVariable<WeaponContainerData> syncingContainerData = new NetworkVariable<WeaponContainerData>();
    public WeaponContainerData ContainerData { get; set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        netTr = GetComponent<NetworkTransform>();
        if (applyStartData)
        {
            ContainerData = new WeaponContainerData
            {
                ammunitionData = startAmmoData,
                weaponName = startResources.weaponName,
                onlyAmmo = startOnlyAmmo                
            };
        }
        outline = GetComponent<Outline>();
        outline.enabled = false;
    }


    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            RequestContainerDataServerRPC(NetworkManager.LocalClientId);
            syncingContainerData.OnValueChanged += OnContainerDataChanged;
        }
    }
    public override void OnNetworkDespawn()
    {
        if (!IsServer)
        {
            syncingContainerData.OnValueChanged -= OnContainerDataChanged;
        }
    }

    public void SetContainerData(WeaponContainerData data)
    {
        if (!IsServer)
            return;
        syncingContainerData.Value = data;
        OnContainerDataChanged(ContainerData, data);
    }
    [ServerRpc(RequireOwnership = false)] private void RequestContainerDataServerRPC(ulong clientID)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams()
        {
            Send = new ClientRpcSendParams()
            {
                TargetClientIds = new ulong[] { clientID }
            }
        };
        SendContainerDataClientRPC(ContainerData, clientRpcParams);
    }
    [ClientRpc] private void SendContainerDataClientRPC(WeaponContainerData data, ClientRpcParams clientRpcParams)
    {
        OnContainerDataChanged(ContainerData, data);
    }
    private void OnContainerDataChanged(WeaponContainerData oldValue, WeaponContainerData newValue)
    {
        ContainerData = newValue;
    }


    [ServerRpc(RequireOwnership = false)] public void ThrowServerRPC(Vector3 force)
    {
        rb.velocity += force;
    }
    [ServerRpc(RequireOwnership = false)] public void DestroyServerRPC()
    {
        this.NetworkObject.Despawn();
        Destroy(gameObject);
    }

    public void ShowOutline(bool activeState)
    {
        outline.enabled = activeState;
    }
}
