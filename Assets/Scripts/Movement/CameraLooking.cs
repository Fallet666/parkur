using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void ScreenResolutionEventHandler(ScreenResolution oldResolution, ScreenResolution newResolution);
[System.Serializable]
public struct ScreenResolution
{
    public int widht;
    public int height;

    public ScreenResolution(int _widht, int _height)
    {
        widht = _widht;
        height = _height;
    }
}
public class CameraLooking : MonoBehaviour
{
    public static CameraLooking instance;
    public Transform CameraTransform
    {
        get
        {
            if (cameraTr != null)
                return cameraTr;
            else 
                return transform;
        }
    }

    [SerializeField] private float sensivity = 1f;
    [Range(0f, 90f)] [SerializeField] private float maxVerticalAngle = 90f;
    [Range(0f, 90f)] [SerializeField] private float minVerticalAngle = 90f;
    [Range(0f, 360f)] [SerializeField] private float rightMaxHorizontalAngle = 360f;
    [Range(0f, 360f)] [SerializeField] private float leftMaxHoriontalAngle = 360f;


    [Header("Optional")]
    [SerializeField] private Transform body;
    [SerializeField] private bool singletone = false;
    /// <summary>
    /// This objects destroys if is not local player
    /// </summary>
    [SerializeField] private GameObject[] localObjects;
    [SerializeField] private ScopingCamera scopingCamera;
    [SerializeField] private Camera viewCamera;

    [Header("Apply if want to control head")]
    [SerializeField] private Walker walker;
    [SerializeField] private float verticalPositionOffset = 0.1f;

    public static Camera ViewCamera
    {
        get
        {
            if (instance != null)
                return instance.viewCamera;
            return null;
        }
    }
    public static Transform Body
    {
        get
        {
            if (instance != null && instance.body != null)
                return instance.body;
            return null;
        }
    }
    public static float FOV
    {
        get
        {
            if (ViewCamera != null)
                return ViewCamera.fieldOfView;
            return 0;
        }
        set
        {
            if (ViewCamera != null)
                ViewCamera.fieldOfView = value;
        }
    }
    
    public bool ShowCursor
    {
        get => cursorIsVisible;
        set
        {
            if (!IsOwner)
                return;

            Cursor.visible = value;
            Cursor.lockState = CursorLockMode.None;
            if (!value)
                Cursor.lockState = CursorLockMode.Locked;
            cursorIsVisible = value;
        }
    }
    public bool LockRotation { get; set; }
    public bool IsOwner { 
        get => isOwner; 
        set
        {
            isOwner = value;
            if (value)
            {
                ShowCursor = false;
                ScopingCamera.SetSingletone(scopingCamera);
            }
            else
            {
                foreach (GameObject _obj in localObjects)
                    Destroy(_obj);
            }
        }
    }
    public Vector3 Rotation { get; set; }

    public Vector2 MouseDelta { get => mouseDelta; }

    private Vector2 mouseDelta = Vector2.zero;
    private bool isOwner = false;
    private bool cursorIsVisible = false;
    private Transform tr;
    private Transform walkerTr;
    private PlayerInputActions inputActions;
    private Transform cameraTr;

    private void Awake()
    {
        if (instance != null && singletone)
            instance = this;
        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();
        tr = transform;
        if (walker != null)
            walkerTr = walker.transform;
        cameraTr = viewCamera.transform;
    }
    private void OnEnable()
    {
        Rotation = transform.localEulerAngles;
        if (body != null)
            Rotation = new Vector3(Rotation.x, body.localEulerAngles.y, Rotation.z);

        if (!IsOwner)
            return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private void OnDisable()
    {
        if (!IsOwner)
            return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    private void Update()
    {
        if (walker != null)
        {
            tr.position = walkerTr.position + walker.Center + walkerTr.up * (walker.ColliderHeight / 2 - verticalPositionOffset);
        }
     
        if (IsOwner)
        {
            if (LockRotation)
                return;

            mouseDelta = inputActions.Player.LookingDelta.ReadValue<Vector2>();
            mouseDelta *= sensivity * 0.1f;

            Rotation += new Vector3(-mouseDelta.y, mouseDelta.x, 0f);
        }

        Rotation = LoopEulers(Rotation);
        Rotation = ClampEulers(Rotation);

        if (body == null)
            tr.localEulerAngles = Rotation;
        else
        {
            tr.localEulerAngles = new Vector3(Rotation.x, tr.localEulerAngles.y, Rotation.z);
            body.localEulerAngles = new Vector3(body.localEulerAngles.x, Rotation.y, body.localEulerAngles.z);
        }
    }
    private Vector3 ClampEulers(Vector3 eulers)
    {
        return new Vector3
        {
            x = Mathf.Clamp(eulers.x, -maxVerticalAngle, minVerticalAngle),
            y = Mathf.Clamp(eulers.y, -leftMaxHoriontalAngle, rightMaxHorizontalAngle),
            z = eulers.z
        };
    }
    private Vector3 LoopEulers(Vector3 eulers)
    {
        return new Vector3
        {
            x = LoopMagnitude(eulers.x),
            y = LoopMagnitude(eulers.y),
            z = LoopMagnitude(eulers.z)
        };
    }
    private float LoopMagnitude(float value)
    {
        if (value >= 360f)
            value -= 360f;
        else if (value <= -360f)
            value += 360f;
        return value;
    }

    public void SetTransformParent(Transform _parent)
    {
        tr.SetParent(_parent);
    }

    public void AddRotation(Vector2 _rotation)
    {
        Rotation += new Vector3(-_rotation.y, _rotation.x, 0f);
    }
}
