using RedDust.Core.GameContext;
using RedDust.Core.GameService;
using RedDust.Core.Structs;
using RedDust.Core.Modules;
using Cinemachine;
using RedDust.Core.Events;
using RedDust.Shared;
using UnityEngine;

namespace RedDust.Services.Camera
{
    [DefaultExecutionOrder(-400)]
    [DisallowMultipleComponent]
    public class CameraService : ModuleChildMono, IGameplaySessionHandler
    {
        [Header("Cinemachine Wiring")]
        [SerializeField] private CinemachineBrain cameraBrain;
        [SerializeField] private CinemachineVirtualCamera defaultVirtualCamera;
        [SerializeField] private bool autoLocateBrain = true;
        [SerializeField] private bool autoLocateDefaultVirtualCamera = true;

        [SerializeField] private GameProfileSO gameProfile;

        [SerializeField] private GameObject anchorPrefab;

        private EventHub _eventHub;
        private LogChannel _log;
        private Transform cameraPivot;
        private bool isFollowingPlayer;

        public Transform CameraPivot => cameraPivot;

        private void Update()
        {
            if (!isFollowingPlayer) return;
            TickCameraPivot();
        }

        public override void OnAssemble()
        {
            _log = LogManager.GetChannel(GetType().Name);

            ValidateConfiguration();

            if (!EnsureCinemachineBrain())
            {
                _log.Error("CinemachineBrain not found — CameraService assembly failed.");
                return;
            }

            EnsureDefaultVirtualCamera();
            CreateCameraPivot();
            InitializeDefaultRig();

            GameContext.Instance.RegisterService(this);
        }

        public override void OnWire()
        {
            if (!GameContext.Instance.TryResolveService(out _eventHub)) return;
            _eventHub.Get<PlayerSpawnedEvent>().Register(HandlePlayerSpawned);
        }

        private void UpdateSnapshot<TSnapshot>(TSnapshot snapshot) where TSnapshot : struct
        {
            GameContext.Instance.UpdateSnapshot(snapshot);
        }

        private void OnDestroy()
        {
            if (_eventHub != null)
            {
                _eventHub.Get<PlayerSpawnedEvent>().Unregister(HandlePlayerSpawned);
            }
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

        private void HandlePlayerSpawned(SPlayerSpawnedEvent evt)
        {
            if (!evt.IsLocalPlayer) return;
            if (cameraPivot == null)
            {
                CreateCameraPivot();
                InitializeDefaultRig();
            }

            // 读取 Body 的 FollowOffset
            Vector3 followOffset = Vector3.zero;
            if (defaultVirtualCamera != null)
            {
                var body = defaultVirtualCamera.GetCinemachineComponent<CinemachineTransposer>();
                if (body != null) followOffset = body.m_FollowOffset;
            }

            // 瞬移摄像机到 玩家位置 + FollowOffset
            var outputCamera = cameraBrain != null ? cameraBrain.OutputCamera : null;
            if (outputCamera != null)
                outputCamera.transform.position = evt.Root.position + followOffset;

            cameraPivot.position = evt.Root.position;
            if (defaultVirtualCamera != null)
                defaultVirtualCamera.PreviousStateIsValid = false;

            isFollowingPlayer = true;
        }

        private void CreateCameraPivot()
        {
            if (anchorPrefab != null)
            {
                var obj = Instantiate(anchorPrefab, transform);
                obj.name = CommonConstants.FollowAnchorName;
                cameraPivot = obj.transform;
            }
            else
            {
                var obj = new GameObject(CommonConstants.FollowAnchorName);
                obj.transform.SetParent(transform, false);
                cameraPivot = obj.transform;
            }
        }

        private void ValidateConfiguration()
        {
            if (gameProfile == null)
            {
                Debug.LogError("[CameraService] Missing GameProfileSO reference.", this);
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
            var mainCamera = UnityEngine.Camera.main;
            if (mainCamera != null && mainCamera.TryGetComponent(out CinemachineBrain brainOnMain))
            {
                return brainOnMain;
            }

            return FindObjectOfType<CinemachineBrain>();
        }

        private void TickCameraPivot()
        {
            if (cameraPivot == null) return;
            if (GameContext.Instance == null || !GameContext.Instance.TryGetSnapshot(out SPlayer player)) return;

            Vector3 pivotPos = player.Character.Position;
            cameraPivot.position = pivotPos;

            var mouseGround = ComputeMouseGroundPosition();

            if (mouseGround.IsValid)
            {
                var dir = mouseGround.WorldPosition - pivotPos;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                {
                    var angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                    cameraPivot.rotation = Quaternion.Euler(0f, angle, 0f);
                }
            }

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

            var snapshot = new SCameraSnapshot(
                camPos, camRot, anchorPos, anchorRot,
                Vector2.zero, mouseGround.WorldPosition, mouseGround.IsValid);
            UpdateSnapshot(snapshot);
        }

        private (Vector3 WorldPosition, bool IsValid) ComputeMouseGroundPosition()
        {
            var outputCamera = cameraBrain != null ? cameraBrain.OutputCamera : null;
            if (outputCamera == null) return (Vector3.zero, false);

            var ray = outputCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            var groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
                return (ray.GetPoint(distance), true);

            return (Vector3.zero, false);
        }
    }
}
