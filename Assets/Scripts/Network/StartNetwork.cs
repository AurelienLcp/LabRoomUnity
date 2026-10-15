using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartNetwork : MonoBehaviour
{
    private static bool serverStarted = false;// Indique si le serveur est déjà démarré
    [SerializeField] public string ipAddress;// Adresse IP pour la connexion réseau
    [SerializeField] private TextMeshProUGUI serverIpAddressText; // Champ pour afficher l'adresse IP locale (VR)
    private UnityTransport transport; // Référence au composant de transport Unity
    public List<GameObject> dropdowns = new List<GameObject>();

    private void Awake()
    {
        //Setup IP address using csv file
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var networkSettings = NetworkSettings.Instance;
        ipAddress = networkSettings.IpAddress;
#if UNITY_STANDALONE_WIN //|| UNITY_EDITOR
            //experienceManager.RemovePanelAndAvatar();
            StartServer();
#else
        /*if(SceneManager.GetActiveScene().name == "XRRoomUpdate") */
        StartCoroutine(WaitForStartClient());
        #endif
    }

    public void StartServer()
    {
        if (!serverStarted)
        {
            Debug.Log("Starting server...");

            try
            {
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport != null)
                {
                    ushort uniquePort = 7777; // Utiliser un port fixe pour la simplicité
                    transport.SetConnectionData(ipAddress, uniquePort, ipAddress);
                    Debug.Log($"Server listening on IP: {ipAddress}, Port: {uniquePort}");

                    if (serverIpAddressText != null)
                    {
                        serverIpAddressText.text = ipAddress;
                    }
                }

                NetworkManager.Singleton.StartServer();
                serverStarted = true;
                Debug.Log("Server started successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to start server: {e.Message}");
            }
        }
        else
        {
            Debug.Log("Server is already running.");
        }
    }
    public void StartClient()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("Cannot start Client while an instance is already running");
            return;
        }

        SetIpAddress();
        // Start client
        NetworkManager.Singleton.StartClient();
        Debug.Log("Client started with IP: " + ipAddress);
    }

    public void SetIpAddress()
    {
        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.ConnectionData.Address = ipAddress; // Define IP address for connection
        Debug.Log("Set IP Address: " + ipAddress);
    }

    public IEnumerator WaitForStartClient()
    {
        //yield on a new YieldInstruction that waits for 6 seconds.
        yield return new WaitForSeconds(15);
        StartClient();
    }
}
