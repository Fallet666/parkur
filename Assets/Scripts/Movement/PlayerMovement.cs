using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public enum MovementEnvironment
{
    onGround,
    inAir,
    ladder,
    water
}
public delegate void PlayerMovementEventHandler(PlayerMovementData _data);
[System.Serializable]
public struct PlayerMovementData
{
    public MovementEnvironment lastMovementEnviroment;
    public MovementEnvironment newMovementEnvironment;

    public float distanceToGround;
    public bool isGrounded;
    public bool isRunning;
    public bool isCrouching;
    public float crouchStep;
    public Vector3 movement;
    public float currentSpeed;
}
[System.Serializable]
public struct PlayerMovementInputData
{
    public Vector2 movement;
    public bool crouching;
    public bool jumping;
    public bool running;
}


[RequireComponent(typeof(Walker))]
public class PlayerMovement : NetworkBehaviour
{
    public static PlayerMovement instance;

    #region parameters
    public Transform cameraTransform;


    [Header("In air and on ground movement")]
    [SerializeField] private float walkingSpeed = 2.75f;
    [SerializeField] private float forwardRunningSpeed = 6f;
    [SerializeField] private float runningSpeed = 4.5f;
    [SerializeField] private float flyingSpeed = 3f;

    [Header("Forces")]
    [SerializeField] private float jumpForce = 5f;

    [Header("Ladder movement")]
    [SerializeField] private float onLadderSpeed = 4f;
    [SerializeField] private float onLadderJumpForce = 3f;
    [SerializeField] private float onLadderJumpsDelay = 1f;
    [SerializeField] private float onLadderJumpingTime = 0.2f;

    [Header("Water movement")]
    [SerializeField] private float inWaterSpeed = 4f;
    [SerializeField] private float drowningSpeed = 1f;
    [SerializeField] private float dampingSwimmingInertionMultiplier = 1.5f;
    [Range(0f, 1f)] [SerializeField] private float savingInertionSwimmingPart = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float drowningStart = 0.7f;
    [SerializeField] private float delayBetweenAscents = 1f;

    [Header("Crouching")]
    [SerializeField] private float crouchingSpeed = 3f;
    [SerializeField] private float verticalCrouchingSpeed = 5f;
    [SerializeField] private float crouchingMaxHeight = 1.8f;
    [SerializeField] private float crouchingMinHeight = 1.2f;
    [SerializeField] private float crouchingMaxDistanceToGround = 0.4f;

    [Header("Optinal")]
    [SerializeField] private float smoothingInputMultiplier = 15f;
    [SerializeField] private float smooothingSpeedTransitionsMultiplier = 15f;
    [SerializeField] private float groundDetermineOffset = 0.2f;
    [SerializeField] private float detectingSpeedDelay = 0.3f;
    #endregion
    #region public values
    public bool isRunning { get; private set; }
    public bool isCrouching { get; private set; }
    public MovementEnvironment currentMovementEnvironment { get; private set; }
    public PlayerMovementData CurrentMovementData { get => currentData; }
    public Walker GetWalker { get => walker; }
    public float CrouchingMaxHeight { get => crouchingMaxHeight; }
    public float CrouchingMinHeight { get => crouchingMinHeight; }
    public static bool LockForwardRunning { 
        set 
        {
            if (instance != null)
                instance.lockForwardRunning = value;
        }
    }
    public static bool LockRunning
    {
        set
        {
            if (instance != null)
                instance.lockRunning = value;
        }
    }
    public static bool LockMovement
    {
        set
        {
            if (instance != null)
                instance.lockMovement = value;
        }
    }
    #endregion
    #region Local values
    private bool lockMovement = false;
    private bool lockForwardRunning = false;
    private bool lockRunning = false;
    public PlayerMovementData currentData;
    private PlayerInputActions inputActions;
    private Walker walker;
    private PlayerMovementInputData inputData = new PlayerMovementInputData();
    private Transform tr;
    private Vector3 inputDirection = Vector3.zero;
    private bool moveOnLadder = false;
    private bool moveInWater = false;
    private float delayJumpingOnLadder = 0f;
    private float addingJumpingVelocityOnLadderTime = 0f;
    private bool isJumpingOnLadder = false;
    private Vector3 jumpingOnLadderVelocity = Vector3.zero;
    private Vector3 swimmingVelocity = Vector3.zero;
    private float ascentAvailableDelay = 0f;
    private float savedSpeed = 0f;
    private Vector3 savedPosition = Vector3.zero;
    private float speedInWorld = 0f;
    private bool lockAllMovement = false;
    private float lockedTimeToCheckSpeed = 0f;
    private Vector3 notSmoothedInputDirection = Vector3.zero;

