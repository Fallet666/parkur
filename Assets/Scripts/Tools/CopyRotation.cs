using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopyRotation : MonoBehaviour
{
    private Transform tr;

    [SerializeField] private Transform targetTr;
    private void Awake()
    {
        tr = transform;
    }

    private void Update()
    {
        if(targetTr != null)
            tr.rotation = targetTr.rotation;
    }
}
