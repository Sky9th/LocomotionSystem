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
    /// 数据层（Dictionary）：Register / Unregister / Get / All / GetByPreset —— 永远生效。
    /// GO 层：Spawn / Despawn —— 调用方请求，EntityService 执行 Instantiate/Destroy。
    ///
    /// Entity 数据可以脱离 GO 存在——物品在背包、NPC 未加载时，Entity 仍在注册表。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EntityService : ModuleChildMono
    {
        private readonly Dictionary<string, Entity> _entities = new();
        private readonly Dictionary<string, GameObject> _views = new();

        public override void OnAssemble()
        {
            GameContext.Instance.RegisterService(this);
        }

        // ────────────────── 数据层 ──────────────────

        /// <summary>注册实体。Id 重复 → LogError + 返回 false。</summary>
        public bool Register(Entity entity)
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

            Despawn(id);
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

        // ────────────────── GO 层 ──────────────────

        /// <summary>
        /// 为指定实体生成 GO 载体。
        /// 从 entity.Preset.Prefab Instantiate，设置 Identity.EntityId。
        /// 已存在 GO → 先 Destroy 旧的再实例化。
        /// </summary>
        public GameObject Spawn(string id, Vector3? position = null, Quaternion? rotation = null)
        {
            var entity = Get(id);
            if (entity == null)
            {
                Debug.LogError($"[EntityService] Spawn: entity '{id}' not found.");
                return null;
            }

            if (entity.Preset == null)
            {
                Debug.LogError($"[EntityService] Spawn: entity '{id}' has null Preset.");
                return null;
            }

            if (entity.Preset.Prefab == null)
            {
                Debug.LogError($"[EntityService] Spawn: Preset '{entity.Preset.name}' has null Prefab.");
                return null;
            }

            Despawn(id);

            var go = Instantiate(entity.Preset.Prefab, position ?? Vector3.zero, rotation ?? Quaternion.identity);
            go.name = entity.Id;

            var identity = go.GetComponent<Identity>();
            if (identity != null)
                identity.BindEntity(entity.Id);

            _views[id] = go;
            return go;
        }

        /// <summary>销毁实体对应的 GO。Entity 数据不受影响。</summary>
        public void Despawn(string id)
        {
            if (!_views.TryGetValue(id, out var go)) return;

            if (go != null) Destroy(go);
            _views.Remove(id);
        }

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
