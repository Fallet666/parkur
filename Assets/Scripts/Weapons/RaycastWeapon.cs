using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ShootingType
{
    semi,
    auto
}

[RequireComponent(typeof(Animator))]
public class RaycastWeapon : Weapon
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackDelay = 1f;
    [SerializeField] private int magazineCapacity = 30;
    [SerializeField] private ShootingType shootingType = ShootingType.auto;

    [Header("Recoil")]
    [SerializeField] private Vector2 displacementPerShootWhileRunning = Vector2.zero;
    [SerializeField] private Vector2 displacementPerShootWhileFalling = Vector2.zero;
    [SerializeField] private Vector2 displacementPerShootWhileWalking = Vector2.zero;
    [SerializeField] private Vector2 displacementPerShootWhileCrouching = Vector2.zero;

    [SerializeField] private float spreadWhileRunning = 4f;
    [SerializeField] private float spreadWhileFalling = 5f;

    [SerializeField] private float spreadChangingSpeed = 5f;
    [SerializeField] private float minSpeedToChangeSpread = 1f;
    [SerializeField] private float inScopeSpreadMultiplier = 0.3f;
    [SerializeField] private float crouchingSpreadMultiplier = 0.7f;
    [SerializeField] private float whileShootingAddingSpread = 1f;

    [Header("Hitting Rigibodyes")]
    public float attackMaxDistance = 500f;
    public float attackRadius = 0.2f;
    public float ragdollHittingForce = 100f;
    public float rigidodyHittingForce = 50f;
    public LayerMask attackLayerMask;

    [Header("Optional")]
    [SerializeField] private float awakeAttackBlockTime = 0.75f;
    [SerializeField] private float minSpeedToEnableMovementAnimations = 2f;
    [SerializeField] private float lerpingMovementAnimationsMultiplier = 10f;
    [SerializeField][Range(0f, 1f)] private float inShootingCameraInfluence = 0.2f;
    [SerializeField] private float scopingFov = 20f;
    [SerializeField] private float mainCameraScopingFovMultiplier = 0.7f;
    [SerializeField] private bool showAmmoText = true;

    [Header("Animations")]
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string attackIdValueName = "AttackID";
    [SerializeField] private int attackVariantsCount = 1;

    [SerializeField] private string reloadTriggerName = "Reload";
    [SerializeField] private float reloadTime = 2.1f;
    [SerializeField] private float lockToReloadTime = 2.3f;

    [SerializeField] private string movementBlendValue_X_Name = "MovementX";
    [SerializeField] private string movementBlendValue_Y_Name = "MovementY";

    [SerializeField] private bool useScope = false;
    [SerializeField] private string scopingLayerName = "Scoping";
    [SerializeField] private float transitionToScopingLayerLerpingMultiplier = 5f;

    [SerializeField] private string magazineOutValueName = "MagOut";
    [SerializeField] private string lastShootValueName = "LastShoot";
    [SerializeField] private string quiteOpenValueName = "QuiteOpen";

    [SerializeField] private Vector2 idlePosition = new Vector2(0f, 0f);
    [SerializeField] private Vector2 runningPosition = new Vector2(0f, 2f);
    [SerializeField] private Vector2 walkingPosition = new Vector2(0f, 1f);
    [SerializeField] private Vector2 crouchingPosition = new Vector2(0f, 0.8f);
    [SerializeField] private Vector2 fallingPosition = new Vector2(1f, 1f);

    [Header("Tracers")]
    [SerializeField] private Transform tracersOut;
    [SerializeField] private Vector3 inScopeTracerOffset;
    [SerializeField] private Vector3 tracerOffset;
    [SerializeField] private float tracerStartDistance = 2f;

    [Header("Fireball")]
    [SerializeField] private ParticleSystem fireball;

    [Header("Magazine decal")]
    [SerializeField] private Transform magazineDecalOut;
    [SerializeField] private GameObject magazineDecalPrefab;
    [SerializeField] private Vector3 magazineThrowingOutForce = Vector3.zero;

    private Animator anim;
    private float attackBlockedTime = 0f;
    private Vector2 lastMovementPosition = Vector2.zero;
    private float scopingLayerWeight = 0f;
    private int scopingLayerId = 1;
    private float spread = 0f;
    private float savedSpeed = 0f;
    private PlayerInputActions inputActions;
    private float savedScopingFov = 0f;
    private float awakeBlockedTime = 0f;

    private int savedMagazineAmmo = -1;

    public bool IsScoping
    {
        get => isScoping;
        set
        {
            if (value == isScoping)
                return;

            animatorData.isScoping = value;
            isScoping = value;
            PlayerRenderSetter.LowResolutionEnabled = value;
            PlayerMovement.LockRunning = value;
            ScopingCamera.instance.Working = value;
            ScopingCamera.instance.SetViewAngle(scopingFov);
        }
    }
    private bool isScoping;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        scopingLayerId = anim.GetLayerIndex(scopingLayerName);

        savedScopingFov = CameraLooking.FOV;
    }
    private void Update()
    {
        if (inputActions.Player.Attack.IsPressed() && shootingType == ShootingType.auto)
            Attack();

        AnimateMovement();
        if (useScope)
            AnimateScoping();
        CalculateSpread();

        if(AmmunitionData.magazineAmmo != savedMagazineAmmo)
        {
            anim.SetBool(magazineOutValueName, AmmunitionData.magazineAmmo == 0);
        }
        savedMagazineAmmo = AmmunitionData.magazineAmmo;
    }
    private void OnEnable()
    {
        attackBlockedTime = Time.time + awakeAttackBlockTime;
        awakeBlockedTime = attackBlockedTime;
        PlayerUI.SetAmmoTextActive(showAmmoText);
        UpdateAmmoUI();

        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();
        inputActions.Player.Reload.started += InputReload;
        if(shootingType == ShootingType.semi)
            inputActions.Player.Attack.started += InputAttack;


        animatorData.isReloading = false;
        anim.SetBool(magazineOutValueName, AmmunitionData.magazineAmmo == 0);
        anim.SetBool(quiteOpenValueName, QuiteOpen);
    }

    private void OnDisable()
    {
        PlayerMovement.LockForwardRunning = false;
        DynamicCrosshair.Visible = true;

        inputActions.Player.Reload.started -= InputReload;
        inputActions.Player.Attack.started -= InputAttack;
    }
    public override bool Attack()
    {
        if (Time.time < attackBlockedTime || WeaponsController.LockWeapons)
            return false;
        if (!TakeAmmo())
        {
            if(GlobalPreferences.Preferences.reloadAfterEndingAmmo)
                InputReload(default);
            return false;
        }

        //last shoot checking
        anim.SetBool(lastShootValueName, AmmunitionData.magazineAmmo == 0);

        //spread
        Vector2 _spread = new Vector2(Random.Range(-spread, spread), Random.Range(-spread, spread));
        Quaternion _spreadedRotation = Quaternion.Euler(new Vector3(-_spread.y, _spread.x, 0f));
        Vector3 _direction = _spreadedRotation * CameraLooking.instance.CameraTransform.forward;

        bool _hitted = Physics.SphereCast(CameraLooking.instance.CameraTransform.position,
            attackRadius, _direction, out RaycastHit _hit,
            attackMaxDistance, attackLayerMask);

        LastHitPoint = CameraLooking.instance.CameraTransform.position + _direction * attackMaxDistance;

        if (_hitted)
        {
            if(_hit.collider.gameObject.TryGetComponent(out HealthCollider _helath))
            {
                if(_helath.MainHealth.HealthPoints > 0)
                    PlayerUI.ActivateHitMarker();
                _helath.Damage(damage);

                if (_helath.MainHealth.HealthPoints <= 0f || _helath.MainHealth.HealthPoints < damage)
                {
                    if (_hit.collider.gameObject.TryGetComponent(out RagdollPart _rp))
                    {
                        Vector3 _force = _direction * ragdollHittingForce;
                        _rp.AddForce(_force, true);
                    }
                }
            }
            LastHitPoint = _hit.point;

            ObjectPool.SpawnHitEffect(_hit);
        }

        anim.SetInteger(attackIdValueName, Random.Range(0, attackVariantsCount));
        anim.SetTrigger(attackTriggerName);

        PlayAttackClip();

        CallSuccessfulAttackEvent();
        animatorData.isHittingAttack = _hitted;

        PlayerMovement.LockForwardRunning = true;
        CameraInfluence = inShootingCameraInfluence;

        attackBlockedTime = Time.time + attackDelay;


        //recoil
        if (MovementData.isGrounded)
        {
            if (MovementData.isCrouching)
                CameraLooking.instance.AddRotation(displacementPerShootWhileCrouching);
            else if (MovementData.isRunning)
                CameraLooking.instance.AddRotation(displacementPerShootWhileRunning);
            else
                CameraLooking.instance.AddRotation(displacementPerShootWhileWalking);
        }
        else
            CameraLooking.instance.AddRotation(displacementPerShootWhileFalling);


        //tracers
        Vector3 _tracerOut = CameraLooking.instance.CameraTransform.position;
        if (tracersOut != null)
            _tracerOut = tracersOut.position;
        _tracerOut += CameraLooking.instance.CameraTransform.forward * tracerStartDistance;
        Vector3 _offset = tracerOffset;
        if (isScoping)
            _offset = inScopeTracerOffset;
        _tracerOut += CameraLooking.instance.CameraTransform.TransformDirection(_offset);

        BulletTracer _tracer = ObjectPool.GetBulletTracer();
        if (_tracer != null)
        {
            _tracer.StartMoving(_tracerOut, LastHitPoint, Resources.tracerSpeed);
        }

        //fireball
        if(Resources.useFireball && fireball != null)
        {
            fireball.Play();
        }

        return true;
    }
    private void InputReload(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (AmmunitionData.magazineAmmo >= magazineCapacity || AmmunitionData.totalAmmo <= 0 ||
            Time.time < attackBlockedTime || WeaponsController.LockWeapons)
            return;

        attackBlockedTime = Time.time + lockToReloadTime;
        anim.SetTrigger(reloadTriggerName);
        animatorData.isReloading = true;

        StartCoroutine(LoadMagazineDelayEnumerator(reloadTime));
        SpawnMagazineDecal();
    }
    private void InputAttack(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        Attack();
    }
    private IEnumerator LoadMagazineDelayEnumerator(float _waitTime)
    {
        float _targetTime = Time.time + _waitTime;
        while(Time.time < _targetTime)
        {
            PlayerMovement.LockForwardRunning = true;
            yield return null;
        }
        PlayerMovement.LockForwardRunning = false;
        LoadMagazine(magazineCapacity);
        animatorData.isReloading = false;
    }
    private void AnimateMovement()
    {
        if (Time.time > attackBlockedTime)
        {
            PlayerMovement.LockForwardRunning = false;
            CameraInfluence = 1f;
        }

        Vector2 _target = idlePosition;
        if (MovementData.isGrounded)
        {
            if (MovementData.currentSpeed > minSpeedToEnableMovementAnimations &&
                MovementData.movement.sqrMagnitude != 0)
            {
                if (MovementData.isCrouching)
                    _target = crouchingPosition;
                else if (MovementData.isRunning)
                    _target = runningPosition;
                else
                    _target = walkingPosition;
            }
        }
        else
            _target = fallingPosition;

        Vector2 _currentMovement = Vector2.Lerp(lastMovementPosition, _target, Time.deltaTime * lerpingMovementAnimationsMultiplier);
        lastMovementPosition = _currentMovement;

        anim.SetFloat(movementBlendValue_X_Name, _currentMovement.x);
        anim.SetFloat(movementBlendValue_Y_Name, _currentMovement.y);
    }
    private void AnimateScoping()
    {
        float _target = 0f;
        if (SecondKeyPressed && MovementData.isGrounded && !animatorData.isReloading && Time.time > awakeBlockedTime && !WeaponsController.LockWeapons)
            _target = 1f;

        scopingLayerWeight = Mathf.Lerp(scopingLayerWeight, _target, Time.deltaTime * transitionToScopingLayerLerpingMultiplier);
        if(_target != scopingLayerWeight)
        {
            anim.SetLayerWeight(scopingLayerId, scopingLayerWeight);
            IsScoping = scopingLayerWeight > 0.5f;

            CameraLooking.FOV = Mathf.Lerp(savedScopingFov, savedScopingFov * mainCameraScopingFovMultiplier, scopingLayerWeight);
        }
    }
    private void PlayAttackClip()
    {
        if (Resources.attackClips.Length == 0)
            return;

        WeaponLocalAudioSource.PlayOneShot(Resources.attackClips[Random.Range(0, Resources.attackClips.Length)],
            Resources.GetAttackPitch());
    }
    private void CalculateSpread()
    {
        float _target = 0f;
        if (MovementData.isGrounded)
        {
            if (MovementData.currentSpeed > minSpeedToChangeSpread)
            {
                if (MovementData.isRunning && !MovementData.isCrouching)
                    _target = spreadWhileRunning;
                else
                    _target = 0f;
            }
        }
        else
            _target = spreadWhileFalling;

        if (Time.time < attackBlockedTime)
            _target += whileShootingAddingSpread;

        if (isScoping)
            _target *= inScopeSpreadMultiplier;
        if (MovementData.isCrouching)
            _target *= crouchingSpreadMultiplier;

        spread = Mathf.Lerp(spread, _target, spreadChangingSpeed * Time.deltaTime);
        savedSpeed = MovementData.currentSpeed;

        DynamicCrosshair.Gap = spread;
        DynamicCrosshair.Visible = !isScoping;
    }
    private void SpawnMagazineDecal()
    {
        if (magazineDecalOut == null || magazineDecalPrefab == null)
            return;
        GameObject _obj = Instantiate(magazineDecalPrefab, magazineDecalOut.position, magazineDecalOut.rotation);
        if(_obj.TryGetComponent(out Rigidbody _rb))
        {
            _rb.velocity += magazineDecalOut.TransformDirection(magazineThrowingOutForce);
        }
    }
    public void PlayOpeningClip()
    {
        if (Resources.openingClips.Length > 0)
            WeaponLocalAudioSource.PlayOneShot(Resources.openingClips[Random.Range(0, Resources.openingClips.Length)]);
    }
}
