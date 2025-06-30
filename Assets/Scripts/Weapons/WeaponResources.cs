using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum TracerType
{
    none,
    standartBullet
}
[System.Serializable]
public struct GameObjectsTransforms
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public GameObjectsTransforms(Transform tr)
    {
        position = tr.position;
        rotation = tr.rotation;
        scale = tr.localScale;
    }

    public void SetTransformsToObject(Transform obj)
    {
        obj.position = position;
        obj.rotation = rotation;
        obj.localScale = scale;
    }
    public void SetLocalTransformsToObject(Transform obj)
    {
        obj.localPosition = position;
        obj.localRotation = rotation;
        obj.localScale = scale;
    }

    public static GameObjectsTransforms GetLocalTransforms(Transform tr)
    {
        return new GameObjectsTransforms
        {
            position = tr.localPosition,
            rotation = tr.localRotation,
            scale = tr.localScale
        };
    }
}
public enum WeaponType
{
    melle,
    pistol,
    rifle,
    physics
}

[CreateAssetMenu(fileName = "WeaponResources", menuName = "ScriptableObjects/WeaponResources")]
public class WeaponResources : ScriptableObject
{
    public string weaponName;
    public Weapon FirstPersonWeaponPrefab;
    public ThirdPersonWeaponObject ThirdPersonWeaponObject;
    public WeaponParent ThirdPersonWeaponObjectParent;
    public GameObjectsTransforms OnSpineWeaponObjectLocalTransforms;
    public RuntimeAnimatorController ThirdPersonAnimatorController;
    public TracerType tracerType;
    public float tracerSpeed = 200f;
    public bool useFireball;
    public bool useAmmo;
    public WorldWeaponContainer worldWeaponContainer;
    public WeaponType weaponType;
    public Sprite ui_icon;

    [Header("Audio")]

    public AudioClip[] attackClips;
    public float attackClipsPitch = 0.8f;
    public float attackClipsMaxDistance = 50f;
    [Range(0f, 1f)] public float attackPitchRandomFactor = 0.05f;

    public float extraClipsMaxDistance = 40f;
    public AudioClip[] openingClips;
    public AudioClip[] extraClips;

    public AudioClip GetExtraClip(string clipName)
    {
        foreach(AudioClip _clip in extraClips)
        {
            if (_clip.name == clipName)
                return _clip;
        }
        return null;
    }
    public float GetAttackPitch()
    {
        float _pitch = attackClipsPitch;
        _pitch += Random.Range(-attackPitchRandomFactor, attackPitchRandomFactor);
        return _pitch;
    }
}
