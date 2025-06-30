using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Transform tr;
    private void Awake()
    {
        tr = transform;
    }
    private void Update()
    {
        if(CameraLooking.instance != null)
        {
            tr.LookAt(CameraLooking.instance.CameraTransform);
        }
    }
}
