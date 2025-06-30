using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class OfflineObjects : MonoBehaviour
{
    public void Start()
    {
        NetworkManager.Singleton.OnClientStarted += Singleton_OnClientStarted;
        NetworkManager.Singleton.OnClientStopped += Singleton_OnClientStopped;
    }
    private void Singleton_OnClientStopped(bool stopObj)
    {
        gameObject.SetActive(true);
    }

    private void Singleton_OnClientStarted()
    {
        gameObject.SetActive(false);
    }
}
