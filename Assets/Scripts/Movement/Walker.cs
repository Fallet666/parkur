using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum WalkerState
{
    onGround, 
    inAir,
    extra
}
public struct WalkerData
{
    public Vector3 velocity;
    public WalkerState oldState;
    public WalkerState newState;
}
public delegate void WalkerDataEventHandler(WalkerData _data);

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class Walker : MonoBehaviour
{
    #region parameters
    //Events
    public event WalkerDataEventHandler OnGroundContactEnter;
    public event WalkerDataEventHandler OnGroundContactExit;
    public event WalkerDataEventHandler OnStartSliding;
    public event WalkerDataEventHandler OnEndSliding;
    public event WalkerDataEventHandler OnExtraModeEnter;
    public event WalkerDataEventHandler OnExtraModeExit;
    public event WalkerDataEventHandler OnContactWithRoof;


    //parameters
    [Header("Main")]
    [SerializeField] private float height = 2f;
    [SerializeField] private float radius = 0.5f;
    [SerializeField] private float maxObstacleHeight = 0.4f;
    [Range(0f, 90f)] public float slopeLimit = 60f;

    [Header("Rotation on ground")]
    [SerializeField] private float lerpingRotationMultiplier = 5f;
    [SerializeField] private bool followTargetRotation = true;

    [Header("Optional")]
    [SerializeField] private float dampingAirMomentumMultiplier = 2f;
    [SerializeField] private float dampingSlidingAfterFallMomentumMultiplier = 20f;
    [SerializeField] private float dampingAngleSlidingMomentumMultiplier = 20f;
    [SerializeField] private float dampingMovementMomentumWhenSlidingMultiplier = 5f;
    [SerializeField] private float dampingSavedMomentumInExtraModeMultiplier = 10f;
    [SerializeField] private float groundAdjustmentSpeed = 20f;
    [SerializeField] private float adjustmingDelayWhenJump = 0.3f;
    [SerializeField] private float groundDeterminationOffset = 0.05f;
    [SerializeField] private LayerMask detectingGroundAndRoofLayerMask;
    #endregion

    #region Public values
    public WalkerState CurrentState { get => state; }
    public bool IsSliding { get => isSliding; }
    public float DistanceToGround { get => distanceToGround; }
    public float DistanceToRoof { get => distanceToRoof; }
    public float Height { get => height; set { height = value; } }
    public float Radius { get => radius; set { radius = value; } }
    public Vector3 Center { get => center; set { center = value; } }
    public float ColliderHeight { get => collider.height; }
    public bool LockMovement 
    {   
        get => lockMovement; 
        set 
        {
            lockMovement = value;
            rb.isKinematic = value;
        } 
    }
    public SurfaceResources CurrentSurface { get; private set; }
    public Rigidbody mainRigidbody { get => rb; }
    #endregion

    #region Local values
    private bool lockMovement = false;
    private WalkerState state = WalkerState.inAir;
    private Vector3 physicsMomentum = Vector3.zero;
    private Vector3 groundAdjustmentVelocity = Vector3.zero;
    private float distanceToGround = 0f;
    private Vector3 groundCollisionNormal = Vector3.zero;
    private Vector3 groundFlatCollisionNormal = Vector3.zero;
    private Vector3 onGroundMovementVelocity = Vector3.zero;
    private Vector3 airMovementVelocity = Vector3.zero;
    private Vector3 extraMovementVelocity = Vector3.zero;
    private Vector3 savedExtraMomentum = Vector3.zero;
    private Vector3 savedAfterEnterExtraMomentum = Vector3.zero;
    private bool applyDownGroundAdjustming = false;
    private float exitDistanceToGround = 0f;
    private float groundDownAdjustmingTimeDelay = 0f;
    private float distanceToRoof = 0f;
    private bool usingExtraMode = false;
    private bool adjustming = true;
    private bool isSliding = false;
    private Transform tr;
    private Rigidbody rb;
    private CapsuleCollider collider;
    private Vector3 center = Vector3.zero;
    private GameObject lastDetectedObject;
    private float availableForFollowGroundNormalTime = 0f;
    private Vector3 targetRotationNormal = Vector3.up;
    private Vector3 savedNormal = Vector3.up;
    #endregion

    #region Base functions
    private void Awake()
    {
        tr = transform;
        rb = GetComponent<Rigidbody>();
        collider = GetComponent<CapsuleCollider>();
        rb.freezeRotation = true;
    }
    private void Start()
    {
        rb.isKinematic = LockMovement;
    }
    private void FixedUpdate()
    {
        if (LockMovement)
        {
            UpdateColliderParameters();
            distanceToGround = GetDistanceToGround();
            distanceToRoof = GetDistanceToRoof();
            return;
        }

        distanceToGround = GetDistanceToGround();
        distanceToRoof = GetDistanceToRoof();
        state = GetCurrentState();
        UpdateColliderParameters();

        if (state == WalkerState.extra)
        {
            applyDownGroundAdjustming = false;
            groundAdjustmentVelocity = CalculateGroundAdjustmentVelocity();

            savedExtraMomentum = extraMovementVelocity;
            rb.velocity = groundAdjustmentVelocity + savedExtraMomentum + savedAfterEnterExtraMomentum;

            //damping
            if (savedAfterEnterExtraMomentum.sqrMagnitude > 0)
                savedAfterEnterExtraMomentum = Vector3.MoveTowards(savedAfterEnterExtraMomentum,
                    Vector3.zero, dampingSavedMomentumInExtraModeMultiplier * Time.fixedDeltaTime);
            //resetting values
            extraMovementVelocity = Vector3.zero;
            physicsMomentum = Vector3.zero;
            onGroundMovementVelocity = Vector3.zero;
            airMovementVelocity = Vector3.zero;
        }
        else
        {
            groundAdjustmentVelocity = CalculateGroundAdjustmentVelocity();
            Vector3 _movementVelocity = CalculateMovementVelocity();
            CalculatePhysicsMomentum();
            if (isSliding)
            {
                physicsMomentum = Vector3.ProjectOnPlane(physicsMomentum, groundCollisionNormal);
                _movementVelocity = Vector3.ProjectOnPlane(_movementVelocity, groundCollisionNormal);
            }
            rb.velocity = physicsMomentum + groundAdjustmentVelocity + _movementVelocity;
            if (state == WalkerState.onGround)
            {
                onGroundMovementVelocity = Vector3.zero;
                extraMovementVelocity = Vector3.zero;
            }
            else
                airMovementVelocity = Vector3.zero;
        }
        usingExtraMode = false;
    }
    private void Update()
    {

        FollowTargetRotation();
    }

    private Vector3 CalculateMovementVelocity()
    {
        Vector3 _velocity = onGroundMovementVelocity + airMovementVelocity;
        if (state == WalkerState.inAir)
        {
            if (onGroundMovementVelocity.sqrMagnitude > 0)
                onGroundMovementVelocity = Vector3.MoveTowards(onGroundMovementVelocity, Vector3.zero,
                dampingAirMomentumMultiplier * Time.fixedDeltaTime);
        }
        else
        {
            if (airMovementVelocity.sqrMagnitude > 0)
                airMovementVelocity = Vector3.MoveTowards(airMovementVelocity, Vector3.zero,
                dampingSlidingAfterFallMomentumMultiplier * Time.fixedDeltaTime);
        }
        float _airMagnitude = airMovementVelocity.magnitude;
        float _maxMagnitude = Mathf.Max(onGroundMovementVelocity.magnitude, _airMagnitude);
        _velocity = Vector3.ClampMagnitude(_velocity, _maxMagnitude);
        return _velocity;
    }
    private void CalculatePhysicsMomentum()
    {
        if (state == WalkerState.onGround && !isSliding)
        {
            if (Time.time > groundDownAdjustmingTimeDelay)
                physicsMomentum = Vector3.MoveTowards(physicsMomentum, Vector3.zero,
                dampingAngleSlidingMomentumMultiplier * Time.fixedDeltaTime);
        }
        else if (state != WalkerState.extra)
        {
            physicsMomentum += -tr.up * Physics.gravity.magnitude * Time.fixedDeltaTime;
        }
    }
    private WalkerState GetCurrentState()
    {
        WalkerState _savedState = state;

        WalkerState _state = WalkerState.inAir;
        if (distanceToGround <= groundDeterminationOffset)
                _state = WalkerState.onGround;

        isSliding = CheckSliding(_state);

        if (usingExtraMode)
            _state = WalkerState.extra;

        if (_savedState != _state)
        {
            WalkerData _data = new WalkerData
            {
                velocity = rb.velocity,
                oldState = state,
                newState = _state
            };
            if (_state == WalkerState.onGround)
                OnGroundContactEnter?.Invoke(_data);
            else if(state == WalkerState.onGround)
                OnGroundContactExit?.Invoke(_data);
            if (_state == WalkerState.extra)
                OnExtraModeEnter?.Invoke(_data);
            else if (state == WalkerState.extra)
                OnExtraModeExit?.Invoke(_data);
        }

        return _state;
    }
    private float GetDistanceToGround()
    {
        float dist = float.MaxValue;
        Vector3 startPoint = tr.position + Center;
        Vector3 direction = -tr.up;
        Ray ray = new Ray(startPoint, direction);
        float _radius = radius - 0.01f;

        if (Physics.SphereCast(ray, _radius, out RaycastHit hit, float.MaxValue,detectingGroundAndRoofLayerMask))
        {

            dist = hit.distance + _radius - height / 2;
            Debug.DrawRay(startPoint + direction * height / 2, direction * dist, Color.red);
            groundCollisionNormal = hit.normal;

            applyDownGroundAdjustming = Physics.Raycast(startPoint, direction, out RaycastHit _flatHit, height / 2 + maxObstacleHeight, detectingGroundAndRoofLayerMask);
            if (applyDownGroundAdjustming)
            {
                groundFlatCollisionNormal = _flatHit.normal;
            }

            if (exitDistanceToGround > maxObstacleHeight || Time.time < groundDownAdjustmingTimeDelay)
                applyDownGroundAdjustming = false;

            if(hit.collider.gameObject != lastDetectedObject)
            {
                if (hit.collider.gameObject.TryGetComponent(out Surface _surface))
                    CurrentSurface = _surface.Resources;
                else
                    CurrentSurface = null;
            }
            lastDetectedObject = hit.collider.gameObject;
        }
        else
        {
            groundCollisionNormal = tr.up;
            applyDownGroundAdjustming = false;
            CurrentSurface = null;
        }
        return dist;
    }
    private bool CheckSliding(WalkerState _currentState)
    {
        float _angle = Vector3.Angle(tr.up, groundCollisionNormal);
        bool _res = _angle > slopeLimit && _currentState == WalkerState.onGround;
        if (_res)
        {
            if(Physics.Raycast(tr.position, -tr.up, out RaycastHit _hit,(height/2) + maxObstacleHeight))
            {
                _angle = Vector3.Angle(tr.up, _hit.normal);
                if (_angle <= slopeLimit)
                    _res = false;
            }
        }

        if (_res != isSliding)
        {
            WalkerData _data = new WalkerData
            {
                velocity = rb.velocity,
                oldState = state,
                newState = _currentState
            };
            if (_res)
                OnStartSliding?.Invoke(_data);
            else
                OnEndSliding?.Invoke(_data);
        }

        return _res;
    }
    private void UpdateColliderParameters()
    {
        float new_height = height - maxObstacleHeight;
        Vector3 _center = new Vector3(0, maxObstacleHeight / 2, 0);

        collider.height = new_height;
        collider.center = _center + center;
    }
    private Vector3 CalculateGroundAdjustmentVelocity()
    {
        Vector3 _velocity = Vector3.zero;

        if (state == WalkerState.inAir)
            exitDistanceToGround = Mathf.Max(exitDistanceToGround, distanceToGround);

        if (!adjustming)
            return _velocity;

        if (state != WalkerState.inAir && distanceToGround < 0f)
        {
            _velocity = tr.up * -distanceToGround * groundAdjustmentSpeed;
            return _velocity;
        }
        else if (applyDownGroundAdjustming && distanceToGround > 0f)
        {
            _velocity = tr.up * -distanceToGround * groundAdjustmentSpeed;
            return _velocity;
        }
        return _velocity;
    }
    private float GetDistanceToRoof()
    {
        float _savedDistance = distanceToRoof;
        float _dist = float.MaxValue;
        float _radius = radius - 0.01f;
        if (Physics.SphereCast(tr.position + Center, _radius, tr.up, out RaycastHit _hit, float.MaxValue, detectingGroundAndRoofLayerMask))
            _dist = _hit.distance + _radius - height / 2;

        float _upPointHeight = 0.1f;
        if (_dist < _upPointHeight && _savedDistance >= _upPointHeight)
            OnContactWithRoof?.Invoke(new WalkerData());
        return _dist;
    }
    private void FollowTargetRotation()
    {
        if (!followTargetRotation)
            return;

        if(Time.time < availableForFollowGroundNormalTime)
        {
            Vector3 startPoint = tr.position + Center;
            Vector3 direction = -tr.up;
            if (state == WalkerState.onGround && Physics.Raycast(startPoint, direction, out RaycastHit _flatHit, height / 2 + maxObstacleHeight, detectingGroundAndRoofLayerMask))
            {
                if (_flatHit.collider.tag == "RotationGround")
                    savedNormal = _flatHit.normal;
                Quaternion _rot = Quaternion.FromToRotation(tr.up, savedNormal);
                rb.MoveRotation(Quaternion.Lerp(tr.rotation, _rot * tr.rotation, Time.deltaTime * lerpingRotationMultiplier));
            }
        }
        else
        {
            savedNormal = targetRotationNormal;
            Quaternion _rot = Quaternion.FromToRotation(tr.up, targetRotationNormal);
            rb.MoveRotation(Quaternion.Lerp(tr.rotation, _rot * tr.rotation, Time.deltaTime * lerpingRotationMultiplier));
        }


        //followGroudNormal = false;
    }
    #endregion

    #region Public Functions
    public void MoveOnPlane(Vector3 _movement)
    {
        float _len = _movement.magnitude;
        _movement = Vector3.ProjectOnPlane(_movement, transform.up).normalized * _len;
        if (state == WalkerState.onGround)
        {
            Vector3 _planeNormal = groundCollisionNormal;
            onGroundMovementVelocity += Vector3.ProjectOnPlane(_movement, _planeNormal).normalized * _len;
        }
        else
        {
            airMovementVelocity += _movement;
        }
    }
    public void MoveInExtraMode(Vector3 _movement, bool saveVelocity = false)
    {
        usingExtraMode = true;
        extraMovementVelocity += _movement;

        if (!saveVelocity)
            savedAfterEnterExtraMomentum = Vector3.zero;
    }
    public void Jump(float _force)
    {
        physicsMomentum += tr.up * _force;
        groundDownAdjustmingTimeDelay = Time.time + adjustmingDelayWhenJump;
    }
    public void SetPosition(Vector3 _position)
    {
        rb.MovePosition(_position);
    }

    public void SetTargetRotation(Vector3 _normal)
    {
        targetRotationNormal = _normal;
    }
    public void ResetTargetRotation()
    {
        targetRotationNormal = Vector3.up;
    }
    public void FollowGroundNormal()
    {
        availableForFollowGroundNormalTime = Time.time + 0.5f;
    }
    public void DontFollowGroundNormal()
    {
        availableForFollowGroundNormalTime = 0f;
    }
    #endregion

    #region Events
    private void OnEnable()
    {
        OnGroundContactEnter += OnGroundEnter;
        OnGroundContactExit += OnGroundExit;
        OnStartSliding += Walker_OnStartSliding;
        OnEndSliding += Walker_OnEndSliding;
        OnExtraModeEnter += Walker_OnExtraModeEnter;
        OnExtraModeExit += Walker_OnExtraModeExit;
        OnContactWithRoof += Walker_OnContactWithRoof;
    }
    private void OnDisable()
    {
        OnGroundContactEnter -= OnGroundEnter;
        OnGroundContactExit -= OnGroundExit;
        OnStartSliding -= Walker_OnStartSliding;
        OnEndSliding -= Walker_OnEndSliding;
        OnExtraModeEnter -= Walker_OnExtraModeEnter;
        OnExtraModeExit -= Walker_OnExtraModeExit;
        OnContactWithRoof -= Walker_OnContactWithRoof;
    }
    private void OnGroundEnter(WalkerData _data)
    {
        if (!isSliding)
            physicsMomentum = Vector3.zero;
        airMovementVelocity = Vector3.ProjectOnPlane(rb.velocity, tr.up);
        exitDistanceToGround = 0f;
    }
    private void OnGroundExit(WalkerData _data)
    {
        exitDistanceToGround = distanceToGround;
    }
    private void Walker_OnEndSliding(WalkerData _data)
    {
        groundDownAdjustmingTimeDelay = Time.time + adjustmingDelayWhenJump;
    }
    private void Walker_OnStartSliding(WalkerData _data)
    {

    }
    private void Walker_OnExtraModeExit(WalkerData _data)
    {
        onGroundMovementVelocity += savedExtraMomentum;
        extraMovementVelocity = Vector3.zero;
    }
    private void Walker_OnExtraModeEnter(WalkerData _data)
    {
        savedAfterEnterExtraMomentum = physicsMomentum;
        savedAfterEnterExtraMomentum += onGroundMovementVelocity;
        savedAfterEnterExtraMomentum += airMovementVelocity;
    }
    private void Walker_OnContactWithRoof(WalkerData _data)
    {
        physicsMomentum = VectorMathf.RemoveDotVector(physicsMomentum, tr.up);
    }
    #endregion
}
