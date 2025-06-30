using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothFollowAtPlane : MonoBehaviour
{
    [SerializeField] private float lerpingMultiplier;
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 plane = Vector3.up;

    private Transform tr;
    private void OnEnable()
    {
        if (target == null)
            enabled = false;
        tr = transform;
    }
    private void Update()
    {
        Vector3 planeDirection = tr.TransformDirection(plane);
        Vector3 positionOutPlane = VectorMathf.ExtractDotVector(tr.position, planeDirection);
        Vector3 positionOnPlane = VectorMathf.RemoveDotVector(target.position, planeDirection);

        tr.position = positionOnPlane + positionOutPlane;
    }
}
