using RedDust.Core;
using RedDust.GameScene;
using UnityEngine;

namespace RedDust.Player
{
    [DisallowMultipleComponent]
    public class PlayerService : ModuleComponent, IGameplaySessionHandler
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject playerStartAnchor;

        private EventDispatcherService _dispatcher; // TODO: 替换为 EventHub — EventDispatcher 即将废弃
        private GameObject playerInstance;

        public Transform CurrentPlayerTransform =>
            playerInstance != null ? playerInstance.transform : null;

        public override void OnAssemble()
        {
        }

        private void Update()
        {
            if (playerInstance == null) return;
            GameContext.Instance?.UpdateSnapshot(SPlayer.FromTransform(playerInstance.transform, isLocalPlayer: true));
        }

        public override void OnWire()
        {
            GameContext.Instance.RegisterService(this);
            GameContext.Instance.TryResolveService(out _dispatcher);
            _dispatcher.Subscribe<SSceneLoadComplete>(HandleSceneLoadComplete);

            GameService.Instance?.NotifyServiceWired();
        }

        private void OnDestroy()
        {
            if (_dispatcher != null)
                _dispatcher.Unsubscribe<SSceneLoadComplete>(HandleSceneLoadComplete);
        }

        private void HandleSceneLoadComplete(SSceneLoadComplete evt, MetaStruct meta)
        {
            if (evt.SceneName != "Core")
                CreatePlayer();
        }

        private void CreatePlayer()
        {
            if (playerStartAnchor == null)
                playerStartAnchor = GameObject.Find("PlayerStart");

            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerService] PlayerPrefab is not assigned.", this);
                return;
            }

            playerInstance = Instantiate(playerPrefab, transform);
            playerInstance.name = playerPrefab.name;
            if (playerStartAnchor != null)
                playerInstance.transform.SetPositionAndRotation(playerStartAnchor.transform.position, playerStartAnchor.transform.rotation);

            var playerSnapshot = SPlayer.FromTransform(playerInstance.transform, isLocalPlayer: true);
            GameContext.Instance.UpdateSnapshot(playerSnapshot);
            _dispatcher.Publish(new SPlayerSpawnedEvent(playerInstance.transform, isLocalPlayer: true));
        }

        public void OnGameplaySessionEnd()
        {
            if (playerInstance != null)
            {
                Destroy(playerInstance);
                playerInstance = null;
            }
        }
    }
}
