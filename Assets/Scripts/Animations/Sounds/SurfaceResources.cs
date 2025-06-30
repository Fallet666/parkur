using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SurfaceResources", menuName = "ScriptableObjects/Surface")]
public class SurfaceResources : ScriptableObject
{
    public AudioClip[] steps;
    public AudioClip hitClip;
    public GameObject hitEffect;
}
