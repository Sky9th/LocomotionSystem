using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace RedDust.Properties
{
    /// <summary>
    /// 单个实体实例的最终属性表。由 PropertyComponent 内部持有。
    /// 通过静态工厂 Create() 构造，运行时提供 Set / Modify / Load / Tick / Snapshot / Guard / 事件。
    /// </summary>
    public class EntityProperties
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

        public event Action<string, float, float> OnFloatChanged;
        public event Action<string> OnZero;
        public event Action<string> OnMax;
        public event Action<string, object, object> OnPropertyChanged;

        // ============================================================
        // 静态工厂
        // ============================================================

        /// <summary>从 EntityDefSO 创建。解析 Tree 结构 → 写值（覆写优先，否则取 Def 默认值）→ 推断伴生属性创建 FloatState。</summary>
        public static EntityProperties Create(EntityDefSO def)
        {
            if (def?.Template == null) { Debug.LogError("[EntityProperties] EntityDefSO or Template is null"); return null; }
            var props = new EntityProperties(def.Template.ResolveStructure());
            var overrides = ParseOverrides(def.OverridesJson);

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
                    Debug.LogWarning($"[EntityProperties] Override key '{path}' not in structure. Skipped.");

            // TODO: 行为配置入口——EntityDefSO 显式声明 consume/restore 后再创建 FloatState
            return props;
        }

        /// <summary>私有构造，只分配字典。值填充由 Build 完成。</summary>
        private EntityProperties(Dictionary<string, PropertyDefSO> structure)
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
        }

        // ============================================================
        // 读
        // ============================================================

        /// <summary>读 Float。有 FloatState 返回 Current，无返回静态值。</summary>
        public float GetFloat(string path) =>
            _floatStates.TryGetValue(path, out var s) ? s.Current : _floats.TryGetValue(path, out var f) ? f : 0f;
        public int GetInt(string path) => _ints.TryGetValue(path, out var v) ? v : 0;
        public bool GetBool(string path) => _bools.TryGetValue(path, out var v) && v;
        public string GetString(string path) => _strings.TryGetValue(path, out var v) ? v : null;
        public string[] GetTagList(string path) => _tagLists.TryGetValue(path, out var v) ? v : null;
        /// <summary>读 AssetRef 类型资产（Sprite, AnimationProfile 等）。</summary>
        public T GetAsset<T>(string path) where T : UnityEngine.Object => _assetRefs.TryGetValue(path, out var v) ? v as T : null;

        /// <summary>Float 属性的 Min 约束。</summary>
        public float GetMin(string path) => _structure.TryGetValue(path, out var d) ? d.Min : 0f;
        /// <summary>Float 属性的 Max 约束。</summary>
        public float GetMax(string path) => _structure.TryGetValue(path, out var d) ? d.Max : 0f;
        /// <summary>该属性是否存在。</summary>
        public bool Has(string path) => _structure.ContainsKey(path);

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
            if (!_structure.TryGetValue(path, out var def)) { WarnPath(path); return; }
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
            switch (def.Type)
            {
                case PropertyType.Float:
                    float f = isDefault ? def.DefaultFloat : isRaw ? ParseFloat((string)value, def) : SafeFloat(value, def.DefaultFloat);
                    f = Mathf.Clamp(f, def.Min, def.Max);
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
                            OnFloatChanged?.Invoke(path, prev, f);
                            OnPropertyChanged?.Invoke(path, prev, f);
                            if (f >= def.Max) OnMax?.Invoke(path);
                            if (f <= def.Min) OnZero?.Invoke(path);
                        }
                    }
                    break;

                case PropertyType.Int:
                    int i = isDefault ? def.DefaultInt : isRaw ? int.Parse((string)value) : SafeInt(value, def.DefaultInt);
                    i = Mathf.Clamp(i, def.MinInt, def.MaxInt);
                    int oldI = _ints.TryGetValue(path, out var pi) ? pi : 0;
                    _ints[path] = i;
                    if (!flags.HasFlag(WriteFlags.SkipEvents) && oldI != i) OnPropertyChanged?.Invoke(path, oldI, i);
                    break;

                case PropertyType.Bool:
                    bool b = isDefault ? def.DefaultBool : isRaw ? bool.Parse((string)value) : SafeBool(value, def.DefaultBool);
                    bool oldB = _bools.TryGetValue(path, out var pb) && pb;
                    _bools[path] = b;
                    if (!flags.HasFlag(WriteFlags.SkipEvents) && oldB != b) OnPropertyChanged?.Invoke(path, oldB, b);
                    break;

                case PropertyType.String:
                case PropertyType.GameplayTag:
                    string s = isDefault ? def.DefaultString : (value as string) ?? def.DefaultString;
                    string oldS = _strings.TryGetValue(path, out var ps) ? ps : null;
                    _strings[path] = s;
                    if (!flags.HasFlag(WriteFlags.SkipEvents) && oldS != s) OnPropertyChanged?.Invoke(path, oldS, s);
                    break;

                case PropertyType.GameplayTagList:
                    string[] tl = isDefault ? Array.Empty<string>() : isRaw ? ParseTagArray((string)value) : (value as string[] ?? Array.Empty<string>());
                    string[] oldTl = _tagLists.TryGetValue(path, out var ptl) ? ptl : null;
                    _tagLists[path] = tl;
                    if (!flags.HasFlag(WriteFlags.SkipEvents)) OnPropertyChanged?.Invoke(path, oldTl, tl);
                    break;

                case PropertyType.AssetRef:
                    var ar = isDefault ? LoadAssetByGuid(def.DefaultAssetGUID, def.AssetTypeConstraint)
                           : isRaw ? ResolveAssetRef((string)value, def) : ResolveAssetRef(value, def);
                    var oldAr = _assetRefs.TryGetValue(path, out var par) ? par : null;
                    _assetRefs[path] = ar;
                    if (!flags.HasFlag(WriteFlags.SkipEvents) && oldAr != ar) OnPropertyChanged?.Invoke(path, oldAr, ar);
                    break;

                case PropertyType.AssetRefList:
                    var arl = isDefault ? Array.Empty<UnityEngine.Object>()
                            : isRaw ? ResolveAssetRefList((string)value, def) : ResolveAssetRefList(value, def);
                    var oldArl = _assetRefLists.TryGetValue(path, out var parl) ? parl : null;
                    _assetRefLists[path] = arl;
                    if (!flags.HasFlag(WriteFlags.SkipEvents)) OnPropertyChanged?.Invoke(path, oldArl, arl);
                    break;
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

        /// <summary>如果 path 对应的 FloatState 不存在，创建一个空行为的。</summary>
        private void EnsureFloatState(string path)
        {
            if (_floatStates.ContainsKey(path)) return;
            if (!_structure.TryGetValue(path, out var def) || def.Type != PropertyType.Float) return;
            float v = _floats.TryGetValue(path, out var f) ? f : def.DefaultFloat;
            WireFloatState(new FloatState(path, def.Min, def.Max, v, false, 0, 0, false, 0, 0), def);
        }

        /// <summary>绑定 FloatState 事件到公开事件，并加入 _floatStates。</summary>
        private void WireFloatState(FloatState s, PropertyDefSO def)
        {
            s.OnZero += () => OnZero?.Invoke(s.Path);
            s.OnChanged += (p, old, cur) => { OnFloatChanged?.Invoke(p, old, cur); OnPropertyChanged?.Invoke(p, old, cur); if (cur >= def.Max) OnMax?.Invoke(p); };
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

        private static void WarnPath(string path) => Debug.LogWarning($"[EntityProperties] Path '{path}' not in structure.");

        private static float SafeFloat(object v, float fallback) { try { return Convert.ToSingle(v); } catch { return fallback; } }
        private static int SafeInt(object v, int fallback) { try { return Convert.ToInt32(v); } catch { return fallback; } }
        private static bool SafeBool(object v, bool fallback) { try { return Convert.ToBoolean(v); } catch { return fallback; } }

        /// <summary>从 JSON string 解析 Float 并钳制到 [Min, Max]。</summary>
        private static float ParseFloat(string raw, PropertyDefSO def) { if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) return Mathf.Clamp(f, def.Min, def.Max); Debug.LogWarning($"[EntityProperties] Bad float '{raw}'"); return def.DefaultFloat; }

        /// <summary>解析 AssetRef：Object 直接返回，GUID string 则加载资产。</summary>
        private static UnityEngine.Object ResolveAssetRef(object value, PropertyDefSO def) { if (value is UnityEngine.Object o) return o; if (value is string g && !string.IsNullOrEmpty(g)) return LoadAssetByGuid(g, def?.AssetTypeConstraint); return null; }

        /// <summary>解析 AssetRefList。</summary>
        private static UnityEngine.Object[] ResolveAssetRefList(object value, PropertyDefSO def) { if (value is UnityEngine.Object[] oa) return oa; if (value is string[] ga) { var r = new List<UnityEngine.Object>(); foreach (var g in ga) { var o = LoadAssetByGuid(g, def?.AssetTypeConstraint); if (o) r.Add(o); } return r.ToArray(); } return Array.Empty<UnityEngine.Object>(); }

        /// <summary>GUID → AssetDatabase 加载。仅 Editor 有效。</summary>
        private static UnityEngine.Object LoadAssetByGuid(string guid, string typeConstraint)
        {
            if (string.IsNullOrEmpty(guid)) return null;
#if UNITY_EDITOR
            var ap = UnityEditor.AssetDatabase.GUIDToAssetPath(guid); if (string.IsNullOrEmpty(ap)) return null;
            var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ap);
            if (obj && !string.IsNullOrEmpty(typeConstraint)) { var et = Type.GetType(typeConstraint); if (et != null && !et.IsInstanceOfType(obj)) { Debug.LogWarning("[EntityProperties] Asset type mismatch"); return null; } }
            return obj;
