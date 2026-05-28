using System.Collections.Generic;
using RedDust.Character;
using RedDust.Core;
using RedDust.SceneService;
using UnityEngine;

namespace RedDust.PlayerService
{
    [DisallowMultipleComponent]
    public class PlayerService : BaseService, IGameplaySessionHandler
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject playerStartAnchor;

        private GameObject playerInstance;
        // TODO: _currentPlayerActor.LastStats is a stopgap — PlayerService should
        // collect stats through a proper push/pull interface with CharacterActor.
        private CharacterActor _currentPlayerActor;

        public Transform CurrentPlayerTransform =>
            playerInstance != null ? playerInstance.transform : null;

        public CharacterActor CurrentPlayerActor => _currentPlayerActor;

        public bool TryGetPlayerStats(out Dictionary<string, (float current, float max)> stats)
        {
            stats = _currentPlayerActor != null ? _currentPlayerActor.LastStats : null;
            return stats != null;
        }

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
            _currentPlayerActor = playerInstance.GetComponent<CharacterActor>();
            if (playerStartAnchor != null)
                playerInstance.transform.SetPositionAndRotation(playerStartAnchor.transform.position, playerStartAnchor.transform.rotation);

            var playerSnapshot = SPlayer.FromTransform(playerInstance.transform, isLocalPlayer: true);
            GameContext?.UpdateSnapshot(playerSnapshot);
            Dispatcher?.Publish(new SPlayerSpawnedEvent(playerInstance.transform, isLocalPlayer: true));
        }

        protected override void OnServicesReady() { }

        public void OnGameplaySessionEnd()
        {
            _currentPlayerActor = null;
            if (playerInstance != null)
            {
                Destroy(playerInstance);
                playerInstance = null;
            }
        }
    }
}
