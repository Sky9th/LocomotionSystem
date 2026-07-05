#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RedDust.UI
{
    /// <summary>
    /// 一键生成 SkillCard.prefab。菜单: RedDust > UI > Create SkillCard Prefab
    /// </summary>
    public static class CreateSkillCardPrefab
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Components/SkillCard.prefab";
        private const float BodySize = 18f;
        private const float SmallSize = 14f;

        [MenuItem("RedDust/UI/Create SkillCard Prefab")]
        public static void Create()
        {
            // ── Load theme for fonts ──
            var theme = AssetDatabase.LoadAssetAtPath<UIThemeSO>("Assets/Data/UI/UIThemeSO.asset");
            var font = theme != null ? theme.bodyFont : null;

            // ── Root ──
            var root = NewGO("SkillCard",
                typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(CanvasGroup),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter),
                typeof(SkillCard));

            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -8);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 320);

            var img = root.GetComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            var cg = root.GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            var vlg = root.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 4;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = root.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var card = root.GetComponent<SkillCard>();

            // ── Children ──
            var icon = NewImage(root, "Icon", 48, 48);
            var nameLabel = NewText(root, "Name", BodySize, Color.white, font);
            var descLabel = NewText(root, "Description", SmallSize, new Color(0.55f, 0.55f, 0.55f), font);
            var cooldownLabel = NewText(root, "Cooldown", BodySize, new Color(0.85f, 0.85f, 0.85f), font);
            var activationLabel = NewText(root, "ActivationInfo", SmallSize, new Color(0.55f, 0.55f, 0.55f), font);

            // Timing section
            var timingSec = NewSection(root, "TimingSection");
            var phaseLabel = NewText(timingSec, "PhaseTiming", SmallSize, new Color(0.55f, 0.55f, 0.55f), font);
            var cancelLabel = NewText(timingSec, "CancelInfo", SmallSize, new Color(0.55f, 0.55f, 0.55f), font);

            // Effects section
            var effectsSec = NewSection(root, "EffectsSection");
            var dmgLabel = NewText(effectsSec, "DamageMod", BodySize, new Color(0.85f, 0.85f, 0.85f), font);
            var impLabel = NewText(effectsSec, "Impact", BodySize, new Color(0.85f, 0.85f, 0.85f), font);
            var costLabel = NewText(effectsSec, "Cost", BodySize, new Color(0.85f, 0.85f, 0.85f), font);
            var buffLabel = NewText(effectsSec, "Buff", BodySize, new Color(0.85f, 0.85f, 0.85f), font);

            // Combo section
            var comboSec = NewSection(root, "ComboSection");
            var comboLabel = NewText(comboSec, "Combo", BodySize, new Color(0.85f, 0.85f, 0.85f), font);

            // Noise
            var noiseLabel = NewText(root, "Noise", SmallSize, new Color(0.55f, 0.55f, 0.55f), font);

            // Sections default hidden
            timingSec.SetActive(false);
            effectsSec.SetActive(false);
            comboSec.SetActive(false);

            // ── Wire Serialized Fields ──
            var so = new SerializedObject(card);

            SetSO(so, "theme", "Assets/Data/UI/UIThemeSO.asset");
            SetSO(so, "background", img);
            SetSO(so, "canvasGroup", cg);

            SetSO(so, "iconImage", icon.GetComponent<Image>());
            SetSO(so, "nameLabel", nameLabel.GetComponent<TMP_Text>());
            SetSO(so, "descriptionLabel", descLabel.GetComponent<TMP_Text>());
            SetSO(so, "cooldownLabel", cooldownLabel.GetComponent<TMP_Text>());
            SetSO(so, "activationInfoLabel", activationLabel.GetComponent<TMP_Text>());
            SetSO(so, "timingSection", timingSec);
            SetSO(so, "phaseTimingLabel", phaseLabel.GetComponent<TMP_Text>());
            SetSO(so, "cancelInfoLabel", cancelLabel.GetComponent<TMP_Text>());
            SetSO(so, "effectsSection", effectsSec);
            SetSO(so, "damageModLabel", dmgLabel.GetComponent<TMP_Text>());
            SetSO(so, "impactLabel", impLabel.GetComponent<TMP_Text>());
            SetSO(so, "costLabel", costLabel.GetComponent<TMP_Text>());
            SetSO(so, "buffLabel", buffLabel.GetComponent<TMP_Text>());
            SetSO(so, "comboSection", comboSec);
            SetSO(so, "comboLabel", comboLabel.GetComponent<TMP_Text>());
            SetSO(so, "noiseLabel", noiseLabel.GetComponent<TMP_Text>());

            so.ApplyModifiedPropertiesWithoutUndo();

            // ── Save Prefab ──
            EnsureDir("Assets/Prefabs/UI/Components");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.Refresh();
            Debug.Log($"[SkillCard] Prefab created at {PrefabPath}");
        }

        // ── Helpers ──

        private static GameObject NewGO(string name, params System.Type[] components)
        {
            var go = new GameObject(name, components);
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) { rt = go.AddComponent<RectTransform>(); }
            // LayoutElement for flexible sizing in VerticalLayoutGroup
            if (go.GetComponent<LayoutElement>() == null)
                go.AddComponent<LayoutElement>();
            return go;
        }

        private static GameObject NewText(GameObject parent, string name, float fontSize, Color color, TMP_FontAsset font)
        {
            var go = NewGO(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent.transform, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.color = color;
            return go;
        }

        private static GameObject NewImage(GameObject parent, string name, float w, float h)
        {
            var go = NewGO(name, typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            go.GetComponent<Image>().raycastTarget = false;
            return go;
        }

        private static GameObject NewSection(GameObject parent, string name)
        {
            var go = NewGO(name, typeof(LayoutElement));
            go.transform.SetParent(parent.transform, false);
            go.GetComponent<LayoutElement>().minHeight = 1;
            // Add VerticalLayoutGroup for section children
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            return go;
        }

        private static void SetSO(SerializedObject so, string propName, Object value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null) prop.objectReferenceValue = value;
        }

        private static void SetSO(SerializedObject so, string propName, string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            SetSO(so, propName, asset);
        }

        private static void EnsureDir(string path)
        {
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);
        }
    }
}
#endif
