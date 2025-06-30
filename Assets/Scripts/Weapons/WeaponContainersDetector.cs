using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class WeaponContainersDetector : MonoBehaviour
{
    [SerializeField] private WeaponsController weaponsController;
    [SerializeField] private float detectingDistance = 4f;
    [SerializeField] private float detectingRadius = 0.1f;
    [SerializeField] private string noWeaponMessage = "You can't take ammo without gun";
    [SerializeField] private string alreadyHaveWeaponMessage = "You can't take this weapon twice";
    [SerializeField] private float weaponMessageLifeTime = 1f;

    private PlayerInputActions inputActions;
    private GameObject savedGameObject;
    public WorldWeaponContainer currentContainer;

    private void Awake()
    {
        if (weaponsController == null)
            return;

        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();

        inputActions.Player.Use.started += OnUseButtonPressed;
    }

    private void Update()
    {
        if (CameraLooking.instance == null)
            return;

        if (Physics.SphereCast(CameraLooking.instance.CameraTransform.position, detectingRadius,
            CameraLooking.instance.CameraTransform.forward, out RaycastHit _hit, detectingDistance))
        {
            if (_hit.collider.gameObject == savedGameObject)
                return;
            if (savedGameObject != null && savedGameObject.TryGetComponent(out WorldWeaponContainer _cont))
            {
                OnWorldWeaponContainerLose(_cont);
            }
            else
                PlayerUI.SetTakingBarActive(false);
            savedGameObject = _hit.collider.gameObject;

            if (_hit.collider.gameObject.TryGetComponent(out WorldWeaponContainer _container))
            {
                OnWorldWeaponContainerDetected(_container);
            }
        }
        else
            OnWorldWeaponContainerLose(currentContainer);
    }

    private void OnUseButtonPressed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if(currentContainer != null)
        {
            bool containsWeapon = weaponsController.IsContainsWeapon(currentContainer.ContainerData.weaponName.ToString());
            if (currentContainer.ContainerData.onlyAmmo && !containsWeapon)
            {
                PlayerUI.ViewMessage(noWeaponMessage, weaponMessageLifeTime);
                return;
            }
            if(!currentContainer.ContainerData.onlyAmmo && containsWeapon)
            {
                PlayerUI.ViewMessage(alreadyHaveWeaponMessage, weaponMessageLifeTime);
                return;
            }

            weaponsController.AddWeapon(currentContainer);
            if (currentContainer.GetComponent<NetworkBehaviour>().IsSpawned)
            {
                currentContainer.DestroyServerRPC();
            }
        }
    }

    private void OnWorldWeaponContainerDetected(WorldWeaponContainer container)
    {
        currentContainer = container;

        PlayerUI.SetTakingBarActive(true);
        container.ShowOutline(true);
    }
    private void OnWorldWeaponContainerLose(WorldWeaponContainer container)
    {
        PlayerUI.SetTakingBarActive(false);
        if(container != null)
            container.ShowOutline(false);
        currentContainer = null;
    }
}
