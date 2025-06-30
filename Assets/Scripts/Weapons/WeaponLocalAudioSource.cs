using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WeaponLocalAudioSource : MonoBehaviour
{
    public static WeaponLocalAudioSource instance;
    public static void SetSingletone(WeaponLocalAudioSource _source) {
        instance = _source;
    }

    private AudioSource source;
    private void Awake()
    {
        source = GetComponent<AudioSource>();
    }
    public static void PlayOneShot(AudioClip _clip, float _pitch = 1f)
    {
        if (instance == null)
            return;

        instance.source.pitch = _pitch;
        instance.source.PlayOneShot(_clip);
    }
}
