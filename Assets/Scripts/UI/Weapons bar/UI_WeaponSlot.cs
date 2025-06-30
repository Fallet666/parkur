using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CanvasGroup))]
public class UI_WeaponSlot : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private Animator anim;
    [SerializeField] private Image weaponIconSlot;

    [SerializeField] private string selectionBoolValueName = "Selection";

    private bool selectedState = false;
    public Weapon ContainedWeapon { get; set; }

    private void Awake()
    {
        anim = GetComponent<Animator>();
        canvasGroup = GetComponent<CanvasGroup>();
    }
    private void OnEnable()
    {
        GetComponent<Animator>().SetBool(selectionBoolValueName, selectedState);
        GetComponent<CanvasGroup>().alpha = 0f;
    }
    public void SetSelected(bool _selectedState)
    {
        selectedState = _selectedState;
        anim.SetBool(selectionBoolValueName, selectedState);
    }
    private IEnumerator FadeTransitionEnumerator(CanvasGroup group, float targetAlpha, float time)
    {
        float starteedAlpha = group.alpha;
        float startedTime = Time.time;
        float t = 0f;

        while (t < 1f)
        {
            t = (Time.time - startedTime) / time;
            group.alpha = Mathf.Lerp(starteedAlpha, targetAlpha, t);
            yield return null;
        }
    }
    public void SetVisible(bool activeState, float transitionTime = 0.2f)
    {
        StopAllCoroutines();

        float target = 0f;
        if (activeState) target = 1f;

        StartCoroutine(FadeTransitionEnumerator(canvasGroup, target, transitionTime));
    }
    public void SetAplha(float alpha, float transitionTime = 0.2f)
    {
        StopAllCoroutines();
        StartCoroutine(FadeTransitionEnumerator(canvasGroup, alpha, transitionTime));
    }
    public void InitializeWeaponIcon(Weapon wep)
    {
        if (weaponIconSlot == null || wep == null)
        {
            Debug.LogWarning("components isn't selected");
            return;
        }

        weaponIconSlot.sprite = wep.Resources.ui_icon;
    }
}
