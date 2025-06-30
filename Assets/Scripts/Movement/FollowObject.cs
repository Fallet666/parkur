using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowObject : MonoBehaviour
{
    public bool followPosition = true;
    public bool folllowRotation = true;
    public Transform obj;


    private Transform tr;
    private Vector3 startPosition = Vector3.zero;
    private Quaternion startRotation = new Quaternion();

    private void Awake()
    {
        tr = transform;
    }
    private void OnEnable()
    {
        if (obj != null)
            startPosition = transform.position - obj.position;
        startRotation = transform.localRotation;
    }

    private void Update()
    {
        if (obj != null)
        {
            if(followPosition)
                tr.position = obj.position + startPosition;

            if (folllowRotation)
            {
                tr.rotation = obj.rotation;
                tr.rotation *= startRotation;
            }
        }
    }
}
