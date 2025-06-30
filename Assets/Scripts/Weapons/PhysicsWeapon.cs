using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PhysicsGun_WorkType
{
    off,
    gragging,
    karrying,
    lowKarrying
}
public enum GrabbableDropType
{
    standart,
    throwing
}
[RequireComponent(typeof(Animator))]
public class PhysicsWeapon : Weapon
{
    #region parameters
    [SerializeField] private float detectingDistance = 5f;
    [SerializeField] private float grabbingDistance = 2f;
    [SerializeField] private float detectingRadius = 0.2f;
    [SerializeField] private float distanceToHead = 2f;
    [SerializeField] private float distanceToKeepHolding = 3f;
    [SerializeField] private float draggingVelocityMultiplier = 5f;
    [SerializeField] private float draggingVelocityLimit = 3f;
    [SerializeField] private float draggingFollowPlayerVelocity = 1f;
    [SerializeField] private float throwingVelocity = 10f;
    [SerializeField] private float lowCarryingVelocity = 5f;
    [SerializeField] private float lowCarryingDistanceWeight = 0.5f;
    [SerializeField] private float draggingTime = 0.5f;
    [SerializeField] private float cooldownTime = 0.5f;
    [SerializeField] private float savingLastInteractableObjectTime = 0.5f;
    [SerializeField] private float maxRandomAngularForce = 5f;
    [SerializeField] [Range(0f, 1f)] private float minAngulareForcePercent = 0.7f;
    [SerializeField] private LayerMask layerMask;

    [Header("Animator parameters")]
    [SerializeField] private string draggingValueName = "Dragging";
    [SerializeField] private string karryingValueName = "Karrying";
    [SerializeField] private string lowKarryingValueName = "LowKarrying";
    [SerializeField] private string throwingTriggerName = "Throw";
    [SerializeField] private float defaultDropAnimationTime = 0.4f;
    [SerializeField] private float throwDropAnimationTime = 0.8f;
    #endregion

    #region local values
    public Grabbable currentInteractable;
    private Grabbable savedInteractable;
    private PlayerInputActions inputActions;
    private float savedDistance;
    private float savedTakingTame;
    private float draggingCanselledTime;
    public PhysicsGun_WorkType workType = PhysicsGun_WorkType.off;
    private PhysicsGun_WorkType savedWorkType = PhysicsGun_WorkType.off;
    private Transform target;
    private Animator anim;
    private float blockedToDetectTime;
    #endregion

    #region public values
    #endregion

    #region Events
    public event VoidEventHandler OnGrabbableDropped;
    public event VoidEventHandler OnGrabbableDroppedAndAnimationsEnded;
    #endregion

    private void OnEnable()
    {
        anim = GetComponent<Animator>();

        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();

        SubscribeInput();

        currentInteractable = null;
        if(target == null && CameraLooking.instance != null)
        {
            GameObject obj = new GameObject();
            obj.name = "Target for grabbable";
            target = obj.transform;
            target.transform.parent = CameraLooking.instance.CameraTransform;
            target.transform.localPosition = Vector3.zero + (CameraLooking.instance.CameraTransform.forward * distanceToHead);
            target.transform.localRotation = Quaternion.identity;
        }
        CameraInfluence = 1f;
    }

