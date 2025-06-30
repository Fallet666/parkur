using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Animator))]
public class MelleWeapon : Weapon
{
    [Header("Base settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float damageDelay = 0.4f;

    [Header("Hitting Rigibodyes")]
    public float attackMaxDistance = 1.5f;
    public float attackRadius = 0.2f;
    public float ragdollHittingForce = 100f;
    public float rigidodyHittingForce = 50f;
    public LayerMask attackLayerMask;

    [Header("Optional")]
    [SerializeField] private float attackTimeDelay = 0.75f;
    [SerializeField] private float awakeAttackBlockTime = 0.75f;
    [SerializeField] private float minSpeedToEnableMovementAnimations = 2f;
    [SerializeField] private float lerpingMovementAnimationsMultiplier = 10f;
    [SerializeField][Range(0f, 1f)] private float inShootingCameraInfluence = 1f;
    /// <summary>
    /// gap while attackRadius = 1
    /// </summary>
    [SerializeField] private float crosshairTargetGap = 5f;


    [Header("animator parameters")]
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string attackIdValueName = "AttackID";
    [SerializeField] private int attacksCount = 3;
    [SerializeField] private string hittingAttackTriggerName = "HittingAttack";
    [SerializeField] private string hittingAttackIdValueName = "HittingAttackID";
    [SerializeField] private int hittingAttacksCount = 3;
    [SerializeField] private string movementBlendValue_X_Name = "MovementX";
    [SerializeField] private string movementBlendValue_Y_Name = "MovementY";
    [SerializeField] private string fallingTransitionName = "Falling";

    [SerializeField] private Vector2 idlePosition = new Vector2(0f, 0f);
    [SerializeField] private Vector2 runningPosition = new Vector2(0f, 2f);
    [SerializeField] private Vector2 walkingPosition = new Vector2(0f, 1f);
    [SerializeField] private Vector2 crouchingPosition = new Vector2(0f, 0.8f);
    [SerializeField] private Vector2 fallingPosition = new Vector2(1f, 1f);

    private Animator anim;
    private float attackBlockedTime = 0f;
    private Vector2 lastMovementPosition = Vector2.zero;
    private PlayerInputActions inputActions;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        inputActions = new PlayerInputActions();
        inputActions.Player.Attack.Enable();
    }
    private void Update()
    {
        if (inputActions.Player.Attack.IsPressed())
            Attack();

        AnimateMovement();

        DynamicCrosshair.Visible = true;
        DynamicCrosshair.Gap = crosshairTargetGap * attackRadius * crosshairTargetGap;
    }
    private void LateUpdate()
    {
        //animatorData.attackTrigger = false;
    }
    private void OnEnable()
    {
        attackBlockedTime = Time.time + awakeAttackBlockTime;
        CameraInfluence = inShootingCameraInfluence;

        DynamicCrosshair.Visible = true;
        DynamicCrosshair.Gap = crosshairTargetGap;

        PlayerUI.SetAmmoTextActive(false);
    }
    public override bool Attack()
    {
        if (Time.time < attackBlockedTime)
            return false;

        Vector3 _direction = CameraLooking.instance.CameraTransform.forward;
        bool _hitted = Physics.SphereCast(CameraLooking.instance.CameraTransform.position,
            attackRadius, _direction, out RaycastHit _hit, 
            attackMaxDistance, attackLayerMask);

        if (_hitted)
        {
            if(_hit.collider.gameObject.TryGetComponent(out HealthCollider _health))
            {
                StartCoroutine(AttackDamagingDelayEnumerator(_health, damage));
                if (_health.MainHealth.HealthPoints <= 0f || _health.MainHealth.HealthPoints < damage)
                {
                    if (_hit.collider.gameObject.TryGetComponent(out RagdollPart _rp))
                    {
                        Vector3 _force = _direction * ragdollHittingForce;
                        _rp.AddForce(_force, true);
                    }
                }
            }
            if (hittingAttacksCount > 0)
            {
                anim.SetInteger(hittingAttackIdValueName, Random.Range(0, hittingAttacksCount));
                anim.SetTrigger(hittingAttackTriggerName);
            }

            ObjectPool.SpawnHitEffect(_hit);
        }
        else
        {
            if (attacksCount > 0)
            {
                anim.SetInteger(attackIdValueName, Random.Range(0, attacksCount));
                anim.SetTrigger(attackTriggerName);
            }
        }
        PlayAttackClip();

        CallSuccessfulAttackEvent();
        animatorData.isHittingAttack = _hitted;

        attackBlockedTime = Time.time + attackTimeDelay;
        return true;
    }
    private IEnumerator AttackDamagingDelayEnumerator(HealthCollider _health, float _damage)
    {
        yield return new WaitForSeconds(damageDelay);
        if (_health.MainHealth.HealthPoints > 0)
            PlayerUI.ActivateHitMarker();
        _health.Damage(_damage);
    }
    private void AnimateMovement()
    {
        anim.SetBool(fallingTransitionName, false);

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
    private void PlayAttackClip()
    {
        if (Resources.attackClips.Length == 0)
            return;

        WeaponLocalAudioSource.PlayOneShot(Resources.attackClips[Random.Range(0, Resources.attackClips.Length)],
            Resources.GetAttackPitch());
    }

    public void PlayOpeningClip()
    {
        if (Resources.openingClips.Length > 0)
            WeaponLocalAudioSource.PlayOneShot(Resources.openingClips[Random.Range(0, Resources.openingClips.Length)]);
    }
}
