using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AnchorLoader : MonoBehaviour
{
    private OVRSpatialAnchor anchorPrefab;
    [SerializeField] private SpatialAnchorManager spatialAnchorManager;
    [SerializeField] TextMeshProUGUI logsText;
    string message = "";
    private List<Guid> numUuids = new List<Guid>();

    Action<bool, OVRSpatialAnchor.UnboundAnchor> _onAnchorLocalized;

    private void Awake()
    {
        //spatialAnchorManager = GetComponent<SpatialAnchorManager>();
        anchorPrefab = spatialAnchorManager.anchorPrefab;
        _onAnchorLocalized = OnLocalized;
    }
    public void LogMessage(string mess)
    {
        message = message + mess + "\n";
        logsText.text = message;
    }
    /// <summary>
    /// Load persistent anchors
    /// </summary>
    public void LoadAnchorsByUuid()
    {
        // Get number of saved anchor uuids
        try
        {
            if (!PlayerPrefs.HasKey(SpatialAnchorManager.NumUuidsPlayerPref))
            {
                Debug.Log("player !!!");
                //LogMessage("player !!!");
                PlayerPrefs.SetInt(SpatialAnchorManager.NumUuidsPlayerPref, 0);
            }

            var playerUuidCount = PlayerPrefs.GetInt(SpatialAnchorManager.NumUuidsPlayerPref);
            Log($"Attempting to load {playerUuidCount} saved anchors.");
            //LogMessage($"Attempting to load {playerUuidCount} saved anchors.");
            if (playerUuidCount == 0)
                return;

            var uuids = new Guid[playerUuidCount];
            for (int i = 0; i < playerUuidCount; ++i)
            {
                var uuidKey = "uuid" + i;
                var currentUuid = PlayerPrefs.GetString(uuidKey);
                Log("QueryAnchorByUuid: " + currentUuid);
                //LogMessage("QueryAnchorByUuid: " + currentUuid);

                uuids[i] = new Guid(currentUuid);
                numUuids.Add(uuids[i]);
            }

            Load(new OVRSpatialAnchor.LoadOptions
            {
                Timeout = 0,
                StorageLocation = OVRSpace.StorageLocation.Local,
                Uuids = uuids
            });
            LogMessage("TestMessage 1");

            if (PlayerPrefs.GetInt(SpatialAnchorManager.NumUuidsPlayerPref) == 3)
            {
                spatialAnchorManager.isLoadedAnchors = true;
            }
            else
            {
                LogMessage($"NumUuids = {PlayerPrefs.GetInt(SpatialAnchorManager.NumUuidsPlayerPref)}");
            }

        }
        catch (Exception e)
        {
            LogMessage("Error while loading anchors: " + e.Message);
            return;
        }
    }

    private void Load(OVRSpatialAnchor.LoadOptions options) => OVRSpatialAnchor.LoadUnboundAnchorsAsync(options)
        .ContinueWith(anchors =>
        {
            if (anchors == null)
            {
                Log("Query failed.");
                LogMessage("Query failed.");
                return;
            }

            foreach (var anchor in anchors)
            {
                if (anchor.Localized)
                {
                    _onAnchorLocalized(true, anchor);
                }
                else if (!anchor.Localizing)
                {
                    anchor.LocalizeAsync().ContinueWith(_onAnchorLocalized, anchor);
                }
            }
        });

    private void OnLocalized(bool success, OVRSpatialAnchor.UnboundAnchor unboundAnchor)
    {
        if (!success)
        {
            Log($"{unboundAnchor} Localization failed!");
            LogMessage($"{unboundAnchor} Localization failed!");
            return;
        }

        var pose = unboundAnchor.Pose;
        var spatialAnchor = Instantiate(anchorPrefab, pose.position, pose.rotation);
        unboundAnchor.BindTo(spatialAnchor);

        // We just loaded it, so we know it exists in persistent storage.
        var uuidd = spatialAnchor.GetComponentsInChildren<TextMeshProUGUI>()[0];
        uuidd.text = "UUID: " + spatialAnchor.Uuid.ToString();

        var savedStatusText = spatialAnchor.GetComponentsInChildren<TextMeshProUGUI>()[1];
        savedStatusText.text = " Loaded ";

        var numText = spatialAnchor.GetComponentsInChildren<TextMeshProUGUI>()[2];
        numText.text = numUuids.IndexOf(spatialAnchor.Uuid).ToString();

        PlayerPrefs.GetInt("" + spatialAnchor.Uuid.ToString());
        LogMessage($"Anchors added with uuid = {spatialAnchor.Uuid.ToString()}");
        DontDestroyOnLoad(spatialAnchor.gameObject);

        spatialAnchorManager.anchors.Add(spatialAnchor);
        spatialAnchorManager.GetLastAnchor();
        LogMessage($"Anchors number = {spatialAnchorManager.anchors.Count}.");

    }

    private static void Log(string message) => Debug.Log($"[SpatialAnchorsUnity]: {message}");
}
