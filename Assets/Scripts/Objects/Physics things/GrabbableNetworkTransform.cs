using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public struct PositionRotation : INetworkSerializable
{
    public Vector3 position;
    public Quaternion rotation;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref rotation);
    }

    public PositionRotation(Transform tr)
    {
        position = tr.position;
        rotation = tr.rotation;
    }
}
public struct RigidbodyVelocities : INetworkSerializable
{
    public Vector3 velocity;
    public Vector3 angularVelocity;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref velocity);
        serializer.SerializeValue(ref angularVelocity);
    }
    public RigidbodyVelocities(Rigidbody rb)
    {
        velocity = rb.velocity;
        angularVelocity = rb.angularVelocity;
    }
}

[RequireComponent(typeof(Grabbable))]
public class GrabbableNetworkTransform : NetworkBehaviour
{
    public bool isInteracting = false;

    //parameters
    [SerializeField] private float clientAuthoritativeTime = 10f;


    //syncing values 
    private NetworkVariable<PositionRotation> syncingTransforms = new NetworkVariable<PositionRotation>(default, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);


    //local values
    private Grabbable grabbable;
    private void Awake()
    {
        grabbable = GetComponent<Grabbable>();
    }
    private void OnEnable()
    {
        syncingTransforms.OnValueChanged += OnTransformsChanged;
    }
    private void OnDisable()
    {
        syncingTransforms.OnValueChanged -= OnTransformsChanged;   
    }
    private void FixedUpdate()
    {
        isInteracting = IsInteracting();
        if (!IsInteracting())
        {
            //if (!IsServer)
            //    grabbable.Rigidbody_.isKinematic = true;
            return;
        }
        grabbable.Rigidbody_.isKinematic = false;
        SetTransformsAndVelocitiesServerRPC(new PositionRotation(transform), new RigidbodyVelocities(grabbable.Rigidbody_));
    }


    #region Network operations
    [ServerRpc(RequireOwnership = false)] private void SetTransformsAndVelocitiesServerRPC(PositionRotation positionRotation, RigidbodyVelocities velocities)
    {
        syncingTransforms.Value = positionRotation;

        if (IsInteracting())
            return;

        transform.position = positionRotation.position;
        transform.rotation = positionRotation.rotation;

        grabbable.Rigidbody_.velocity = velocities.velocity;
        grabbable.Rigidbody_.angularVelocity = velocities.angularVelocity;
    }
    #endregion
    #region Events
    private void OnTransformsChanged(PositionRotation oldTransforms, PositionRotation newTransforms)
    {
        if (IsInteracting())
            return;

        transform.position = newTransforms.position;
        transform.rotation = newTransforms.rotation;
    }
    #endregion
    #region LocalFunctions
    public bool IsInteracting()
    {
        return Time.time < grabbable.LastTimeInteraction + clientAuthoritativeTime;
    }
    #endregion
}
