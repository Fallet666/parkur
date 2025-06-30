using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ScopingViewSetter : MonoBehaviour
{
    [SerializeField] private bool useLowResolutionRenderTextureWhileNotActive = true;

    [SerializeField] private int materialId = 1;

    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material scopingMaterial;

    private Renderer renderer;
    private void OnEnable()
    {
        renderer = GetComponent<Renderer>();

        if(ScopingCamera.instance != null)
        {
            ScopingCamera.instance.OnResolutionChanged += ScopingCamera_OnResolutionChanged;
            ScopingCamera.instance.OnWorkingStateChanged += ChangeMaterial;
            ScopingCamera.instance.UseLowResolution = useLowResolutionRenderTextureWhileNotActive;

            if (ScopingCamera.instance.GetScopeView() != null)
            {
                scopingMaterial.mainTexture = ScopingCamera.instance.GetScopeView();
                if(useLowResolutionRenderTextureWhileNotActive)
                    defaultMaterial.mainTexture = ScopingCamera.instance.GetScopeView();
            }
        }
    }
    private void OnDisable()
    {
        if (ScopingCamera.instance != null)
        {
            ScopingCamera.instance.OnResolutionChanged -= ScopingCamera_OnResolutionChanged;
            ScopingCamera.instance.OnWorkingStateChanged -= ChangeMaterial;
            ScopingCamera.instance.UseLowResolution = false;
            ScopingCamera.instance.Working = false;
        }
    }
    private void ScopingCamera_OnResolutionChanged(ScreenResolution oldResolution, ScreenResolution newResolution)
    {
        scopingMaterial.mainTexture = ScopingCamera.instance.GetScopeView();
        if(useLowResolutionRenderTextureWhileNotActive)
            defaultMaterial.mainTexture = ScopingCamera.instance.GetScopeView();
    }
    private void ChangeMaterial()
    {
        if (ScopingCamera.instance == null || materialId >= renderer.sharedMaterials.Length)
            return;

        Material _targetMaterial = defaultMaterial;
        if (ScopingCamera.instance.Working)
            _targetMaterial = scopingMaterial;

        var mats = renderer.sharedMaterials;
        mats[materialId] = _targetMaterial;

        renderer.sharedMaterials = mats;

        if(useLowResolutionRenderTextureWhileNotActive)
            _targetMaterial.mainTexture = ScopingCamera.instance.GetScopeView();

    }

}
