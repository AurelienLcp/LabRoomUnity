using Meta.XR.MultiplayerBlocks.NGO;
using Meta.XR.MultiplayerBlocks.Shared;
using Oculus.Avatar2;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class LocalAvatarBridge : NetworkBehaviour
{
    [SerializeField] private AvatarEntity _localAvatarOnCameraRig;
    [SerializeField] private float _streamInterval = 0.1f;

    /*// ── NOUVEAU : référence au ReperePos (parent de l'avatar réseau) ──
    [SerializeField] private Transform _anchorRepere;
    // ── NOUVEAU : référence au CameraRig pour lire la position du joueur ──
    [SerializeField] private OVRCameraRig _cameraRig;*/

    private AvatarBehaviourNGO _avatarBehaviourNGO;
    private AvatarEntity _networkAvatarEntity;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _avatarBehaviourNGO = GetComponent<AvatarBehaviourNGO>();
        _networkAvatarEntity = GetComponent<AvatarEntity>();

        if (IsOwner)
            SetupLocalAvatar();
        else
            SetupRemoteAvatar();
    }

    private void SetupLocalAvatar()
    {
        if (_localAvatarOnCameraRig == null)
        {
            _localAvatarOnCameraRig = GameObject.Find("Camera Rig").GetComponentInChildren<AvatarEntity>();
        }

        /*if (_cameraRig == null)
            _cameraRig = GameObject.Find("Camera Rig").GetComponent<OVRCameraRig>();

        if (_anchorRepere == null)
        {
            var repere = GameObject.Find("ReperePos");
            if (repere != null) _anchorRepere = repere.transform;
        }*/

        if (_localAvatarOnCameraRig == null)
        {
            Debug.LogError("[LocalAvatarBridge] AvatarEntity " +
                           "introuvable sur le CameraRig.");
            return;
        }

        // Désactiver AvatarEntity du NetworkObject pour l'owner
        if (_networkAvatarEntity != null)
            _networkAvatarEntity.enabled = false;
        /*if(_avatarBehaviourNGO != null)
            _avatarBehaviourNGO.enabled = false;*/

        // Désactiver les renderers du NetworkObject pour l'owner
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        // Lancer le bridge de stream
        StartCoroutine(BridgeLocalStream());

        Debug.Log("[LocalAvatarBridge] Avatar local SDK v2 sur CameraRig actif.");
    }

    private void SetupRemoteAvatar()
    {
        // Désactiver l'avatar local sur CameraRig pour les non-owners
        if (_localAvatarOnCameraRig != null)
            _localAvatarOnCameraRig.enabled = false;

        Debug.Log("[LocalAvatarBridge] Avatar distant — comportement normal.");
    }

    // ── NOUVEAU : LateUpdate pour corriger la position root avant
    //             que ClientNetworkTransform ne la lise ──
    /*private void LateUpdate()
    {
        if (!IsOwner) return;
        if (_cameraRig == null || _anchorRepere == null) return;

        SyncRootToAnchorSpace();
    }

    private void SyncRootToAnchorSpace()
    {
        // Position/rotation du casque (centerEye) en world space
        Vector3 playerWorldPos = _cameraRig.centerEyeAnchor.position;
        Quaternion playerWorldRot = _cameraRig.centerEyeAnchor.rotation;

        // Convertir en local space du ReperePos
        // → c'est ce que ClientNetworkTransform (LocalSpace=true) va envoyer
        transform.localPosition = _anchorRepere.InverseTransformPoint(playerWorldPos);
        transform.localRotation = Quaternion.Inverse(_anchorRepere.rotation) * playerWorldRot;
    }*/


    private IEnumerator BridgeLocalStream()
    {
        // Attendre que l'avatar local soit prêt
        yield return new WaitUntil(() =>
            _localAvatarOnCameraRig != null &&
            _localAvatarOnCameraRig._isSkeletonLoaded);

        Debug.Log("[LocalAvatarBridge] Bridge stream actif.");

        while (IsOwner && _localAvatarOnCameraRig != null)
        {
            // Enregistrer la pose de l'avatar local SDK v2
            var bytes = _localAvatarOnCameraRig
                .RecordStreamData(OvrAvatarEntity.StreamLOD.Medium);

            if (bytes != null && bytes.Length > 0)
                _avatarBehaviourNGO.ReceiveStreamData(bytes);

            yield return new WaitForSeconds(_streamInterval);
        }
    }
}