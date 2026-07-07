using System;
using System.Collections.Generic;
using System.Globalization;
using RedDust.Core;
using UnityEngine;

namespace RedDust.Properties
{
    /// <summary>
    /// 运行时属性平表。静态工厂 FromPreset() 从 PropertyPresetSO 构造。
    /// 提供 Get/Set/Modify/Load/Tick/Guard/事件。
    /// </summary>
    public class PropertyTable
    {
        private readonly Dictionary<string, PropertyDefSO> _structure;
        private readonly Dictionary<string, float> _floats;
        private readonly Dictionary<string, int> _ints;
        private readonly Dictionary<string, bool> _bools;
        private readonly Dictionary<string, string> _strings;
        private readonly Dictionary<string, string[]> _tagLists;
        private readonly Dictionary<string, UnityEngine.Object> _assetRefs;
        private readonly Dictionary<string, UnityEngine.Object[]> _assetRefLists;
        private readonly Dictionary<string, FloatState> _floatStates;
        private readonly Dictionary<string, List<Guard>> _guards;
        private readonly Dictionary<string, List<FloatModifier>> _modifiers;
        private readonly Dictionary<string, List<FloatAdjunct>> _adjuncts;
        private readonly Dictionary<string, string> _structJsons;
        private readonly Dictionary<string, float> _minOverrides;
        private readonly Dictionary<string, float> _maxOverrides;

        public event Action<string> OnZero;
        public event Action<string> OnMax;
        public event Action<string, object, object> OnPropertyChanged;

        // ============================================================
        // 静态工厂
        // ============================================================

        /// <summary>从 PropertyPresetSO 创建 Table。解析 Tree 结构 → 写值（覆写优先，否则取 Def 默认值）。</summary>
        public static PropertyTable FromPreset(PropertyPresetSO preset)
        {
            var tree = ResolveTree(preset);
            if (tree == null) { Debug.LogError($"[PropertyTable] Cannot resolve PropertyTreeSO for preset '{preset?.name}' — both templateId and serialized Template are null."); return null; }
            var props = new PropertyTable(tree.ResolveStructure());
            var overrides = ParseOverrides(preset.OverridesJson, props._minOverrides, props._maxOverrides);

            foreach (var (path, d) in props._structure)
            {
                if (d == null) continue;
                if (overrides.TryGetValue(path, out var raw))
                    props.WriteFromRaw(path, raw, d);
                else
                    props.WriteDefault(path, d);
            }

            foreach (var (path, _) in overrides)
                if (!props._structure.ContainsKey(path))
                    Debug.LogWarning($"[PropertyTable] Override key '{path}' not in structure. Skipped.");

            // TODO: 行为配置入口——PropertyPresetSO 显式声明 consume/restore 后再创建 FloatState
            return props;
        }

        /// <summary>
        /// Resolve a PropertyTreeSO for the preset.
        /// If preset.templateId is set → look up from PropertyTreeRegistry (Addressables path).
        /// Otherwise fall back to preset.Template (serialized reference, backward compatible).
        /// </summary>
        private static PropertyTreeSO ResolveTree(PropertyPresetSO preset)
        {
            if (!string.IsNullOrEmpty(preset.templateId))
            {
                var fromRegistry = GameService.Instance.Assets.FindPropertyTree(preset.templateId);
                if (fromRegistry != null)
                    return fromRegistry;
                Debug.LogWarning($"[PropertyTable] templateId='{preset.templateId}' not found in registry, falling back to serialized Template.");
            }
            return preset.Template;
        }

        /// <summary>私有构造，只分配字典。值填充由 Build 完成。</summary>
        private PropertyTable(Dictionary<string, PropertyDefSO> structure)
        {
            _structure = structure;
            _floats = new();
            _ints = new();
            _bools = new();
            _strings = new();
            _tagLists = new();
            _assetRefs = new();
            _assetRefLists = new();
            _floatStates = new();
            _guards = new();
            _modifiers = new();
            _adjuncts = new();
            _structJsons = new();
            _minOverrides = new();
            _maxOverrides = new();
        }

