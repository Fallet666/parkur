using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRenderSetter : MonoBehaviour
{
    public static PlayerRenderSetter instance { get; private set; }

    [SerializeField] private Camera mainRenderCamera;
    [SerializeField] private Camera lowResolutionRenderCamera;
    //[SerializeField] private 

    [SerializeField] private GameObject lowResolutionGameObject;
    [SerializeField] private RawImage lowResolutionRenderImageSlot;
    [SerializeField] private int rendersDowngrade = 5;

    private RenderTexture lowResolutionRenderTexture;
    public static bool LowResolutionEnabled 
    { 
        get => instance.lowResolutionEnabled; 
        set
        {
            if (instance == null)
                return;
            if (instance.lowResolutionEnabled == value)
                return;
            instance.lowResolutionEnabled = value;
            instance.SetupRender();
        } 
    }

    private ScreenResolution oldScreenResolution;
    private bool lowResolutionEnabled = false;

    private void Update()
    {
        if (Screen.height != oldScreenResolution.height || Screen.width != oldScreenResolution.widht)
            InitializeRender();
    }

    private void OnEnable()
    {
        if (mainRenderCamera == null || lowResolutionRenderCamera == null||
            lowResolutionRenderImageSlot == null )
        {
            enabled = false;
            return;
        }


        InitializeRender();    
    }

    private void InitializeRender()
    {
        lowResolutionRenderTexture = new RenderTexture(Screen.width / rendersDowngrade, Screen.height / rendersDowngrade, 8);
        lowResolutionRenderCamera.targetTexture = lowResolutionRenderTexture;
        lowResolutionRenderTexture.Create();

        lowResolutionRenderImageSlot.texture = lowResolutionRenderTexture;

        oldScreenResolution = new ScreenResolution(Screen.width, Screen.height);

        //SetupRender();
    }

    public void SetupRender()
    {
        if (lowResolutionEnabled)
        {
            mainRenderCamera.enabled = false;
            lowResolutionRenderCamera.enabled = true;
            lowResolutionRenderImageSlot.enabled = true;
        }
        {
            mainRenderCamera.enabled = true;
            lowResolutionRenderCamera.enabled = false;
            lowResolutionRenderImageSlot.enabled = false;
        }
    }

    public static void SetSingletone(PlayerRenderSetter playerRenderSetter)
    {
        instance = playerRenderSetter;
    }
}
