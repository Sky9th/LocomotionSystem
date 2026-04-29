using Cinemachine;
using UnityEngine;

/// <summary>
/// Central coordinator for all runtime Cinemachine rigs. It keeps the active
/// camera snapshot in sync with GameContext so other systems can query
/// deterministic pose/FOV data without touching scene objects directly.
/// </summary>
[DefaultExecutionOrder(-400)]
[DisallowMultipleComponent]
public class CameraManager : BaseService
{
    [Header("Cinemachine Wiring")]
    [SerializeField] private CinemachineBrain cameraBrain;
    [SerializeField] private CinemachineVirtualCamera defaultVirtualCamera;
    [SerializeField] private bool autoLocateBrain = true;
    [SerializeField] private bool autoLocateDefaultVirtualCamera = true;

    [Header("Local Player Anchor")]
    [SerializeField] private Transform localPlayerAnchor;
    [SerializeField] private bool followPlanarOnly = true;
    [SerializeField] private float verticalOffset;
    [SerializeField] private GameProfile gameProfile;
    [SerializeField, Range(0f, 90f)] private float maxPitchDegrees = 75f;

    private SLookIAction lastLookAction;
    private Vector2 lastAppliedLookDelta;

    private SCameraContext lastSnapshot;
    private bool hasSnapshot;

    private void Update()
    {
        TickLocalPlayerAnchor();
    }

    private void LateUpdate()
    {
        PushCameraSnapshotToContext();
    }

    protected override void OnSubscriptionsActivated()
    {
        base.OnSubscriptionsActivated();

        if (Dispatcher != null)
        {
            Dispatcher.Subscribe<SLookIAction>(HandleLook);
        }
    }

    private void OnDestroy()
    {
        if (Dispatcher != null)
        {
            Dispatcher.Unsubscribe<SLookIAction>(HandleLook);
        }
    }

    protected override bool OnRegister(GameContext context)
    {
        ValidateConfiguration();

        if (!EnsureCinemachineBrain())
        {
            return false;
        }

        EnsureDefaultVirtualCamera();
        InitializeDefaultRig();

        context.RegisterService(this);
        return true;
    }
    
    protected override void OnServicesReady()
    {
        PushCameraSnapshotToContext();
        localPlayerAnchor = GameObject.Find(CommonConstants.FollowAnchorName)?.transform;

        defaultVirtualCamera.Follow = localPlayerAnchor;
        defaultVirtualCamera.LookAt = localPlayerAnchor;
    }

    private void ValidateConfiguration()
    {
        if (gameProfile == null)
        {
            Debug.LogError("CameraManager is missing a GameProfile reference. Please assign one in the inspector.", this);
        }
    }

    private bool EnsureCinemachineBrain()
    {
        if (autoLocateBrain && cameraBrain == null)
        {
            cameraBrain = FindCinemachineBrain();
        }

        if (cameraBrain == null)
        {
            Debug.LogError("CameraManager could not locate a CinemachineBrain. Please assign one in the inspector.", this);
            return false;
        }

        return true;
    }

    private void EnsureDefaultVirtualCamera()
    {
        if (autoLocateDefaultVirtualCamera && defaultVirtualCamera == null)
        {
            defaultVirtualCamera = GetComponentInChildren<CinemachineVirtualCamera>(true);
        }
    }

    private void InitializeDefaultRig()
    {
        if (defaultVirtualCamera != null)
        {
            ApplySerializedTargets(defaultVirtualCamera);
            defaultVirtualCamera.gameObject.SetActive(true);
            return;
        }

        Debug.LogWarning("CameraManager does not have a default virtual camera assigned.", this);
    }

    private CinemachineBrain FindCinemachineBrain()
    {
        var mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.TryGetComponent(out CinemachineBrain brainOnMain))
        {
            return brainOnMain;
        }

