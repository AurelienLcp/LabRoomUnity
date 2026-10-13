
using Meta.XR.MultiplayerBlocks.NGO;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class AvatarSelectionServer : NetworkBehaviour
{

    public AvatarManager avatarManager;
    public DataCollectionManager dataCollectionManager;
    [SerializeField] int[] avatarIdList = new int[] { 0, 0, 0 };
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeAvatarID(int user)
    {
        if(dataCollectionManager.networkAvatar.Count < user) return;
        UpdateAvatarIDList(user);
        dataCollectionManager.networkAvatar[user - 1].GetComponent<AvatarBehaviourNGO>().LocalAvatarIndex = avatarIdList[user - 1];
        dataCollectionManager.networkAvatar[user - 1].GetComponent<PresetAvatarBehaviour>()._presetIndex = avatarIdList[user - 1];
        ChangeAvatarIDClientRpc(user, avatarIdList[user - 1]);
    }

    public void UpdateAvatarIDList(int user)
    {
        switch (user)
        {
            case 1:
                avatarIdList[0] = int.Parse(avatarManager.avatarID1.text);
                if (IsServer) Debug.Log($"[AVATAR SELECTION] AVATAR1 ID_{avatarIdList[0]}");
                break;
            case 2:
                avatarIdList[1] = int.Parse(avatarManager.avatarID2.text);
                if (IsServer) Debug.Log($"[AVATAR SELECTION] AVATAR2 ID_{avatarIdList[1]}");
                break;
            case 3:
                avatarIdList[2] = int.Parse(avatarManager.avatarID3.text);
                if (IsServer) Debug.Log($"[AVATAR SELECTION] AVATAR3 ID_{avatarIdList[2]}");
                break;
        }
    }

    [Rpc(SendTo.NotServer)]
    public void ChangeAvatarIDClientRpc(int id, int avatarId)
    {
        if ((ulong)id == NetworkManager.LocalClientId)
        {
            avatarIdList[id - 1] = avatarId;
            dataCollectionManager.networkAvatar[id - 1].GetComponent<AvatarBehaviourNGO>().LocalAvatarIndex = avatarId;
            dataCollectionManager.networkAvatar[id - 1].GetComponent<PresetAvatarBehaviour>()._presetIndex = avatarId;
            Debug.Log($"[AVATAR SELECTION] Start to reload avatar with ID {avatarId}");
            avatarManager.ReloadAvatar(avatarId);
            StartCoroutine(WaitForSeconds());
            //dataCollectionManager.CallAvatarSetupOnSelect();
        }
    }

    public IEnumerator WaitForSeconds()
    {
        yield return new WaitForSeconds(4f);
        dataCollectionManager.CallAvatarSetupOnSelect();
    }
}
