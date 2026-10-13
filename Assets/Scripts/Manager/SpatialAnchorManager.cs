using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpatialAnchorManager : MonoBehaviour
{
    public OVRSpatialAnchor anchorPrefab;
    public GameObject previewAnchorPrefab;
    public GameObject currentPreview;

    public const string NumUuidsPlayerPref = "numUuid";
    public List<OVRSpatialAnchor> anchors = new List<OVRSpatialAnchor>();
    public bool unsaveAll = false;
    public bool loadOnAwake = false;
    public bool destroyAndSaveAll = false;
    //public bool loadOnly = false;
    public bool isLoadedAnchors = false;

    [SerializeField]
    private int sceneToLoad = 1;
    //[SerializeField] SceneGroupSO homeScene;

    private Canvas canvas;
    private TextMeshProUGUI uuidText;
    private TextMeshProUGUI SavedStatusText;
    private TextMeshProUGUI numberText;

    private OVRSpatialAnchor lastCreatedAnchor;
    private AnchorLoader anchorLoader;
    [SerializeField] private TextMeshProUGUI anchorsNumberText;
    private bool isBoolCheck;
    // Test Debug
    [SerializeField] List<Transform> anchorsPos = new List<Transform>();

    private void Awake()
    {
        anchorLoader = GetComponent<AnchorLoader>();
        if (loadOnAwake)
        {
            LoadSavedAnchors();
        }

        anchorsNumberText.text = "Number of registered anchors: " + anchors.Count;
    }

    // Update is called once per frame
    void Update()
    {

        if (OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch) && OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.LTouch) && anchors.Count >= 3)
            {
                if (!destroyAndSaveAll)
                    DestroyLastCreatedAnchor();
                else
                    DestroyAllCreatedAnchor();
            }
            PreviewAndPlacementOnScene();

            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {

                if (!destroyAndSaveAll)
                {
                    SaveLastCreatedAnchor();
                }
                else
                {
                    SaveCreatedAnchor();
                }
        }

            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
            {
                if (!destroyAndSaveAll)
                    DestroyLastCreatedAnchor();
                else
                    DestroyAllCreatedAnchor();
                anchorsNumberText.text = $"Number of registered anchors: {anchors.Count}";
            }

            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch) && anchors.Count >= 3 || isLoadedAnchors && anchors.Count >= 3)
            {
            anchorsNumberText.text = $"Anchors loaded, environment can be started";
            
        }
    }
    private void PreviewAndPlacementOnScene()
    {
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            CreateSpatialAnchor(OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch), OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch));
        }
    }

    public void CreateSpatialAnchor(Vector3 position, Quaternion rotation)
    {
        OVRSpatialAnchor workingAnchor = Instantiate(anchorPrefab, position, rotation);
        
        canvas = workingAnchor.gameObject.GetComponentInChildren<Canvas>();
        uuidText = canvas.gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        SavedStatusText = canvas.gameObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        numberText = canvas.gameObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>();

        DontDestroyOnLoad(workingAnchor.gameObject);

        StartCoroutine(AnchorCreated(workingAnchor));

    }

    private IEnumerator AnchorCreated(OVRSpatialAnchor workingAnchor)
    {
        while (!workingAnchor.Created || !workingAnchor.Localized)
        {
            yield return new WaitForEndOfFrame();
        }
        Guid anchorGuid = workingAnchor.Uuid;
        anchors.Add(workingAnchor);
        anchorsNumberText.text = $"Number of registered anchors: {anchors.Count}";
        lastCreatedAnchor = workingAnchor;

        uuidText.text = "UUID : " + anchorGuid.ToString();
        SavedStatusText.text = "not saved";
        numberText.text = anchors.Count.ToString();
    }
    private void SaveLastCreatedAnchor()
    {
        var anchorToSave = lastCreatedAnchor;
        var statusText = SavedStatusText; // capturer la ref actuelle

        anchorToSave.SaveAnchorAsync().ContinueWith((success) =>
        {
            if (!success) return;
            statusText.text = "saved";
            SaveUuidToPlayerPrefs(anchorToSave.Uuid);
        });
    }

    private void SaveCreatedAnchor()
    {
        ClearAllUuidsFromPlayerPrefs(); // éviter l'accumulation entre sessions

        anchors.ForEach(anchor =>
        {
            anchor.SaveAnchorAsync().ContinueWith((success) =>
            {
                if (!success) return;
                SaveUuidToPlayerPrefs(anchor.Uuid);
                canvas = anchor.gameObject.GetComponentInChildren<Canvas>();
                uuidText = canvas.gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                SavedStatusText = canvas.gameObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                numberText = canvas.gameObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
                SavedStatusText.text = "saved";
                
            });
        });

    }

    private void SaveUuidToPlayerPrefs(Guid uuid)
    {

        if (PlayerPrefs.HasKey(NumUuidsPlayerPref) == false)
        {
            Debug.Log("player !!!");
            PlayerPrefs.SetInt(NumUuidsPlayerPref, 0);
        }

        int playerNumUuids = PlayerPrefs.GetInt(NumUuidsPlayerPref);
        Debug.Log("player ap: " + playerNumUuids);
        PlayerPrefs.SetString("uuid" + playerNumUuids, uuid.ToString());
        PlayerPrefs.SetInt(NumUuidsPlayerPref, ++playerNumUuids);

    }

    private void UnsaveLastCreatedAnchor()
    {
        lastCreatedAnchor.EraseAnchorAsync().ContinueWith((success) =>
        {
            Debug.Log("anchor unsaved : " + success);
            if (success)
            {
                SavedStatusText.text = "Not saved";
            }
            int playerNumUuids = PlayerPrefs.GetInt(NumUuidsPlayerPref);
            PlayerPrefs.DeleteKey("uuid" + playerNumUuids);
            PlayerPrefs.SetInt(NumUuidsPlayerPref, playerNumUuids--);
        });
    }

    private void UnsaveAllCreatedAnchor()
    {
        foreach (var anchor in anchors)
        {
            UnsaveAnchor(anchor);
        }
        Debug.Log("Anchor unsaved");
        anchors.Clear();
        ClearAllUuidsFromPlayerPrefs();
    }

    private void DestroyLastCreatedAnchor()
    {
        GameObject workingAnchor = lastCreatedAnchor.gameObject;
        anchors.Remove(lastCreatedAnchor);
        lastCreatedAnchor.EraseAnchorAsync().ContinueWith((success) => Destroy(workingAnchor));
        SavedStatusText.text = "destroyed";


        if (anchors.Count == 0)
            return;

        GetLastAnchor();
    }

    private void DestroyAllCreatedAnchor()
    {
        foreach (var anchor in anchors)
        {
            GameObject workingAnchor = anchor.gameObject;
            canvas = anchor.gameObject.GetComponentInChildren<Canvas>();
            SavedStatusText = canvas.gameObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            SavedStatusText.text = "destroyed";

            anchor.EraseAnchorAsync().ContinueWith((success) =>
            {
                if (success)
                {
                    Destroy(workingAnchor);
                    Debug.Log("SUCCEED TO UNSAVED ANCHOR");
                }
                else
                    Debug.Log("FAILED TO UNSAVED ANCHOR");
            });
        }
        anchors.Clear();
        ClearAllUuidsFromPlayerPrefs();
        isLoadedAnchors = false;
    }

    private void UnsaveAnchor(OVRSpatialAnchor anchor)
    {
        anchor.EraseAnchorAsync().ContinueWith((success) =>
        {
            var textComponents = anchor.GetComponentsInChildren<TextMeshPro>();
            if (textComponents.Length > 1)
            {
                textComponents[1].text = "not saved";
            }

        });

    }

    private void ClearAllUuidsFromPlayerPrefs()
    {
        if (PlayerPrefs.HasKey(NumUuidsPlayerPref))
        {
            int playerNumUuids = PlayerPrefs.GetInt(NumUuidsPlayerPref);
            for (int i = 0; i < playerNumUuids; i++)
            {
                PlayerPrefs.DeleteKey("uuid" + i);
            }
            PlayerPrefs.DeleteKey(NumUuidsPlayerPref);
            PlayerPrefs.Save();
        }
    }

    public void LoadSavedAnchors()
    {
        anchorLoader.LoadAnchorsByUuid();

    }

    public void GetLastAnchor()
    {
        lastCreatedAnchor = anchors.Last();
        canvas = lastCreatedAnchor.gameObject.GetComponentInChildren<Canvas>();
        uuidText = canvas.gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        SavedStatusText = canvas.gameObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
    }
}
