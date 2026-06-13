using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Properties
{
    /// <summary>
    /// GameObject 上的属性门面。一切属性操作（读/写/Modifier/Guard/事件/快照）的唯一入口。
    /// 内部持有 EntityProperties，外部不可直接访问。
    ///
    /// 其他子系统通过 GetComponent&lt;PropertyAgent&gt;() 获取引用，
    /// 不直接引用 EntityProperties、EntityDefSO、PropertyTreeSO。
    /// </summary>
    [DisallowMultipleComponent]
    public class PropertyAgent : MonoBehaviour, IPropertyReader
    {
        [Header("Properties")]
        [SerializeField] private EntityDefSO _def;

        private EntityProperties _props;

        // ====== 构造 ======

        private void Awake()
        {
            if (_def != null) Init(_def);
        }

        /// <summary>延迟初始化（CharacterActor.Start 中调用）。已初始化则覆盖。</summary>
        public void Init(EntityDefSO def)
        {
            if (def == null) return;
            _def = def;
            _props = EntityProperties.Create(def);
            _props.OnFloatChanged += (p, o, n) => OnFloatChanged?.Invoke(p, o, n);
            _props.OnZero += p => OnZero?.Invoke(p);
            _props.OnMax += p => OnMax?.Invoke(p);
            _props.OnPropertyChanged += (p, o, n) => OnPropertyChanged?.Invoke(p, o, n);
        }

        /// <summary>打印所有 Float 属性当前值。</summary>
        public void LogAll()
        {
            if (_props == null) { Debug.LogWarning("[PropertyAgent] Not initialized."); return; }
            Debug.Log($"[PropertyAgent] === {gameObject.name} Float properties ===");
            foreach (var (path, _) in _def.Template.ResolveStructure())
                if (_props.Has(path))
                    Debug.Log($"  {path}: {_props.GetFloat(path):F1} / {_props.GetMax(path):F1}");
        }

        private void Update()
        {
            _props?.Tick(Time.deltaTime);
        }

        // ====== 读 ======

        public float GetFloat(string path) => _props?.GetFloat(path) ?? 0f;
        public float GetEffectiveFloat(string path) => _props?.GetEffectiveFloat(path) ?? 0f;
        public int GetInt(string path) => _props?.GetInt(path) ?? 0;
        public bool GetBool(string path) => _props != null && _props.GetBool(path);
        public string GetString(string path) => _props?.GetString(path);
        public string[] GetTagList(string path) => _props?.GetTagList(path);
        public T GetAsset<T>(string path) where T : UnityEngine.Object => _props?.GetAsset<T>(path);
        public float GetMin(string path) => _props?.GetMin(path) ?? 0f;
        public float GetMax(string path) => _props?.GetMax(path) ?? 0f;
        public bool Has(string path) => _props != null && _props.Has(path);

        // ====== 一次性修改 ======

        public void Set(string path, object value) => _props?.Set(path, value);
        public void Modify(string path, float delta) => _props?.Modify(path, delta);
        public void Load(Dictionary<string, object> values) => _props?.Load(values);

        // ====== 持久修改器 ======

        public void AddModifier(FloatModifier mod) => _props?.AddModifier(mod);
        public void RemoveModifiers(object owner) => _props?.RemoveModifiers(owner);

        // ====== 只读修正 ======

        public void AddAdjunct(FloatAdjunct a) => _props?.AddAdjunct(a);
        public void RemoveAdjuncts(object owner) => _props?.RemoveAdjuncts(owner);

        // ====== Guard 拦截 ======

        public void AddGuard(string path, Func<float, float, bool> validate, object owner)
            => _props?.AddGuard(path, validate, owner);
        public void RemoveGuards(object owner) => _props?.RemoveGuards(owner);

        // ====== 事件 ======

        public event Action<string, float, float> OnFloatChanged;
        public event Action<string> OnZero;
        public event Action<string> OnMax;
        public event Action<string, object, object> OnPropertyChanged;

    }
}
