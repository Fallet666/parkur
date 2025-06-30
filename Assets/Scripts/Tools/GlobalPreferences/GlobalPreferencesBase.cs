using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalPreferencesBase : MonoBehaviour
{
    public static GlobalPreferencesBase instance;
    [SerializeField] private GlobalPreferences preferences;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }
    public static GlobalPreferences GetPreferences()
    {
        return instance.preferences;
    }
}
