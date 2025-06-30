using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PlaceForWeaponOnSpine
{
    public Transform parentForWeapon;
    public WeaponResources[] availableWeapons;
}
public class ThirdPersonOnSpineWeaponPlacer : MonoBehaviour
{
    [SerializeField] private PlaceForWeaponOnSpine[] places;

    public void PlaceToSpine(ThirdPersonWeaponObject weaponObject)
    {
        foreach (PlaceForWeaponOnSpine _place in places)
        {
            foreach (WeaponResources _res in _place.availableWeapons)
            {
                if(weaponObject.resources == _res)
                {
                    weaponObject.SetChildOf(_place.parentForWeapon, _res.OnSpineWeaponObjectLocalTransforms);
                }
            }
        }
    }
}
