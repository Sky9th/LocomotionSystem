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
    ///     BeginVertical(helpBox) → Space(EditorTokens.Pad) → title → Space(EditorTokens.Pad) → body → Space(EditorTokens.Pad) → EndVertical
    ///
    ///   Body 内容模式（对齐 TagEditorWindow.DrawTagDetails）:
    ///     BeginHorizontal → Space(EditorTokens.Pad) → BeginVertical → [fields] → EndVertical → Space(EditorTokens.Pad) → EndHorizontal
    ///
    ///   Field rows:  plain BeginHorizontal, 字段间无多余间距
    ///   Groups 间:   Space(EditorTokens.Pad)
    /// </summary>
    public static class AbilityEditorMiddlePanel
    {
        private static Action _onChanged;

        private static Rect _abilityTagButtonRect;

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
            EditorLabel.Draw("Select an ability from the left panel,\nor create a new one with ＋ Create New.",
                style: EditorUIUtility.GreyPlaceholder);
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
            if (ability is ActiveAbilitySO def)
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
            EditorCard.Draw(title, drawBody);
            EditorCard.Gap(EditorTokens.Pad);
        }

        // ═══════════════════════════════════════════════════════════
        // Identity
        // ═══════════════════════════════════════════════════════════
        private static void DrawIdentityFields(AbilitySO a)
        {
            EditorForm.Draw(a, form =>
            {
                EditorFormItem.TextField("internalName");
                EditorFormItem.TextField("displayName");
                EditorFormItem.ObjectField<Sprite>("icon");
                EditorFormItem.TextField("description");
                form.OnChange += () => { EditorUtility.SetDirty(a); _onChanged?.Invoke(); };
            });
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
                EditorLabel.Draw(asset.name);
                EditorCard.Gap(EditorTokens.Pad);

                if (EditorButton.Draw("✕", EditorButtonType.Danger, EditorButtonSize.Small, width: 24f))
                    onClear?.Invoke(slot);
                if (EditorButton.Draw("...", size: EditorButtonSize.Small, width: 30f))
                    onEdit?.Invoke(slot);
            }
            else
            {
                EditorLabel.Draw("— (none)", style: EditorUIUtility.GreyPlaceholder);

                if (EditorButton.Draw("...", EditorButtonType.Success, width: 30f))
                    onEdit?.Invoke(slot);
            }

            EditorGUILayout.EndHorizontal();

            if (asset != null && !string.IsNullOrEmpty(summary))
            {
                EditorCard.Gap(2f);
                EditorLabel.Draw(summary, style: EditorTokens.DimLabelStyle);
            }

            if (asset != null)
            {
                EditorLabel.Draw(asset.GetType().Name, style: EditorTokens.DimLabelStyle);
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
                    if (si > 0) EditorCard.Gap(EditorTokens.Pad);
                    var e = sorted[si].effect;
                    var origIdx = sorted[si].origIdx;

                    EditorCard.Draw(() =>
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorLabel.Draw(GetEffectIcon(e), 36f);

                        var name = e != null ? e.name : "(missing)";
                        var st = e != null ? EditorLabel.DefaultStyle
                            : EditorTokens.ErrorLabelStyle;
                        EditorLabel.Draw(name, style: st);

                        if (EditorButton.Delete())
                            onRemove?.Invoke(origIdx);
                        EditorGUILayout.EndHorizontal();

                        if (e != null)
                        {
                            EditorLabel.Draw(GetEffectSummary(e), style: EditorTokens.DimLabelStyle);
                        }
                    });
                }
            }

            if (effects == null || effects.Length == 0)
            {
                EditorLabel.Draw("(empty)", style: EditorUIUtility.GreyPlaceholder);
            }

            EditorCard.Gap(EditorTokens.Pad);
            if (EditorButton.Draw("＋ Add Effect", EditorButtonType.Success, EditorButtonSize.Small))
                onEdit?.Invoke(slot);
        }

        // ═══════════════════════════════════════════════════════════
        // Tags
        // ═══════════════════════════════════════════════════════════
        private static void DrawTagFields(AbilitySO a)
        {
            EditorForm.Draw(a, form =>
            {
                EditorFormItem.ObjectFieldWithTag<GameplayTagDefinitionSO>("abilityTag",
                    ref _abilityTagButtonRect, label: "Ability");
                EditorFormItem.ObjectFieldWithTag<GameplayTagDefinitionSO>("sharedCooldownTag",
                    ref _abilityTagButtonRect, label: "Shared CD");
                form.OnChange += () => { EditorUtility.SetDirty(a); _onChanged?.Invoke(); };
            });

            if (a is ActiveAbilitySO def)
            {
                EditorForm.Draw(def, form =>
                {
                    EditorFormItem.Toggle("overrideExclusion", label: "Override Exclusion");
                    form.OnChange += () => { EditorUtility.SetDirty(def); _onChanged?.Invoke(); };
                });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // Cooldown
        // ═══════════════════════════════════════════════════════════
        private static void DrawCooldownFields(AbilitySO a)
        {
            EditorForm.Draw(a, form =>
            {
                EditorFormItem.Float("cooldownDuration", label: "Duration (s)");
                form.OnChange += () => { EditorUtility.SetDirty(a); _onChanged?.Invoke(); };
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Passive
        // ═══════════════════════════════════════════════════════════
        private static void DrawPassiveFields(PassiveAbilitySO p)
        {
            EditorForm.Draw(p, form =>
            {
                EditorFormItem.Enum<ETriggerEvent>("trigger");
                EditorFormItem.Float("triggerValue", label: "Trigger Value");
                EditorFormItem.ObjectField<GameEvent>("triggerChannel", label: "Channel");
                EditorFormItem.ObjectFieldWithTag<GameplayTagDefinitionSO>("targetRequiredTag",
                    ref _abilityTagButtonRect, label: "Target Tag");
                form.OnChange += () => { EditorUtility.SetDirty(p); _onChanged?.Invoke(); };
            });
        }

        // ═══════════════════════════════════════════════════════════
        // Combo
        // ═══════════════════════════════════════════════════════════
        private static void DrawComboList(ActiveAbilitySO def)
        {
            var links = def.comboLinks;
            if (links != null)
            {
                int removeAt = -1;
                for (var i = 0; i < links.Length; i++)
                {
                    if (i > 0) EditorCard.Gap(EditorTokens.Pad);
                    var l = links[i];

                    EditorGUILayout.BeginHorizontal();
                    EditorLabel.Draw("→", 14f);

                    var next = (ActiveAbilitySO)EditorGUILayout.ObjectField(
                        l.NextSkill, typeof(ActiveAbilitySO), false, GUILayout.Width(140));
                    if (next != l.NextSkill) { l.NextSkill = next; def.comboLinks[i] = l; EditorUtility.SetDirty(def); _onChanged?.Invoke(); }

                    EditorLabel.Draw("Start", 35f);
                    var ns = EditorGUILayout.FloatField(l.WindowStart, GUILayout.Width(40));
                    if (Mathf.Abs(ns - l.WindowStart) > 0.001f)
                    { l.WindowStart = ns; def.comboLinks[i] = l; EditorUtility.SetDirty(def); _onChanged?.Invoke(); }

                    EditorLabel.Draw("Dur", 25f);
                    var nd = EditorGUILayout.FloatField(l.WindowDuration, GUILayout.Width(40));
                    if (Mathf.Abs(nd - l.WindowDuration) > 0.001f)
                    { l.WindowDuration = nd; def.comboLinks[i] = l; EditorUtility.SetDirty(def); _onChanged?.Invoke(); }

                    var bp = EditorGUILayout.Toggle(l.BypassCooldown, GUILayout.Width(16));
                    if (bp != l.BypassCooldown) { l.BypassCooldown = bp; def.comboLinks[i] = l; EditorUtility.SetDirty(def); _onChanged?.Invoke(); }
                    EditorLabel.Draw("BypassCD", 58f, style: EditorStyles.miniLabel);

                    if (EditorButton.Delete())
                        removeAt = i;
                    EditorGUILayout.EndHorizontal();
                }

                if (removeAt >= 0)
                    RemoveComboLink(def, removeAt);
            }

            if (links == null || links.Length == 0)
            {
                EditorLabel.Draw("(no combo links)", style: EditorUIUtility.GreyPlaceholder);
            }

            EditorCard.Gap(EditorTokens.Pad);
            if (EditorButton.Draw("＋ Add Combo Link", EditorButtonType.Success, EditorButtonSize.Small))
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

        private static void AddComboLink(ActiveAbilitySO def)
        {
            var links = def.comboLinks ?? Array.Empty<SComboLink>();
            var arr = AbilityEditorUtility.Append(links, new SComboLink { WindowStart = 0.2f, WindowDuration = 0.3f });
            def.comboLinks = arr;
            EditorUtility.SetDirty(def); _onChanged?.Invoke();
        }

        private static void RemoveComboLink(ActiveAbilitySO def, int index)
        {
            var links = def.comboLinks;
            if (links == null || index < 0 || index >= links.Length) return;
            def.comboLinks = AbilityEditorUtility.RemoveAt(links, index);
            EditorUtility.SetDirty(def); _onChanged?.Invoke();
        }
    }
}
#endif
