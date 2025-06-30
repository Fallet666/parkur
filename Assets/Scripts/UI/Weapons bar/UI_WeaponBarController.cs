using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class UI_WeaponBarController : MonoBehaviour
{
    [SerializeField] private float startTransitionFade = 0.3f;
    [SerializeField] private float holdTime = 1f;
    [SerializeField] private float endTransitionTime = 0.7f;

    [SerializeField] private Transform spawnParent;
    [SerializeField] private UI_WeaponSlot masterSlot;

    private List<UI_WeaponSlot> weaponSlots = new List<UI_WeaponSlot>();
    private WeaponType[] types = new WeaponType[] { WeaponType.rifle, WeaponType.pistol, WeaponType.melle, WeaponType.physics };

    private bool isInitialized = false;

    private void OnEnable()
    {
        WeaponsController.onWeaponsInitialized += OnNetworkComponentsInitialized;
    }
    private void OnDisable()
    {
        WeaponsController.onWeaponsInitialized -= OnNetworkComponentsInitialized;
    }
    private void OnNetworkComponentsInitialized()
    {
        if (WeaponsController.instance == null)
            return;

        InitializeWeapons();
        WeaponsController.instance.onWeaponChanged += SetWeaponSelected;
        WeaponsController.instance.onWeaponListUpdated += Reinitialize;
    }
    private void OnNetworkComponentsDeinitialized(bool value)
    {
        if (WeaponsController.instance == null)
            return;

        WeaponsController.instance.onWeaponChanged -= SetWeaponSelected;
    }
    private void SetWeaponSelected(Weapon weapon)
    {
        StartScenario();
        foreach(var slot in weaponSlots)
        {
            slot.SetSelected(slot.ContainedWeapon == weapon);
        }
    }
    private void InitializeWeapons()
    {
        if (WeaponsController.instance == null || isInitialized || masterSlot == null) return;

        foreach(var typ in types)
        {
            List<Weapon> weapons = GetWeaponsByType(typ);
            foreach(var wep in weapons)
            {

                UI_WeaponSlot slot = Instantiate(masterSlot, spawnParent);
                slot.ContainedWeapon = wep;
                slot.InitializeWeaponIcon(wep);
                weaponSlots.Add(slot);
            }
        }
        isInitialized = true;
    }
    private List<Weapon> GetWeaponsByType(WeaponType _type)
    {
        List<Weapon> outWeapons = new List<Weapon>();
        if (WeaponsController.instance == null) return outWeapons;

        foreach(var wep in WeaponsController.instance.Weapons)
        {
            if (wep.Resources.weaponType == _type)
                outWeapons.Add(wep);
        }
        return outWeapons;
    }

    private void StartScenario()
    {
        StopAllCoroutines();
        StartCoroutine(ShowBarScenario());
    }
    private IEnumerator ShowBarScenario()
    {
        TransitionOnAllSlots(1f, startTransitionFade);
        yield return new WaitForSeconds(holdTime);
        TransitionOnAllSlots(0f, endTransitionTime);
    }
    private void TransitionOnAllSlots(float alpha, float transitionTime)
    {
        foreach(var slot in weaponSlots)
        {
            slot.SetAplha(alpha, transitionTime);
        }
    }
    private void Reinitialize()
    {
        foreach(var slot in weaponSlots)
        {
            Destroy(slot.gameObject);
        }
        weaponSlots.Clear();
        isInitialized = false;

        InitializeWeapons();
        StartScenario();
    }
}
