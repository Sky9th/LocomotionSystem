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
    [SerializeField] private float cameraHeight = 15f;
    [SerializeField] private GameProfile gameProfile;

    private Transform cameraPivot;
    private bool isFollowingPlayer;

    public Transform CameraPivot => cameraPivot;

    private void Update()
    {
        if (!isFollowingPlayer) return;
        TickCameraPivot();
    }

    protected override void OnSubscriptionsActivated()
    {
        base.OnSubscriptionsActivated();

        if (Dispatcher != null)
        {
            Dispatcher.Subscribe<SPlayerSpawnedEvent>(HandlePlayerSpawned);
        }
    }

    private void OnDestroy()
    {
        if (Dispatcher != null)
        {
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
    }

    public void OnGameplaySessionEnd()
    {
        isFollowingPlayer = false;
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

            StripCinemachineProceduralComponents();
            return;
        }

        Debug.LogWarning("[CameraService] No default virtual camera assigned.", this);
    }

    private void StripCinemachineProceduralComponents()
    {
        if (defaultVirtualCamera == null) return;

        foreach (var comp in defaultVirtualCamera.GetComponents<CinemachineComponentBase>())
            Destroy(comp);

        var collider = defaultVirtualCamera.GetComponent<CinemachineCollider>();
        if (collider != null) Destroy(collider);
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

    private void TickCameraPivot()
    {
        if (cameraPivot == null) return;
        if (GameContext == null || !GameContext.TryGetSnapshot(out SPlayer player)) return;

        Vector3 pivotPos = player.Character.Position;
        cameraPivot.position = pivotPos;
        cameraPivot.rotation = Quaternion.Euler(90f, 0f, 0f);

        if (defaultVirtualCamera != null)
        {
            var vcamT = defaultVirtualCamera.transform;
            vcamT.position = pivotPos + Vector3.up * cameraHeight;
            vcamT.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        var mouseGround = ComputeMouseGroundPosition();

        var outputCamera = cameraBrain != null ? cameraBrain.OutputCamera : null;
        Vector3 camPos, anchorPos;
        Quaternion camRot, anchorRot;

        if (outputCamera != null)
        {
            camPos = outputCamera.transform.position;
            camRot = outputCamera.transform.rotation;
        }
        else
        {
            camPos = cameraPivot.position;
            camRot = cameraPivot.rotation;
        }

        anchorPos = cameraPivot.position;
        anchorRot = cameraPivot.rotation;

        PublishState(new SCameraContext(
            camPos, camRot, anchorPos, anchorRot,
            Vector2.zero, mouseGround.WorldPosition, mouseGround.IsValid));
    }

    private (Vector3 WorldPosition, bool IsValid) ComputeMouseGroundPosition()
    {
        var outputCamera = cameraBrain != null ? cameraBrain.OutputCamera : null;
        if (outputCamera == null) return (Vector3.zero, false);

        var ray = outputCamera.ScreenPointToRay(Input.mousePosition);
        var groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
            return (ray.GetPoint(distance), true);

        return (Vector3.zero, false);
    }
}
