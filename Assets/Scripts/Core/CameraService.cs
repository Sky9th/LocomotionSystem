using Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(-400)]
[DisallowMultipleComponent]
public class CameraService : BaseService, IGameplaySessionHandler
{
    [Header("Cinemachine Wiring")]
    [SerializeField] private CinemachineBrain cameraBrain;
    [SerializeField] private CinemachineVirtualCamera defaultVirtualCamera;
    [SerializeField] private bool autoLocateBrain = true;
    [SerializeField] private bool autoLocateDefaultVirtualCamera = true;

    [Header("Camera Pivot")]
    [SerializeField] private float verticalOffset;
    [SerializeField] private GameProfile gameProfile;
    [SerializeField, Range(0f, 90f)] private float maxPitchDegrees = 75f;

    private Transform cameraPivot;
    private SIActionLook lastLookAction;
    private Vector2 lastAppliedLookDelta;

    private SCameraContext lastSnapshot;
    private bool hasSnapshot;
    private bool isFollowingPlayer;

    public Transform CameraPivot => cameraPivot;

    private void Update()
    {
        if (!isFollowingPlayer) return;
        TickCameraPivot();
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
            Dispatcher.Subscribe<SIActionLook>(HandleLook);
            Dispatcher.Subscribe<SPlayerSpawnedEvent>(HandlePlayerSpawned);
        }
    }

    private void OnDestroy()
    {
        if (Dispatcher != null)
        {
            Dispatcher.Unsubscribe<SIActionLook>(HandleLook);
            Dispatcher.Unsubscribe<SPlayerSpawnedEvent>(HandlePlayerSpawned);
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
        CreateCameraPivot();
        InitializeDefaultRig();

        context.RegisterService(this);
        return true;
    }

    protected override void OnServicesReady()
    {
        PushCameraSnapshotToContext();
    }

    public void OnGameplaySessionEnd()
    {
        isFollowingPlayer = false;
        lastLookAction = SIActionLook.None;
        lastAppliedLookDelta = Vector2.zero;
        DestroyCameraPivot();
    }

    private void DestroyCameraPivot()
    {
        if (cameraPivot != null)
        {
            Destroy(cameraPivot.gameObject);
            cameraPivot = null;
        }
    }

    private void HandlePlayerSpawned(SPlayerSpawnedEvent evt, MetaStruct meta)
    {
        if (!evt.IsLocalPlayer) return;
        if (cameraPivot == null)
        {
            CreateCameraPivot();
            InitializeDefaultRig();
        }
        isFollowingPlayer = true;
    }

    private void CreateCameraPivot()
    {
        var pivotObj = new GameObject(CommonConstants.FollowAnchorName);
        pivotObj.transform.SetParent(transform, false);
        cameraPivot = pivotObj.transform;
    }

    private void ValidateConfiguration()
    {
        if (gameProfile == null)
        {
            Debug.LogError("[CameraService] Missing GameProfile reference.", this);
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
            Debug.LogError("[CameraService] Could not locate a CinemachineBrain.", this);
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
            defaultVirtualCamera.Follow = cameraPivot;
            defaultVirtualCamera.LookAt = cameraPivot;
            defaultVirtualCamera.gameObject.SetActive(true);
            return;
        }

        Debug.LogWarning("[CameraService] No default virtual camera assigned.", this);
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
        if (cameraBrain == null || GameContext == null) return;

        var outputCamera = cameraBrain.OutputCamera;
        if (outputCamera == null) return;

        Vector3 pivotPosition = cameraPivot != null ? cameraPivot.position : outputCamera.transform.position;
        Quaternion pivotRotation = cameraPivot != null ? cameraPivot.rotation : outputCamera.transform.rotation;

        lastSnapshot = new SCameraContext(
            outputCamera.transform.position,
            outputCamera.transform.rotation,
            pivotPosition,
            pivotRotation,
            lastAppliedLookDelta);

        hasSnapshot = true;
        GameContext.UpdateSnapshot(lastSnapshot);
    }

    public bool TryGetLatestSnapshot(out SCameraContext snapshot)
    {
        snapshot = lastSnapshot;
        return hasSnapshot;
    }

    private void HandleLook(SIActionLook payload, MetaStruct meta)
    {
        lastLookAction = payload;
    }

    private void TickCameraPivot()
    {
        if (cameraPivot == null) return;

        GameContext context = GameContext.Instance;
        if (context == null || !context.TryGetSnapshot(out SCharacterSnapshot snapshot)) return;

        Vector3 targetPosition = snapshot.Kinematic.Position;
        targetPosition.y = snapshot.Kinematic.Position.y + verticalOffset;
        cameraPivot.position = targetPosition;

        ApplyLookRotationToPivot(cameraPivot, lastLookAction, out Vector2 appliedLookDelta);
        lastAppliedLookDelta = appliedLookDelta;

        if (Dispatcher != null)
        {
            var outputCamera = cameraBrain != null ? cameraBrain.OutputCamera : null;
            if (outputCamera != null)
            {
                Dispatcher.Publish(new SCameraContext(
                    outputCamera.transform.position,
                    outputCamera.transform.rotation,
                    cameraPivot.position,
                    cameraPivot.rotation,
                    appliedLookDelta));
            }
            else
            {
                Dispatcher.Publish(new SCameraContext(
                    cameraPivot.position,
                    cameraPivot.rotation,
                    cameraPivot.position,
                    cameraPivot.rotation,
                    appliedLookDelta));
            }
        }

        lastLookAction = SIActionLook.None;
    }

    private void ApplyLookRotationToPivot(Transform pivot, SIActionLook lookAction, out Vector2 appliedLookDelta)
    {
        appliedLookDelta = Vector2.zero;

        if (pivot == null || !lookAction.HasDelta) return;

        float rotationSpeed = gameProfile != null ? gameProfile.cameraLookRotationSpeed : 1f;
        Vector2 lookDelta = lookAction.Delta * rotationSpeed;
        appliedLookDelta = lookDelta;

        Vector3 euler = pivot.rotation.eulerAngles;
        euler.z = 0f;

        float pitch = NormalizeAngle180(euler.x);
        pitch += lookDelta.y;
        if (maxPitchDegrees > 0f)
        {
            pitch = Mathf.Clamp(pitch, -maxPitchDegrees, maxPitchDegrees);
        }

        euler.x = pitch;
        euler.y += lookDelta.x;

        pivot.rotation = Quaternion.Euler(euler);
    }

    private static float NormalizeAngle180(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;
        else if (angle < -180f)
            angle += 360f;

        return angle;
    }
}