        // ============================================================
        // 读
        // ============================================================

        /// <summary>读 Float。返回原始 Current（不含 Adjunct 修正）。路径不存在报错。</summary>
        public float GetFloat(string path)
        {
            if (_floatStates.TryGetValue(path, out var s)) return s.Current;
            if (_floats.TryGetValue(path, out var f)) return f;
            if (!_structure.ContainsKey(path)) { ErrorPath(path); return 0f; }
            return 0f; // 路径合法但尚未写入（如构造期 DoWrite 取旧值）
        }

        public float GetEffectiveFloat(string path)
        {
            if (_floatStates.TryGetValue(path, out var s)) return s.Effective;
            if (_floats.TryGetValue(path, out var f)) return f;
            if (!_structure.ContainsKey(path)) { ErrorPath(path); return 0f; }
            return 0f;
        }

        public int GetInt(string path)
        {
            if (_ints.TryGetValue(path, out var v)) return v;
            if (!_structure.ContainsKey(path)) { ErrorPath(path); return 0; }
            return 0;
        }

        public bool GetBool(string path)
        {
            if (_bools.TryGetValue(path, out var v)) return v;
            if (!_structure.ContainsKey(path)) { ErrorPath(path); return false; }
            return false;
        }

        public string GetString(string path)
        {
            if (_strings.TryGetValue(path, out var v)) return v;
            if (!_structure.ContainsKey(path)) { ErrorPath(path); return null; }
            return null;
        }

        public string[] GetTagList(string path)
        {
            if (_tagLists.TryGetValue(path, out var v)) return v;
            if (!_structure.ContainsKey(path)) { ErrorPath(path); return null; }
            return null;
        }

        public T GetAsset<T>(string path) where T : UnityEngine.Object
        {
            if (_assetRefs.TryGetValue(path, out var v)) return v as T;
            if (!_structure.ContainsKey(path)) { ErrorPath(path); return null; }
            return null;
        }

        public T[] GetAssetList<T>(string path) where T : UnityEngine.Object
        {
            if (_assetRefLists.TryGetValue(path, out var v))
            {
                var result = new T[v.Length];
                for (int i = 0; i < v.Length; i++)
                    result[i] = v[i] as T;
                return result;
            }
            if (!_structure.ContainsKey(path)) { ErrorPath(path); return null; }
            return null;
        }

        /// <summary>Float 属性的 Min 约束。路径不存在报错。覆写优先于 Def。</summary>
        public float GetMin(string path) => _structure.TryGetValue(path, out var d)
            ? (_minOverrides.TryGetValue(path, out var min) ? min : ((FloatPropertyDefSO)d).Min)
            : ErrorPath<float>(path);
        /// <summary>Float 属性的 Max 约束。路径不存在报错。覆写优先于 Def。</summary>
        public float GetMax(string path) => _structure.TryGetValue(path, out var d)
            ? (_maxOverrides.TryGetValue(path, out var max) ? max : ((FloatPropertyDefSO)d).Max)
            : ErrorPath<float>(path);
        /// <summary>该属性是否存在。</summary>
        public bool Has(string path) => _structure.ContainsKey(path);

        // TODO: TryGetPath 当前是 O(n) 全表扫描。属性数增长到数百个时，Ability Cost 每帧调用会成为瓶颈。
        // 方案：构造时建反向索引 Dictionary<PropertyDefSO, string> _pathByDef，TryGetPath 降为 O(1)。
        // 注意 PropertyDefSO 可能被多个路径共享（同一 def 挂在不同节点）→ 用 List<string> 或只取第一个。
        /// <summary>通过 PropertyDefSO 反查路径。用于 Ability Cost 等持有 def 但需要完整路径的场景。</summary>
        public bool TryGetPath(PropertyDefSO def, out string path)
        {
            foreach (var kv in _structure)
            {
                if (kv.Value == def)
                {
                    path = kv.Key;
                    return true;
                }
            }
            path = null;
            return false;
        }

