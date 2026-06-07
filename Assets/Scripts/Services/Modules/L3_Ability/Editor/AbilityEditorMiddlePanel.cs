#if UNITY_EDITOR
using System;
using RedDust.Core;
using RedDust.Core.Editor;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;

namespace RedDust.Ability
{
    /// <summary>
    /// 中间栏渲染器。
    ///
    ///   Section 卡片模式（对齐 TagEditorWindow helpBox 卡片）:
    ///     BeginVertical(helpBox) → Space(Pad) → title → Space(Pad) → body → Space(Pad) → EndVertical
    ///
    ///   Body 内容模式（对齐 TagEditorWindow.DrawTagDetails）:
    ///     BeginHorizontal → Space(Pad) → BeginVertical → [fields] → EndVertical → Space(Pad) → EndHorizontal
    ///
    ///   Field rows:  plain BeginHorizontal, 字段间无多余间距
    ///   Groups 间:   Space(Pad)
    /// </summary>
    public static class AbilityEditorMiddlePanel
    {
        private const float Pad = 6f;
        private static Action _onChanged;
        public delegate void EditSubAssetHandler(SubAssetSlot slot);
        public delegate void ClearSubAssetHandler(SubAssetSlot slot);
        public delegate void RemoveEffectHandler(int index, bool isTargetEffects);

        // ═══════════════════════════════════════════════════════════
        // Empty
        // ═══════════════════════════════════════════════════════════
        public static void DrawEmpty()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var greyLabel = new GUIStyle(EditorStyles.label)
                { alignment = TextAnchor.MiddleCenter, fontSize = 13,
                  normal = { textColor = Color.grey } };
            EditorGUILayout.LabelField(
                "Select an ability from the left panel,\nor create a new one with ＋ Create New.",
                greyLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        // ═══════════════════════════════════════════════════════════
        // Edit
        // ═══════════════════════════════════════════════════════════
        public static void DrawEdit(AbilitySO ability, EditSubAssetHandler onEditSubAsset,
            ClearSubAssetHandler onClearSubAsset, RemoveEffectHandler onRemoveEffect,
            Action onChanged = null)
        {
            if (ability == null) { DrawEmpty(); return; }

            _onChanged = onChanged;
            DrawSection("Identity", () => DrawIdentityFields(ability));
            if (ability is AbilityDefSO def)
            {
                DrawSection("Activation",
                    () => DrawSubAssetSlot(def.activation, GetActivationSummary(def.activation),
                        SubAssetSlot.Activation, onEditSubAsset, onClearSubAsset));
                DrawSection("Search",
                    () => DrawSubAssetSlot(def.search, GetSearchSummary(def.search),
                        SubAssetSlot.Search, onEditSubAsset, onClearSubAsset));
                DrawSection($"Target Effects [{def.targetEffects?.Length ?? 0}]",
                    () => DrawEffectList(def.targetEffects, SubAssetSlot.TargetEffects,
                        onEditSubAsset, i => onRemoveEffect(i, true)));
                DrawSection($"Self Effects [{def.selfEffects?.Length ?? 0}]",
                    () => DrawEffectList(def.selfEffects, SubAssetSlot.SelfEffects,
                        onEditSubAsset, i => onRemoveEffect(i, false)));
                DrawSection("Noise",
                    () => DrawSubAssetSlot(def.noise, GetNoiseSummary(def.noise),
                        SubAssetSlot.Noise, onEditSubAsset, onClearSubAsset));
                DrawSection("Tags", () => DrawTagFields(def));
                DrawSection("Cooldown", () => DrawCooldownFields(def));
                DrawSection($"Combo Links [{def.comboLinks?.Length ?? 0}]",
                    () => DrawComboList(def));
            }
            else if (ability is PassiveAbilitySO passive)
            {
                DrawSection("Tags", () => DrawTagFields(passive));
                DrawSection("Trigger", () => DrawPassiveFields(passive));
                DrawSection($"Target Effects [{passive.targetEffects?.Length ?? 0}]",
                    () => DrawEffectList(passive.targetEffects, SubAssetSlot.TargetEffects,
                        onEditSubAsset, i => onRemoveEffect(i, true)));
                DrawSection($"Self Effects [{passive.selfEffects?.Length ?? 0}]",
                    () => DrawEffectList(passive.selfEffects, SubAssetSlot.SelfEffects,
                        onEditSubAsset, i => onRemoveEffect(i, false)));
                DrawSection("Cooldown", () => DrawCooldownFields(passive));
            }
        }

        private static void DrawSection(string title, Action drawBody)
        {
            EditorUIUtility.DrawCard(Pad, () =>
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                GUILayout.Space(Pad);
                drawBody();
            });
            EditorUIUtility.CardGap(Pad);
        }