    #endregion
    #region SyncValues
    private NetworkVariable<Vector3> sync_movement = new NetworkVariable<Vector3>(
        Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> sync_running = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void UpdateSyncValues()
    {
        if (notSmoothedInputDirection != sync_movement.Value)
            sync_movement.Value = notSmoothedInputDirection;

        if (sync_running.Value != currentData.isRunning)
            sync_running.Value = currentData.isRunning;
    }

    private void OnMovementSynced(Vector3 oldValue, Vector3 newValue)
    {
        currentData.movement = newValue;
    }
    private void OnRunningSynced(bool oldValue, bool newValue)
    {
        currentData.isRunning = newValue;
    }

    #endregion
    #region Events
    public event PlayerMovementEventHandler OnWaterEnter;
    private void OnEnable()
    {
        OnWaterEnter += PlayerMovement_OnWaterEnter;
    }
    private void OnDisable()
    {
        OnWaterEnter -= PlayerMovement_OnWaterEnter;
    }
    private void PlayerMovement_OnWaterEnter(PlayerMovementData _data)
    {
        if(_data.lastMovementEnviroment == MovementEnvironment.inAir)
            ascentAvailableDelay = Time.time + delayBetweenAscents;
    }

    #endregion
    #region Base
    private void Awake()
    {
        walker = GetComponent<Walker>();
        tr = transform;
    }
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            if (IsOwner)
                instance = this;

            inputActions = new PlayerInputActions();
            inputActions.Player.Enable();
            inputActions.Player.Jump.started += Jump;
        }
        else
        {
            walker.LockMovement = true;

            sync_movement.OnValueChanged += OnMovementSynced;
            sync_running.OnValueChanged += OnRunningSynced;
        }
    }
    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
        {
            sync_movement.OnValueChanged -= OnMovementSynced;
            sync_running.OnValueChanged -= OnRunningSynced;
        }
    }
    private void Update()
    {
        Crouching();
    }
    private void FixedUpdate()
    {
        UpdateMovementData();

        if (lockAllMovement || !IsOwner)
            return;

        //calculating movement direction
        Vector3 inputDirection = GetInputDirection();
        if (lockMovement)
            inputDirection = Vector3.zero;
        Vector3 movementDirection = CalculateVectorOfMovement(inputDirection);
        Vector3 movementVector = movementDirection;

        //adding speed
        currentMovementEnvironment = GetCurrentMovementEnvironment();
        movementDirection *= GetCurrentSpeed(currentMovementEnvironment);

        //adding drowning
        Vector3 drowningVelocity = Vector3.zero;
        if(currentMovementEnvironment == MovementEnvironment.water && movementVector.magnitude < drowningStart)
        {
            drowningVelocity += -tr.up * drowningSpeed;
        }

        //applying movement to walker
        if (currentMovementEnvironment == MovementEnvironment.ladder)
        {
            Vector3 _jumpVelocity = Vector3.zero;
            if (isJumpingOnLadder)
            {
                if (Time.time < addingJumpingVelocityOnLadderTime)
                    _jumpVelocity = jumpingOnLadderVelocity;
                else
                    isJumpingOnLadder = false;
            }
            walker.MoveInExtraMode(movementDirection + _jumpVelocity);

            //resetting values
            swimmingVelocity = Vector3.zero;
        }
        else if(currentMovementEnvironment == MovementEnvironment.water)
        {
            //smoothing velocity
            if (swimmingVelocity != movementDirection && movementVector.magnitude < savingInertionSwimmingPart)
                swimmingVelocity = Vector3.MoveTowards(swimmingVelocity, movementDirection,
                    dampingSwimmingInertionMultiplier * Time.fixedDeltaTime);
            else
                swimmingVelocity = movementDirection;
            //applying velocity 
            walker.MoveInExtraMode(swimmingVelocity + drowningVelocity, true);
        }
        else
        {
            walker.MoveOnPlane(movementDirection);
            isJumpingOnLadder = false;

            //resetting values
            swimmingVelocity = Vector3.zero;
        }

        //resetting values
        moveOnLadder = false;
        moveInWater = false;
    }

    private void UpdateMovementData()
    {
        currentData.isGrounded = walker.DistanceToGround < groundDetermineOffset;
        currentData.distanceToGround = walker.DistanceToGround;
        currentData.isCrouching = isCrouching;
        currentData.crouchStep = 1f - ((walker.Height - crouchingMinHeight) / (crouchingMaxHeight - crouchingMinHeight));

        if (IsOwner)
        {
            currentData.isRunning = isRunning && !lockRunning;
            currentData.movement = inputDirection;
            UpdateSyncValues();
        }
        else
        {
            inputData.crouching = currentData.movement.y == -1f;
        }

        if (Time.time > lockedTimeToCheckSpeed)
        {
            float _speed = Vector3.Distance(savedPosition, tr.position) / 
               (Time.time - (lockedTimeToCheckSpeed - detectingSpeedDelay));
            currentData.currentSpeed = _speed;
            savedPosition = tr.position;

            lockedTimeToCheckSpeed = Time.time + detectingSpeedDelay;
        }
    }
    private Vector3 CalculateVectorOfMovement(Vector3 _inputDirection)
    {
        Vector3 _movement = new Vector3(_inputDirection.x, 0f, _inputDirection.z);

        if (moveOnLadder)
            _movement = new Vector3(_inputDirection.x, 0f, 0f);
        if(cameraTransform != null)
            _movement = cameraTransform.TransformDirection(_movement);

        if (moveOnLadder)
            _movement += tr.up * _inputDirection.z;

        if(currentMovementEnvironment == MovementEnvironment.water)
        {
            if (Time.time < ascentAvailableDelay && _inputDirection.y < 0)
                _movement += tr.up * _inputDirection.y;
            else if(Time.time >= ascentAvailableDelay)
                _movement += tr.up * _inputDirection.y;
        }

        _movement = Vector3.ClampMagnitude(_movement, 1f);
        float _len = _movement.magnitude;
        _movement.Normalize();

        return _movement * _len;
    }
    private Vector3 GetInputDirection()
    {
        inputData = GetInputData();

        Vector2 _input = inputData.movement;
        float _vertical = 0f;
        if (inputData.jumping)
            _vertical += 1;
        if (inputData.crouching)
            _vertical -= 1f;

        Vector3 _inputDirection = new Vector3(_input.x, _vertical, _input.y);
        notSmoothedInputDirection = _inputDirection;

        //smoothing
        if (_inputDirection != inputDirection)
            _inputDirection = Vector3.Lerp(inputDirection, _inputDirection, smoothingInputMultiplier * Time.fixedDeltaTime);
        inputDirection = _inputDirection;

        return _inputDirection;
    }
    private PlayerMovementInputData GetInputData()
    {
        PlayerMovementInputData _newData = new PlayerMovementInputData
        {
            movement = inputActions.Player.Movement.ReadValue<Vector2>(),
            crouching = inputActions.Player.Crouch.IsPressed(),
            jumping = inputActions.Player.Jump.IsPressed(),
            running = inputActions.Player.Run.IsPressed()
        };
        return _newData;
    }
    private void Crouching()
    {
        float _distance = walker.DistanceToRoof + walker.Height;
        bool isRoofeed = walker.DistanceToRoof <= 0f;
        float _currentHeight = walker.Height;

        float _targetHeight = Mathf.Clamp(crouchingMaxHeight, crouchingMinHeight, _distance);
        if (isRoofeed)
            _targetHeight = _currentHeight;


        if (inputData.crouching && walker.DistanceToGround < crouchingMaxDistanceToGround)
            _targetHeight = crouchingMinHeight;

        if (_currentHeight != _targetHeight)
        {
            _currentHeight = Mathf.MoveTowards(_currentHeight, _targetHeight, verticalCrouchingSpeed * Time.deltaTime);
            walker.Height = _currentHeight;
            walker.Center = tr.up * (walker.Height - crouchingMaxHeight) / 2;
            isCrouching = _currentHeight < crouchingMaxHeight - ((crouchingMaxHeight - crouchingMinHeight) / 2);
        }
    }
    private void Jump(InputAction.CallbackContext obj)
    {
        if (lockMovement)
            return;
        if (walker.CurrentState == WalkerState.onGround && !walker.IsSliding)
            walker.Jump(jumpForce);
        else if (moveOnLadder && Time.time > delayJumpingOnLadder)
        {
            Vector3 _direction = Vector3.zero;
            if(cameraTransform != null)
                _direction = cameraTransform.forward;
            float _len = _direction.magnitude;
            _direction = Vector3.ProjectOnPlane(_direction, tr.up).normalized * _len;
            jumpingOnLadderVelocity = _direction * onLadderJumpForce;

            delayJumpingOnLadder = Time.time + onLadderJumpsDelay;
            addingJumpingVelocityOnLadderTime = Time.time + onLadderJumpingTime;
            isJumpingOnLadder = true;
        }
    }
    private MovementEnvironment GetCurrentMovementEnvironment()
    {
        bool isGrounded = walker.DistanceToGround < groundDetermineOffset;
        if (moveOnLadder)
            return MovementEnvironment.ladder;
        else if (moveInWater)
        {
            if(currentMovementEnvironment != MovementEnvironment.water)
            {
                PlayerMovementData _data = new PlayerMovementData()
                {
                    lastMovementEnviroment = currentMovementEnvironment,
                    newMovementEnvironment = MovementEnvironment.water
                };
                OnWaterEnter?.Invoke(_data);
            }
            return MovementEnvironment.water;
        }
        else if (isGrounded)
            return MovementEnvironment.onGround;
        else
            return MovementEnvironment.inAir;
    }
    private float GetCurrentSpeed(MovementEnvironment _environment)
    {
        float _speed = savedSpeed;
        float _targetSpeed = 0f;
        isRunning = false;
        switch (_environment)
        {
            case MovementEnvironment.ladder:
                _targetSpeed = onLadderSpeed;
                break;
            case MovementEnvironment.water:
                _targetSpeed = inWaterSpeed;
                break;
            case MovementEnvironment.onGround:
                isRunning = inputData.running;
                if (isCrouching)
                    _targetSpeed = crouchingSpeed;
                else if (isRunning && !lockRunning)
                {
                    if (inputDirection.z > 0.9f && !lockForwardRunning)
                        _targetSpeed = forwardRunningSpeed;
                    else
                        _targetSpeed = runningSpeed;
                }
                else
                    _targetSpeed = walkingSpeed;
                break;
            case MovementEnvironment.inAir:
                _targetSpeed = flyingSpeed;
                break;
        }
        _speed = Mathf.MoveTowards(_speed, _targetSpeed, smooothingSpeedTransitionsMultiplier * Time.fixedDeltaTime);
        savedSpeed = _speed;
        return _speed;
    } 
    #endregion
    #region Public Functions
    public void MoveOnLadder()
    {
        moveOnLadder = true;
    }
    public void MoveInWater()
    {
        moveInWater = true;
    }
    public void SetInputData(PlayerMovementInputData _data)
    {
        inputData = _data;
    }
    public void SetPosition(Vector3 _position)
    {
        walker.SetPosition(_position);
    }
    public Vector3 GetPosition()
    {
        return tr.position;
    }
    public void DisableAllMovement(bool disableCameraRotations = false)
    {
        lockAllMovement = true;
        walker.LockMovement = true;

        if (disableCameraRotations && CameraLooking.instance != null)
        {
            CameraLooking.instance.LockRotation = true;
        }
    }
    public void EnableAllMovement(bool enableCameraRotations = false)
    {
        lockAllMovement = false;
        walker.LockMovement = false;

        if (enableCameraRotations && CameraLooking.instance != null)
        {
            CameraLooking.instance.LockRotation = false;
        }
    }

    public void SetHeight(float _height)
    {
        walker.Height = Mathf.Clamp(_height, crouchingMinHeight, crouchingMaxHeight);
        walker.Center = tr.up * (walker.Height - crouchingMaxHeight) / 2;
    }
    public void Teleport(Vector3 _position)
    {
        walker.mainRigidbody.MovePosition(_position);
    }
    public void Teleport(Vector3 _position, Quaternion _rotation)
    {
        walker.mainRigidbody.MovePosition(_position);
        walker.mainRigidbody.MoveRotation(_rotation);
    }
    #endregion
}
