using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void RagdollPartForceEventHandler(int partId, Vector3 force);

[RequireComponent(typeof(Rigidbody))]
public class RagdollPart : MonoBehaviour
{
    public event RagdollPartForceEventHandler OnForceAdded;
    public int LocalId { get; set; }
    public bool IsKinematic
    {
        get => rb.isKinematic;
        set
        {
            if (value == rb.isKinematic) return;
            rb.isKinematic = value;

            if (!rb.isKinematic)
            {
                if(Time.time <= savedForce.time + forceSavingTime)
                    rb.AddForce(savedForce.vector, ForceMode.Impulse);
            }           
        }
    }

    [SerializeField] private float forceSavingTime = 1f;

    private Rigidbody rb;
    private TimedVector savedForce = new TimedVector();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void AddForce(Vector3 force, bool callSyncEvent = false)
    {
        if (rb.isKinematic)
        {
            savedForce = new TimedVector(force, Time.time);
        }
        else
        {
            rb.AddForce(force, ForceMode.Impulse);
        }
        if(callSyncEvent)
            OnForceAdded?.Invoke(LocalId, force);
    }
}
