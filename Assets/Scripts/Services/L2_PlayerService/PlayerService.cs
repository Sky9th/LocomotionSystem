using RedDust.Character;
using RedDust.Core;
using RedDust.Entities;
using RedDust.GameScene;
using UnityEngine;

namespace RedDust.Player
{
    [DisallowMultipleComponent]
    public class PlayerService : ModuleChildMono, IGameplaySessionHandler
    {
        [Header("Identity")]
        [SerializeField] private CharacterDefSO characterDef;

        [Header("Spawn")]
        [SerializeField] private GameObject playerStartAnchor;

        [Header("Event Channels")]
        [SerializeField] private EntitySpawnRequestEvent spawnRequestEvent;
        [SerializeField] private EntitySpawnedEvent spawnedEvent;
        [SerializeField] private EntityDespawnRequestEvent despawnRequestEvent;

        private EventDispatcherService _dispatcher;
        private GameObject playerInstance;
        private string playerEntityId;

        public Transform CurrentPlayerTransform =>
            playerInstance != null ? playerInstance.transform : null;

        public override void OnAssemble()
        {
            GameContext.Instance.RegisterService(this);
        }

        private void Update()
        {
            if (playerInstance == null) return;
            GameContext.Instance?.UpdateSnapshot(SPlayer.FromTransform(playerInstance.transform, isLocalPlayer: true));
        }

        public override void OnWire()
        {
            GameContext.Instance.TryResolveService(out _dispatcher);
            _dispatcher.Subscribe<SSceneLoadComplete>(HandleSceneLoadComplete);

            if (spawnedEvent != null) spawnedEvent.Register(OnPlayerSpawned);
        }

        private void OnDestroy()
        {
            if (_dispatcher != null)
                _dispatcher.Unsubscribe<SSceneLoadComplete>(HandleSceneLoadComplete);
            if (spawnedEvent != null) spawnedEvent.Unregister(OnPlayerSpawned);
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

            if (characterDef == null)
            {
                Debug.LogError("[PlayerService] CharacterDef is not assigned.", this);
                return;
            }

            if (spawnRequestEvent == null)
            {
                Debug.LogError("[PlayerService] SpawnRequestEvent channel is not assigned.", this);
                return;
            }

            var pos = playerStartAnchor != null
                ? playerStartAnchor.transform.position : Vector3.zero;
            var rot = playerStartAnchor != null
                ? playerStartAnchor.transform.rotation : Quaternion.identity;

            spawnRequestEvent.Raise(new SEntitySpawnRequest(characterDef, pos, rot));
        }

        private void OnPlayerSpawned(SEntitySpawned e)
        {
            playerInstance = e.View;
            playerEntityId = e.EntityId;

            GameContext.Instance.UpdateSnapshot(
                SPlayer.FromTransform(playerInstance.transform, isLocalPlayer: true));
            _dispatcher.Publish(new SPlayerSpawnedEvent(playerInstance.transform, isLocalPlayer: true));
        }

        public void OnGameplaySessionEnd()
        {
            if (!string.IsNullOrEmpty(playerEntityId))
                despawnRequestEvent?.Raise(new SEntityDespawnRequest(playerEntityId));

            playerInstance = null;
            playerEntityId = null;
        }
    }
}