        // ═══════════════════════════════════════════════════════════
        // Identity
        // ═══════════════════════════════════════════════════════════
        private static void DrawIdentityFields(AbilitySO a)
        {
            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(a, "internalName", 90);
            var v = EditorGUILayout.TextField(a.internalName ?? "");
            if (v != a.internalName) { a.internalName = v; EditorUtility.SetDirty(a); _onChanged?.Invoke(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(a, "displayName", 90);
            v = EditorGUILayout.TextField(a.displayName ?? "");
            if (v != a.displayName) { a.displayName = v; EditorUtility.SetDirty(a); _onChanged?.Invoke(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(a, "icon", 90);
            var icon = (Sprite)EditorGUILayout.ObjectField(a.icon, typeof(Sprite), false);
            if (icon != a.icon) { a.icon = icon; EditorUtility.SetDirty(a); _onChanged?.Invoke(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(a, "description", 90);
            var desc = EditorGUILayout.TextArea(a.description ?? "", GUILayout.Height(48));
            if (desc != a.description) { a.description = desc; EditorUtility.SetDirty(a); _onChanged?.Invoke(); }
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════════════
        // 单引用子资产槽位
        // ═══════════════════════════════════════════════════════════
        private static void DrawSubAssetSlot(
            ScriptableObject asset, string summary, SubAssetSlot slot,
            EditSubAssetHandler onEdit, ClearSubAssetHandler onClear)
        {
            EditorGUILayout.BeginHorizontal();

            if (asset != null)
            {
                EditorGUILayout.LabelField(asset.name, EditorStyles.label,
                    GUILayout.ExpandWidth(true));
                GUILayout.Space(Pad);

                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20)))
                    onClear?.Invoke(slot);
                if (GUILayout.Button("...", EditorStyles.miniButton, GUILayout.Width(30)))
                    onEdit?.Invoke(slot);
            }
            else
            {
                var grey = new GUIStyle(EditorStyles.label)
                    { normal = { textColor = Color.grey } };
                EditorGUILayout.LabelField("— (none)", grey, GUILayout.ExpandWidth(true));

                GUI.backgroundColor = new Color(0.4f, 0.7f, 0.4f);
                if (GUILayout.Button("...", EditorStyles.miniButton, GUILayout.Width(30)))
                    onEdit?.Invoke(slot);
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            if (asset != null && !string.IsNullOrEmpty(summary))
            {
                GUILayout.Space(2f);
                var s = new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = Color.grey } };
                EditorGUILayout.LabelField(summary, s);
            }

            if (asset != null)
            {
                var s = new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } };
                EditorGUILayout.LabelField(asset.GetType().Name, s);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // EffectSO[] 列表
        // ═══════════════════════════════════════════════════════════
        private static void DrawEffectList(
            EffectSO[] effects, SubAssetSlot slot, EditSubAssetHandler onEdit,
            Action<int> onRemove)
        {
            if (effects != null)
            {
                for (var i = 0; i < effects.Length; i++)
                {
                    if (i > 0) EditorUIUtility.CardGap(Pad);
                    var e = effects[i];

                    EditorUIUtility.DrawCard(Pad, () =>
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(GetEffectIcon(e), EditorStyles.label,
                            GUILayout.Width(36));

                        var name = e != null ? e.name : "(missing)";
                        var st = e != null ? EditorStyles.label
                            : new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } };
                        EditorGUILayout.LabelField(name, st, GUILayout.ExpandWidth(true));

                        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                        if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20)))
                            onRemove?.Invoke(i);
                        GUI.backgroundColor = Color.white;
                        EditorGUILayout.EndHorizontal();

                        if (e != null)
                        {
                            var ss = new GUIStyle(EditorStyles.miniLabel)
                                { normal = { textColor = Color.grey } };
                            EditorGUILayout.LabelField(GetEffectSummary(e), ss);
                        }
                    });
                }
            }

            if (effects == null || effects.Length == 0)
            {
                var grey = new GUIStyle(EditorStyles.label)
                    { normal = { textColor = Color.grey } };
                EditorGUILayout.LabelField("(empty)", grey);
            }

            EditorUIUtility.CardGap(Pad);
            GUI.backgroundColor = new Color(0.4f, 0.7f, 0.4f);
            if (GUILayout.Button("＋ Add Effect", GUILayout.Height(22)))
                onEdit?.Invoke(slot);
            GUI.backgroundColor = Color.white;
        }

        // ═══════════════════════════════════════════════════════════
        // Tags
        // ═══════════════════════════════════════════════════════════
        private static void DrawTagFields(AbilitySO a)
        {
            // abilityTag — 基类字段，主动被动通用
            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(a, "abilityTag", 90, "Ability");
            var at = (GameplayTagDefinitionSO)EditorGUILayout.ObjectField(
                a.abilityTag, typeof(GameplayTagDefinitionSO), false);
            if (at != a.abilityTag) { a.abilityTag = at; EditorUtility.SetDirty(a); _onChanged?.Invoke(); }
            if (GUILayout.Button("Tag", EditorStyles.miniButton, GUILayout.Width(35)))
            {
                var r = GUIUtility.GUIToScreenRect(GUILayoutUtility.GetLastRect());
                TagPicker.Show(r, allowCreate: true, currentFullTag: a.abilityTag?.FullTag,
                    onSelected: t => { if (a.abilityTag != t) { a.abilityTag = t; EditorUtility.SetDirty(a); _onChanged?.Invoke(); } });
            }
            EditorGUILayout.EndHorizontal();

            if (a is AbilityDefSO def)
            {
                EditorGUILayout.BeginHorizontal();
                EditorUIUtility.LabelWithTooltip(def, "overrideExclusion", 90, "Override Exclusion");
                var ex = EditorGUILayout.Toggle(def.overrideExclusion);
                if (ex != def.overrideExclusion) { def.overrideExclusion = ex; EditorUtility.SetDirty(def); _onChanged?.Invoke(); }
                EditorGUILayout.EndHorizontal();
            }

            // sharedCooldownTag — 基类字段
            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(a, "sharedCooldownTag", 90, "Shared CD");
            v = (GameplayTagDefinitionSO)EditorGUILayout.ObjectField(
                a.sharedCooldownTag, typeof(GameplayTagDefinitionSO), false);
            if (v != a.sharedCooldownTag) { a.sharedCooldownTag = v; EditorUtility.SetDirty(a); _onChanged?.Invoke(); }
            if (GUILayout.Button("Tag", EditorStyles.miniButton, GUILayout.Width(35)))
            {
                var r = GUIUtility.GUIToScreenRect(GUILayoutUtility.GetLastRect());
                TagPicker.Show(r, allowCreate: true, currentFullTag: a.sharedCooldownTag?.FullTag,
                    onSelected: t => { if (a.sharedCooldownTag != t) { a.sharedCooldownTag = t; EditorUtility.SetDirty(a); _onChanged?.Invoke(); } });
            }
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════════════
        // Cooldown
        // ═══════════════════════════════════════════════════════════
        private static void DrawCooldownFields(AbilitySO a)
        {
            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(a, "cooldownDuration", 90, "Duration (s)");
            var v = EditorGUILayout.FloatField(a.cooldownDuration);
            if (Mathf.Abs(v - a.cooldownDuration) > 0.001f)
            { a.cooldownDuration = v; EditorUtility.SetDirty(a); _onChanged?.Invoke(); }
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════════════
        // Passive
        // ═══════════════════════════════════════════════════════════
        private static void DrawPassiveFields(PassiveAbilitySO p)
        {
            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(p, "trigger", 90);
            var v = (ETriggerEvent)EditorGUILayout.EnumPopup(p.trigger);
            if (v != p.trigger) { p.trigger = v; EditorUtility.SetDirty(p); _onChanged?.Invoke(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(p, "triggerValue", 90, "Trigger Value");
            var f = EditorGUILayout.FloatField(p.triggerValue);
            if (Mathf.Abs(f - p.triggerValue) > 0.001f)
            { p.triggerValue = f; EditorUtility.SetDirty(p); _onChanged?.Invoke(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(p, "triggerChannel", 90, "Channel");
            var ch = (EventChannelBase)EditorGUILayout.ObjectField(
                p.triggerChannel, typeof(EventChannelBase), false);
            if (ch != p.triggerChannel) { p.triggerChannel = ch; EditorUtility.SetDirty(p); _onChanged?.Invoke(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorUIUtility.LabelWithTooltip(p, "targetRequiredTag", 90, "Target Tag");
            var tag = (GameplayTagDefinitionSO)EditorGUILayout.ObjectField(
                p.targetRequiredTag, typeof(GameplayTagDefinitionSO), false);
            if (tag != p.targetRequiredTag) { p.targetRequiredTag = tag; EditorUtility.SetDirty(p); _onChanged?.Invoke(); }
            if (GUILayout.Button("Tag", EditorStyles.miniButton, GUILayout.Width(35)))
            {
                var r = GUIUtility.GUIToScreenRect(GUILayoutUtility.GetLastRect());
                TagPicker.Show(r, allowCreate: true, currentFullTag: p.targetRequiredTag?.FullTag,
                    onSelected: t => { if (p.targetRequiredTag != t) { p.targetRequiredTag = t; EditorUtility.SetDirty(p); _onChanged?.Invoke(); } });
            }
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════════════
        // Combo
        // ═══════════════════════════════════════════════════════════
        private static void DrawComboList(AbilityDefSO def)
        {
            var links = def.comboLinks;
            if (links != null)
            {
                for (var i = 0; i < links.Length; i++)
                {
                    if (i > 0) GUILayout.Space(4f);
                    var l = links[i];

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("→", EditorStyles.label, GUILayout.Width(14));

                    var next = (AbilityDefSO)EditorGUILayout.ObjectField(
                        l.NextSkill, typeof(AbilityDefSO), false, GUILayout.Width(140));
                    if (next != l.NextSkill) { l.NextSkill = next; def.comboLinks[i] = l; EditorUtility.SetDirty(def); _onChanged?.Invoke(); }

                    EditorGUILayout.LabelField("Start", GUILayout.Width(35));
                    var ns = EditorGUILayout.FloatField(l.WindowStart, GUILayout.Width(40));
                    if (Mathf.Abs(ns - l.WindowStart) > 0.001f)
                    { l.WindowStart = ns; def.comboLinks[i] = l; EditorUtility.SetDirty(def); _onChanged?.Invoke(); }

                    EditorGUILayout.LabelField("Dur", GUILayout.Width(25));
                    var nd = EditorGUILayout.FloatField(l.WindowDuration, GUILayout.Width(40));
                    if (Mathf.Abs(nd - l.WindowDuration) > 0.001f)
                    { l.WindowDuration = nd; def.comboLinks[i] = l; EditorUtility.SetDirty(def); _onChanged?.Invoke(); }

                    var bp = EditorGUILayout.Toggle(l.BypassCooldown, GUILayout.Width(16));
                    if (bp != l.BypassCooldown) { l.BypassCooldown = bp; def.comboLinks[i] = l; EditorUtility.SetDirty(def); _onChanged?.Invoke(); }
                    EditorGUILayout.LabelField("BypassCD", EditorStyles.miniLabel, GUILayout.Width(58));

                    GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                    if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20)))
                        RemoveComboLink(def, i);
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (links == null || links.Length == 0)
            {
                var grey = new GUIStyle(EditorStyles.label)
                    { normal = { textColor = Color.grey } };
                EditorGUILayout.LabelField("(no combo links)", grey);
            }

            GUILayout.Space(Pad);
            if (GUILayout.Button("＋ Add Combo Link", GUILayout.Height(22)))
                AddComboLink(def);
        }

        // ═══════════════════════════════════════════════════════════
        // 摘要
        // ═══════════════════════════════════════════════════════════
        private static string GetActivationSummary(AbilityActivationSO a)
            => a == null ? null : $"{a.activationType} · speed:{a.animationSpeed:F1}";

        private static string GetSearchSummary(AbilitySearchSO s)
            => s == null ? null : $"{s.searchType} · range:{s.range:F1} · max:{s.maxTargets}";

        private static string GetEffectSummary(EffectSO e)
        {
            if (e == null) return null;
            if (e is DamageEffectSO d)
                return $"Damage · {e.effectTag?.FullTag ?? "—"} · base:{d.baseDamage:F0} · dur:{e.duration:F1}s";
            if (e is ImpactEffectSO i)
                return $"Impact · {e.effectTag?.FullTag ?? "—"} · stagger:{i.staggerValue:F0}";
            if (e is ExecuteEffectSO x)
                return $"Execute · {e.effectTag?.FullTag ?? "—"} · threshold:{x.hpThreshold:P0}";
            if (e is CostEffectSO c)
                return $"Cost · {c.statDef?.name ?? "—"} · amount:{c.amount:F0}";
            return $"{e.GetType().Name.Replace("EffectSO", "")} · {e.effectTag?.FullTag ?? "—"}";
        }

        private static string GetNoiseSummary(NoiseEventSO n)
            => n == null ? null : $"level:{n.level:F0} · decay:{n.decayRadius:F1}m";

        private static string GetEffectIcon(EffectSO e)
        {
            if (e == null) return "?";
            if (e is DamageEffectSO) return "Dmg";
            if (e is ImpactEffectSO) return "Imp";
            if (e is ExecuteEffectSO) return "Exe";
            if (e is CostEffectSO) return "Cost";
            return "*";
        }

        private static void AddComboLink(AbilityDefSO def)
        {
            var links = def.comboLinks ?? Array.Empty<SComboLink>();
            var arr = new SComboLink[links.Length + 1];
            Array.Copy(links, arr, links.Length);
            arr[links.Length] = new SComboLink { WindowStart = 0.2f, WindowDuration = 0.3f };
            def.comboLinks = arr;
            EditorUtility.SetDirty(def); _onChanged?.Invoke();
        }

        private static void RemoveComboLink(AbilityDefSO def, int index)
        {
            var links = def.comboLinks;
            if (links == null || index < 0 || index >= links.Length) return;
            var arr = new SComboLink[links.Length - 1];
            for (int i = 0, j = 0; i < links.Length; i++)
                if (i != index) arr[j++] = links[i];
            def.comboLinks = arr;
            EditorUtility.SetDirty(def); _onChanged?.Invoke();
        }
    }
}
#endif