        return FindObjectOfType<CinemachineBrain>();
    }

    private void PushCameraSnapshotToContext()
    {
        if (cameraBrain == null)
        {
            return;
        }

        var outputCamera = cameraBrain.OutputCamera;
        if (outputCamera == null || GameContext == null)
        {
            return;
        }

        Vector3 anchorPosition = localPlayerAnchor != null ? localPlayerAnchor.position : outputCamera.transform.position;
        Quaternion anchorRotation = localPlayerAnchor != null ? localPlayerAnchor.rotation : outputCamera.transform.rotation;

        lastSnapshot = new SCameraContext(
            outputCamera.transform.position,
            outputCamera.transform.rotation,
            anchorPosition,
            anchorRotation,
            lastAppliedLookDelta);

        hasSnapshot = true;
        GameContext.UpdateSnapshot(lastSnapshot);
    }

    private void ApplySerializedTargets(CinemachineVirtualCamera virtualCamera)
    {
        if (virtualCamera == null)
        {
            return;
        }

        if (localPlayerAnchor != null)
        {
            virtualCamera.Follow = localPlayerAnchor;
            virtualCamera.LookAt = localPlayerAnchor;
        }
    }

    public bool TryGetLatestSnapshot(out SCameraContext snapshot)
    {
        snapshot = lastSnapshot;
        return hasSnapshot;
    }

    private void HandleLook(SLookIAction payload, MetaStruct meta)
    {
        lastLookAction = payload;
    }

    private void TickLocalPlayerAnchor()
    {
        if (localPlayerAnchor == null) return;

        GameContext context = GameContext.Instance;
        if (context == null || !context.TryGetSnapshot(out SCharacterSnapshot snapshot)) return;

        Vector3 targetPosition = snapshot.Kinematic.Position;
        targetPosition.y = snapshot.Kinematic.Position.y + verticalOffset;
        localPlayerAnchor.position = targetPosition;

        ApplyLookRotationToAnchor(localPlayerAnchor, lastLookAction, out Vector2 appliedLookDelta);
        lastAppliedLookDelta = appliedLookDelta;

        if (Dispatcher != null)
        {
            var outputCamera = cameraBrain != null ? cameraBrain.OutputCamera : null;
            if (outputCamera != null)
            {
                Dispatcher.Publish(new SCameraContext(
                    outputCamera.transform.position,
                    outputCamera.transform.rotation,
                    localPlayerAnchor.position,
                    localPlayerAnchor.rotation,
                    appliedLookDelta));
            }
            else
            {
                Dispatcher.Publish(new SCameraContext(
                    localPlayerAnchor.position,
                    localPlayerAnchor.rotation,
                    localPlayerAnchor.position,
                    localPlayerAnchor.rotation,
                    appliedLookDelta));
            }
        }

        lastLookAction = SLookIAction.None;
    }

    private void ApplyLookRotationToAnchor(Transform anchor, SLookIAction lookAction, out Vector2 appliedLookDelta)
    {
        appliedLookDelta = Vector2.zero;

        if (anchor == null)
        {
            return;
        }

        if (!lookAction.HasDelta)
        {
            return;
        }

        float rotationSpeed = gameProfile != null ? gameProfile.cameraLookRotationSpeed : 1f;
        Vector2 lookDelta = lookAction.Delta * rotationSpeed;
        appliedLookDelta = lookDelta;

        Vector3 euler = anchor.rotation.eulerAngles;
        euler.z = 0f;

        float pitch = NormalizeAngle180(euler.x);
        pitch += lookDelta.y;
        if (maxPitchDegrees > 0f)
        {
            pitch = Mathf.Clamp(pitch, -maxPitchDegrees, maxPitchDegrees);
        }

        euler.x = pitch;
        euler.y += lookDelta.x;

        anchor.rotation = Quaternion.Euler(euler);
    }

    private static float NormalizeAngle180(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
        {
            angle -= 360f;
        }
        else if (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }
}
