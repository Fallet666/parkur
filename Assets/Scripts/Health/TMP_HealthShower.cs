using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class TMP_HealthShower : MonoBehaviour
{
    [SerializeField] private TMP_Text tmp_textObject;
    [SerializeField] private Health health;

    private void OnEnable()
    {
        if (tmp_textObject == null || health == null)
            return;

        health.OnHealthChanged += Health_OnHealthChanged;
    }

    private void Health_OnHealthChanged(float oldValue, float newValue)
    {
        tmp_textObject.text = $"HP: {newValue}";
    }
}
