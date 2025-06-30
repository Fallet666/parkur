using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ragdoll : MonoBehaviour
{
    public Transform MainBone { get => mainBone; }
    public event RagdollPartForceEventHandler OnRagdollPartForceAdded;

    [SerializeField] private RagdollPart[] allRagdollParts;
    [SerializeField] private Animator anim;
    [SerializeField] private bool physicsOnAwake = false;
    [SerializeField] private Transform mainBone;

    private void Start()
    {
        InitializeRagdollParts();

        if (physicsOnAwake)
            ActivatePhysicsRagdoll();
        else
            DeactivateRagdoll();
    }
    private void InitializeRagdollParts()
    {
        for (int i = 0; i < allRagdollParts.Length; i++)
        {
            allRagdollParts[i].LocalId = i;
            allRagdollParts[i].OnForceAdded += CallEvent;
        }
    }
    private void CallEvent(int partId, Vector3 force)
    {
        OnRagdollPartForceAdded?.Invoke(partId, force);
    }
    public void ActivatePhysicsRagdoll()
    {
        foreach(RagdollPart _rb in allRagdollParts)
        {
            _rb.IsKinematic = false;
        }
        anim.enabled = false;
    }
    public void DeactivateRagdoll()
    {
        foreach (RagdollPart _rb in allRagdollParts)
        {
            _rb.IsKinematic = true;
        }
        anim.enabled = true;
    }
    public void DeactivateRagdollWithDelay(float _delay)
    {
        Invoke("DeactivateRagdoll", _delay);
    }
    public void AddForceToRagdollPart(int partId, Vector3 force)
    {
        foreach(RagdollPart _rb in allRagdollParts)
        {
            if(partId == _rb.LocalId)
            {
                _rb.AddForce(force);
            }
        }
    }
}