#else
            return null;
#endif
        }

        // JSON 序列化辅助
        [Serializable] private class OverrideEntry { public string Path; public string Value; }
        [Serializable] private class OverrideContainer { public List<OverrideEntry> Overrides = new(); }
        [Serializable] private class TagListWrapper { public string[] Items; }

        /// <summary>解析 OverridesJson：[{Path, Value}] → Dictionary。</summary>
        private static Dictionary<string, string> ParseOverrides(string json)
        {
            var r = new Dictionary<string, string>(); if (string.IsNullOrEmpty(json)) return r;
            try { var c = JsonUtility.FromJson<OverrideContainer>(json); if (c?.Overrides != null) foreach (var e in c.Overrides) { if (!string.IsNullOrEmpty(e.Path)) r[e.Path] = e.Value; } }
            catch (Exception e) { Debug.LogError($"[EntityProperties] Parse overridesJson failed: {e.Message}"); }
            return r;
        }

        /// <summary>解析 Tag 数组 JSON：["Tag.A","Tag.B"] → string[]。</summary>
        private static string[] ParseTagArray(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return Array.Empty<string>();
            try { return JsonUtility.FromJson<TagListWrapper>($"{{\"Items\":{raw}}}")?.Items ?? Array.Empty<string>(); }
            catch (Exception e) { Debug.LogWarning($"[EntityProperties] Parse tag array: {e.Message}"); return Array.Empty<string>(); }
        }
    }

}
