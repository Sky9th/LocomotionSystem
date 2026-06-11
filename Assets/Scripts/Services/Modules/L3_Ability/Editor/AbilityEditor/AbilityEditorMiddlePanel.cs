#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
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

        // ── EditorForm（每 section 独立，避免字段覆盖）──
        private static EditorForm _identityForm;
        private static EditorForm _tagForm;
        private static EditorForm _exclusionForm;
        private static EditorForm _cooldownForm;
        private static EditorForm _passiveForm;

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
            EditorGUILayout.LabelField(
                "Select an ability from the left panel,\nor create a new one with ＋ Create New.",
                EditorUIUtility.GreyPlaceholder);
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
            if (_identityForm?.Target != a)
            {
                _identityForm = new EditorForm(a);
                _identityForm.TextField("internalName")
                     .TextField("displayName")
                     .ObjectField<Sprite>("icon")
                     .TextArea("description");
                _identityForm.OnAnyChange += () => { EditorUtility.SetDirty(a); _onChanged?.Invoke(); };
            }
            _identityForm?.Draw();
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
                EditorGUILayout.LabelField("— (none)", EditorUIUtility.GreyPlaceholder, GUILayout.ExpandWidth(true));

                GUI.backgroundColor = EditorUIUtility.ColorGreenDark;
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
                // 固定排序：Damage → Impact → Execute → Cost，同类型内按 effectTag
                var sorted = effects
                    .Select((e, idx) => (effect: e, origIdx: idx))
                    .OrderBy(x => EffectTypeOrder(x.effect))
                    .ThenBy(x => x.effect?.effectTag?.FullTag ?? "")
                    .ToArray();

                for (var si = 0; si < sorted.Length; si++)
                {
                    if (si > 0) EditorUIUtility.CardGap(Pad);
                    var e = sorted[si].effect;
                    var origIdx = sorted[si].origIdx;

                    EditorUIUtility.DrawCard(Pad, () =>
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(GetEffectIcon(e), EditorStyles.label,
                            GUILayout.Width(36));

                        var name = e != null ? e.name : "(missing)";
                        var st = e != null ? EditorStyles.label
                            : new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } };
                        EditorGUILayout.LabelField(name, st, GUILayout.ExpandWidth(true));

                        GUI.backgroundColor = EditorUIUtility.ColorRed;
                        if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20)))
                            onRemove?.Invoke(origIdx);
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
                EditorGUILayout.LabelField("(empty)", EditorUIUtility.GreyPlaceholder);
            }

            EditorUIUtility.CardGap(Pad);
            GUI.backgroundColor = EditorUIUtility.ColorGreenDark;
            if (GUILayout.Button("＋ Add Effect", GUILayout.Height(22)))
                onEdit?.Invoke(slot);
            GUI.backgroundColor = Color.white;
        }

        // ═══════════════════════════════════════════════════════════
        // Tags
        // ═══════════════════════════════════════════════════════════
        private static void DrawTagFields(AbilitySO a)
        {
            // abilityTag / sharedCooldownTag
            if (_tagForm?.Target != a)
            {
                _tagForm = new EditorForm(a);
                _tagForm.ObjectField<GameplayTagDefinitionSO>("abilityTag", label: "Ability")
                        .PostInput(() => DrawTagPickerButton(a, "abilityTag"))
                     .ObjectField<GameplayTagDefinitionSO>("sharedCooldownTag", label: "Shared CD")
                        .PostInput(() => DrawTagPickerButton(a, "sharedCooldownTag"));
                _tagForm.OnAnyChange += () => { EditorUtility.SetDirty(a); _onChanged?.Invoke(); };
            }
            _tagForm?.Draw();

            // overrideExclusion — AbilityDefSO 独有字段
            if (a is AbilityDefSO def)
            {
                if (_exclusionForm?.Target != def)
                {
                    _exclusionForm = new EditorForm(def);
                    _exclusionForm.Toggle("overrideExclusion", label: "Override Exclusion");
                    _exclusionForm.OnAnyChange += () => { EditorUtility.SetDirty(def); _onChanged?.Invoke(); };
                }
                _exclusionForm?.Draw();
            }
        }

        /// <summary>TagPicker 按钮（PostInput 回调）。修改直接写 SO 字段并触发 form 重建。</summary>
        private static void DrawTagPickerButton(AbilitySO a, string fieldName)
        {
            if (!GUILayout.Button("Tag", EditorStyles.miniButton, GUILayout.Width(35))) return;
            var field = a.GetType().GetField(fieldName,
                BindingFlags.Public | BindingFlags.Instance);
            if (field == null)
            {
                Debug.LogWarning($"[MiddlePanel] TagPicker: field '{fieldName}' not found on {a.GetType().Name}");
                return;
            }
            var rect = GUIUtility.GUIToScreenRect(GUILayoutUtility.GetLastRect());
            var currentTag = field.GetValue(a) as GameplayTagDefinitionSO;
            TagPicker.Show(rect, allowCreate: true, currentFullTag: currentTag?.FullTag,
                onSelected: t =>
                {
                    if (currentTag != t)
                    {
                        field.SetValue(a, t);
                        EditorUtility.SetDirty(a);
                        _onChanged?.Invoke();
                        _tagForm = null; _passiveForm = null; // force rebuild
                    }
                });
        }

        // ═══════════════════════════════════════════════════════════
        // Cooldown
        // ═══════════════════════════════════════════════════════════
        private static void DrawCooldownFields(AbilitySO a)
        {
            if (_cooldownForm?.Target != a)
            {
                _cooldownForm = new EditorForm(a);
                _cooldownForm.Float("cooldownDuration", label: "Duration (s)");
                _cooldownForm.OnAnyChange += () => { EditorUtility.SetDirty(a); _onChanged?.Invoke(); };
            }
            _cooldownForm?.Draw();
        }

        // ═══════════════════════════════════════════════════════════
        // Passive
        // ═══════════════════════════════════════════════════════════
        private static void DrawPassiveFields(PassiveAbilitySO p)
        {
            if (_passiveForm?.Target != p)
            {
                _passiveForm = new EditorForm(p);
                _passiveForm.Enum<ETriggerEvent>("trigger")
                     .Float("triggerValue", label: "Trigger Value")
                     .ObjectField<EventChannelBase>("triggerChannel", label: "Channel")
                     .ObjectField<GameplayTagDefinitionSO>("targetRequiredTag", label: "Target Tag")
                        .PostInput(() => DrawTagPickerButton(p, "targetRequiredTag"));
                _passiveForm.OnAnyChange += () => { EditorUtility.SetDirty(p); _onChanged?.Invoke(); };
            }
            _passiveForm?.Draw();
        }

        // ═══════════════════════════════════════════════════════════
        // Combo
        // ═══════════════════════════════════════════════════════════
        private static void DrawComboList(AbilityDefSO def)
        {
            var links = def.comboLinks;
            if (links != null)
            {
                int removeAt = -1;
                for (var i = 0; i < links.Length; i++)
                {
                    if (i > 0) EditorUIUtility.CardGap(Pad);
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

                    GUI.backgroundColor = EditorUIUtility.ColorRed;
                    if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20)))
                        removeAt = i;
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                }

                if (removeAt >= 0)
                    RemoveComboLink(def, removeAt);
            }

            if (links == null || links.Length == 0)
            {
                EditorGUILayout.LabelField("(no combo links)", EditorUIUtility.GreyPlaceholder);
            }

            GUILayout.Space(Pad);
            if (GUILayout.Button("＋ Add Combo Link", GUILayout.Height(22)))
                AddComboLink(def);
        }

        // ═══════════════════════════════════════════════════════════
        // 摘要
        // ═══════════════════════════════════════════════════════════
        private static string GetActivationSummary(AbilityActivationSO a)
            => AbilityEditorUtility.GetActivationSummary(a);

        private static string GetSearchSummary(AbilitySearchSO s)
            => AbilityEditorUtility.GetSearchSummary(s);

        private static string GetEffectSummary(EffectSO e)
            => AbilityEditorUtility.GetEffectSummary(e, includeDuration: true);

        private static string GetNoiseSummary(NoiseEventSO n)
            => AbilityEditorUtility.GetNoiseSummary(n);

        private static int EffectTypeOrder(EffectSO e)
            => AbilityEditorUtility.GetEffectTypeOrder(e);

        private static string GetEffectIcon(EffectSO e)
            => AbilityEditorUtility.GetEffectIcon(e);

        private static void AddComboLink(AbilityDefSO def)
        {
            var links = def.comboLinks ?? Array.Empty<SComboLink>();
            var arr = AbilityEditorUtility.Append(links, new SComboLink { WindowStart = 0.2f, WindowDuration = 0.3f });
            def.comboLinks = arr;
            EditorUtility.SetDirty(def); _onChanged?.Invoke();
        }

        private static void RemoveComboLink(AbilityDefSO def, int index)
        {
            var links = def.comboLinks;
            if (links == null || index < 0 || index >= links.Length) return;
            def.comboLinks = AbilityEditorUtility.RemoveAt(links, index);
            EditorUtility.SetDirty(def); _onChanged?.Invoke();
        }
    }
}
#endif
