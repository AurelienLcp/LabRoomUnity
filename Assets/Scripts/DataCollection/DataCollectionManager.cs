using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DataCollectionManager : NetworkBehaviour
{

    [Header("Avatar Management")]
    public List<GameObject> networkAvatar = new List<GameObject>();
    public List<ulong> AvatarNetIds = new List<ulong>();
    public List<ulong> AvatarUserIds = new List<ulong>();
    [SerializeField] public List<RenderTexture> avatarCameras;
    private NetworkVariable<int> userConnected = new NetworkVariable<int>();

    [Header("Psi Data Collection")]
    PsiPipelineManager psiPipelineManager;
    [SerializeField] private TextMeshProUGUI EndPointCount;
    public List<GameObject> remoteAvatarExporters = new List<GameObject>();
    public List<GameObject> networkedObjectsExporters = new List<GameObject>();
    //public List<PsiExporterObjectInteraction> avatarInteractionsExporters = new List<PsiExporterObjectInteraction>();
    public List<PsiExporterPiecesState> avatarInteractionsExporters = new List<PsiExporterPiecesState>();
    public List<PsiExporterGazeEvent> avatarEyeGazeExporters = new List<PsiExporterGazeEvent>();
    public List<PsiExporterString> avatarEyeGazeStringExporters = new List<PsiExporterString>();
    public int userID = -1;
    private string deviceid = "";
    [SerializeField] private GameObject referentialTransform;
    public bool _isServerSetup;
    private static DataCollectionManager _instance;
    public static DataCollectionManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<DataCollectionManager>();
            return _instance;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        psiPipelineManager = PsiPipelineManager.Instance;
        //StartPsiClient();
    }

    private void Start()
    {
        
    }

    private void StartPsiClient()
    {
        if (IsClient)
        {
            deviceid = SystemInfo.deviceUniqueIdentifier;
            AddUser();

            if (psiPipelineManager != null)
            {
                psiPipelineManager.StartPsi();
                psiPipelineManager.InitializeExporters();
                psiPipelineManager.AddProcess();
                psiPipelineManager.StartPipeline();
            }
        }
    }

    private void AddUser()
    {
        userID = (int)userConnected.Value + 1;

        NewUserConnectedRpc();

        if (psiPipelineManager != null)
        {
            psiPipelineManager.RendezVousAppName = $"Quest" + userID.ToString();

            if (userID == 1) psiPipelineManager.ExportersStartingPort = 12000;
            else if (userID == 2) psiPipelineManager.ExportersStartingPort = 13000;
            else if (userID == 3) psiPipelineManager.ExportersStartingPort = 14000;
        }
        Debug.Log("userID : " + userID);
    }

    [Rpc(SendTo.Server)]
    private void NewUserConnectedRpc()
    {
        userConnected.Value += 1;
    }

    public void CallToStartUnityServerOnPsi()
    {
        psiPipelineManager.RendezVousAppName = "UnityServer";

        var exporterNumber = (networkAvatar.Count  * 3) + networkedObjectsExporters.Count + avatarEyeGazeExporters.Count + avatarInteractionsExporters.Count;
        psiPipelineManager.StreamNumberExpectedAtStart = exporterNumber;
        UnityEngine.Debug.Log($"Expected Endpoint count is {exporterNumber}");

        psiPipelineManager.StartPsi();
        psiPipelineManager.InitializeExporters();
        psiPipelineManager.AddProcess();
        //EndPointCount.text = $"Endpoint count: {psiPipelineManager.GetEndpointsCount()}";
        UnityEngine.Debug.Log($"Endpoint count: {psiPipelineManager.GetEndpointsCount()}");
        psiPipelineManager.StartPipeline();
        _isServerSetup = true;
    }

    public void AddNetworkAvatarsExporters()
    {
        int i = 1;
        AddNetworkedObject();
        foreach (var element in networkAvatar)
        {
            if (element == null) return;

            var jointHead = element.transform.GetChild(1).gameObject;
            var jointLeftWristHand = element.transform.GetChild(2).gameObject;
            var jointRightWristHand = element.transform.GetChild(3).gameObject;
            jointHead.AddComponent<PsiExporterPositionOrientation>();
            jointHead.GetComponent<PsiExporterPositionOrientation>().TopicName = $"{i}-Head";
            jointHead.GetComponent<PsiExporterPositionOrientation>().ExportType = PsiPipelineManager.ExportType.TCPWriter;
            jointHead.GetComponent<PsiExporterPositionOrientation>().FrequencyMilliseconds = 33;
            jointLeftWristHand.AddComponent<PsiExporterPositionOrientation>();
            jointLeftWristHand.GetComponent<PsiExporterPositionOrientation>().TopicName = $"{i}-LeftWrist";
            jointLeftWristHand.GetComponent<PsiExporterPositionOrientation>().ExportType = PsiPipelineManager.ExportType.TCPWriter;
            jointLeftWristHand.GetComponent<PsiExporterPositionOrientation>().FrequencyMilliseconds = 33;
            jointRightWristHand.AddComponent<PsiExporterPositionOrientation>();
            jointRightWristHand.GetComponent<PsiExporterPositionOrientation>().TopicName = $"{i}-RightWrist";
            jointRightWristHand.GetComponent<PsiExporterPositionOrientation>().ExportType = PsiPipelineManager.ExportType.TCPWriter;
            jointRightWristHand.GetComponent<PsiExporterPositionOrientation>().FrequencyMilliseconds = 33;
            // Eyegaze exporters
            // AddInteractionExporters(i);
            AddEyeGazeExporter(i);
            i++;
            
        }
        //Debug.Log("Number of EndPoint added is : " + psiPipelineManager.GetEndpointsCount());
        //AddTaskEventExporters();
    }
    public void AddNetworkedObject()
    {
        var networkedObject = GameObject.FindGameObjectsWithTag("NetworkedObject");
        var snappableObject = GameObject.FindGameObjectsWithTag("SnappableObject");
        foreach (var item in networkedObject)
        {
            networkedObjectsExporters.Add(item);
        }
        foreach (var item in snappableObject)
        {
            networkedObjectsExporters.Add(item);
        }

        foreach (var element in networkedObjectsExporters)
        {
            if (element == null) return;
            var objectExporter = element.transform.gameObject;
            objectExporter.AddComponent<PsiExporterPositionOrientation>();
            objectExporter.GetComponent<PsiExporterPositionOrientation>().TopicName = $"{element.name}";
            objectExporter.GetComponent<PsiExporterPositionOrientation>().ExportType = PsiPipelineManager.ExportType.TCPWriter;
            objectExporter.GetComponent<PsiExporterPositionOrientation>().FrequencyMilliseconds = 50;
            UnityEngine.Debug.Log("Add PosRot exporter to " + element.name);
        }
    }
    public void AddEyeGazeExporter(int id)
    {
        var eyeGazeExporter = this.transform.gameObject.AddComponent<PsiExporterGazeEvent>();
        eyeGazeExporter.TopicName = $"{id}-GazeEvent";
        eyeGazeExporter.ExportType = PsiPipelineManager.ExportType.TCPWriter;
        avatarEyeGazeExporters.Add(eyeGazeExporter);

        /*var eyeGazeStringExporter = this.transform.gameObject.AddComponent<PsiExporterString>();
        eyeGazeStringExporter.TopicName = $"{id}-GazeEventString";
        eyeGazeStringExporter.ExportType = PsiPipelineManager.ExportType.TCPWriter;
        avatarEyeGazeStringExporters.Add(eyeGazeStringExporter);*/
    }

    public void AddInteractionExporters(int id)
    {
        var objectInteractionExporter = this.transform.gameObject.AddComponent<PsiExporterPiecesState>();
        objectInteractionExporter.TopicName = $"{id}-Interactions";
        objectInteractionExporter.ExportType = PsiPipelineManager.ExportType.TCPWriter;
        avatarInteractionsExporters.Add(objectInteractionExporter);
        /*var objectInteractionExporter = this.transform.gameObject.AddComponent<PsiExporterObjectInteraction>();
        objectInteractionExporter.TopicName = $"{id}-Interactions";
        objectInteractionExporter.ExportType = PsiPipelineManager.ExportType.TCPWriter;
        avatarInteractionsExporters.Add(objectInteractionExporter);*/
    }

    public void AddTaskEventExporters()
    {
        var taskExporter = this.transform.gameObject.AddComponent<PsiExporterString>();
        taskExporter.TopicName = "TaskEvent";
        taskExporter.ExportType = PsiPipelineManager.ExportType.TCPWriter;
    }

    public void CallAvatarSetupOnSelect()
    {
        networkAvatar[(int)NetworkManager.LocalClientId - 1].GetComponent<PimpMyAvatar>().PimpAvatarAfterSelection();
    }

    public void StartUnityServerPsi()
    {
        try
        {
            var networkSettings = NetworkSettings.Instance;
            psiPipelineManager.RendezVousServerAddress = networkSettings.IpAddress;
            psiPipelineManager.HostAddress = networkSettings.IpAddress;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start server: {e.Message}");
        }
        var psiLogs = GameObject.Find("LogsPsiText");
        psiPipelineManager.TextLogObject = psiLogs;
        AddNetworkAvatarsExporters();
        StartCoroutine(WaitForSecond());
    }
    
    public IEnumerator WaitForSecond()
    {
        yield return new WaitForSeconds(2);
        CallToStartUnityServerOnPsi();
    }
}