    private void OnDisable()
    {
        UnsubscribeInput();

        currentInteractable = null;
    }
    private void ReinitializeInputAction(PlayerInputActions newInputActions)
    {
        if (newInputActions == null || newInputActions == inputActions)
            return;

        UnsubscribeInput();

        inputActions = newInputActions;

        SubscribeInput();
    }
    private void SubscribeInput()
    {
        inputActions.Player.Attack.started += Attack_started;
        inputActions.Player.Attack.canceled += Attack_canceled;
        inputActions.Player.Use.started += Use_started;
        inputActions.Player.Use.canceled += Use_canceled;
    }
    private void UnsubscribeInput()
    {
        inputActions.Player.Attack.started -= Attack_started;
        inputActions.Player.Attack.canceled -= Attack_canceled;
        inputActions.Player.Use.started -= Use_started;
        inputActions.Player.Use.canceled -= Use_canceled;
    }
    private void FixedUpdate()
    {
        if (CameraLooking.instance == null)
            return;

        SetupAnimator();

        if(savedInteractable != null)
        {
            if(Time.time > draggingCanselledTime + savingLastInteractableObjectTime)
            {
                savedInteractable = null;
                return;
            }

            if (inputActions.Player.SecondAttack.IsPressed())
            {
                ThrowInteractable(savedInteractable);
                savedInteractable = null;
                return;
            }

        }
        if(currentInteractable != null)
        {
            float distance = Vector3.Distance(currentInteractable.Transform_.position, target.position);
            if (distance > distanceToKeepHolding)
            {
                RemoveInteractable();
                return;
            }

            if (workType == PhysicsGun_WorkType.karrying)
            {
                if(target != null && currentInteractable != null)
                {
                    currentInteractable.MoveTo(target, CameraLooking.Body);
                }
            }
            else if (workType == PhysicsGun_WorkType.lowKarrying)
            {
                if (target != null && currentInteractable != null)
                {
                    //if (inputActions.Player.Use.IsPressed())
                    //{
                    if (distance < grabbingDistance)
                    {
                        workType = PhysicsGun_WorkType.karrying;
                        return;
                    }

                    Vector3 targetPoint = CameraLooking.instance.CameraTransform.position;
                    targetPoint += CameraLooking.instance.CameraTransform.forward * savedDistance;
                    Vector3 _direction = targetPoint - currentInteractable.Transform_.position;
                    _direction = Vector3.ClampMagnitude(_direction, draggingVelocityLimit);
                    _direction += (target.position - currentInteractable.Transform_.position).normalized * draggingFollowPlayerVelocity;
                    _direction *= draggingVelocityMultiplier * 0.01f;

                    Vector3 direction = (target.position - currentInteractable.Transform_.position).normalized * lowCarryingVelocity;
                    currentInteractable.AddVelocity(direction + _direction);
                    //}
                }
            }
            else if(workType == PhysicsGun_WorkType.gragging)
            {
                if (Time.time > savedTakingTame + draggingTime)
                {
                    RemoveInteractable();
                    return;
                }

                Vector3 targetPoint = CameraLooking.instance.CameraTransform.position;
                targetPoint += CameraLooking.instance.CameraTransform.forward * savedDistance;
                Vector3 direction = targetPoint - currentInteractable.Transform_.position;
                direction = Vector3.ClampMagnitude(direction, draggingVelocityLimit);
                direction += (target.position - currentInteractable.Transform_.position).normalized * draggingFollowPlayerVelocity;
                direction *= draggingVelocityMultiplier;
                currentInteractable.SetVelocity(direction);
            }
        }

    }

    #region Input Events
    private void Use_started(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (workType == PhysicsGun_WorkType.karrying)
        {
            RemoveInteractable();
        }
        else
        {

            currentInteractable = DetectInteractable(detectingDistance);
            if (currentInteractable != null)
            {
                if (Vector3.Distance(target.position, currentInteractable.Transform_.position) > grabbingDistance)
                {
                    workType = PhysicsGun_WorkType.lowKarrying;
                }
                else
                {
                    savedTakingTame = Time.time;
                    workType = PhysicsGun_WorkType.karrying;
                }
            }
        }
    }

    private void Use_canceled(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if(currentInteractable == null)
        {
            workType = PhysicsGun_WorkType.off;
            return;
        }

        if(workType == PhysicsGun_WorkType.lowKarrying)
        {
            if(Vector3.Distance(target.position, currentInteractable.Transform_.position) <= grabbingDistance)
            {
                workType = PhysicsGun_WorkType.karrying;
            }
            else
            {
                RemoveInteractable();
            }
        }
    }

