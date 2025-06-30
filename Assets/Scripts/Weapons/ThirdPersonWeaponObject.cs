using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ThirdPersonWeaponObject : MonoBehaviour
{
    [SerializeField] private ThirdPersonWeaponObject otherHandPrefab;
    private ThirdPersonWeaponObject otherHandObject;

    [SerializeField] private GameObject magazine;
    [SerializeField] private GameObject magazineDecalPrefab;
    [SerializeField] private Vector3 throwOutMagazineDecalForce = Vector3.zero;
    #region Base
    public GameObjectsTransforms inHandLocalTransforms { get; set; }
    public WeaponResources resources { get; set; }
    public void DestroyMe()
    {
        Destroy(gameObject);
    }

    public void SetChildOf(Transform _parent, GameObjectsTransforms _transforms)
    {
        transform.parent = _parent;
        _transforms.SetLocalTransformsToObject(transform);
    }
    #endregion

    [SerializeField] private Transform tracersOut;
    public Transform TracersOut { get => tracersOut; }


    [SerializeField] private ParticleSystem fireball;
    public void ActivateFireball()
    {
        if (fireball == null)
            return;
        fireball.Play();
    }

    public void InitializeOtherHandObject(Transform otherHand)
    {
        if (otherHandPrefab == null)
            return;

        otherHandObject = Instantiate(otherHandPrefab, otherHand);
        otherHandObject.gameObject.SetActive(false);
    }
    public void SetOtherHandObjectActive(bool activeSelf)
    {
        if (otherHandObject == null)
            return;

        otherHandObject.gameObject.SetActive(activeSelf);
    }
    public void SetMagazineActive(bool activeSelf)
    {
        if (magazine == null)
            return;

        magazine.SetActive(activeSelf);
    }

    public void SpawnMagazineDecal()
    {
        if (magazine == null || magazineDecalPrefab == null)
            return;
        GameObject _obj = Instantiate(magazineDecalPrefab, magazine.transform.position, magazine.transform.rotation);
        if(_obj.TryGetComponent(out Rigidbody _rb))
        {
            _rb.velocity += _obj.transform.TransformDirection(throwOutMagazineDecalForce);
        }
    }

    private void OnDisable()
    {
        SetOtherHandObjectActive(false);
        SetMagazineActive(true);
    }
}
