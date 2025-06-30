using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponResourcesBase : MonoBehaviour
{
    public static WeaponResourcesBase instance;
    [SerializeField] private WeaponResources[] resources;
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (instance == null)
            instance = this;
        else
            Destroy(this);
    }
    
    public static WeaponResources GetWeaponResources(string _weaponName)
    {
        foreach(WeaponResources _res in instance.resources)
        {
            if (_res.weaponName == _weaponName)
                return _res;
        }
        return null;
    }
}
