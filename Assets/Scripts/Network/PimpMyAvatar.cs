using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PimpMyAvatar : NetworkBehaviour
{
    private DataCollectionManager dataCollectionManager;
    private NetworkAvatar networkAvatar;
    // private UpdateEyesPosition updateEyesPosition;
    public bool isAvatarColliderCanBeSet = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        networkAvatar = GetComponent<NetworkAvatar>();
        dataCollectionManager = FindAnyObjectByType<DataCollectionManager>();
        // updateEyesPosition = FindAnyObjectByType<UpdateEyesPosition>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isAvatarColliderCanBeSet) return;

        if (/*transform.childCount >= 5 && */networkAvatar.path.Value != -1)
        {
            SetAvatarColliderAndEyeInteractableID("Light");
        }
    }
    public void PimpAvatarAfterSelection()
    {
        Debug.Log($"Pimping avatar for user {networkAvatar.userId} with local id {NetworkManager.Singleton.LocalClientId}");
        isAvatarColliderCanBeSet = true;
    }
    private void SetAvatarColliderAndEyeInteractableID(string avatarType)
    {
        isAvatarColliderCanBeSet = false;
        SetAvatarColliderAndEyeInteractableIDServerRpc(avatarType);
    }

    [Rpc(SendTo.Server)]
    private void SetAvatarColliderAndEyeInteractableIDServerRpc(string avatarType)
    {
        GameObject jointHead = gameObject;
        GameObject jointChest = gameObject;
        StartCoroutine(WaitForAvatarLoaded());
        SetupNetworkAvatarBodyArchitecture(avatarType, out jointHead, out jointChest);
        AddCameraOnNetworkAvatarServerSide(jointHead);
        isAvatarColliderCanBeSet = false;
    }
    [Rpc(SendTo.NotServer)]
    private void SetAvatarColliderAndEyeInteractableIDClientRpc(string avatarType)
    {
        Debug.Log($"1_Setting avatar collider and eye interactable ID for user {networkAvatar.userId} on client with local id {NetworkManager.Singleton.LocalClientId}");
        if (networkAvatar.userId != (int)NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"2_Setting avatar collider and eye interactable ID for remote avatar of user {networkAvatar.userId} on client with local id {NetworkManager.Singleton.LocalClientId}");
            GameObject jointHead = gameObject;
            GameObject jointChest = gameObject;
            SetupNetworkAvatarBodyArchitecture(avatarType, out jointHead, out jointChest);
            SetupCollidersAndEyesDetectionClientSide(jointHead, jointChest);
            isAvatarColliderCanBeSet = false;
        }
    }

    private IEnumerator WaitForAvatarLoaded()
    {
        while (transform.childCount < 5)
        {
            yield return null;
        }

    }

    private void SetupNetworkAvatarBodyArchitecture(string avatarType, out GameObject jointHead, out GameObject jointChest)
    {
        int value = 0;
        GameObject jointLeftWristHand = gameObject;
        GameObject jointRightWristHand = gameObject;
        jointHead = gameObject;
        jointChest = gameObject;
        if (transform.childCount >= 5)
        {
            switch (avatarType)
            {
                case "Full":

                    jointLeftWristHand = this.transform.GetChild(value + 1).gameObject;
                    jointRightWristHand = this.transform.GetChild(value + 2).gameObject;
                    jointHead = this.transform.GetChild(value + 3).gameObject;
                    jointChest = this.transform.GetChild(value + 4).gameObject;
                    break;
                case "Light":

                    jointChest = this.transform.GetChild(value).gameObject;
                    jointHead = this.transform.GetChild(value + 1).gameObject;
                    jointLeftWristHand = this.transform.GetChild(value + 2).gameObject;
                    jointRightWristHand = this.transform.GetChild(value + 3).gameObject;
                    break;
            }
            jointLeftWristHand.name = $"{networkAvatar.userId}-Joint LeftHandWrist";
            jointRightWristHand.name = $"{networkAvatar.userId}-Joint RightHandWrist";
            jointHead.name = $"{networkAvatar.userId}-Joint Head";
            jointChest.name = $"{networkAvatar.userId}-Joint Chest";
        }
    }

    private void SetupCollidersAndEyesDetectionClientSide(GameObject jointHead, GameObject jointChest)
    {
        var jointChestCollider = jointChest.AddComponent<CapsuleCollider>(); 
        var jointHeadCollider = jointHead.AddComponent<CapsuleCollider>();
        jointHeadCollider.radius = 0.1f;
        jointHeadCollider.height = 0.33f;
        jointHeadCollider.direction = 0;
        jointHeadCollider.center = new Vector3(0.08f, 0.03f, 0);
        jointChestCollider.radius = 0.15f;
        jointChestCollider.height = 0.5f;
        jointChestCollider.direction = 0;
        jointChestCollider.center = new Vector3(-0.085f, 0, 0);

        var jointChestRigidbody = jointChest.AddComponent<Rigidbody>();
        var jointHeadRigidbody = jointHead.AddComponent<Rigidbody>();
        jointChestRigidbody.useGravity = false;
        jointHeadRigidbody.useGravity = false;
        jointChestRigidbody.isKinematic = true;
        jointHeadRigidbody.isKinematic = true;

    }

    private void SetupEyeInteractable(GameObject jointHead, GameObject jointChest)
    {
        /*jointHead.layer = 6;
        jointChest.layer = 6;
        var idChest = jointChest.AddComponent<IDinteractable>();
        idChest.id = 1;
        idChest.type = jointChest.name;
        var idHead = jointHead.AddComponent<IDinteractable>();
        idHead.id = 1;
        idHead.type = jointHead.name;

        var eyeInteractableChest = jointChest.AddComponent<EyeInteractable>();
        eyeInteractableChest.avatarID = networkAvatar.userId;
        eyeInteractableChest.InitializeComponent();
        var eyeInteractableHead = jointHead.AddComponent<EyeInteractable>();
        eyeInteractableHead.avatarID = networkAvatar.userId;
        eyeInteractableHead.InitializeComponent();
        UnityEngine.Debug.Log($"Avatar collider and eye interactable ID set for {transform.gameObject.name}");*/
    }

    private void AddCameraOnNetworkAvatarServerSide(GameObject jointHead)
    {
        GameObject cam = new GameObject("camera");
        cam.AddComponent<Camera>();
        cam.GetComponent<Camera>().targetTexture = dataCollectionManager.avatarCameras[networkAvatar.userId - 1];
        GameObject camera = Instantiate(cam, jointHead.transform);
        Destroy(cam);
        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.Skybox;
        camera.GetComponent<Camera>().nearClipPlane = .2f;
        camera.transform.Rotate(-90f, 0f, -90f);
        camera.GetComponent<Camera>().depth = 0;
    }

    private void SetupEyeTrackingOnServerSide(GameObject jointHead)
    {
        /*GameObject leftEyes = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftEyes.name = $"{networkAvatar.userId}-LeftEyes";
        leftEyes.transform.SetParent(this.transform);
        leftEyes.transform.localPosition = new Vector3(-0.118f, 0f, 0f);
        leftEyes.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        leftEyes.transform.GetComponent<MeshRenderer>().enabled = false;
        leftEyes.transform.GetComponent<SphereCollider>().enabled = false;
        leftEyes.AddComponent<EyeTrackingRay1>();
        var trackingLeft = leftEyes.GetComponent<EyeTrackingRay1>();
        trackingLeft.rayDistance = 5f;
        trackingLeft.rayWidth = 0.01f;
        trackingLeft.userID = networkAvatar.userId;
        trackingLeft.logData = false;

        GameObject rightEyes = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightEyes.name = $"{networkAvatar.userId}-RightEyes";
        rightEyes.transform.SetParent(this.transform);
        rightEyes.transform.localPosition = new Vector3(0.096f, 0f, 0f);
        rightEyes.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        rightEyes.transform.GetComponent<MeshRenderer>().enabled = false;
        rightEyes.transform.GetComponent<SphereCollider>().enabled = false;
        rightEyes.AddComponent<EyeTrackingRay1>();
        var trackingRight = rightEyes.GetComponent<EyeTrackingRay1>();
        trackingRight.rayDistance = 5f;
        trackingRight.rayWidth = 0.01f;
        trackingRight.userID = networkAvatar.userId;
        trackingRight.logData = true;

        AddRemoteAvatarEyes(jointHead, leftEyes, rightEyes);*/
    }

    private void AddRemoteAvatarEyes(GameObject jointHead, GameObject leftEyes, GameObject rightEyes)
    {
        /*switch (networkAvatar.userId)
        {
            case 1:
                updateEyesPosition.user1ServerEyes.Add(leftEyes);
                updateEyesPosition.user1ServerEyes.Add(rightEyes);
                updateEyesPosition.user1ServerHead = jointHead;
                break;
            case 2:
                updateEyesPosition.user2ServerEyes.Add(leftEyes);
                updateEyesPosition.user2ServerEyes.Add(rightEyes);
                updateEyesPosition.user2ServerHead = jointHead;
                break;
            case 3:
                updateEyesPosition.user3ServerEyes.Add(leftEyes);
                updateEyesPosition.user3ServerEyes.Add(rightEyes);
                updateEyesPosition.user3ServerHead = jointHead;
                break;
            case 4:
                updateEyesPosition.user4ServerEyes.Add(leftEyes);
                updateEyesPosition.user4ServerEyes.Add(rightEyes);
                updateEyesPosition.user4ServerHead = jointHead;
                break;
            case 5:
                updateEyesPosition.user5ServerEyes.Add(leftEyes);
                updateEyesPosition.user5ServerEyes.Add(rightEyes);
                updateEyesPosition.user5ServerHead = jointHead;
                break;
            case 6:
                updateEyesPosition.user6ServerEyes.Add(leftEyes);
                updateEyesPosition.user6ServerEyes.Add(rightEyes);
                updateEyesPosition.user6ServerHead = jointHead;
                break;
        }*/
    }
}
