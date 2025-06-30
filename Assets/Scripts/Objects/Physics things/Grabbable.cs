using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void BoolEventHandler(bool value);
public enum GrabbingType
{
    keepTargetRotation,
    keepStartRotation,
    keepPresetRotation
}
public class Grabbable : MonoBehaviour
{
    public float LastTimeInteraction;

    public Rigidbody Rigidbody_ { get => rb; }
    public Transform Transform_ { get => tr; }
    public bool IsGrabbing
    {
        get => isGrabbing;
        private set
        {
            if(rb == null || tr == null)
            {
                if (TryGetComponent(out Rigidbody _rb))
                    rb = _rb;
                tr = transform;
                if (overrideRigibody != null)
                {
                    rb = overrideRigibody.Rb;
                    tr = overrideRigibody.transform;

                    overrideRigibody.onCollisionEnter += OnCollisionEnter;
                    overrideRigibody.onCollisionExit += OnCollisionExit;
                }
            }

            if (value == isGrabbing)
                return;
            isGrabbing = value;
            if (isGrabbing)
            {
                gameObject.layer = karryingLayer;
                rb.useGravity = false;
                rb.mass = startMass * 0.1f;

                if (childTarget != null)
                    Destroy(childTarget.gameObject);

                if (grabbingType == GrabbingType.keepStartRotation || grabbingType == GrabbingType.keepPresetRotation)
                {
                    GameObject obj = new GameObject();
                    childTarget = obj.transform;
                    if (grabbingType != GrabbingType.keepPresetRotation)
                        childTarget.rotation = transform.rotation;
                    childTarget.parent = target;
                    if (grabbingType == GrabbingType.keepPresetRotation)
                        childTarget.localEulerAngles = presetRotation;

                    if (ignoreVerticalRotation)
                    {
                        childTarget.parent = null;
                        if (childTargetParent != null)
                            childTarget.parent = childTargetParent;
                        startAngleDifference = childTarget.eulerAngles.y - target.eulerAngles.y;
                    }
                }
            }
            else
            {
                if (childTarget != null)
                    Destroy(childTarget.gameObject);
                rb.useGravity = true;
                rb.mass = startMass;
                gameObject.layer = defaultLayer;
                childTargetParent = null;
            }
        }
    }


    [Header("Preferences")]
    [SerializeField] private float velocityMP = 15f;
    [SerializeField] private float angularVelocityMP = 15f;
    [SerializeField] private GrabbingType grabbingType = GrabbingType.keepStartRotation;
    [SerializeField] private Vector3 presetRotation = new Vector3();
    [SerializeField] private bool ignoreVerticalRotation = true;

    [Header("Not necessarily")]
    [SerializeField] private OverrideRigidbody overrideRigibody;
    [SerializeField] private int defaultLayer = 0;
    [SerializeField] private int karryingLayer = 7;

    //local
    private Rigidbody rb;
    private Transform tr;
    private bool isGrabbing;
    private Transform target;
    private Transform childTargetParent;
    private Transform childTarget;
    private float lastTimeGrabbing;
    [SerializeField] private bool collisionWithOther;
    private float startMass;
    private float startAngleDifference;

    private void Awake()
    {
        LastTimeInteraction = -100f;
        if(TryGetComponent(out Rigidbody _rb))
            rb = _rb;
        tr = transform;
        if (overrideRigibody != null)
        {
            rb = overrideRigibody.GetRigidbody();
            tr = overrideRigibody.transform;

            overrideRigibody.onCollisionEnter += OnCollisionEnter;
            overrideRigibody.onCollisionExit += OnCollisionExit;
        }

        startMass = rb.mass;
    }

    //local functions
    private void Update()
    {
        Moving();
    }
    private void Moving()
    {
        if (!isGrabbing)
            return;

        if (Time.time > lastTimeGrabbing + 0.1f || target == null)
        {
            IsGrabbing = false;
            return;
        }

        Vector3 direction = target.position - tr.position;
        rb.velocity = direction * velocityMP;
        LastTimeInteraction = Time.time;

        if (!collisionWithOther)
        {
            Quaternion rTarget = new Quaternion();
            if (grabbingType == GrabbingType.keepTargetRotation)
            {
                if (!ignoreVerticalRotation)
                    rTarget = Quaternion.LookRotation(target.forward);
                else
                    rTarget = Quaternion.LookRotation(Vector3.ProjectOnPlane(target.forward, Vector3.up));
            }
            else
            {
                if (ignoreVerticalRotation)
                {
                    Vector3 rot = new Vector3();
                    rot.y = startAngleDifference;
                    if(childTarget.parent == null)
                        rot.y += target.eulerAngles.y;
                    if (grabbingType == GrabbingType.keepStartRotation)
                    {
                        rot.x = childTarget.localEulerAngles.x;
                        rot.z = childTarget.localEulerAngles.z;
                    }
                    else
                    {
                        rot.x = presetRotation.x;
                        rot.z = presetRotation.z;
                    }
                    childTarget.localEulerAngles = rot;
                }
                rTarget = childTarget.rotation;
            }
            rb.MoveRotation(Quaternion.Lerp(tr.rotation, rTarget, angularVelocityMP * Time.deltaTime));
            rb.angularVelocity = Vector3.zero;
        }
    }
    private void OnDisable()
    {
        if (childTarget != null)
            Destroy(childTarget.gameObject);
    }
    //public functions
    public void MoveTo(Transform _target, Transform _playerBody)
    {
        childTargetParent = _playerBody;
        MoveTo(_target);
    }
    public void MoveTo(Transform _target)
    {
        if (_target == null)
            return;
        lastTimeGrabbing = Time.time;
        target = _target;
        IsGrabbing = true;
        LastTimeInteraction = Time.time;
    }
    public void Throw(Vector3 velocity, Vector3 angularVelocity)
    {
        IsGrabbing = false;

        rb.velocity += velocity;
        rb.angularVelocity += angularVelocity;
        LastTimeInteraction = Time.time;
    }
    public void SetVelocity(Vector3 velocity)
    {
        rb.velocity = velocity;
        LastTimeInteraction = Time.time;
    }
    public  void AddVelocity(Vector3 velocity)
    {
        rb.velocity += velocity;
        LastTimeInteraction = Time.time;
    }

    private void OnCollisionEnter(Collision collision)
    {
        collisionWithOther = true;
    }
    private void OnCollisionExit(Collision collision)
    {
        collisionWithOther = false;
    }
}
