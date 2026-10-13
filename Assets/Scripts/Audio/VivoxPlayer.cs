using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Vivox;
using UnityEngine.Android;
using Unity.Services.Core;
using Microsoft.Psi.Components;
using TMPro;
using System.IO;
using Newtonsoft.Json.Bson;
using Unity.Netcode;

public class VivoxPlayer : MonoBehaviour
{
    private VivoxVoiceManager _vvm;
    private int m_PermissionAskedCount;
    float _nextUpdate = 0;
    private bool connected = false;
    [SerializeField] string voiceChannel = "Lobby";
    [SerializeField] Transform xrCam; //position of our Main Camera
    [SerializeField] TextMeshProUGUI logText;
    // private HomeManager homeManager;

    public void Awake()
    {
        InitializeAsync();
    }
    // Start is called before the first frame update
    async void InitializeAsync()
    {
        Debug.Log("Vivox InitializeAsync called");
        await UnityServices.InitializeAsync();
        //await AuthenticationService.Instance.SignInAnonymouslyAsync();
        await VivoxService.Instance.InitializeAsync();
    }

    void Start()
    {
        VivoxService.Instance.LoggedIn += OnUserLoggedIn;
        VivoxService.Instance.LoggedOut += OnUserLoggedOut;
        // homeManager = FindAnyObjectByType<HomeManager>();

#if (UNITY_ANDROID && !UNITY_EDITOR) || UNITY_ANDROID
        try
        {
            xrCam = GameObject.Find("CenterEyeAnchor").transform;
            LogText($"[Vivox Player] xrCam is '{xrCam.name}'");
        }
        catch
        {
            LogText($"[Vivox Player] Not able to find CenterEyeAnchor");
        }
        /*if(homeManager?.isVivoxActivated == true)
            SignIntoVivox("2");*/
#endif
    }

    //Cleanup elegantly
    void OnDestroy()
    {
        if(VivoxService.Instance != null)
        {
            VivoxService.Instance.LoggedIn -= OnUserLoggedIn;
            VivoxService.Instance.LoggedOut -= OnUserLoggedOut;
        }
    }

#if (UNITY_ANDROID && !UNITY_EDITOR) || UNITY_ANDROID
    bool IsAndroid12AndUp()
        {
            // android12VersionCode is hardcoded because it might not be available in all versions of Android SDK
            const int android12VersionCode = 31;
            AndroidJavaClass buildVersionClass = new AndroidJavaClass("android.os.Build$VERSION");
            int buildSdkVersion = buildVersionClass.GetStatic<int>("SDK_INT");

            return buildSdkVersion >= android12VersionCode;
        }

        string GetBluetoothConnectPermissionCode()
        {
            if (IsAndroid12AndUp())
            {
                // UnityEngine.Android.Permission does not contain the BLUETOOTH_CONNECT permission, fetch it from Android
                AndroidJavaClass manifestPermissionClass = new AndroidJavaClass("android.Manifest$permission");
                string permissionCode = manifestPermissionClass.GetStatic<string>("BLUETOOTH_CONNECT");

                return permissionCode;
            }

            return "";
        }
#endif


    bool IsMicPermissionGranted()
    {
        bool isGranted = Permission.HasUserAuthorizedPermission(Permission.Microphone);
#if (UNITY_ANDROID && !UNITY_EDITOR) || UNITY_ANDROID

        LogText("[Vivox Player] Android Version is up to 12: {IsAndroid12AndUp()}");
            if (IsAndroid12AndUp())
            {
                // On Android 12 and up, we also need to ask for the BLUETOOTH_CONNECT permission for all features to work
                isGranted &= Permission.HasUserAuthorizedPermission(GetBluetoothConnectPermissionCode());
                LogText($"[Vivox Player] Bluetooth Permission is {isGranted}");
            }
#endif
        return isGranted;
    }

    void AskForPermissions()
    {
        string permissionCode = Permission.Microphone;

#if (UNITY_ANDROID && !UNITY_EDITOR) || UNITY_ANDROID
        if (m_PermissionAskedCount == 1 && IsAndroid12AndUp())
            {
                permissionCode = GetBluetoothConnectPermissionCode();
            }
#endif
        m_PermissionAskedCount++;
        Permission.RequestUserPermission(permissionCode);
    }

    bool IsPermissionsDenied()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR) || UNITY_ANDROID
        // On Android 12 and up, we also need to ask for the BLUETOOTH_CONNECT permission
        if (IsAndroid12AndUp())
            {
                return m_PermissionAskedCount == 2;
            }
#endif
        return m_PermissionAskedCount == 1;
    }

    public void SignIntoVivox(string value)
    {
        try
        {
            if (IsMicPermissionGranted())
            {
                LogText("[Vivox Player] MicPermissionGranted OK");
                LoginToVivox(value);
            }
            else
            {
                if (IsPermissionsDenied())
                {
                    LogText("[Vivox Player] MicPermissionGranted DENIED");
                    m_PermissionAskedCount = 0;
                    LoginToVivox(value);
                }
                else
                    AskForPermissions();
                    LoginToVivox(value);
            }
            //connected = true;
        }
        catch (IOException e)
        {
            LogText(e.Message);
        }
        
    }

    void LogText(string message)
    {
        if(logText != null) logText.text += message + "\n";
    }

    async void LoginToVivox(string value)
    {
        try
        {
            await VivoxVoiceManager.Instance.InitializeAsync($"User_{NetworkManager.Singleton.LocalClientId}");
            var loginOptions = new LoginOptions()
            {
                ParticipantUpdateFrequency = ParticipantPropertyUpdateFrequency.FivePerSecond,
                DisplayName = $"User_{NetworkManager.Singleton.LocalClientId}",
                EnableTTS = false
            };
            await VivoxService.Instance.LoginAsync(loginOptions);
            if(value == "1")
                await VivoxService.Instance.JoinEchoChannelAsync(voiceChannel, ChatCapability.AudioOnly);
            else if (value == "2")
                await VivoxService.Instance.JoinPositionalChannelAsync(voiceChannel, ChatCapability.AudioOnly, new Channel3DProperties());
            
            //
            LogText("[Vivox Player] Yeaaahhh, I'm connected to the channeeeel");
        }
        catch (IOException e)
        {
            LogText("[Vivox Player] " + e.Message);
        }
    }

    void OnUserLoggedIn()
    {
        LogText("[Vivox Player] Logged into Vivox");
        LogText($"[Vivox Player] Joining voice channel: {voiceChannel}");

        connected = true;
    }
    void OnUserLoggedOut()
    {
        Debug.Log("Logged out from Vivox");
        connected = false;
    }
    
    void Update()
    {
        if (!connected || xrCam == null) return;

        //LogText("[Vivox Player] Updating audio position");
        if (Time.time > _nextUpdate)
        {
            if(VivoxService.Instance.ActiveChannels.Count >= 1) VivoxService.Instance.Set3DPosition(xrCam.gameObject, voiceChannel); //new in API>1.6.0
            _nextUpdate += 0.5f;
        }
    }
    public async void JoinEchoChannelAsync()
    {
        string channelToJoin = "Lobby";
        await VivoxService.Instance.JoinEchoChannelAsync(channelToJoin, ChatCapability.TextAndAudio);
    }
}
