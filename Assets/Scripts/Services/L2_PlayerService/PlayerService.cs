using System;
using RedDust.Character;
using RedDust.Core;
using RedDust.Core.Events;
using RedDust.Entities;
using RedDust.GameInput;
using RedDust.GameScene;
using RedDust.Prop;
using RedDust.Properties;
using RedDust.Weapon;
using UnityEngine;

namespace RedDust.Player
{
    [DisallowMultipleComponent]
    public class PlayerService : ModuleChildMono, IGameplaySessionHandler
    {
        [Header("Identity")]
        [Tooltip("CharacterDefSO asset name，从 Assets 查找。")]
        [SerializeField] private string characterDefKey = "Human";

        [Header("Spawn")]
        [SerializeField] private GameObject playerStartAnchor;

        [Header("Test — 临时生成")]
        [Tooltip("CharacterDefSO key")]
        [SerializeField] private string zombieDefKey = "Zombie";
        [Tooltip("ItemDefSO key，从 Assets 查找。")]
        [SerializeField] private string bladeDefKey = "Blade";
        [SerializeField] private string pistolDefKey = "Pistol";
        [SerializeField] private string backpackDefKey = "Backpack";

        [Header("Event Channels")]
        [SerializeField] private EntitySpawnRequestEvent spawnRequestEvent;
        [SerializeField] private EntitySpawnedEvent spawnedEvent;
        [SerializeField] private EntityDespawnRequestEvent despawnRequestEvent;

        private EventHub _eventHub;
        private GameObject playerInstance;
        private string playerEntityId;
        private Entity _playerEntity;

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
            _eventHub.Get<SceneTransitionEvent>().Register(HandleSceneTransition);

            if (spawnedEvent != null) spawnedEvent.Register(OnPlayerSpawned);

            // ── 输入绑定 ──
            BindInput<InputSecondaryInteractEvent>(_ =>
            {
                if (!TryGetMouseGround(out var pos)) return;
                _playerEntity?.Command?.MoveTo(pos);
            });
            BindInput<InputSkillEvent>(p => _playerEntity?.Command?.UseActiveAbility(p.BindingIndex));
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
                _eventHub.Get<SceneTransitionEvent>().Unregister(HandleSceneTransition);
            if (spawnedEvent != null) spawnedEvent.Unregister(OnPlayerSpawned);
        }

        private void HandleSceneTransition(SSceneTransition evt)
        {
            if (evt.Phase == SceneTransitionPhase.Completed && evt.SceneName != "Core" && evt.SceneName != "MainMenu")
                CreatePlayer();
        }

        private const string PlayerEntityId = "player_local";

        private void CreatePlayer()
        {
            if (playerStartAnchor == null)
                playerStartAnchor = GameObject.Find("PlayerStart");

            var characterDef = GameService.Instance.Assets.FindCharacter(characterDefKey);
            if (characterDef == null)
            {
                Debug.LogError($"[PlayerService] CharacterDef '{characterDefKey}' not found in Assets.", this);
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

            GameContext.Instance.UpdateSnapshot(
                SPlayer.FromTransform(playerInstance.transform, isLocalPlayer: true));
            _eventHub.Get<PlayerSpawnedEvent>().Raise(new SPlayerSpawnedEvent(playerInstance.transform, _playerEntity.Id, isLocalPlayer: true));

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

            if (_playerEntity == null)
            {
                Debug.LogError("[PlayerService] Player entity not found.");
                return;
            }

            // Zombie 在玩家右侧 3 米
            var zombieDef = GameService.Instance.Assets.FindCharacter(zombieDefKey);
            if (zombieDef != null)
            {
                var pos = playerStartAnchor.transform.position + Vector3.forward * 3f;
                pos += Vector3.up * 3f;
                spawnRequestEvent.Raise(new SEntitySpawnRequest(zombieDef, "test_zombie", pos));
                Debug.Log($"[PlayerService] Spawned zombie at {pos}");
            }
            else { Debug.LogWarning($"[PlayerService] CharacterDef '{zombieDefKey}' not found in Registry."); }

            // Backpack → Back，武器进背包
            var backpackDef = GameService.Instance.Assets.FindItem<ContainerSO>(backpackDefKey);
            if (backpackDef == null)
            {
                Debug.LogError($"[PlayerService] ContainerDef '{backpackDefKey}' not found in Assets — no backpack spawned.");
                return;
            }
            var bladeDef = GameService.Instance.Assets.FindItem<MeleeWeaponSO>(bladeDefKey);
            if (bladeDef == null) Debug.LogWarning($"[PlayerService] bladeDef '{bladeDefKey}' not found in Assets.");
            var pistolDef = GameService.Instance.Assets.FindItem<RangedWeaponSO>(pistolDefKey);
            if (pistolDef == null) Debug.LogWarning($"[PlayerService] pistolDef '{pistolDefKey}' not found in Assets.");

            const string backpackId = "test_backpack";
            spawnRequestEvent.Raise(new SEntitySpawnRequest(backpackDef, backpackId, null));
            var bp = entityService.Get(backpackId);
            if (bp == null)
            {
                Debug.LogError($"[PlayerService] Spawn backpack failed.", this);
                return;
            }

            _playerEntity.Command.Place("Back", bp);

            // Blade → 背包
            if (bladeDef != null)
            {
                const string bladeId = "test_blade";
                spawnRequestEvent.Raise(new SEntitySpawnRequest(bladeDef, bladeId, null));
                var blade = entityService.Get(bladeId);
                if (blade != null)
                    bp.Command.Place("ContainerSlot", blade);
            }

            // Pistol → 背包
            if (pistolDef != null)
            {
                const string pistolId = "test_pistol";
                spawnRequestEvent.Raise(new SEntitySpawnRequest(pistolDef, pistolId, null));
                var pistol = entityService.Get(pistolId);
                if (pistol != null)
                    bp.Command.Place("ContainerSlot", pistol);
            }
        }

        public void OnGameplaySessionEnd()
        {
            if (!string.IsNullOrEmpty(playerEntityId))
                despawnRequestEvent?.Raise(new SEntityDespawnRequest(playerEntityId));

            playerInstance = null;
            playerEntityId = null;
            _playerEntity = null;
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
            if (_playerEntity == null) return;
            bool hasAim = TryGetMouseGround(out var aimPoint);
            _playerEntity.Command.SetInputState(new SCharacterInputState
            {
                AimPoint = aimPoint,
                HasAimPoint = hasAim,
                DesiredPosture = _currentPosture,
                WantsSprint = _wantsSprint,
            });
        }
    }
}
