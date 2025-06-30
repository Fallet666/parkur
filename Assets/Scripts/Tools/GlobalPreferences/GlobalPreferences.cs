using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = " GlobalPreferences", menuName = "ScriptableObjects/GlobalPreferences")]
public class GlobalPreferences : ScriptableObject
{
    public bool openWeaponAfterTaking = true;
    public bool reloadAfterEndingAmmo = true;
    public bool enableHitMarkers = true;

    public static GlobalPreferences Preferences
    {
        get
        {
            return GlobalPreferencesBase.GetPreferences();
        }
    }
    public static void SetBooleanProperty(string propertyName, bool value)
    {
        GlobalPreferences _preferences = Preferences;
        if (_preferences == null)
            return;

        var _propertyField = _preferences.GetType().GetField(propertyName);
        if(_propertyField != null)
        {
            _propertyField.SetValue(_preferences, value);
        }
    }
    public static bool GetBooleanProperty(string propertyName, out bool isPropertyExists)
    {
        isPropertyExists = false;
        GlobalPreferences _preferences = Preferences;
        if (_preferences == null)
            return false;

        var _propertyField = _preferences.GetType().GetField(propertyName);
        if (_propertyField != null)
        {
            isPropertyExists = true;
            return (bool)_propertyField.GetValue(_preferences);
        }

        return false;
    }
}
