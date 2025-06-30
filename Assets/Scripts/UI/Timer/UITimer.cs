using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
public class UITimer : MonoBehaviour
{
    [SerializeField] private string prefix;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private bool serverTime = false;
    public float TargetTime { get; set; }

    private void OnEnable()
    {
        if (timerText == null)
            enabled = false;
    }
    private void Update()
    {
        float currentTime = Time.time;
        if (serverTime)
            currentTime = NetworkManager.Singleton.ServerTime.TimeAsFloat;
        timerText.text = prefix + $"{Mathf.RoundToInt(TargetTime - currentTime)}";
    }
}
