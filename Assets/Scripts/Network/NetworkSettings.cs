using UnityEngine;

public class NetworkSettings : ScriptableObject
{
    private static NetworkSettings _instance;

    public static NetworkSettings Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }

            InitialilzeInstance();

            return _instance;
        }
    }

    [field: SerializeField] public string IpAddress { get; private set; } = "127.0.0.1";

    [field: SerializeField] public ushort Port { get; private set; } = 7777;

    private static void InitialilzeInstance()
    {
        var allNetworkSettings = Resources.LoadAll<NetworkSettings>("");

        if (allNetworkSettings.Length == 0)
        {
            Debug.LogError("No NetworkSettings asset found in any Resources folder.");
            return;
        }

        if (allNetworkSettings.Length > 1)
        {
            Debug.LogError("Multiple NetworkSettings assets found in Resources. Expected only one.");
        }

        _instance = allNetworkSettings[0];
    }
}
