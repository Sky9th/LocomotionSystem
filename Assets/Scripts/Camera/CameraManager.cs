using Cinemachine;
using UnityEngine;

/// <summary>
/// Central coordinator for all runtime Cinemachine rigs. It keeps the active
/// camera snapshot in sync with GameContext so other systems can query
/// deterministic pose/FOV data without touching scene objects directly.
/// </summary>
[DisallowMultipleComponent]
public class CameraManager : BaseService
{
    [Header("Cinemachine Wiring")]
    [SerializeField] private CinemachineBrain cameraBrain;
    [SerializeField] private CinemachineVirtualCamera defaultVirtualCamera;
    [SerializeField] private bool autoLocateBrain = true;
    [SerializeField] private bool autoLocateDefaultVirtualCamera = true;

    [Header("Runtime Targets")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Transform lookAtTarget;

    private SCameraContext lastSnapshot;
    private bool hasSnapshot;
    private bool isInitialized;

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

        PushSnapshot();
        return true;
    }

    protected override void SubscribeToDispatcher()
    {
        // CameraManager no longer consumes look actions directly; LocomotionAgent handles follow rotation.
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

        PushSnapshot();
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

        lastSnapshot = new SCameraContext(
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

    public bool TryGetLatestSnapshot(out SCameraContext snapshot)
    {
        snapshot = lastSnapshot;
        return hasSnapshot;
    }

    private CinemachineVirtualCamera ResolveActiveVirtualCamera()
    {
        if (cameraBrain != null && cameraBrain.ActiveVirtualCamera is CinemachineVirtualCamera typed)
        {
            return typed;
        }

        return defaultVirtualCamera;
    }

}
