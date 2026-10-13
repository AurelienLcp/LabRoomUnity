using Meta.XR.MultiplayerBlocks.Shared;
using UnityEngine;

public class GhostAvatarBehaviour : MonoBehaviour, IAvatarBehaviour
{
    [SerializeField] private ulong _oculusId = 0;
    [SerializeField] private int _localAvatarIndex = 0;

    // true  = IsLocal → avatar local avec body tracking ✅
    // false = remote  → avatar distant
    [SerializeField] private bool _hasInputAuthority = true;

    public ulong OculusId
    {
        get => _oculusId;
        set => _oculusId = value;
    }

    public int LocalAvatarIndex
    {
        get => _localAvatarIndex;
        set => _localAvatarIndex = value;
    }

    public bool HasInputAuthority => _hasInputAuthority;

    public void ReceiveStreamData(byte[] bytes) { }
}