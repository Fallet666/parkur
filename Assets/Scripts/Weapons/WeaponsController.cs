using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

public delegate void WeaponEventHandler(Weapon wep);
public delegate void WeaponHitDataEventHandler(WeaponHitData _hitData);
[System.Serializable]
public struct WeaponControllerState : INetworkSerializable
{
    public FixedString32Bytes openedWeapon;
    public FixedString32Bytes initializedWeapons;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref openedWeapon);
        serializer.SerializeValue(ref initializedWeapons);
    }

    public WeaponControllerState (string _opened, string _initialized)
    {
        openedWeapon = _opened;
        initializedWeapons = _initialized;
    }

}
[System.Serializable]
public struct WeaponAnimatorData : INetworkSerializable
{
    public bool attackTrigger;
    public bool isHittingAttack;
    public bool isScoping;
    public bool isReloading;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref attackTrigger);
        serializer.SerializeValue(ref isHittingAttack);
        serializer.SerializeValue(ref isScoping);
        serializer.SerializeValue(ref isReloading);
    }
}
[System.Serializable]
public struct WeaponHitData : INetworkSerializable
{
    public Vector3 hitPoint;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref hitPoint);
    }
}
[System.Serializable]
public struct WeaponAmmunitionData: INetworkSerializable
{
    public int magazineAmmo;
    public int totalAmmo;

    public WeaponAmmunitionData(int _magazine, int _total)
    {
        magazineAmmo = _magazine;
        totalAmmo = _total;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref magazineAmmo);
        serializer.SerializeValue(ref totalAmmo);
    }
}

public class WeaponsController : NetworkBehaviour
{
    public static WeaponsController instance;


    [SerializeField] private ThirdPersonCharacterController characterAnimations;
    [SerializeField] private MovementStepsPlayer stepsPlayer;

    [Header("Weapons prefabs")]
    [SerializeField] private WeaponResources[] weaponPrefabs;

    [Header("Parameters")]
    [SerializeField] private Transform weaponsParent;
    [SerializeField] private Transform cameraObject;
    [SerializeField] private Transform weaponCameraObject;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerNetwork playerNetwork;
    [SerializeField] private WeaponLocalAudioSource localAudioSource;
    [SerializeField] private DynamicCrosshair dynamicCrosshair;
    [SerializeField] private float defaultCrosshairGap = 0f;
    [SerializeField] private Transform dropOut;
    [SerializeField] private float throwDropForce = 5f;
    [SerializeField] private float quiteOpenAfterClosingPhysicsWeaponInterval = 0.5f;


    private List<Weapon> weapons = new List<Weapon>();
    public Weapon currentWeapon { get; private set; }
    private int openedWeaponId = 0;

    public string openedWeapon;
    public string initializedWeapons;
    private bool lockWeapons;

    private PhysicsWeapon currentPhysicsWeapon;
    private string openedBeforePhysicsWeapon;
    private float blockedToOpenWeaponTime;
    private float phisicsWeaponWorkedTime;

    public List<Weapon> Weapons { get => weapons; }
    public static bool LockWeapons
    {
        get
        {
            if (instance != null)
                return instance.lockWeapons;
            return false;
        }
        set
        {
            if (instance != null)
                instance.lockWeapons = value;
        }
    }

    public WeaponControllerState CurrentState
    {
        get => controllerState;
        set
        {
            if (IsOwner)
                return;
            Debug.Log(gameObject.name);
            if(value.initializedWeapons != controllerState.initializedWeapons)
            {
                string[] _newWeaponList = StringHelpers.DeserializeStringArray(value.initializedWeapons.ToString());
                foreach (string _weaponName in _newWeaponList)
                {
                    WeaponResources _res = WeaponResourcesBase.GetWeaponResources(_weaponName);
                    if (_res != null && !characterAnimations.IsWeaponInitialized(_weaponName))
                    {
                        characterAnimations.InitializeWeapon(_res);
                    }
                }
                controllerState.initializedWeapons = value.initializedWeapons;
            }
            if(value.openedWeapon != controllerState.openedWeapon)
            {
                characterAnimations.OpenWeapon(value.openedWeapon.ToString());
                controllerState.openedWeapon = value.openedWeapon;
            }
        }
    }

