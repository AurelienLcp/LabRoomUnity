using UnityEngine;

public class AnchorRecenterStabilizer : MonoBehaviour
{
    // Ce script se met sur le SceneAnchorRoot lui-même
    [SerializeField] private OVRCameraRig cameraRig;

    private Vector3 _lastCameraPos;
    private Quaternion _lastCameraRot;

    private void Start()
    {
        CaptureCamera();
        OVRManager.TrackingAcquired += OnRecenter;
    }

    private void OnDestroy()
    {
        OVRManager.TrackingAcquired -= OnRecenter;
    }

    private void CaptureCamera()
    {
        _lastCameraPos = cameraRig.centerEyeAnchor.position;
        _lastCameraRot = cameraRig.centerEyeAnchor.rotation;
    }

    private void OnRecenter()
    {
        // Delta de position et rotation introduit par le recentrage
        Quaternion deltaRot = cameraRig.centerEyeAnchor.rotation
                            * Quaternion.Inverse(_lastCameraRot);
        Vector3 deltaPos = cameraRig.centerEyeAnchor.position
                            - _lastCameraPos;

        // Compenser sur le SceneAnchorRoot — tout ce qui en est enfant
        // (avatar, objets de scène) reste visuellement stable
        transform.rotation = Quaternion.Inverse(deltaRot) * transform.rotation;
        transform.position -= deltaPos;

        CaptureCamera();
    }
}