        /// <summary>内部取 Float Min——覆写优先，路径不存在返回 0（调用方保证路径合法）。</summary>
        private float EffectiveMin(string path)
        {
            if (_minOverrides.TryGetValue(path, out var min)) return min;
            return _structure.TryGetValue(path, out var d) ? ((FloatPropertyDefSO)d).Min : 0f;
        }

        /// <summary>内部取 Float Max——覆写优先，路径不存在返回 0（调用方保证路径合法）。</summary>
        private float EffectiveMax(string path)
        {
            if (_maxOverrides.TryGetValue(path, out var max)) return max;
            return _structure.TryGetValue(path, out var d) ? ((FloatPropertyDefSO)d).Max : 0f;
        }

        /// <summary>
        /// 返回 parentPath 文件夹下所有直接子属性的完整路径。
        /// parentPath 为空或 "" 时返回根级属性。
        /// 例：GetChildren("Slots") → ["Slots/RightHand", "Slots/LeftHand", ...]
        /// </summary>
        public IEnumerable<string> GetChildren(string parentPath)
        {
            var prefix = string.IsNullOrEmpty(parentPath) ? "" : parentPath + "/";
            var seen = new HashSet<string>();
            foreach (var key in _structure.Keys)
            {
                if (!key.StartsWith(prefix)) continue;
                var relative = key.Substring(prefix.Length);
                var slash = relative.IndexOf('/');
                var child = slash >= 0 ? relative.Substring(0, slash) : relative;
                if (seen.Add(child))
                    yield return string.IsNullOrEmpty(parentPath) ? child : prefix + child;
            }
        }

        // ============================================================
        // 修改 —— 统一类型分发
        // ============================================================

        /// <summary>设值，统一入口。走 Guard 拦截 + 事件广播。</summary>
        public void Set(string path, object value) => WriteFromObject(path, value, WriteFlags.None);
        /// <summary>Float 增量快捷方式。等价于 Set(path, current + delta)。</summary>
        public void Modify(string path, float delta) => Set(path, GetFloat(path) + delta);
        /// <summary>全量设值。跳过 Guard 和事件，用于读档/重生。</summary>
        public void Load(Dictionary<string, object> values)
        {
            foreach (var (p, v) in values) WriteFromObject(p, v, WriteFlags.SkipGuards | WriteFlags.SkipEvents);
        }

        /// <summary>写入模式：None = 走 Guard + 事件，SkipGuards = 跳过拦截，SkipEvents = 跳过广播。</summary>
        [Flags]
        private enum WriteFlags { None = 0, SkipGuards = 1, SkipEvents = 2 }

        /// <summary>运行时写入入口（object → 类型分发）。</summary>
        private void WriteFromObject(string path, object value, WriteFlags flags)
        {
            if (!_structure.TryGetValue(path, out var def)) { ErrorPath(path); return; }
            DoWrite(path, value, def, flags);
        }

        /// <summary>工厂构造时从 raw string 写入。</summary>
        private void WriteFromRaw(string path, string raw, PropertyDefSO def) =>
            DoWrite(path, raw, def, WriteFlags.SkipGuards | WriteFlags.SkipEvents, isRaw: true);

        /// <summary>工厂构造时写入默认值。</summary>
        private void WriteDefault(string path, PropertyDefSO def) =>
            DoWrite(path, null, def, WriteFlags.SkipGuards | WriteFlags.SkipEvents, isDefault: true);