    public WeaponControllerState controllerState = new WeaponControllerState();
    private NetworkVariable<WeaponControllerState> syncingControllerState = new NetworkVariable<WeaponControllerState>(
        new WeaponControllerState(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public WeaponAnimatorData currentAnimatorData = new WeaponAnimatorData();
    private NetworkVariable<WeaponAnimatorData> syncingAnimatorData = new NetworkVariable<WeaponAnimatorData>(
    new WeaponAnimatorData(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private PlayerInputActions inputActions;

    private void OnEnable()
    {
        if (playerMovement == null || playerNetwork == null)
            enabled = false;
    }
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            #region Input
            inputActions = new PlayerInputActions();
            inputActions.Player.Enable();

            inputActions.Player.Rilfe.started += (InputAction.CallbackContext obj) => OpenWeaponByType(WeaponType.rifle);
            inputActions.Player.Pistol.started += (InputAction.CallbackContext obj) => OpenWeaponByType(WeaponType.pistol);
            inputActions.Player.Melle.started += (InputAction.CallbackContext obj) => OpenWeaponByType(WeaponType.melle);
            inputActions.Player.CloseSlots.started += (InputAction.CallbackContext obj) => CloseAllWeapons();
            inputActions.Player.Drop.started += (InputAction.CallbackContext obj) => DropCurrentWeapon();
            inputActions.Player.Use.started += UsePhysicsWeapon;
            //inputActions.Player.Attack.started += (InputAction.CallbackContext obj) => Attack();
            #endregion

            onWeaponListUpdated += CheckPhysicsWeapon;

            if (instance == null)
                instance = this;
            //Destroy(characterAnimations.gameObject);
            characterAnimations.SetLocal();

            InstantiateWeapons();
            OpenWeaponById(openedWeaponId);


            if(localAudioSource != null)
            {
                WeaponLocalAudioSource.SetSingletone(localAudioSource);
            }
            if (dynamicCrosshair != null)
                DynamicCrosshair.SetSingletone(dynamicCrosshair);
        }
        else
        {
            syncingControllerState.OnValueChanged += OnControllerStateChanged;
            syncingAnimatorData.OnValueChanged += OnWeaponAnimatorDataSynced;
            if (!IsServer)
                RequestControllerStateServerRPC(NetworkManager.LocalClientId);
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {

        }
        else
        {
            syncingControllerState.OnValueChanged -= OnControllerStateChanged;
            syncingAnimatorData.OnValueChanged -= OnWeaponAnimatorDataSynced;
        }
    }
    private void Update()
    {
        if (IsOwner)
        {
            if (currentWeapon != null)
            {
                currentWeapon.MovementData = playerMovement.CurrentMovementData;
                currentWeapon.SecondKeyPressed = inputActions.Player.SecondAttack.IsPressed();

                cameraObject.rotation = Quaternion.Lerp(cameraObject.rotation, currentWeapon.CameraParent.rotation, 
                    currentWeapon.CameraInfluence);
                weaponCameraObject.position = currentWeapon.CameraParent.position;
                weaponCameraObject.rotation = currentWeapon.CameraParent.rotation;

                currentAnimatorData = currentWeapon.AnimatorData;
                syncingAnimatorData.Value = currentAnimatorData;
            }

            characterAnimations.MovementData = playerMovement.CurrentMovementData;
        }
        else
        {
            characterAnimations.MovementData = playerMovement.CurrentMovementData;
            characterAnimations.CameraRotation = playerNetwork.CameraRotation;
        }
        openedWeapon = controllerState.openedWeapon.ToString();
        initializedWeapons = controllerState.initializedWeapons.ToString();

        if(stepsPlayer != null)
        {
            stepsPlayer.MovementData = playerMovement.CurrentMovementData;
            stepsPlayer.Resources = playerMovement.GetWalker.CurrentSurface;
        }
    }
    private void InstantiateWeapons()
    {
        foreach (WeaponResources _res in weaponPrefabs)
        {
            if (IsOwner)
            {
                InitializeWeapon(_res.weaponName);
            }
            else
            {
                characterAnimations.InitializeWeapon(_res);
            }
        }
        if(IsOwner)
            syncingControllerState.Value = controllerState;
    }

    #region Events
    public WeaponEventHandler onWeaponChanged;
    public VoidEventHandler onWeaponListUpdated;
    public static VoidEventHandler onWeaponsInitialized;
    #endregion
    #region Opening and closing Weapons
    private void OpenWeaponByName(string weaponName)
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i].Resources.weaponName == weaponName) 
            {
                OpenWeaponById(i);
                return;
            }
        }
    }
    private void OpenWeaponById(int _id)
    {
        if (_id >= weapons.Count || (openedWeaponId == _id && currentWeapon != null) || Time.time < blockedToOpenWeaponTime)
            return;


        for (int i = 0; i < weapons.Count; i++)
        {
            if (i == _id)
            {
                weapons[i].QuiteOpen = Time.time < phisicsWeaponWorkedTime + quiteOpenAfterClosingPhysicsWeaponInterval;
                weapons[i].Open();
                currentWeapon = weapons[i];

                controllerState.openedWeapon = weapons[i].Resources.weaponName;
                syncingControllerState.Value = controllerState;

                weapons[i].OnSuccessfulAttack += OnSuccessfulAttackServerRPC;
                onWeaponChanged?.Invoke(weapons[i]);

            }
            else
            {
                weapons[i].Close();
                weapons[i].OnSuccessfulAttack -= OnSuccessfulAttackServerRPC;
            }
        }
        openedWeaponId = _id;
    }
    private void OpenWeaponByType(WeaponType type)
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i].Resources.weaponType == type)
            {
                OpenWeaponById(i);
                return;
            }
        }
    }

    private void CloseAllWeapons()
    {
        foreach (Weapon _wep in weapons)
        {
            _wep.Close();
            _wep.OnSuccessfulAttack -= OnSuccessfulAttackServerRPC;
        }
        currentWeapon = null;

        controllerState.openedWeapon = "";
        syncingControllerState.Value = controllerState;

        DynamicCrosshair.Visible = true;
        DynamicCrosshair.Gap = defaultCrosshairGap;
        PlayerUI.SetAmmoTextActive(false);
    }
    #endregion
    #region Attacks
    private void Attack()
    {
        if (weapons.Count == 0 || openedWeaponId >= weapons.Count)
            return;

        weapons[openedWeaponId].Attack();
    }
    [ServerRpc] private void OnSuccessfulAttackServerRPC(WeaponHitData _hitData = default)
    {
        if (!IsOwner)
        {
            characterAnimations.HitData = _hitData;
            characterAnimations.AnimateAttack();
        }
        OnSuccessfulAttackClientRPC(_hitData);
    }
    [ClientRpc] private void OnSuccessfulAttackClientRPC(WeaponHitData _hitData)
    {
        if (IsOwner || IsServer) 
            return;
        characterAnimations.HitData = _hitData;
        characterAnimations.AnimateAttack();
    }

    #endregion
    #region Adding and droping weapons
    public void AddWeapon(IWeaponContainable container)
    {
        Weapon _wep = GetWeaponByName(container.ContainerData.weaponName.ToString());
        if(_wep != null)
        {
            _wep.AddAmmo(container.ContainerData.ammunitionData);
            if (GlobalPreferences.Preferences.openWeaponAfterTaking && !container.ContainerData.onlyAmmo)
                OpenWeaponByName(container.ContainerData.weaponName.ToString());
        }
        else if(!container.ContainerData.onlyAmmo)
        {
            InitializeWeapon(container.ContainerData.weaponName.ToString(), container.ContainerData.ammunitionData);
            if (GlobalPreferences.Preferences.openWeaponAfterTaking)
                OpenWeaponByName(container.ContainerData.weaponName.ToString());
        }
    }
    public void DropCurrentWeapon()
    {
        if (!IsOwner)
            return;
        if (currentWeapon == null)
            return;

        DropCurrentWeaponServerRPC(new WeaponContainerData(currentWeapon));
        DeinitializeWeapon(currentWeapon.Resources.weaponName);
    }
    [ServerRpc] private void DropCurrentWeaponServerRPC(WeaponContainerData containerData)
    {
        WeaponResources _res = WeaponResourcesBase.GetWeaponResources(containerData.weaponName.ToString());

        if (_res.worldWeaponContainer == null || dropOut == null)
            return;

        
        WorldWeaponContainer container = Instantiate(_res.worldWeaponContainer, dropOut.position, dropOut.rotation);

        container.applyStartData = false;

        container.GetComponent<NetworkObject>().Spawn(true);
        container.GetComponent<NetworkTransform>().Teleport(dropOut.position, dropOut.rotation, container.transform.lossyScale);
        container.ThrowServerRPC(dropOut.forward * (throwDropForce + playerMovement.CurrentMovementData.currentSpeed));

        WeaponContainerData _data = new WeaponContainerData
        {
            weaponName = _res.weaponName,
            ammunitionData = containerData.ammunitionData
        };
        container.SetContainerData(_data);
    }
    #endregion
    #region Special weapons functions
    private void CheckPhysicsWeapon()
    {
        if (currentPhysicsWeapon != null)
        {
            currentPhysicsWeapon.OnGrabbableDroppedAndAnimationsEnded -= StopPhysicsWeapon;
            currentPhysicsWeapon.OnGrabbableDropped -= BlockWeaponsOpening;
        }
        foreach (var wep in weapons)
        {
            if(wep.gameObject.TryGetComponent(out PhysicsWeapon physWep))
            {
                if (currentWeapon != null)
                    openedBeforePhysicsWeapon = currentWeapon.Resources.weaponName;
                else
                    openedBeforePhysicsWeapon = null;
                currentPhysicsWeapon = physWep;
                currentPhysicsWeapon.OnGrabbableDroppedAndAnimationsEnded += StopPhysicsWeapon;
                currentPhysicsWeapon.OnGrabbableDropped += BlockWeaponsOpening;
                break;
            }
        }
    }
    private void BlockWeaponsOpening()
    {
        blockedToOpenWeaponTime = Time.time + 1;
    }
    private void UsePhysicsWeapon(InputAction.CallbackContext obj)
    {
        if (currentPhysicsWeapon == null || currentWeapon == currentPhysicsWeapon)
            return;
        if (!currentPhysicsWeapon.CheckGrabbableObject())
            return;
        if(currentWeapon != null)
            openedBeforePhysicsWeapon = currentWeapon.Resources.weaponName;
        OpenWeaponByName(currentPhysicsWeapon.Resources.weaponName);
        currentPhysicsWeapon.TryGrab(inputActions);
    }
    private void StopPhysicsWeapon()
    {
        if (currentWeapon != null && openedBeforePhysicsWeapon == currentWeapon.Resources.weaponName)
            return;
        phisicsWeaponWorkedTime = Time.time;
        if (openedBeforePhysicsWeapon != "" && openedBeforePhysicsWeapon != null)
        {
            blockedToOpenWeaponTime = 0f;
            CloseAllWeapons();
            OpenWeaponByName(openedBeforePhysicsWeapon);
        }
    }
    #endregion
    private void OnWeaponAnimatorDataSynced(WeaponAnimatorData oldData, WeaponAnimatorData newData)
    {
        currentAnimatorData = newData;
        if (oldData.attackTrigger == newData.attackTrigger)
            currentAnimatorData.attackTrigger = false;

        characterAnimations.WepAnimatorData = newData;
    }
    [ServerRpc(RequireOwnership = false)] private void RequestControllerStateServerRPC(ulong clientId)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams()
        {
            Send = new ClientRpcSendParams()
            {
                TargetClientIds = new ulong[] {clientId}
            }
        };
        SendControllerStateClientRPC(controllerState, clientRpcParams);
    }
    [ClientRpc] private void SendControllerStateClientRPC(WeaponControllerState _state, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"information getted");

        OnControllerStateChanged(controllerState, _state);
    }
    private void OnControllerStateChanged(WeaponControllerState oldState, WeaponControllerState newState)
    {
        controllerState = newState;

        if (oldState.initializedWeapons != newState.initializedWeapons)
        {
            string[] oldInitialization = StringHelpers.DeserializeStringArray(oldState.initializedWeapons.ToString());

            List<string> toRemove = new List<string>();
            List<string> toAdd = StringHelpers.DeserializeStringList(newState.initializedWeapons.ToString());

            foreach (string j in oldInitialization)
            {
                if(toAdd.Contains(j))
                {
                    toAdd.Remove(j);
                }
                else
                {
                    toRemove.Add(j);
                }
            }

            foreach (string _add in toAdd)
            {
                InitializeWeapon(_add);
            }
            foreach(string _remove in toRemove)
            {
                DeinitializeWeapon(_remove);
            }
        }

        if(oldState.openedWeapon != newState.openedWeapon)
        {
            if (newState.openedWeapon.ToString() == "")
            {
                characterAnimations.CloseAllWeapons();
            }
            else
            {
                characterAnimations.OpenWeapon(newState.openedWeapon.ToString());
            }
        }
    }
    private void InitializeWeapon(string weaponName, WeaponAmmunitionData _ammoData = default)
    {
        WeaponResources _res = WeaponResourcesBase.GetWeaponResources(weaponName);
        if (_res == null)
            return;

        if (IsOwner)
        {
            if (GetWeaponByName(weaponName) != null)
                return;

            Weapon _wep = Instantiate(_res.FirstPersonWeaponPrefab, weaponsParent);
            _wep.Resources = _res;
            _wep.AmmunitionData = _ammoData;
            weapons.Add(_wep);
            _wep.Close();

            onWeaponListUpdated?.Invoke();

            controllerState = GetCurrentControlellerState();
            syncingControllerState.Value = controllerState;
        }
        else
            characterAnimations.InitializeWeapon(_res);

        onWeaponsInitialized?.Invoke();
    }
    private void DeinitializeWeapon(string weaponName)
    {
        WeaponResources _res = WeaponResourcesBase.GetWeaponResources(weaponName);
        if (_res == null)
            return;

        if (IsOwner)
        {
            Weapon _wep = GetWeaponByName(weaponName);
            if (_wep == null)
                return;

            if (_wep == currentWeapon)
                CloseAllWeapons();

            Destroy(_wep.gameObject);
            weapons.Remove(_wep);
            onWeaponListUpdated?.Invoke();

            controllerState = GetCurrentControlellerState();
            syncingControllerState.Value = controllerState;
        }
        else
            characterAnimations.DeinitializeWeapon(_res);
    }
    private Weapon GetWeaponByName(string _name)
    {
        foreach (var _wep in weapons)
        {
            if (_wep.Resources.weaponName == _name)
            {
                return _wep;
            }
        }
        return null;
    }
    private WeaponControllerState GetCurrentControlellerState()
    {
        string _opened = "";
        if (currentWeapon != null)
            _opened = currentWeapon.Resources.weaponName;

        List<string> _weaponNames = new List<string>();
        foreach (Weapon _wep in weapons)
            _weaponNames.Add(_wep.Resources.weaponName);
        string _initialized = StringHelpers.SerializeStringArray(_weaponNames.ToArray());

        return new WeaponControllerState(_opened, _initialized);
    }

    public bool IsContainsWeapon(string weaponName)
    {
        return GetWeaponByName(weaponName) != null;
    }
}
