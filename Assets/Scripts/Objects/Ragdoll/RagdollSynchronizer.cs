using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RagdollSynchronizer : NetworkBehaviour
{
    [SerializeField] private Ragdoll localRagdoll;
    private void OnEnable()
    {
        if (localRagdoll == null || localRagdoll.MainBone == null)
            enabled = false;
    }
    public override void OnNetworkSpawn()
    {
        if (localRagdoll == null)
            return;

        localRagdoll.OnRagdollPartForceAdded += OnAddingForceToRagdoll;
    }
    public override void OnNetworkDespawn()
    {
        if (localRagdoll == null)
            return;

        localRagdoll.OnRagdollPartForceAdded -= OnAddingForceToRagdoll;
    }
    private void OnAddingForceToRagdoll(int partId, Vector3 force)
    {
        OnAddingForceToRagdollServerRPC(partId, force,localRagdoll.MainBone.position, NetworkManager.LocalClientId);
    }
    [ServerRpc(RequireOwnership = false)]
    private void OnAddingForceToRagdollServerRPC(int partId, Vector3 force, Vector3 mainBonePosition ,ulong attackOwnerClientId)
    {
        if (NetworkManager.LocalClientId != attackOwnerClientId)
        {
            localRagdoll.AddForceToRagdollPart(partId, force);
            localRagdoll.MainBone.position = mainBonePosition;
        }
        #region RPc params
        IReadOnlyList<ulong> ConnectedIds = NetworkManager.ConnectedClientsIds;
        List<ulong> Ids = new List<ulong>();
        foreach (ulong _id in ConnectedIds)
        {
            if (_id != attackOwnerClientId)
                Ids.Add(_id);
        }

        ClientRpcParams clientRpcParams = new ClientRpcParams()
        {
            Send = new ClientRpcSendParams()
            {
                TargetClientIds = Ids.ToArray()
            }
        };

        #endregion

        OnAddingForceToRagdollClientRpc(partId, force, mainBonePosition,clientRpcParams);
    }
    [ClientRpc]
    private void OnAddingForceToRagdollClientRpc(int partId, Vector3 force, Vector3 mainBonePosition, ClientRpcParams clientRpcParams)
    {
        if (IsServer)
            return;
        localRagdoll.AddForceToRagdollPart(partId, force);
        localRagdoll.MainBone.position = mainBonePosition;
    }
}
