using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ScopingCamera : MonoBehaviour
{
    public static ScopingCamera instance;

    public event ScreenResolutionEventHandler OnResolutionChanged;
    public event VoidEventHandler OnWorkingStateChanged;


    /// <summary>
    /// This resolution will use while screen height is 1024
    /// </summary>
    [SerializeField] private ScreenResolution defaultResolution = new ScreenResolution(512, 512);
    [SerializeField] private ScreenResolution defaultLowResolution = new ScreenResolution(32, 32);

    private RenderTexture outputRenderTexture;
    private RenderTexture lowResolutionTexture;
    private Camera cam;
    private ScreenResolution savedResolution;

    public bool UseLowResolution = true;

    public bool Working
    {
        get => working;
        set
        {
            if (value == working)
                return;

            if (!UseLowResolution)
            {
                cam.targetTexture = outputRenderTexture;
                cam.enabled = value;
            }
            else
            {
                if (value)
                    cam.targetTexture = outputRenderTexture;
                else
                    cam.targetTexture = lowResolutionTexture;
                cam.enabled = true;
            }

            working = value;
            OnWorkingStateChanged?.Invoke();
        }
    }
    public bool working = false;

    private void OnEnable()
    {
        cam = GetComponent<Camera>();
        ChangeRenderTextureResolution();

        Working = true;
        Working = false;
    }
    private void Update()
    {
        if (savedResolution.height != Screen.height || savedResolution.widht != Screen.width)
        {
            ChangeRenderTextureResolution();
            OnResolutionChanged?.Invoke(savedResolution, new ScreenResolution(Screen.width, Screen.height));
        }
    }

    private void ChangeRenderTextureResolution()
    {
        savedResolution = new ScreenResolution(Screen.width, Screen.height);

        float _difference = (float)Screen.height / 1024f;
        int _height = (int)(defaultResolution.height * _difference);
        int _widht = (int)(defaultResolution.widht * _difference);

        outputRenderTexture = new RenderTexture(_widht, _height, 8);
        outputRenderTexture.format = RenderTextureFormat.DefaultHDR;
        outputRenderTexture.Create();
        cam.targetTexture = outputRenderTexture;

        _height = (int)(defaultLowResolution.height * _difference);
        _widht = (int)(defaultLowResolution.widht * _difference);
        lowResolutionTexture = new RenderTexture(_widht, _height, 8);
        lowResolutionTexture.format = RenderTextureFormat.DefaultHDR;
        lowResolutionTexture.Create();
    }
    private void InitializeRenderTexture(int height, int widht)
    {
        //outputRenderTexture = new RenderTexture(widht, height, );

    }

    public static void SetSingletone(ScopingCamera _scopingCamera)
    {
        instance = _scopingCamera;
    }

    public RenderTexture GetScopeView()
    {
        if(!UseLowResolution)
            return outputRenderTexture;
        else
        {
            if (working)
                return outputRenderTexture;
            else
                return lowResolutionTexture;
        }
    }

    public void SetViewAngle(float fov)
    {
        cam.fieldOfView = fov;
    }
}
