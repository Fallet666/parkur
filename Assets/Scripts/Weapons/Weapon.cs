using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void VoidEventHandler();
public class Weapon : MonoBehaviour
{
    public WeaponResources Resources { get; set; }
    public PlayerMovementData MovementData;
    public bool SecondKeyPressed { get; set; }
    public Transform CameraParent { get => cameraParent; }
    public WeaponAnimatorData AnimatorData { get => animatorData;  set { animatorData = value; } }

    public WeaponAmmunitionData AmmunitionData;

    public float CameraInfluence { get; protected set; }
    public Vector3 LastHitPoint { get; protected set; }
    public bool QuiteOpen { get; set; }

    [SerializeField] protected Transform cameraParent;
    protected WeaponAnimatorData animatorData = new WeaponAnimatorData();

    public virtual void Open()
    {
        gameObject.SetActive(true);
    }
    public virtual void Close()
    {
        gameObject.SetActive(false);
    }
    public virtual bool Attack()
    {
        return true;
    }
    public virtual void Remove()
    {
        PlayerUI.SetAmmoTextActive(false);
        Destroy(gameObject);
    }

    public event WeaponHitDataEventHandler OnSuccessfulAttack;
    protected void CallSuccessfulAttackEvent()
    {
        OnSuccessfulAttack?.Invoke(new WeaponHitData { hitPoint = LastHitPoint }) ;
    }

    #region Ammunition data operations
    public bool TakeAmmo(int bulletsUse = 1)
    {
        int magazineAmmo = AmmunitionData.magazineAmmo;
        int totalAmmo = AmmunitionData.totalAmmo;

        if (bulletsUse > magazineAmmo)
        {
            return false;
        }

        magazineAmmo -= bulletsUse;

        AmmunitionData = new WeaponAmmunitionData(magazineAmmo, totalAmmo);
        UpdateAmmoUI();

        return true;
    }
    public bool LoadMagazine(int magazineCapacity)
    {
        int magazineAmmo = AmmunitionData.magazineAmmo;
        int totalAmmo = AmmunitionData.totalAmmo;

        if (magazineAmmo >= magazineCapacity || totalAmmo <= 0)
        {
            return false;
        }

        totalAmmo += magazineAmmo;
        magazineAmmo = 0;

        if (totalAmmo <= magazineCapacity)
        {
            magazineAmmo = totalAmmo;
            totalAmmo = 0;
        }
        else
        {
            magazineAmmo = magazineCapacity;
            totalAmmo -= magazineCapacity;
        }

        AmmunitionData = new WeaponAmmunitionData(magazineAmmo, totalAmmo);
        UpdateAmmoUI();

        return true;
    }
    public void AddAmmo(WeaponAmmunitionData _ammunitionData)
    {
        int magazineAmmo = AmmunitionData.magazineAmmo;
        int totalAmmo = AmmunitionData.totalAmmo;

        totalAmmo += _ammunitionData.magazineAmmo + _ammunitionData.totalAmmo;

        AmmunitionData = new WeaponAmmunitionData(magazineAmmo, totalAmmo);
        UpdateAmmoUI();
    }
    protected void UpdateAmmoUI()
    {
        if(gameObject.activeSelf)
            PlayerUI.UpdateAmmoTexts(AmmunitionData);
    }
    #endregion

    #region Animations and sounds
    public void PlayExtraClip(string clipName)
    {
        AudioClip _clip = Resources.GetExtraClip(clipName);
        if(_clip != null)
        {
            WeaponLocalAudioSource.PlayOneShot(_clip);
        }
    }
    #endregion
}
