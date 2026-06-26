using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Properties
{
    /// <summary>
    /// GameObject 上的属性门面。一切属性操作（读/写/Modifier/Guard/事件/快照）的唯一入口。
    /// 内部持有 PropertyTable，外部不可直接访问。
    ///
    /// 其他子系统通过 GetComponent&lt;PropertyAgent&gt;() 获取引用，
    /// 不直接引用 PropertyTable、PropertyPresetSO、PropertyTreeSO。
    /// </summary>
    [DisallowMultipleComponent]
    public class PropertyAgent : MonoBehaviour, IPropertyReader
    {
        [Header("Properties")]
        [SerializeField] private PropertyPresetSO _preset;

        private PropertyTable _table;

        // ====== 构造 ======

        private void Awake()
        {
            if (_preset != null) Init(_preset);
        }

        /// <summary>延迟初始化（CharacterActor.Start 中调用）。已初始化则覆盖。</summary>
        public void Init(PropertyPresetSO preset)
        {
            if (preset == null) return;
            _preset = preset;
            _table = PropertyTable.FromPreset(preset);
            _table.OnFloatChanged += (p, o, n) => OnFloatChanged?.Invoke(p, o, n);
            _table.OnZero += p => OnZero?.Invoke(p);
            _table.OnMax += p => OnMax?.Invoke(p);
            _table.OnPropertyChanged += (p, o, n) => OnPropertyChanged?.Invoke(p, o, n);
        }

        /// <summary>打印所有 Float 属性当前值。</summary>
        public void LogAll()
        {
            if (_table == null) { Debug.LogWarning("[PropertyAgent] Not initialized."); return; }
            Debug.Log($"[PropertyAgent] === {gameObject.name} Float properties ===");
            foreach (var (path, _) in _preset.Template.ResolveStructure())
                if (_table.Has(path))
                    Debug.Log($"  {path}: {_table.GetFloat(path):F1} / {_table.GetMax(path):F1}");
        }

        private void Update()
        {
            _table?.Tick(Time.deltaTime);
        }

        // ====== 读 ======

        public float GetFloat(string path) => _table?.GetFloat(path) ?? 0f;
        public float GetEffectiveFloat(string path) => _table?.GetEffectiveFloat(path) ?? 0f;
        public int GetInt(string path) => _table?.GetInt(path) ?? 0;
        public bool GetBool(string path) => _table != null && _table.GetBool(path);
        public string GetString(string path) => _table?.GetString(path);
        public string[] GetTagList(string path) => _table?.GetTagList(path);
        public T GetAsset<T>(string path) where T : UnityEngine.Object => _table?.GetAsset<T>(path);
        public T GetStruct<T>(string path) => _table != null ? _table.GetStruct<T>(path) : default;
        public T[] GetStructArray<T>(string path) => _table?.GetStructArray<T>(path) ?? Array.Empty<T>();
        public float GetMin(string path) => _table?.GetMin(path) ?? 0f;
        public float GetMax(string path) => _table?.GetMax(path) ?? 0f;
        public bool Has(string path) => _table != null && _table.Has(path);

        // ====== 一次性修改 ======

        public void Set(string path, object value) => _table?.Set(path, value);
        public void Modify(string path, float delta) => _table?.Modify(path, delta);
        public void Load(Dictionary<string, object> values) => _table?.Load(values);

        // ====== 持久修改器 ======

        public void AddModifier(FloatModifier mod) => _table?.AddModifier(mod);
        public void RemoveModifiers(object owner) => _table?.RemoveModifiers(owner);

        // ====== 只读修正 ======

        public void AddAdjunct(FloatAdjunct a) => _table?.AddAdjunct(a);
        public void RemoveAdjuncts(object owner) => _table?.RemoveAdjuncts(owner);

        // ====== Guard 拦截 ======

        public void AddGuard(string path, Func<float, float, bool> validate, object owner)
            => _table?.AddGuard(path, validate, owner);
        public void RemoveGuards(object owner) => _table?.RemoveGuards(owner);

        // ====== 事件 ======

        public event Action<string, float, float> OnFloatChanged;
        public event Action<string> OnZero;
        public event Action<string> OnMax;
        public event Action<string, object, object> OnPropertyChanged;

    }
}
