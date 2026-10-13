using Meta.XR.MultiplayerBlocks.NGO;
using Meta.XR.MultiplayerBlocks.Shared;
using Oculus.Avatar2.Experimental;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkAvatar : NetworkBehaviour
{
    private DataCollectionManager dataCollectionManager;
    private AvatarManager avatarManager;
    [SerializeField] private AvatarEntity entity;
    private AvatarBehaviourNGO avatarBehaviourNGO;
    private GhostAvatarBehaviour _localGhostBehaviour;
    private PresetAvatarBehaviour _presetAvatarBehaviour;
    public bool isAvatarColliderSet = false;
    private bool isNameChanged = false;
    public int userId = 0;
    public int avatarId = 0;
    public NetworkVariable<int> path = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        AvatarManager.Instance.onAvatarReload -= ChangePath;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        avatarBehaviourNGO = GetComponent<AvatarBehaviourNGO>();
        dataCollectionManager = FindAnyObjectByType<DataCollectionManager>();
        avatarManager = AvatarManager.Instance;
        entity = GetComponent<AvatarEntity>();
        _presetAvatarBehaviour = GetComponent<PresetAvatarBehaviour>();

        /*if (IsOwner)
        {
            *//*#if META_AVATAR_SDK_DEFINED
                        entity.enabled = false;
                        entity = Camera.main.GetComponentInParent<AvatarEntity>();
                        avatarBehaviourNGO._avatarEntity = entity;
            #endif*//*
            //GetComponent<OvrAvatarAnimationBehavior>().enabled = false;
            *//*var cameraRig = GameObject.Find("Camera Rig");
            transform.SetParent(cameraRig.transform, false);*//*
            //_localGhostBehaviour = GameObject.Find("Camera Rig").GetComponentInChildren<GhostAvatarBehaviour>();
            path.Value = avatarBehaviourNGO.LocalAvatarIndex;
            //SyncLocalAvatarIndex(avatarBehaviourNGO.LocalAvatarIndex);

        }
        else
        {
            path.OnValueChanged += ReloadAvatar;
            ReloadAvatar(path.Value, path.Value);
        }*/

        InitializeAvatar();
    }

    private void SyncLocalAvatarIndex(int index)
    {
        if (_localGhostBehaviour == null) return;

        _localGhostBehaviour.LocalAvatarIndex = index;

        Debug.Log($"[NetworkAvatar] LocalAvatarIndex synchronisé " +
                  $"vers GhostAvatarBehaviour : {index}");
    }

    public void InitializeAvatar()
    {
        if (IsOwner)
        {
            path.Value = avatarBehaviourNGO.LocalAvatarIndex;
            avatarManager.onAvatarReload += ChangePath;

            if (IsClient)
            {
                ChangeAvatarNameRequestServerRpc(GetComponent<NetworkObject>().NetworkObjectId);
            }
        }
        else
        {
            path.OnValueChanged += ReloadAvatar;
            ReloadAvatar(path.Value, path.Value);
        }

        if (IsServer)
        {
            dataCollectionManager.AvatarUserIds.Add((ulong)avatarBehaviourNGO.LocalAvatarIndex);
            isNameChanged = true;
        }
    }

    [Rpc(SendTo.Server)]
    public void ChangeAvatarNameRequestServerRpc(ulong value, RpcParams rpcParams = default)
    {
        var senderClientId = (int)rpcParams.Receive.SenderClientId;
        userId = senderClientId;
        dataCollectionManager.networkAvatar.Add(this.gameObject);
        dataCollectionManager.AvatarNetIds.Add(value);
        this.gameObject.name = $"RemoteAvatar{senderClientId}";
        string message = "";
        message += $"{this.gameObject.name};{senderClientId}";
        for(int i = 0; i < dataCollectionManager.AvatarNetIds.Count; i++)
        {
            message += $";{dataCollectionManager.AvatarNetIds[i]}";
        }
        Debug.Log($"Sending ChangeAvatarNameClientRpc with message: {message}");
        ChangeAvatarNameClientRpc(message);
    }

    [Rpc(SendTo.NotServer)]
    public void ChangeAvatarNameClientRpc(string n)
    {
        Debug.Log($"Receiving ChangeAvatarNameClientRpc with message: {n}");
        var message = n.Split(';');
        name = message[0];
        userId = int.Parse(message[1]);
        var netid = int.Parse(message[2]);
        List<string> data = new List<string>();
        for(int i = 2; i < message.Length; i++)
        {
            Debug.Log($"Adding netid {message[i]} to data list");
            data.Add(message[i]);
        }
        CheckForExistingRemoteAvatar(data);
        isNameChanged = true;
        this.gameObject.name = name;
    }

    public void CheckForExistingRemoteAvatar(List<string> value)
    {
        int i = 1;
        foreach (var item in value)
        {
            if(!dataCollectionManager.AvatarNetIds.Contains(ulong.Parse(item)))
            {
                dataCollectionManager.AvatarNetIds.Add(ulong.Parse(item));
                GameObject existingAvatar = GetObjectById(ulong.Parse(item));
                existingAvatar.name = $"RemoteAvatar{i}";
                existingAvatar.transform.GetComponent<NetworkAvatar>().userId = i;
                dataCollectionManager.networkAvatar.Add(existingAvatar);
            }
            i++;
        }
    }

    public void ChangePath(int newPath)
    {
        if (IsOwner)
        {
            path.Value = newPath;
            //SyncLocalAvatarIndex(newPath);
        }
        ChangeAvatarPathServerRpc(newPath);
    }

    [Rpc(SendTo.Server)]
    public void ChangeAvatarPathServerRpc(int value)
    {
        avatarBehaviourNGO.LocalAvatarIndex = value;
        avatarId = value;

        if (_presetAvatarBehaviour != null)
            _presetAvatarBehaviour._presetIndex = value;
        StartCoroutine(WaitAndLoadAvatar(value));

        
        ChangeAvatarPathClientRpc((int) value);
    }

    [Rpc(SendTo.NotServer)]
    public void ChangeAvatarPathClientRpc(int value)
    {
        avatarBehaviourNGO.LocalAvatarIndex = value;
        if (_presetAvatarBehaviour != null)
            _presetAvatarBehaviour._presetIndex = value;

        avatarId = value;
        if (IsOwner)
        {
            path.Value = value;
        }
        StartCoroutine(WaitAndLoadAvatar(value));
    }

    private IEnumerator WaitAndLoadAvatar(int avatarIndex)
    {
        yield return new WaitForSeconds(1f);

        avatarBehaviourNGO.LocalAvatarIndex = avatarIndex;
        avatarId = avatarIndex;

        if (_presetAvatarBehaviour != null)
            _presetAvatarBehaviour._presetIndex = avatarIndex;

        entity.Teardown();

        yield return new WaitForEndOfFrame();

        entity.CreateEntity();

        yield return new WaitForEndOfFrame();

        Debug.Log($"[NetworkAvatar] LoadAvatar() appelé avec index {avatarIndex}");
        entity.LoadAvatar();
    }

    public void ReloadAvatar(int previous, int current)
    {
        StartCoroutine(WaitForSecond());
    }

    void Update()
    {

    }

    public GameObject GetObjectById(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            return netObj.gameObject;
        }

        return null;
    }

    public IEnumerator WaitForSecond()
    {
        yield return new WaitForSeconds(4f);
        entity.LoadAvatar();
    }
    public IEnumerator WaitForSeconds()
    {
        yield return new WaitForSeconds(3f);
        InitializeAvatar();
    }
}
