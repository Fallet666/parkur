using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowWalkerBottom : MonoBehaviour
{
    [SerializeField] private Walker walker;

    private Vector3 offset = Vector3.zero;
    private Transform tr;
    private Transform walkerTr;

    private void OnEnable()
    {
        if (walker == null)
        {
            enabled = false;
            return;
        }
        offset = transform.position - walker.transform.position;
    }
    private void Awake()
    {
        tr = transform;
        if (walker != null)
            walkerTr = walker.transform;
    }

    private void Update()
    {
        Vector3 _newPosition = walkerTr.position;
        //_newPosition += offset;

        tr.position = _newPosition;
    }
}
