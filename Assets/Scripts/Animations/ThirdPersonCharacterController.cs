using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponParent
{
    rightHand,
    leftHand
}

[RequireComponent(typeof(Animator))]
public class ThirdPersonCharacterController : MonoBehaviour
{
    public float distanceToGround = 0f;
    [SerializeField] private CameraLooking localCameraLooking;
    [SerializeField] private float minSpeedToEnableMovementAnimations = 2f;
    [SerializeField] private float lerpingMovementAnimationsMultiplier = 10f;
    [SerializeField] private RuntimeAnimatorController defaultAnimatorController;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;
    [SerializeField] private AudioSource source;
    [SerializeField] private ThirdPersonOnSpineWeaponPlacer weaponPlacer;
    [SerializeField] private Transform[] uselessInLocalBones;
    [SerializeField] private Transform neck;
    [SerializeField] private Vector2 neckOffset = Vector2.zero;
    [SerializeField] private SkinnedMeshRenderer[] bodyMeshes;
    [SerializeField] private Ragdoll ragdoll;
    [SerializeField] private GameObject headCamera;

    [Header("Air")]
    [SerializeField] private float minDistanceToStartGroundToAnimateFalling = 0.1f;
    [SerializeField] private float minDistanceToEndGroundAnimateFalling = 0.4f;
    [SerializeField] private float lerpingOnStartToAirMovementTransitionMultiplier = 10f;
    [SerializeField] private float lerpingInEndToAirMovementTransitionMultiplier = 20f;
    [SerializeField] private float lerpingTransitionOfFallingStatesMultiplier = 2f;
    [SerializeField] private float minHeightWhenOpenedStateOfFallingStarts = 0.7f;

    [Header("Animators parameters")]
    private string xMovementBlendValueName = "MovementX";
    private string yMovementBlendValueName = "MovementY";
    private string crouchMovementLayerName = "CrouchMovement";
    private string airMovementLayerName = "AirMovement";
    private string fallingStateBlendValueName = "Falling";
    private string cameraLookingBlendValueName = "Looking";
    private string attackTriggerName = "Attack";
    private string scopingValueName = "Scoping";
    private string reloadTriggerName = "Reload";
    private string resetWeaponAnimationsTriggerName = "ResetWeaponAnimations";

    public PlayerMovementData MovementData { get; set; }
    public WeaponAnimatorData WepAnimatorData { get => animatorData; set { animatorData = value; } }
    public WeaponHitData HitData { get; set; }
    public Vector3 CameraRotation { get; set; }
    public bool IsLocal { get; private set; }
    public Ragdoll CharacterRagdoll { get => ragdoll; }

    private List<ThirdPersonWeaponObject> weapons = new List<ThirdPersonWeaponObject>();
    private ThirdPersonWeaponObject currentWeaponObject;
    private int openedWeaponId = 0;


    private Animator anim;
    private Vector2 movementPosition;
    private WeaponAnimatorData animatorData;
    private float airMovementLayerWeight = 0f;
    private float transitionOfFallingStates = 0f;
    private float crouchMovementLayerWeight = 0f;
    private bool savedReloadingState = false;
    private Vector3 startLocalPosition = Vector3.zero;
    private Transform tr;

    private int airMovementLayerIndex = 0;
    private int crouchMovementLayerIndex = 0;


    private void Awake()
    {
        anim = GetComponent<Animator>();
        anim.runtimeAnimatorController = defaultAnimatorController;
        tr = transform;
    }
    private void OnEnable()
    {
        airMovementLayerIndex = anim.GetLayerIndex(airMovementLayerName);
        crouchMovementLayerIndex = anim.GetLayerIndex(crouchMovementLayerName);
        startLocalPosition = transform.localPosition;
        SetHeadCameraActive(false);
    }