        /// <summary>唯一的类型分发写入口。isRaw: 值来自 JSON string → 需解析。isDefault: 值来自 Def 默认值。</summary>
        private void DoWrite(string path, object value, PropertyDefSO def, WriteFlags flags, bool isRaw = false, bool isDefault = false)
        {
            object newValue = def.ComputeWriteValue(value, isRaw, isDefault);

            // Float has unique runtime: FloatState, guards, multiple events
            if (def is FloatPropertyDefSO fd)
            {
                float f = Mathf.Clamp((float)newValue, EffectiveMin(path), EffectiveMax(path));
                float oldF = GetFloat(path);
                if (!flags.HasFlag(WriteFlags.SkipGuards) && !RunGuards(path, oldF, f)) return;

                if (_floatStates.TryGetValue(path, out var fs))
                {
                    if (flags.HasFlag(WriteFlags.SkipEvents)) fs.SetCurrentSilent(f);
                    else if (Math.Abs(fs.Current - f) > 0.001f) { fs.SetCurrent(f); OnPropertyChanged?.Invoke(path, oldF, f); }
                }
                else
                {
                    float prev = _floats.TryGetValue(path, out var pf) ? pf : 0f;
                    _floats[path] = f;
                    if (!flags.HasFlag(WriteFlags.SkipEvents) && Math.Abs(prev - f) > 0.001f)
                    {
                        OnPropertyChanged?.Invoke(path, prev, f);
                        if (f >= EffectiveMax(path)) OnMax?.Invoke(path);
                        if (f <= EffectiveMin(path)) OnZero?.Invoke(path);
                    }
                }
                return;
            }

            // All other types: simple dict write + single event
            WriteSimpleTyped(path, def.Type, newValue, flags);
        }

        /// <summary>简单类型写入：选字典 → 写值 → 发事件（仅 OnPropertyChanged）。</summary>
        private void WriteSimpleTyped(string path, PropertyType type, object newValue, WriteFlags flags)
        {
            switch (type)
            {
                case PropertyType.Int:
                {
                    int i = (int)newValue;
                    int oldI = _ints.TryGetValue(path, out var pi) ? pi : 0;
                    _ints[path] = i;
                    if (!flags.HasFlag(WriteFlags.SkipEvents) && oldI != i) OnPropertyChanged?.Invoke(path, oldI, i);
                    break;
                }
                case PropertyType.Bool:
                {
                    bool b = (bool)newValue;
                    bool oldB = _bools.TryGetValue(path, out var pb) && pb;
                    _bools[path] = b;
                    if (!flags.HasFlag(WriteFlags.SkipEvents) && oldB != b) OnPropertyChanged?.Invoke(path, oldB, b);
                    break;
                }
                case PropertyType.String:
                case PropertyType.RdTag:
                {
                    string s = (string)newValue;
                    string oldS = _strings.TryGetValue(path, out var ps) ? ps : null;
                    _strings[path] = s;
                    if (!flags.HasFlag(WriteFlags.SkipEvents) && oldS != s) OnPropertyChanged?.Invoke(path, oldS, s);
                    break;
                }
                case PropertyType.RdTagList:
                {
                    string[] tl = (string[])newValue;
                    string[] oldTl = _tagLists.TryGetValue(path, out var ptl) ? ptl : null;
                    _tagLists[path] = tl;
                    if (!flags.HasFlag(WriteFlags.SkipEvents)) OnPropertyChanged?.Invoke(path, oldTl, tl);
                    break;
                }
                case PropertyType.AssetRef:
                {
                    var ar = (UnityEngine.Object)newValue;
                    var oldAr = _assetRefs.TryGetValue(path, out var par) ? par : null;
                    _assetRefs[path] = ar;
                    if (!flags.HasFlag(WriteFlags.SkipEvents) && oldAr != ar) OnPropertyChanged?.Invoke(path, oldAr, ar);
                    break;
                }
                case PropertyType.AssetRefList:
                {
                    var arl = (UnityEngine.Object[])newValue;
                    var oldArl = _assetRefLists.TryGetValue(path, out var parl) ? parl : null;
                    _assetRefLists[path] = arl;
                    if (!flags.HasFlag(WriteFlags.SkipEvents)) OnPropertyChanged?.Invoke(path, oldArl, arl);
                    break;
                }
                case PropertyType.Struct:
                {
                    var json = (string)newValue;
                    var oldJson = _structJsons.TryGetValue(path, out var oj) ? oj : null;
                    _structJsons[path] = json;
                    if (!flags.HasFlag(WriteFlags.SkipEvents) && json != oldJson) OnPropertyChanged?.Invoke(path, oldJson, json);
                    break;
                }
            }
        }

