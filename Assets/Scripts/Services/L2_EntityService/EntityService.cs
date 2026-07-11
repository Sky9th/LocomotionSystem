using RedDust.Core.GameContext;
using RedDust.Core.Modules;
using System.Collections.Generic;
using System.Linq;
using RedDust.Gameplay.Container;
using RedDust.Core.Events;
using RedDust.Gameplay.Properties;
using RedDust.Gameplay.Character;
using UnityEngine;

namespace RedDust.Services.EntityService
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

        [Header("Prefabs")]
        [SerializeField] private GameObject defaultItemPrefab;

        private readonly Dictionary<string, Entity> _entities = new();

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
            if (req.Preset == null && string.IsNullOrEmpty(req.EntityId))
            {
                Debug.LogError("[EntityService] SpawnRequest: both Preset and EntityId are null.");
                return;
            }

            // ── 新 Entity 创建 ──
            if (req.Preset != null)
            {
                var id = req.EntityId ?? System.Guid.NewGuid().ToString();
                var entity = new Entity(id, req.Preset);
                Register(entity);

                if (req.Position.HasValue)
                {
                    var go = CreateGameObject(entity, req.Position.Value, req.Rotation);
                    entity.View = go;
                    spawnedEvent?.Raise(new SEntitySpawned(entity.Id, entity.Preset, go));
                }
                else
                {
                    spawnedEvent?.Raise(new SEntitySpawned(entity.Id, entity.Preset, null));
                }
                return;
            }

            // ── 已有 Entity，生成 GO ──
            var existing = Get(req.EntityId);
            if (existing == null)
            {
                Debug.LogError($"[EntityService] SpawnRequest: entity '{req.EntityId}' not found.");
                return;
            }

            if (!req.Position.HasValue)
            {
                Debug.LogError($"[EntityService] SpawnRequest: Position required when Preset is null.");
                return;
            }

            if (existing.HasView)
            {
                Debug.LogError($"[EntityService] SpawnRequest: entity '{req.EntityId}' already has a View.");
                return;
            }

            var go2 = CreateGameObject(existing, req.Position.Value, req.Rotation);
            existing.View = go2;
            spawnedEvent?.Raise(new SEntitySpawned(existing.Id, existing.Preset, go2));
        }

        private GameObject CreateGameObject(Entity entity, Vector3 pos, Quaternion rot)
        {
            GameObject go;
            if (entity.Preset is CharacterDefSO)
            {
                // TODO: 角色后续也走 Common/VisualPrefab（同物品路径）
                // TODO: 远期角色使用模块化装配，届时 CreateGameObject 再重构
                if (entity.Preset.Prefab == null)
                {
                    Debug.LogError($"[EntityService] Character '{entity.Preset.name}' has null Prefab.");
                    return null;
                }
                go = Instantiate(entity.Preset.Prefab, pos, rot);
                go.name = entity.Id;
                var identity = go.GetComponent<Identity>() ?? go.AddComponent<Identity>();
                identity.BindEntity(entity.Id);
                identity.Entity = entity;
                identity.SetProperties(entity.Properties);
                return go;
            }

            // 物品：VisualPrefab → defaultItemPrefab → Cube
            var visualPrefab = entity.Properties.GetAsset<GameObject>("Common/VisualPrefab");
            if (visualPrefab != null)
                go = Instantiate(visualPrefab, pos, rot);
            else if (defaultItemPrefab != null)
                go = Instantiate(defaultItemPrefab, pos, rot);
            else
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.localScale = Vector3.one * 0.3f;

            go.name = entity.Id;
            go.transform.position = pos;
            go.transform.rotation = rot;
            var id = go.AddComponent<Identity>();
            id.BindEntity(entity.Id);
            id.Entity = entity;
            id.SetProperties(entity.Properties);
            return go;
        }

        private void OnDespawnRequest(SEntityDespawnRequest req)
        {
            if (string.IsNullOrEmpty(req.EntityId)) return;

            var entity = Get(req.EntityId);
            if (entity == null || !entity.HasView) return;

            var go = entity.View;
            entity.View = null;
            Destroy(go);

            despawnedEvent?.Raise(new SEntityDespawned(req.EntityId, go));
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
            TryCreateNestedContainer(entity);
            return true;
        }

        /// <summary>注销实体。级联清理嵌套容器子实体。</summary>
        public void Unregister(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            if (_entities.TryGetValue(id, out var entity))
            {
                if (entity.NestedContainer != null)
                {
                    foreach (var child in entity.NestedContainer.AllItems())
                        Unregister(child.Id);
                    entity.NestedContainer = null;
                }

                if (entity.HasView)
                {
                    Destroy(entity.View);
                    entity.View = null;
                }
                _entities.Remove(id);
            }
        }

        private void TryCreateNestedContainer(Entity entity)
        {
            var slotDefs = new List<SlotDef>();
            foreach (var path in entity.Properties.GetChildren("Slots"))
            {
                var def = entity.Properties.GetStruct<SlotDef>(path);
                def.SlotId = path.Substring(path.LastIndexOf('/') + 1);
                slotDefs.Add(def);
            }

            if (slotDefs.Count == 0)
            {
                return;
            }

            entity.NestedContainer = new RdContainer($"{entity.Id}/Storage", slotDefs.ToArray());
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

        /// <summary>所有已生成 GO 的实体。</summary>
        public IEnumerable<Entity> GetSpawnedEntities()
            => _entities.Values.Where(e => e.HasView);
    }
}