    private void Update()
    {
        AnimateMovement();
        AnimateLookRotation();
        UpdateTriggers();
        MoveNeckToCamera();
    }
    private void AnimateMovement()
    {
        Vector2 _targetPosition = Vector2.zero;
        if (MovementData.currentSpeed >= minSpeedToEnableMovementAnimations)
        {
            _targetPosition = new Vector2(MovementData.movement.x, MovementData.movement.z);
            if (MovementData.isRunning)
                _targetPosition *= 2f;
        }
        movementPosition = Vector2.Lerp(movementPosition, _targetPosition,
            Time.deltaTime * lerpingMovementAnimationsMultiplier);
        anim.SetFloat(xMovementBlendValueName, movementPosition.x);
        anim.SetFloat(yMovementBlendValueName, movementPosition.y);

        //transition to air movement
        float _airTarget = 0f;
        float _multiplier = lerpingOnStartToAirMovementTransitionMultiplier;
        if (MovementData.distanceToGround > minDistanceToStartGroundToAnimateFalling)
        {
            _airTarget = 1f;
        }
        if (MovementData.distanceToGround < minDistanceToEndGroundAnimateFalling && MovementData.distanceToGround <= distanceToGround)
        {
            _multiplier = lerpingInEndToAirMovementTransitionMultiplier;
            _airTarget = 0f;
        }
        _airTarget = Mathf.Lerp(airMovementLayerWeight, _airTarget, Time.deltaTime * _multiplier);


        if (_airTarget != airMovementLayerWeight)
            anim.SetLayerWeight(airMovementLayerIndex, _airTarget);
        airMovementLayerWeight = _airTarget;

        //transition between falling states
        if (airMovementLayerWeight > 0.01f)
        {
            float _fallingTarget = 0f;
            if (distanceToGround > minHeightWhenOpenedStateOfFallingStarts)
                _fallingTarget = 1f;
            transitionOfFallingStates = Mathf.Lerp(transitionOfFallingStates, _fallingTarget,
                Time.deltaTime * lerpingTransitionOfFallingStatesMultiplier);
        }
        else
            transitionOfFallingStates = 0f;
        anim.SetFloat(fallingStateBlendValueName, transitionOfFallingStates);

        //transition to crouch layer
        if(MovementData.crouchStep != crouchMovementLayerWeight)
            anim.SetLayerWeight(crouchMovementLayerIndex, MovementData.crouchStep);
        crouchMovementLayerWeight = MovementData.crouchStep;

        distanceToGround = MovementData.distanceToGround;

        //scoping
        anim.SetBool(scopingValueName, animatorData.isScoping);
    }
    private void AnimateLookRotation()
    {
        float _looking = CameraRotation.x / 90f * -1f;
        anim.SetFloat(cameraLookingBlendValueName, _looking);
    }
    private void UpdateTriggers()
    {
        if(savedReloadingState != WepAnimatorData.isReloading)
        {
            if (WepAnimatorData.isReloading)
            {
                anim.SetTrigger(reloadTriggerName);
            }
        }
        savedReloadingState = WepAnimatorData.isReloading;
    }
    private void PlayAudioClip(AudioClip _clip, float _pitch = 1f, float _maxDist = 50f)
    {
        float _distance = Vector3.Distance(CameraLooking.instance.CameraTransform.position, transform.position);
        float _time = _distance / 300f;

        StartCoroutine(PlayAudioWithDelayEnumerator(_clip, _time, _pitch, _maxDist));
    }
    private IEnumerator PlayAudioWithDelayEnumerator(AudioClip _clip, float _delay, float _pitch = 1f, float _maxDist = 50f)
    {
        if (source == null)
            yield break;
        yield return new WaitForSeconds(_delay);
        source.pitch = _pitch;
        source.maxDistance = _maxDist;
        source.PlayOneShot(_clip);
    }
    public void AnimateAttack()
    {
        if (currentWeaponObject == null)
            return;

        //base 
        anim.SetTrigger(attackTriggerName);
        if (currentWeaponObject.resources != null && currentWeaponObject.resources.attackClips.Length != 0)
        {
            AudioClip _clip = currentWeaponObject.resources.attackClips[Random.Range(
                0, currentWeaponObject.resources.attackClips.Length)];
            PlayAudioClip(_clip, currentWeaponObject.resources.GetAttackPitch(), currentWeaponObject.resources.attackClipsMaxDistance);
        }
        animatorData.attackTrigger = false;

        //tracer
        if(currentWeaponObject.resources.tracerType == TracerType.standartBullet)
        {
            BulletTracer _tracer = ObjectPool.GetBulletTracer();
            if (_tracer != null)
            {
                Vector3 _tracerOut = currentWeaponObject.transform.position;
                if (currentWeaponObject.TracersOut != null)
                    _tracerOut = currentWeaponObject.TracersOut.position;
                _tracer.StartMoving(_tracerOut, HitData.hitPoint, currentWeaponObject.resources.tracerSpeed);
            }
        }

        //fireball
        if (currentWeaponObject.resources.useFireball)
            currentWeaponObject.ActivateFireball();
    }
    public void InitializeWeapon(WeaponResources _res)
    {
        foreach(ThirdPersonWeaponObject j in weapons)
        {
            if (j.resources == _res)
                return;
        }

        Transform _parent = leftHand;
        Transform _otherParent = rightHand;
        if (_res.ThirdPersonWeaponObjectParent == WeaponParent.rightHand)
        {
            _parent = rightHand;
            _otherParent = leftHand;
        }

        if (_res.ThirdPersonWeaponObject == null)
            return;

        ThirdPersonWeaponObject _obj = Instantiate(_res.ThirdPersonWeaponObject, _parent);
        _obj.resources = _res;
        _obj.inHandLocalTransforms = GameObjectsTransforms.GetLocalTransforms(_obj.transform);

        weapons.Add(_obj);

        _obj.InitializeOtherHandObject(_otherParent);

        if(weaponPlacer != null)
            weaponPlacer.PlaceToSpine(_obj);
    }
    private void PlaceWeaponToSpine(ThirdPersonWeaponObject _obj)
    {
        weaponPlacer.PlaceToSpine(_obj);
    }
    private void PlaceWeaponToHand(ThirdPersonWeaponObject _obj)
    {
        Transform _parent = leftHand;
        if (_obj.resources.ThirdPersonWeaponObjectParent == WeaponParent.rightHand)
            _parent = rightHand;
        _obj.SetChildOf(_parent, _obj.inHandLocalTransforms);
    }
    public void DeinitializeWeapon(WeaponResources _res)
    {
        ThirdPersonWeaponObject[] _weapons = weapons.ToArray();
        foreach (ThirdPersonWeaponObject _obj in _weapons)
        {
            if(_obj.resources == _res)
            {
                if (_obj == currentWeaponObject)
                    CloseAllWeapons();

                weapons.Remove(_obj);
                _obj.DestroyMe();
            }
        }
    }
    public void OpenWeapon(string _weaponName)
    {
        if (weapons.Count == 0)
            return;

        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i].resources.weaponName == _weaponName)
            {
                if (weapons[i].resources.ThirdPersonAnimatorController != null)
                {
                    anim.runtimeAnimatorController = weapons[i].resources.ThirdPersonAnimatorController;
                    anim.SetTrigger(resetWeaponAnimationsTriggerName);
                }
                openedWeaponId = i;
                currentWeaponObject = weapons[i];
                PlaceCurrentWeaponToSpine();
            }
            else
            {
                PlaceWeaponToSpine(weapons[i]);
            }
        }
    }
    public void CloseAllWeapons()
    {
        foreach(ThirdPersonWeaponObject _wep in weapons)
        {
            PlaceWeaponToSpine(_wep);
        }
        anim.runtimeAnimatorController = defaultAnimatorController;
        currentWeaponObject = null;
    }

    public bool IsWeaponInitialized(string _weaponName)
    {
        bool _result = false;
        foreach (var _wep in weapons)
        {
            if(_wep.resources.weaponName == _weaponName)
            {
                _result = true;
                break;
            }
        }
        return _result;
    }
    public void SetLocal()
    {
        IsLocal = true;
        foreach (Transform _tr in uselessInLocalBones)
            _tr.localScale = Vector3.zero;
        foreach (SkinnedMeshRenderer _mesh in bodyMeshes)
            _mesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    public void ResetLocal()
    {
        IsLocal = false;
        foreach (Transform _tr in uselessInLocalBones)
            _tr.localScale = Vector3.one;
        foreach (SkinnedMeshRenderer _mesh in bodyMeshes)
            _mesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }
    private void MoveNeckToCamera()
    {
        if (!IsLocal || CameraLooking.instance == null || neck == null)
            return;
        Vector3 _dir = CameraLooking.instance.CameraTransform.position - neck.transform.position;
        _dir = VectorMathf.RemoveDotVector(_dir, tr.up);
        tr.position += _dir;
        tr.localPosition += new Vector3(neckOffset.x, 0f, neckOffset.y);
    }

    public void SetupHealthColliders(Health _health)
    {
        HealthCollider[] _colls = GetComponentsInChildren<HealthCollider>();
        foreach(HealthCollider _col in _colls)
        {
            _col.MainHealth = _health;
        }
    }

    public void ActivateRagdoll()
    {
        if (ragdoll == null) return;
        ragdoll.ActivatePhysicsRagdoll();
    }
    public void DisableRagdoll()
    {
        if (ragdoll == null) return;
        ragdoll.DeactivateRagdoll();
    }
    public void SetHeadCameraActive(bool activeState)
    {
        if (headCamera != null)
            headCamera.SetActive(activeState);
    }
    public void AddForceToRagdoll(int ragdollPartId, Vector3 force)
    {
        ragdoll.AddForceToRagdollPart(ragdollPartId, force);
    }
    #region Animator eventas
    public void ShowOtherHandObject()
    {
        SetOtherHandObjectVisible(true);
    }
    public void HideOtherHandObject()
    {
        SetOtherHandObjectVisible(false);
    }
    public void SetOtherHandObjectVisible(bool activeSelf)
    {
        if (currentWeaponObject == null)
            return;
        currentWeaponObject.SetOtherHandObjectActive(activeSelf);
    }
    public void ShowWeaponMagazine()
    {
        SetWeaponMagazineVisible(true);
    }
    public void HideWeaponMagazine()
    {
        SetWeaponMagazineVisible(false);
    }
    public void SpawnMagazineDecal()
    {
        if (currentWeaponObject == null)
            return;
        currentWeaponObject.SpawnMagazineDecal();
    }
    public void SetWeaponMagazineVisible(bool activeSelf)
    {
        if (currentWeaponObject == null)
            return;
        currentWeaponObject.SetMagazineActive(activeSelf);
    }
    public void PlaceCurrentWeaponToSpine()
    {
        if (currentWeaponObject != null)
            PlaceWeaponToSpine(currentWeaponObject);
    }
    public void PlaceCurrentWeaponToHand()
    {
        if (currentWeaponObject != null)
            PlaceWeaponToHand(currentWeaponObject);
    }
    public void PlayOpeningClip()
    {
        if (currentWeaponObject == null)
            return;

        if (currentWeaponObject.resources.openingClips.Length > 0)
        {
            PlayAudioClip(currentWeaponObject.resources.openingClips[Random.Range(0, currentWeaponObject.resources.openingClips.Length)],
                1f, currentWeaponObject.resources.extraClipsMaxDistance);
        }
    }
    public void PLayExtraClip(string clipName)
    {
        if (currentWeaponObject == null)
            return;

        AudioClip _clip = currentWeaponObject.resources.GetExtraClip(clipName);
        if(_clip != null)
        {
            PlayAudioClip(_clip, 1f, currentWeaponObject.resources.extraClipsMaxDistance);
        }
    }
    #endregion
}
