using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Surface : MonoBehaviour
{
    [SerializeField] private SurfaceResources surfaceResources;

    public SurfaceResources Resources { get => surfaceResources; private set { surfaceResources = value; } }

    private void Awake()
    {
        Resources = surfaceResources;
    }
}