    private void Attack_canceled(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if(currentInteractable != null && workType == PhysicsGun_WorkType.gragging)
        {
            savedInteractable = currentInteractable;
            draggingCanselledTime = Time.time;
        }
        RemoveInteractable();
    }

    private void Attack_started(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if(workType == PhysicsGun_WorkType.karrying)
        {
            ThrowInteractable(currentInteractable);
            RemoveInteractable(GrabbableDropType.throwing);
            draggingCanselledTime = Time.time;
            return;
        }

        if (Time.time < draggingCanselledTime + cooldownTime || workType == PhysicsGun_WorkType.karrying)
            return;

        currentInteractable = DetectInteractable(detectingDistance);
        if(currentInteractable != null)
        {
            savedTakingTame = Time.time;
            workType = PhysicsGun_WorkType.gragging;
        }
    }
    #endregion
    private void ThrowInteractable(Grabbable interactable)
    {
        if (interactable == null || CameraLooking.instance == null)
            return;


        Ray ray = new Ray(CameraLooking.instance.CameraTransform.position, CameraLooking.instance.CameraTransform.forward);
        bool raycast = Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, layerMask);

        Vector3 direction = CameraLooking.instance.CameraTransform.forward;
        Vector3 angularVelocity = direction * Random.Range(minAngulareForcePercent, 1f) * Mathf.Sign(Random.Range(-1f, 1f)) * maxRandomAngularForce;
        //if (raycast)
        //{
        //    direction = (hit.point - interactable.Transform_.position).normalized;
        //}

        interactable.Throw(direction * throwingVelocity, angularVelocity);

        anim.SetTrigger(throwingTriggerName);

    }
    private Grabbable DetectInteractable(float distance)
    {
        if (CameraLooking.instance == null || Time.time < blockedToDetectTime)
        {
            return null;
        }
        Ray ray = new Ray(CameraLooking.instance.CameraTransform.position, CameraLooking.instance.CameraTransform.forward);
        bool raycast = Physics.SphereCast(ray, detectingRadius, out RaycastHit hit, distance, layerMask);

        if (raycast && hit.collider.TryGetComponent(out Grabbable interactable))
        {
            savedDistance = Vector3.Distance(ray.origin, hit.transform.position);
            return interactable;
        }
        return null;
    }
    private void RemoveInteractable(GrabbableDropType dropType = GrabbableDropType.standart)
    {
        if (currentInteractable != null)
            OnGrabbableDropped?.Invoke();
        currentInteractable = null;
        workType = PhysicsGun_WorkType.off;

        float delayTime = 0f;
        switch (dropType)
        {
            case GrabbableDropType.standart:
                delayTime = defaultDropAnimationTime;
                break;
            case GrabbableDropType.throwing:
                delayTime = throwDropAnimationTime;
                break;
        }

        blockedToDetectTime = Time.time + delayTime;
        Invoke("CallGrabbableDroppedAnAnimationEndedEvent", delayTime);
    }

    private void SetupAnimator()
    {
        if (savedWorkType == workType)
            return;

        anim.SetBool(draggingValueName, workType == PhysicsGun_WorkType.gragging);
        anim.SetBool(karryingValueName, workType == PhysicsGun_WorkType.karrying);
        anim.SetBool(lowKarryingValueName, workType == PhysicsGun_WorkType.lowKarrying);

        savedWorkType = workType;
    }
    private void CallGrabbableDroppedAnAnimationEndedEvent()
    {
        OnGrabbableDroppedAndAnimationsEnded?.Invoke();
    }
    public bool CheckGrabbableObject()
    {
        return DetectInteractable(detectingDistance) != null;
    }
    public void TryGrab(PlayerInputActions currentInputActions = null)
    {
        ReinitializeInputAction(currentInputActions);

        Use_started(default);
    }
    public void StopGrab()
    {
        Use_canceled(default);
    }
}
