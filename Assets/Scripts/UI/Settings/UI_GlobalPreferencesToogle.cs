using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class UI_GlobalPreferencesToogle : MonoBehaviour
{
    public string propertyName;

    private Toggle toggle;
    private void Start()
    {
        toggle = GetComponent<Toggle>();

        bool property = GlobalPreferences.GetBooleanProperty(propertyName, out bool isExists);
        if (isExists)
        {
            toggle.isOn = property;
        }
        toggle.onValueChanged.AddListener(ChangeProperty);
    }

    private void ChangeProperty(bool value)
    {
        GlobalPreferences.SetBooleanProperty(propertyName, toggle.isOn);
    }
}
