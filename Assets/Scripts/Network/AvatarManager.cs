using Meta.XR.MultiplayerBlocks.Shared;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class AvatarManager : MonoBehaviour
{
    public static AvatarManager Instance { get; private set; }

    public AvatarEntity streamAvatar;
    [SerializeField] private SampleAvatarEntity avatarMirror;
    //[SerializeField] public ExperienceManager experienceManager;
    [SerializeField] public TMP_InputField avatarID1;
    [SerializeField] public TMP_InputField avatarID2;
    [SerializeField] public TMP_InputField avatarID3;

    public delegate void OnAvatarReload(int current);
    public event OnAvatarReload onAvatarReload;

    public int pathAvatar = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    public void ReloadAvatar(int path)
    {
        pathAvatar = path;
        onAvatarReload?.Invoke(path);
    }

    public void ChangeAvatar(int path)
    {
        pathAvatar = (pathAvatar + path) % 32;
        if (pathAvatar < 0) pathAvatar = 31;

        // Create a set of used avatar IDs for quick lookup
        CheckAndChooseAvailableAvatar(path);
        ReloadAvatar(pathAvatar);
    }

    private void CheckAndChooseAvailableAvatar(int path)
    {
        int avatarId = 1;
        var usedAvatars = new HashSet<int>(avatarId);

        // Loop until we find an available avatar
        while (usedAvatars.Contains(pathAvatar))
        {
            pathAvatar = (pathAvatar + path + 32) % 32;
        }
    }
}
