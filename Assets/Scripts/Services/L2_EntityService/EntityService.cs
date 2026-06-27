using System.Collections.Generic;
using System.Linq;
using RedDust.Core;
using RedDust.Properties;
using UnityEngine;

namespace RedDust.Entities
{
    /// <summary>
    /// 实体管理服务——所有 Entity 数据的唯一拥有者。
    ///
    /// 数据层：Register / Unregister / Get / All / GetByPreset —— 永远生效。
    /// GO 层：通过 SpawnRequest / DespawnRequest 通道触发，完成后发布 Spawned / Despawned。
    ///
    /// Entity 数据可以脱离 GO 存在——物品在背包、NPC 未加载时，Entity 仍在注册表。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EntityService : ModuleChildMono
    {
        [Header("Event Channels")]
        [SerializeField] private EntitySpawnRequestEvent spawnRequestEvent;
        [SerializeField] private EntitySpawnedEvent spawnedEvent;
        [SerializeField] private EntityDespawnRequestEvent despawnRequestEvent;
        [SerializeField] private EntityDespawnedEvent despawnedEvent;

        private readonly Dictionary<string, Entity> _entities = new();
        private readonly Dictionary<string, GameObject> _views = new();

        public override void OnAssemble()
        {
            GameContext.Instance.RegisterService(this);
        }

        public override void OnWire()
        {
            if (spawnRequestEvent != null) spawnRequestEvent.Register(OnSpawnRequest);
            if (despawnRequestEvent != null) despawnRequestEvent.Register(OnDespawnRequest);
        }

        private void OnDestroy()
        {
            if (spawnRequestEvent != null) spawnRequestEvent.Unregister(OnSpawnRequest);
            if (despawnRequestEvent != null) despawnRequestEvent.Unregister(OnDespawnRequest);
        }

        // ────────────────── 事件处理 ──────────────────

        private void OnSpawnRequest(SEntitySpawnRequest req)
        {
            if (req.Preset == null)
            {
                Debug.LogError("[EntityService] SpawnRequest: Preset is null.");
                return;
            }

            if (req.Preset.Prefab == null)
            {
                Debug.LogError($"[EntityService] SpawnRequest: Preset '{req.Preset.name}' has null Prefab.");
                return;
            }

            var entity = new Entity(null, req.Preset);
            Register(entity);

            var go = Instantiate(req.Preset.Prefab, req.Position, req.Rotation);
            go.name = entity.Id;

            var identity = go.GetComponent<Identity>();
            if (identity != null)
                identity.BindEntity(entity.Id);

            _views[entity.Id] = go;

            spawnedEvent?.Raise(new SEntitySpawned(entity.Id, go));
        }

        private void OnDespawnRequest(SEntityDespawnRequest req)
        {
            if (string.IsNullOrEmpty(req.EntityId)) return;

            if (!_views.TryGetValue(req.EntityId, out var go)) return;

            if (go != null) Destroy(go);
            _views.Remove(req.EntityId);

            despawnedEvent?.Raise(new SEntityDespawned(req.EntityId));
        }

        // ────────────────── 数据层 ──────────────────

        /// <summary>注册实体。Id 重复 → LogError + 返回 false。</summary>
        private bool Register(Entity entity)
        {
            if (entity == null)
            {
                Debug.LogError("[EntityService] Register: entity is null.");
                return false;
            }

            if (_entities.ContainsKey(entity.Id))
            {
                Debug.LogError($"[EntityService] Register: duplicate Id '{entity.Id}', skipped.");
                return false;
            }

            _entities[entity.Id] = entity;
            return true;
        }

        /// <summary>注销实体。同时 Despawn（如果已生成 GO）。</summary>
        public void Unregister(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            if (_views.TryGetValue(id, out var go))
            {
                if (go != null) Destroy(go);
                _views.Remove(id);
            }

            _entities.Remove(id);
        }

        /// <summary>按 Id 检索实体。未找到返回 null。</summary>
        public Entity Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _entities.TryGetValue(id, out var entity);
            return entity;
        }

        /// <summary>所有已注册实体。</summary>
        public IEnumerable<Entity> All => _entities.Values;

        /// <summary>按 Preset 类型筛选。</summary>
        public IEnumerable<Entity> GetByPreset<T>() where T : PropertyPresetSO
            => _entities.Values.Where(e => e.Preset is T);

        /// <summary>实体是否已 Spawn（当前存在 GO）。</summary>
        public bool IsSpawned(string id) => _views.ContainsKey(id);

        /// <summary>获取实体对应的 GO。未生成返回 null。</summary>
        public GameObject GetView(string id)
        {
            _views.TryGetValue(id, out var go);
            return go;
        }
    }
}
