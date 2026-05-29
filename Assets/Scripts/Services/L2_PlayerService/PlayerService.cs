using RedDust.Core;
using RedDust.GameScene;
using UnityEngine;

namespace RedDust.Player
{
    [DisallowMultipleComponent]
    public class PlayerService : BaseService, IGameplaySessionHandler
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject playerStartAnchor;

        private GameObject playerInstance;

        public Transform CurrentPlayerTransform =>
            playerInstance != null ? playerInstance.transform : null;

        protected override bool OnRegister(GameContext context)
        {
            context.RegisterService(this);
            return true;
        }

        private void Update()
        {
            if (playerInstance == null) return;
            GameContext?.UpdateSnapshot(SPlayer.FromTransform(playerInstance.transform, isLocalPlayer: true));
        }

        protected override void OnSubscriptionsActivated()
        {
            Dispatcher.Subscribe<SSceneLoadComplete>(HandleSceneLoadComplete);
        }

        private void OnDestroy()
        {
            if (Dispatcher != null)
                Dispatcher.Unsubscribe<SSceneLoadComplete>(HandleSceneLoadComplete);
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
            GameContext?.UpdateSnapshot(playerSnapshot);
            Dispatcher?.Publish(new SPlayerSpawnedEvent(playerInstance.transform, isLocalPlayer: true));
        }

        protected override void OnServicesReady() { }

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
