using System;
using RedDust.Character;
using RedDust.Core;
using RedDust.Core.Events;
using RedDust.Entities;
using RedDust.GameInput;
using RedDust.GameScene;
using RedDust.Items;
using RedDust.Properties;
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

        [Header("Test — 临时生成")]
        [SerializeField] private CharacterDefSO zombieDef;
        [SerializeField] private ItemDefSO bladeDef;
        [SerializeField] private ItemDefSO pistolDef;
        [SerializeField] private ItemDefSO backpackDef;

        [Header("Event Channels")]
        [SerializeField] private EntitySpawnRequestEvent spawnRequestEvent;
        [SerializeField] private EntitySpawnedEvent spawnedEvent;
        [SerializeField] private EntityDespawnRequestEvent despawnRequestEvent;

        private EventHub _eventHub;
        private GameObject playerInstance;
        private string playerEntityId;
        private Entity _playerEntity;
        private CharacterActor _playerActor;

        // ── 持久输入状态 ──
        private EPosture _currentPosture = EPosture.Standing;
        private bool _wantsSprint;

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
            if (!GameContext.Instance.TryResolveService(out _eventHub)) return;
            _eventHub.Get<SceneLoadCompleteEvent>().Register(HandleSceneLoadComplete);

            if (spawnedEvent != null) spawnedEvent.Register(OnPlayerSpawned);

            // ── 输入绑定 ──
            BindInput<InputSecondaryInteractEvent>(_ =>
            {
                if (!TryGetMouseGround(out var pos)) return;
                _playerEntity?.Command?.MoveTo(pos);
            });
            BindInput<InputSkill1Event>(_ => _playerEntity?.Command?.UseActiveAbility(0));
            BindInput<InputSkill2Event>(_ => _playerEntity?.Command?.UseActiveAbility(1));
            BindInput<InputEquip1Event>(_ => _playerEntity?.Command?.CycleEquip(0));
            BindInput<InputEquip2Event>(_ => _playerEntity?.Command?.CycleEquip(1));
            BindInput<InputEquip3Event>(_ => _playerEntity?.Command?.CycleEquip(2));
            BindInput<InputCrouchEvent>(_ => SetPosture(EPosture.Crouching));
            BindInput<InputProneEvent>(_ => SetPosture(EPosture.Prone));
            BindInput<InputStandEvent>(_ => SetPosture(EPosture.Standing));
            BindInput<InputSprintEvent>(_ => ToggleSprint());

        }

        private void OnDestroy()
        {
            if (_eventHub != null)
                _eventHub.Get<SceneLoadCompleteEvent>().Unregister(HandleSceneLoadComplete);
            if (spawnedEvent != null) spawnedEvent.Unregister(OnPlayerSpawned);
        }

        private void HandleSceneLoadComplete(SSceneLoadComplete evt)
        {
            if (evt.SceneName != "Core")
                CreatePlayer();
        }

        private const string PlayerEntityId = "player_local";

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

            spawnRequestEvent.Raise(new SEntitySpawnRequest(characterDef, PlayerEntityId, pos, rot));
        }

        private void OnPlayerSpawned(SEntitySpawned e)
        {
            if (e.EntityId != PlayerEntityId) return;

            playerInstance = e.View;
            playerEntityId = e.EntityId;

            if (GameContext.Instance.TryResolveService<EntityService>(out var es))
                _playerEntity = es.Get(e.EntityId);
            _playerActor = e.View?.GetComponent<CharacterActor>();

            GameContext.Instance.UpdateSnapshot(
                SPlayer.FromTransform(playerInstance.transform, isLocalPlayer: true));
            _eventHub.Get<PlayerSpawnedEvent>().Raise(new SPlayerSpawnedEvent(playerInstance.transform, isLocalPlayer: true));

            StartCoroutine(SpawnTestEntitiesNextFrame());
        }

        private System.Collections.IEnumerator SpawnTestEntitiesNextFrame()
        {
            yield return null;  // 等 Start → OnWire 完成
            SpawnTestEntities();
        }

        private void SpawnTestEntities()
        {
            if (!GameContext.Instance.TryResolveService<EntityService>(out var entityService))
            {
                Debug.LogError("[PlayerService] EntityService not found — spawn aborted.");
                return;
            }

            if (_playerActor == null)
            {
                Debug.LogError("[PlayerService] CharacterActor not found on player instance.");
                return;
            }

            var container = _playerActor.BuildContext?.CharacterContainer?.BodyContainer;
            if (container == null)
            {
                Debug.LogError("[PlayerService] BodyContainer is null.");
                return;
            }

            // Zombie 在玩家右侧 3 米
            if (zombieDef != null)
            {
                var pos = playerStartAnchor.transform.position + Vector3.forward * 3f;
                pos += Vector3.up * 3f;
                spawnRequestEvent.Raise(new SEntitySpawnRequest(zombieDef, "test_zombie", pos));
                Debug.Log($"[PlayerService] Spawned zombie at {pos}");
            }
            else { Debug.LogWarning("[PlayerService] zombieDef is null."); }

            // Backpack → Back，武器进背包
            if (backpackDef == null)
            {
                Debug.LogError("[PlayerService] backpackDef is null — no backpack spawned.");
                return;
            }
            if (bladeDef == null)  Debug.LogWarning("[PlayerService] bladeDef is null.");
            if (pistolDef == null) Debug.LogWarning("[PlayerService] pistolDef is null.");

            const string backpackId = "test_backpack";
            spawnRequestEvent.Raise(new SEntitySpawnRequest(backpackDef, backpackId, null));
            var bp = entityService.Get(backpackId);
            if (bp == null)
            {
                Debug.LogError($"[PlayerService] Spawn backpack failed — entityService.Get(\"{backpackId}\") returned null.");
                return;
            }

            container.Place("Back", bp);

            // Blade → 背包
            if (bladeDef != null)
            {
                const string bladeId = "test_blade";
                spawnRequestEvent.Raise(new SEntitySpawnRequest(bladeDef, bladeId, null));
                var blade = entityService.Get(bladeId);
                if (blade != null && bp.NestedContainer != null)
                    bp.NestedContainer.Place("ContainerSlot", blade);
            }

            // Pistol → 背包
            if (pistolDef != null)
            {
                const string pistolId = "test_pistol";
                spawnRequestEvent.Raise(new SEntitySpawnRequest(pistolDef, pistolId, null));
                var pistol = entityService.Get(pistolId);
                if (pistol != null && bp.NestedContainer != null)
                    bp.NestedContainer.Place("ContainerSlot", pistol);
            }
        }

        public void OnGameplaySessionEnd()
        {
            if (!string.IsNullOrEmpty(playerEntityId))
                despawnRequestEvent?.Raise(new SEntityDespawnRequest(playerEntityId));

            playerInstance = null;
            playerEntityId = null;
            _playerEntity = null;
            _playerActor = null;
        }

        // ═══════════════════════════════════════════════════════════════
        // 输入绑定
        // ═══════════════════════════════════════════════════════════════

        private void BindInput<T>(Action<SButtonInputPayload> onPressed)
            where T : GameEvent<SButtonInputPayload>
        {
            _eventHub.Get<T>().Register(p =>
            {
                if (!p.IsRequested) return;
                onPressed(p);
            });
        }

        private static bool TryGetMouseGround(out Vector3 pos)
        {
            pos = default;
            if (GameContext.Instance == null) return false;
            if (!GameContext.Instance.TryGetSnapshot(out SCameraSnapshot cam) || !cam.IsMouseGroundValid) return false;
            pos = cam.MouseGroundPosition;
            return true;
        }

        private void SetPosture(EPosture posture) { _currentPosture = posture; WriteInputState(); }
        private void ToggleSprint() { _wantsSprint = !_wantsSprint; WriteInputState(); }

        private void WriteInputState()
        {
            if (_playerActor == null) return;
            bool hasAim = TryGetMouseGround(out var aimPoint);
            _playerActor.InputState = new SCharacterInputState
            {
                AimPoint = aimPoint,
                HasAimPoint = hasAim,
                DesiredPosture = _currentPosture,
                WantsSprint = _wantsSprint,
            };
        }
    }
}