        // ============================================================
        // Modifier
        // ============================================================

        /// <summary>注入持久修改器。若目标尚无 FloatState，自动懒创建。</summary>
        public void AddModifier(FloatModifier mod)
        {
            if (mod == null) return;
            if (!_modifiers.TryGetValue(mod.TargetPath, out var list)) _modifiers[mod.TargetPath] = list = new();
            list.Add(mod);
            EnsureFloatState(mod.TargetPath);
            _floatStates[mod.TargetPath].AddModifier(mod);
        }

        /// <summary>按 Owner 批量移除修改器。</summary>
        public void RemoveModifiers(object owner)
        {
            foreach (var list in _modifiers.Values) list.RemoveAll(m => m.Owner == owner);
            foreach (var s in _floatStates.Values) s.RemoveModifiers(owner);
        }

        /// <summary>注入只读修正。若目标尚无 FloatState，自动懒创建。</summary>
        public void AddAdjunct(FloatAdjunct a)
        {
            if (a == null) return;
            if (!_adjuncts.TryGetValue(a.TargetPath, out var list)) _adjuncts[a.TargetPath] = list = new();
            list.Add(a);
            EnsureFloatState(a.TargetPath);
            _floatStates[a.TargetPath].AddAdjunct(a);
        }

        /// <summary>按 Owner 批量移除只读修正。</summary>
        public void RemoveAdjuncts(object owner)
        {
            foreach (var list in _adjuncts.Values) list.RemoveAll(a => a.Owner == owner);
            foreach (var s in _floatStates.Values) s.RemoveAdjuncts(owner);
        }

        /// <summary>如果 path 对应的 FloatState 不存在，创建一个空行为的。</summary>
        private void EnsureFloatState(string path)
        {
            if (_floatStates.ContainsKey(path)) return;
            if (!_structure.TryGetValue(path, out var def) || def.Type != PropertyType.Float) return;
            float v = _floats.TryGetValue(path, out var f) ? f : ((FloatPropertyDefSO)def).DefaultValue;
            float effMin = EffectiveMin(path);
            float effMax = EffectiveMax(path);
            WireFloatState(new FloatState(path, effMin, effMax, v, false, 0, 0, false, 0, 0), effMax);
        }

        /// <summary>绑定 FloatState 事件到公开事件，并加入 _floatStates。effectiveMax 用于 OnMax 判定。</summary>
        private void WireFloatState(FloatState s, float effectiveMax)
        {
            s.OnZero += () => OnZero?.Invoke(s.Path);
            s.OnChanged += (p, old, cur) => { OnPropertyChanged?.Invoke(p, old, cur); if (cur >= effectiveMax) OnMax?.Invoke(p); };
            _floatStates[s.Path] = s;
        }

        // ============================================================
        // Guard — 修改前拦截
        // ============================================================

        /// <summary>注册修改前拦截器。validate(old, new) 返回 false 阻止修改。用于"禁止治疗"等。</summary>
        public void AddGuard(string path, Func<float, float, bool> validate, object owner)
        {
            if (!_guards.TryGetValue(path, out var list)) _guards[path] = list = new();
            list.Add(new Guard { Owner = owner, Validate = validate });
        }

        /// <summary>按 Owner 批量移除拦截器。</summary>
        public void RemoveGuards(object owner) { foreach (var list in _guards.Values) list.RemoveAll(g => g.Owner == owner); }

