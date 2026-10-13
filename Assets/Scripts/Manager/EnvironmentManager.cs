using Oculus.Avatar2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnvironmentManager : NetworkBehaviour
{
    [SerializeField] private Transform reperePos;
    [SerializeField] private Transform repereDir;
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private Transform repereAngle;
    [SerializeField] private TextMeshProUGUI logsText;

    private List<Transform> anchorsPos = new List<Transform>();
    private List<Transform> anchorsList = new List<Transform>();
    private List<GameObject> relocateObjectsList = new List<GameObject>();

    string message = "";
    bool start = true;

    // ── AJOUT : événements et état de stabilité ───────────────────────
    public event Action OnRepereStable;
    public event Action OnRepereStartedDrifting;
    public event Action OnRepereRestabilized;

    public enum RepereState { Localizing, Stable, Drifting }
    public RepereState State { get; private set; } = RepereState.Localizing;

    private Vector3 _lastReperePosition;
    private Quaternion _lastRepereRotation;
    private int _stableCount = 0;

    private const float STABILITY_POS = 0.001f;
    private const float STABILITY_ROT = 0.01f;
    private const float DRIFT_POS = 0.02f;
    private const float DRIFT_ROT = 1f;
    private const int STABLE_CALLS_REQUIRED = 4;
    // ─────────────────────────────────────────────────────────────────

    public void LogMessage(string mess)
    {
        message += mess + "\n";
        if (logsText != null)
            logsText.text = message;
    }
    private void Awake()
    {
        var anchors = FindObjectsByType<OVRSpatialAnchor>(FindObjectsSortMode.None);
        logsText = GameObject.Find("TextLogs").GetComponent<TextMeshProUGUI>();

        foreach (var anchor in anchors)
        {
            anchorsList.Add(anchor.transform);
            MeshRenderer[] mesh = anchor.GetComponentsInChildren<MeshRenderer>();
            foreach (var meshRenderer in mesh)
                meshRenderer.gameObject.SetActive(false);

            if (anchor.transform.GetChild(1).gameObject.activeSelf)
            {
                Canvas canvas = anchor.GetComponentInChildren<Canvas>();
                canvas.gameObject.SetActive(false);
            }
        }

        anchorsList.Sort((anchor1, anchor2) =>
        {
            int anchor1Num = int.Parse(anchor1.transform.GetChild(1).transform.GetChild(2)
                .GetComponent<TextMeshProUGUI>().text);
            int anchor2Num = int.Parse(anchor2.transform.GetChild(1).transform.GetChild(2)
                .GetComponent<TextMeshProUGUI>().text);
            return anchor1Num.CompareTo(anchor2Num);
        });

        if (anchorsList.Count > 0)
        {
            anchorsPos.Add(anchorsList[0]);
            anchorsPos.Add(anchorsList[1]);
        }

        RelocateWorld();
    }

    // ── AJOUT : fallback simulateur ───────────────────────────────────
    // Sans ancres, RelocateRepere retourne immédiatement et CheckStability
    // n'est jamais appelé → OnRepereStable ne se déclenche jamais.
    // OnNetworkSpawn est le bon endroit car IsClient est défini ici.
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsClient && anchorsList.Count == 0)
        {
            State = RepereState.Stable;
            StartCoroutine(TriggerStableNextFrame());
        }
    }

    private IEnumerator TriggerStableNextFrame()
    {
        yield return null;
        OnRepereStable?.Invoke();
    }
    // ─────────────────────────────────────────────────────────────────

    private void GetRelocateObjectsList()
    {
        Scene scene = SceneManager.GetActiveScene();
        scene.GetRootGameObjects(relocateObjectsList);
        relocateObjectsList.RemoveAll(gameObject =>
            gameObject.transform.tag == "repere" || gameObject.transform.tag == "Player");
    }

    private void RelocateWorld()
    {
        InvokeRepeating("RelocateRepere", 0, 1f);
    }

    private void AssigneGameObj()
    {
        foreach (var obj in relocateObjectsList)
        {
            NetworkObject netObj = null;
            try
            {
                netObj = obj.GetComponentOrNull<NetworkObject>();
                if (IsServer)
                    netObj.TrySetParent(reperePos);
            }
            catch { }
            if (obj.transform.parent != reperePos && netObj == null)
                obj.transform.SetParent(reperePos);
        }
    }

    private void RelocateRepere()
    {
        GetRelocateObjectsList();
        AssigneGameObj();

        if (anchorsPos.Count >= 2)
        {
            Vector3 newPosition = new Vector3(
                anchorsPos.Average(t => t.position.x),
                anchorsPos.Average(t => t.position.y),
                anchorsPos.Average(t => t.position.z));

            reperePos.position = new Vector3(newPosition.x, reperePos.position.y, newPosition.z);

            Vector3 vecteurRepere = reperePos.position - repereAngle.position;
            Vector3 vecteurAnchor = anchorsList[1].position - anchorsList[0].position;

            float newAngle = Vector3.Angle(vecteurRepere, vecteurAnchor);

            reperePos.localRotation = Quaternion.FromToRotation(vecteurRepere, vecteurAnchor) * reperePos.localRotation;
            reperePos.localEulerAngles = new Vector3(0, reperePos.localEulerAngles.y, 0);

            if (start)
            {
                start = false;
                float rAngle = Vector2.SignedAngle(
                    new Vector2(repereDir.position.x - anchorsList[0].position.x,
                                repereDir.position.z - anchorsList[0].position.z),
                    new Vector2(vecteurAnchor.x, vecteurAnchor.z));
                float aAngle = Vector2.SignedAngle(
                    new Vector2(anchorsList[2].position.x - anchorsList[0].position.x,
                                anchorsList[2].position.z - reperePos.position.z),
                    new Vector2(vecteurAnchor.x, vecteurAnchor.z));

                if (rAngle > 0 && aAngle < 0 || rAngle < 0 && aAngle > 0)
                {
                    repereAngle.localPosition = -repereAngle.localPosition;
                    vecteurRepere = reperePos.position - repereAngle.position;
                }
                reperePos.localRotation = Quaternion.FromToRotation(vecteurRepere, vecteurAnchor) * reperePos.localRotation;
                reperePos.localEulerAngles = new Vector3(0, reperePos.localEulerAngles.y, 0);
            }

            // ── AJOUT : vérification de stabilité à chaque cycle ─────
            CheckStability();
            // ─────────────────────────────────────────────────────────
        }
    }

    // ── AJOUT : méthode de vérification de stabilité ─────────────────
    private void CheckStability()
    {
        float posDelta = Vector3.Distance(reperePos.position, _lastReperePosition);
        float rotDelta = Quaternion.Angle(reperePos.rotation, _lastRepereRotation);

        switch (State)
        {
            case RepereState.Localizing:
                if (posDelta < STABILITY_POS && rotDelta < STABILITY_ROT)
                {
                    _stableCount++;
                    if (_stableCount >= STABLE_CALLS_REQUIRED)
                    {
                        State = RepereState.Stable;
                        OnRepereStable?.Invoke();
                        LogMessage("Repère stabilisé");
                    }
                }
                else _stableCount = 0;
                break;

            case RepereState.Stable:
                if (posDelta > DRIFT_POS || rotDelta > DRIFT_ROT)
                {
                    State = RepereState.Drifting;
                    _stableCount = 0;
                    OnRepereStartedDrifting?.Invoke();
                    LogMessage("Repère en dérive");
                }
                break;

            case RepereState.Drifting:
                if (posDelta < STABILITY_POS && rotDelta < STABILITY_ROT)
                {
                    _stableCount++;
                    if (_stableCount >= STABLE_CALLS_REQUIRED)
                    {
                        State = RepereState.Stable;
                        OnRepereRestabilized?.Invoke();
                        LogMessage("Repère restabilisé");
                    }
                }
                else _stableCount = 0;
                break;
        }

        _lastReperePosition = reperePos.position;
        _lastRepereRotation = reperePos.rotation;
    }
    // ─────────────────────────────────────────────────────────────────
}
