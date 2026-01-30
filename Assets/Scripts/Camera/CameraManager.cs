using Cinemachine;
using UnityEngine;

/// <summary>
/// Central coordinator for all runtime Cinemachine rigs. It keeps the active
/// camera snapshot in sync with GameContext so other systems can query
/// deterministic pose/FOV data without touching scene objects directly.
/// </summary>
[DisallowMultipleComponent]
public class CameraManager : RuntimeServiceBase
{
    [Header("Cinemachine Wiring")]
    [SerializeField] private CinemachineBrain cameraBrain;
    [SerializeField] private CinemachineVirtualCamera defaultVirtualCamera;
    [SerializeField] private bool autoLocateBrain = true;
    [SerializeField] private bool autoLocateDefaultVirtualCamera = true;

    [Header("Runtime Targets")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Transform lookAtTarget;

    private CameraContextStruct lastSnapshot;
    private bool hasSnapshot;
    private bool isInitialized;
    private EventDispatcher eventDispatcher;
    private PlayerLookIntentStruct lastLookIntent;
    private MetaStruct lastLookMeta;
    private bool hasLookIntent;

    public bool IsInitialized => isInitialized;
    public CinemachineVirtualCamera ActiveVirtualCamera => ResolveActiveVirtualCamera();

    protected override bool OnRegister(GameContext context)
    {
        if (isInitialized)
        {
            return true;
        }

        if (autoLocateBrain && cameraBrain == null)
        {
            cameraBrain = FindFirstBrain();
        }

        if (cameraBrain == null)
        {
            Debug.LogError("CameraManager could not locate a CinemachineBrain. Please assign one in the inspector.", this);
            return false;
        }

        if (autoLocateDefaultVirtualCamera && defaultVirtualCamera == null)
        {
            defaultVirtualCamera = GetComponentInChildren<CinemachineVirtualCamera>(true);
        }

        if (defaultVirtualCamera != null)
        {
            BindTargets(defaultVirtualCamera);
            defaultVirtualCamera.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("CameraManager does not have a default virtual camera assigned.", this);
        }

        context.RegisterService(this);
        isInitialized = true;

        TryBindEventDispatcher(context);
        PushSnapshot();
        return true;
    }

    private CinemachineBrain FindFirstBrain()
    {
        var mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.TryGetComponent(out CinemachineBrain brainOnMain))
        {
            return brainOnMain;
        }

        return FindObjectOfType<CinemachineBrain>();
    }

    private void LateUpdate()
    {
        if (!isInitialized)
        {
            return;
        }

        TryBindEventDispatcher(GameContext);
        PushSnapshot();
    }

    private void HandlePlayerLookIntent(PlayerLookIntentStruct intent, MetaStruct meta)
    {
        if (!isInitialized)
        {
            return;
        }

        lastLookIntent = intent;
        lastLookMeta = meta;
        hasLookIntent = true;

        RotateFollowTarget(intent);
    }

    private void RotateFollowTarget(PlayerLookIntentStruct intent)
    {
        if (followTarget == null)
        {
            return;
        }

        var euler = followTarget.rotation.eulerAngles;
        euler.z = 0f; // Prevent roll
        var pitch = NormalizeAngle(euler.x);
        pitch = Mathf.Clamp(pitch + intent.Delta.y, -85f, 85f);
        euler.x = pitch;
        euler.y += intent.Delta.x;
        followTarget.rotation = Quaternion.Euler(euler);
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
        {
            angle -= 360f;
        }

        if (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }

    private void TryBindEventDispatcher(GameContext context)
    {
        if (eventDispatcher != null || context == null)
        {
            return;
        }

        if (context.TryResolveService<EventDispatcher>(out var dispatcher))
        {
            eventDispatcher = dispatcher;
            eventDispatcher.Subscribe<PlayerLookIntentStruct>(HandlePlayerLookIntent);
        }
    }

    private void PushSnapshot()
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

        lastSnapshot = new CameraContextStruct(
            outputCamera.transform.position,
            outputCamera.transform.rotation,
            outputCamera.fieldOfView,
            outputCamera.nearClipPlane,
            outputCamera.farClipPlane,
            outputCamera.orthographic,
            outputCamera.orthographicSize);

        hasSnapshot = true;
        GameContext.UpdateSnapshot(lastSnapshot);
    }

    private void BindTargets(CinemachineVirtualCamera virtualCamera)
    {
        if (virtualCamera == null)
        {
            return;
        }

        if (followTarget != null)
        {
            virtualCamera.Follow = followTarget;
        }

        if (lookAtTarget != null)
        {
            virtualCamera.LookAt = lookAtTarget;
        }
    }

    public void SetFollowTarget(Transform target, bool retargetActiveCamera = true)
    {
        followTarget = target;
        if (retargetActiveCamera)
        {
            var activeCamera = ResolveActiveVirtualCamera();
            if (activeCamera != null)
            {
                activeCamera.Follow = followTarget;
            }
        }
    }

    public void SetLookAtTarget(Transform target, bool retargetActiveCamera = true)
    {
        lookAtTarget = target;
        if (retargetActiveCamera)
        {
            var activeCamera = ResolveActiveVirtualCamera();
            if (activeCamera != null)
            {
                activeCamera.LookAt = lookAtTarget;
            }
        }
    }

    public bool TryGetLatestSnapshot(out CameraContextStruct snapshot)
    {
        snapshot = lastSnapshot;
        return hasSnapshot;
    }

    public bool TryGetLastLookIntent(out PlayerLookIntentStruct intent, out MetaStruct meta)
    {
        intent = lastLookIntent;
        meta = lastLookMeta;
        return hasLookIntent;
    }

    private CinemachineVirtualCamera ResolveActiveVirtualCamera()
    {
        if (cameraBrain != null && cameraBrain.ActiveVirtualCamera is CinemachineVirtualCamera typed)
        {
            return typed;
        }

        return defaultVirtualCamera;
    }

    private void OnDestroy()
    {
        if (eventDispatcher != null)
        {
            eventDispatcher.Unsubscribe<PlayerLookIntentStruct>(HandlePlayerLookIntent);
            eventDispatcher = null;
        }
    }
}