        /// <summary>遍历 Guard 链，任一返回 false 即阻止写入。</summary>
        private bool RunGuards(string path, float oldVal, float newVal)
        {
            if (!_guards.TryGetValue(path, out var list)) return true;
            foreach (var g in list) { if (!g.Validate(oldVal, newVal)) return false; }
            return true;
        }

        private struct Guard { public object Owner; public Func<float, float, bool> Validate; }

        // ============================================================
        // Tick / Snapshot
        // ============================================================

        /// <summary>每帧驱动所有 FloatState 消耗/恢复/Modifier。</summary>
        public void Tick(float dt) { foreach (var s in _floatStates.Values) s.Tick(dt); }


        // ============================================================
        // 内部工具
        // ============================================================

        private static void ErrorPath(string path) => Debug.LogError($"[PropertyTable] Path '{path}' not found. Check property path string for typos.");
        private static T ErrorPath<T>(string path) { ErrorPath(path); return default; }


        // ============================================================
        // Struct 读取
        // ============================================================

        /// <summary>校验 StructTypeName 与泛型 T 是否一致。不一致报错返回 true。</summary>
        private bool StructTypeMismatch<T>(string path)
        {
            if (!_structure.TryGetValue(path, out var def) || def.Type != PropertyType.Struct)
                return false;

            return !((StructPropertyDefSO)def).TypeMatches<T>();
        }

        /// <summary>从 JSON 反序列化单个 struct。类型不匹配或 JSON 无效返回 default。</summary>
        public T GetStruct<T>(string path)
        {
            if (StructTypeMismatch<T>(path)) return default;
            if (!_structJsons.TryGetValue(path, out var json) || string.IsNullOrEmpty(json))
                return default;
            return JsonUtility.FromJson<T>(json);
        }

        /// <summary>从 JSON 反序列化 struct 数组。内部自动处理 {Items:[...]} 包装。</summary>
        public T[] GetStructArray<T>(string path)
        {
            if (StructTypeMismatch<T>(path)) return Array.Empty<T>();
            if (!_structJsons.TryGetValue(path, out var json) || string.IsNullOrEmpty(json))
                return Array.Empty<T>();
            var wrapper = JsonUtility.FromJson<StructArrayWrapper<T>>(json);
            return wrapper?.Items ?? Array.Empty<T>();
        }

        [Serializable]
        private class StructArrayWrapper<T> { public T[] Items; }

        // JSON 序列化辅助
        [Serializable] private class OverrideEntry { public string Path; public string Value; public string Min; public string Max; }
        [Serializable] private class OverrideContainer { public List<OverrideEntry> Overrides = new(); }
        /// <summary>解析 OverridesJson：[{Path, Value, Min?, Max?}] → (值字典, 填充约束字典)。</summary>
        private static Dictionary<string, string> ParseOverrides(string json, Dictionary<string, float> minOverrides, Dictionary<string, float> maxOverrides)
        {
            var r = new Dictionary<string, string>(); if (string.IsNullOrEmpty(json)) return r;
            try
            {
                var c = JsonUtility.FromJson<OverrideContainer>(json);
                if (c?.Overrides != null)
                    foreach (var e in c.Overrides)
                    {
                        if (string.IsNullOrEmpty(e.Path)) continue;
                        if (e.Value != null) r[e.Path] = e.Value;
                        if (!string.IsNullOrEmpty(e.Min) && float.TryParse(e.Min, NumberStyles.Float, CultureInfo.InvariantCulture, out var min))
                            minOverrides[e.Path] = min;
                        if (!string.IsNullOrEmpty(e.Max) && float.TryParse(e.Max, NumberStyles.Float, CultureInfo.InvariantCulture, out var max))
                            maxOverrides[e.Path] = max;
                    }
            }
            catch (Exception e) { Debug.LogError($"[PropertyTable] Parse overridesJson failed: {e.Message}"); }
            return r;
        }
    }

}
