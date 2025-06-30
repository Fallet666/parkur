using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TMP_InputField portInputField;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        ipInputField.text = "127.0.0.1";
        portInputField.text = "7777";

        hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
        });
        clientButton.onClick.AddListener(() =>
        {
            SetTransportSettings();
            NetworkManager.Singleton.StartClient();
        });
        quitButton.onClick.AddListener(() =>
        {
            Application.Quit();
        });
    }

    private void SetTransportSettings()
    {
        if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport transport)
        {
            transport.ConnectionData.Address = ipInputField.text;
            transport.ConnectionData.Port = ushort.Parse(portInputField.text);
        }
    }
}
