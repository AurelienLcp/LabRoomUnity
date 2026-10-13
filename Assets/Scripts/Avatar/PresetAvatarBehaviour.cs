using Meta.XR.MultiplayerBlocks.NGO;
using Meta.XR.MultiplayerBlocks.Shared;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Wrapper autour de AvatarBehaviourNGO qui force OculusId = 0
/// pour que AvatarEntity charge un preset local au lieu de l'avatar CDN.
///
/// Délègue tout à AvatarBehaviourNGO sauf OculusId.
/// AvatarBehaviourNGO gère toujours le streaming NGO.
///
/// STRUCTURE DU PREFAB (ordre important) :
///   1. NetworkObject
///   2. AvatarEntity              ← cherche IAvatarBehaviour via GetInterfaceComponent
///   3. PresetAvatarBehaviour     ← ce script, trouvé EN PREMIER par GetInterfaceComponent
///   4. AvatarBehaviourNGO        ← gère le streaming, pas trouvé par AvatarEntity
///   5. ClientNetworkTransform
///   6. OvrAvatarAnimationBehavior
///   7. NetworkAvatar
///
/// IMPORTANT : PresetAvatarBehaviour doit être AVANT AvatarBehaviourNGO
/// dans l'Inspector pour que GetInterfaceComponent le trouve en premier.
/// </summary>
[DefaultExecutionOrder(-1)] // avant AvatarEntity (DefaultExecutionOrder 50)
public class PresetAvatarBehaviour : NetworkBehaviour, IAvatarBehaviour
{
    [Tooltip("Index du preset avatar à charger (0-32).")]
    [SerializeField] public int _presetIndex = 0;

    private AvatarBehaviourNGO _ngo;

    private void Awake()
    {
        _ngo = GetComponent<AvatarBehaviourNGO>();
        if (_ngo == null)
            Debug.LogError("[PresetAvatarBehaviour] AvatarBehaviourNGO introuvable sur ce GameObject.");
    }

    // ── IAvatarBehaviour ────────────────────────────────────────────────────

    /// <summary>
    /// Force à 0 → AvatarEntity appelle LoadLocalAvatar() au lieu du CDN.
    /// </summary>
    public ulong OculusId => 0;

    /// <summary>
    /// Index du preset zip (0-32) passé à AvatarEntity.LoadLocalAvatar().
    /// </summary>
    public int LocalAvatarIndex => _presetIndex;

    /// <summary>
    /// Délégué à AvatarBehaviourNGO pour la gestion owner/remote.
    /// </summary>
    public bool HasInputAuthority => _ngo != null ? _ngo.HasInputAuthority : IsOwner;

    /// <summary>
    /// Délégué à AvatarBehaviourNGO qui gère l'envoi NGO des bytes.
    /// C'est lui qui fait le ServerRpc/ClientRpc du stream.
    /// </summary>
    public void ReceiveStreamData(byte[] bytes)
    {
        _ngo?.ReceiveStreamData(bytes);
    }
}
