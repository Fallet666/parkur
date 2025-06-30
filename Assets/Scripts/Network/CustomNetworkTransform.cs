using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CustomNetworkTransform : NetworkBehaviour
{
    [SerializeField] private bool syncPosition = true;
    [SerializeField] private float minDistanceToSync = 0.01f;

    [SerializeField] private bool syncRotation = true;
    [SerializeField] private float minAngleToSync = 1f;
    [SerializeField] private float timeToDontInterpolateRotation = 0.5f;

    private NetworkVariable<Vector3> position = new NetworkVariable<Vector3>(Vector3.zero,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private TimedVector savedPosition;
    private TimedVector targetPosition;

    private NetworkVariable<Vector3> rotation = new NetworkVariable<Vector3>(Vector3.zero,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private TimedVector savedRotation;
    private TimedVector targetRotation;

    private Transform tr;

    private void Awake()
    {
        tr = transform;
    }
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            position.OnValueChanged += OnPositionChanged;
            rotation.OnValueChanged += OnRotationChanged;

            RequestDataServerRPC(NetworkManager.LocalClientId);
        }
    }
    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
        {
            position.OnValueChanged -= OnPositionChanged;
            rotation.OnValueChanged -= OnRotationChanged;
        }
    }
    private void Update()
    {
        if (IsOwner)
        {
            if(Vector3.Distance(position.Value, tr.position) > minDistanceToSync)
            {
                position.Value = tr.position;
            }
            if(Vector3.Distance(rotation.Value, tr.eulerAngles) > minAngleToSync)
            {
                rotation.Value = tr.eulerAngles;
            }
        }
        else
        {
            tr.position = VectorMathf.LerpVectors(savedPosition, targetPosition, true);
            tr.eulerAngles = VectorMathf.LerpVectors(savedRotation, targetRotation, true);
        }
    }
    private void OnPositionChanged(Vector3 oldValue, Vector3 newValue)
    {
        savedPosition = targetPosition;
        targetPosition = new TimedVector(newValue, Time.time);
    }
    private void OnRotationChanged(Vector3 oldRotation, Vector3 newRotation)
    {
        if (Time.time - savedRotation.time > timeToDontInterpolateRotation)
            savedRotation = new TimedVector(newRotation, Time.time);
        else
            savedRotation = targetRotation;
        targetRotation = new TimedVector(newRotation, Time.time);
    }
    [ServerRpc(RequireOwnership = false)] private void RequestDataServerRPC(ulong clientID)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams()
        {
            Send = new ClientRpcSendParams()
            {
                TargetClientIds = new ulong[] { clientID }
            }
        };
        SendDataClientRPC(tr.position, tr.eulerAngles, clientRpcParams);
    }
    [ClientRpc] private void SendDataClientRPC(Vector3 _position, Vector3 _rotation, ClientRpcParams clientRpcParams = default)
    {
        targetPosition = new TimedVector(_position, 0f);
        targetRotation = new TimedVector(_rotation, 0f);
    }

    [ServerRpc] private void TeleportServerRPC(Vector3 _position, Vector3 _rotation)
    {
        SendDataClientRPC(_position, _rotation);
    }

    public void Teleport(Vector3 _position, Quaternion _rotation)
    {
        if (!IsOwner)
            return;
        TeleportServerRPC(_position, _rotation.eulerAngles);
    }

}
