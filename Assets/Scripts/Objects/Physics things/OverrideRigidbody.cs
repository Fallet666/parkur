using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void CollisionEventHandler(Collision collision);

[RequireComponent(typeof(Rigidbody))]
public class OverrideRigidbody : MonoBehaviour
{
    public Rigidbody Rb { get => rb; }
    public event CollisionEventHandler onCollisionEnter;
    public event CollisionEventHandler onCollisionExit;

    private Rigidbody rb;
    private void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void OnCollisionEnter(Collision collision)
    {
        onCollisionEnter?.Invoke(collision);
    }
    private void OnCollisionExit(Collision collision)
    {
        onCollisionExit?.Invoke(collision);
    }
    public Rigidbody GetRigidbody()
    {
        return GetComponent<Rigidbody>();
    }
}
