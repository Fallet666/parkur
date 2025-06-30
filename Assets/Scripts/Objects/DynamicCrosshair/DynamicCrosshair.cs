using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DynamicCrosshair : MonoBehaviour
{
    public static DynamicCrosshair instance;
    public static void SetSingletone(DynamicCrosshair _crosshair)
    {
        instance = _crosshair;
    }

    public static float Gap
    {
        set
        {
            if (instance == null || value == instance.currentGap)
                return;
            instance.currentGap = value;
            instance.crosshairBase.sizeDelta = instance.startSize * (1 + value * instance.scaleFactor);
        }
    }
    public static bool Visible
    {
        set
        {
            if (instance == null || instance.isVisible == value)
                return;
            instance.isVisible = value;
            instance.crosshairBase.gameObject.SetActive(value);
        }
    }

    [SerializeField] private RectTransform crosshairBase;
    [SerializeField] private float scaleFactor = 1f;

    private Vector2 startSize = Vector2.zero;
    private float currentGap = 0f;
    private bool isVisible = false;

    private void Awake()
    {
        if (crosshairBase == null)
        {
            enabled = false;
            return;
        }
        startSize = crosshairBase.sizeDelta;
    }


}
