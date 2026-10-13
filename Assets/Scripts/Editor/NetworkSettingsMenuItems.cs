using UnityEditor;
using UnityEngine;

public static class NetworkSettingsMenuItems
{
    [MenuItem("Fusion/Open Network Settings")]
    public static void ShowNetworkSettings()
    {
        var settings = NetworkSettings.Instance;

        if (settings == null)
        {
            Debug.LogError(
                "NetworkSettings asset not found. Create one at Assets/Resources/NetworkSettings.asset"
            );
            return;
        }

        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }
}