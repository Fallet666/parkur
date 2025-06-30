using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyDelay : MonoBehaviour
{
    [SerializeField] private bool startTimerOnAwake = true;
    [SerializeField] private float delay = 60f;

    private void Awake()
    {
        if (startTimerOnAwake)
            StartTimer();
    }
    public void StartTimer()
    {
        Invoke("DestroyMe", delay);
    }
    private void DestroyMe()
    {
        Destroy(gameObject);
    }
}
