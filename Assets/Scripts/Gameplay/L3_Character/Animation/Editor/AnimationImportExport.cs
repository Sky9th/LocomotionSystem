#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Animancer;
using RedDust.Core;
using RedDust.Ability.Editor;
using RedDust.Shared.EditorUI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RedDust.Character.Animation
{
    // ═══════════════════════════════════════════════════════════════
    // DTO Classes
    // ═══════════════════════════════════════════════════════════════

    /// <summary>根容器 — 一个 JSON 文件可包含多个 Profile。</summary>
    [Serializable]
    public class AnimationExportFile
    {
        public string version = "1.0";
        public string description;
        public ProfileEntry[] profiles;
        public LocomotionConfigEntry[] locomotionConfigs;
        public ModeConfigEntry[] modeProfiles;
        public LocomotionSetEntry[] locomotionSets;
        public GripTableEntry[] gripTables;
    }

    [Serializable]
    public class ProfileEntry
    {
        public string name;
        public string directory;
        public string locomotionConfig;       // {directory}/{name} path
        public string[] modeProfiles;         // {directory}/{name} paths
        public string defaultLocomotionSet;   // {directory}/{name} path
        public string gripTable;              // {directory}/{name} path
    }

    [Serializable]
    public class LocomotionConfigEntry
    {
        public string name;
        public string directory;
        public string[] modeProfiles;              // {directory}/{name} paths
        // headLookSmoothingSpeed — 已移除。Head Look IK 延后。
        public float defaultInPlaceTurnSpeed = 360f;
        public float defaultMovingTurnSpeed = 720f;
        public float landDistanceThreshold = 0.5f;
        public float landMinFallDistance = 0.2f;
        public float landLightMaxFallDistance = 1f;
        public float landMediumMaxFallDistance = 3f;
        public float landLightTriggerDistance = 0.3f;
        public float landMediumTriggerDistance = 0.6f;
        public float landHardTriggerDistance = 1f;
    }

    [Serializable]
    public class ModeConfigEntry
    {
        public string name;
        public string directory;
        public string posture;            // EPosture enum name
        public string gait;               // EMovementGait enum name
        public float movingTurnSpeed = 720f;
        public float enterAngle = 90f;
        public float exitAngle = 20f;
    }

    [Serializable]
    public class LocomotionSetEntry
    {
        public string name;
        public string directory;
        // Idle / Move / Turn (ClipTransition x5)
        public ClipTransitionEntry idleL;
        public ClipTransitionEntry crouchIdle;
        public ClipTransitionEntry sprint;
        public ClipTransitionEntry turnInPlace90L;
        public ClipTransitionEntry turnInPlace90R;
        // Move (MixerTransition2D x3)
        public Mixer2DTransitionEntry walkMixer;
        public Mixer2DTransitionEntry runMixer;
        public Mixer2DTransitionEntry crouchMixer;
        // Air / Land (LinearMixerTransition x4)
        public LinearMixerTransitionEntry airLight;
        public LinearMixerTransitionEntry airHard;
        public LinearMixerTransitionEntry landLight;
        public LinearMixerTransitionEntry landHard;
        // Native speeds
        public float walkAnimNativeSpeed = 1.5f;
        public float runAnimNativeSpeed = 5f;
        public float sprintAnimNativeSpeed = 7f;
        public float crawlAnimNativeSpeed = 1f;
        // Hit Reaction (MixerTransition2D x4 — 4-directional blend)
        public Mixer2DTransitionEntry hitReactionFlinch;
        public Mixer2DTransitionEntry hitReactionStagger;
        public Mixer2DTransitionEntry hitReactionKnockdown;
        public Mixer2DTransitionEntry hitReactionGetUp;
        // Traversal
        public ClipTransitionEntry climbUpHalfMeter;
        public ClipTransitionEntry climbUp1meter;
        public ClipTransitionEntry climbUp2meter;
        public ClipTransitionEntry climbDown1meter;
        public ClipTransitionEntry climbDown2meter;
        public ClipTransitionEntry landFromWall;
    }

    [Serializable]
    public class ClipTransitionEntry
    {
        public float _FadeDuration = 0.25f;
        public float _Speed = 1f;
        public TransitionEventsEntry _Events;
        public string _Clip;                     // AnimationClip GUID, null for none
        public string _NormalizedStartTime;      // "NaN" or float string — must be string type!
    }

    [Serializable]
    public class Mixer2DTransitionEntry
    {
        public float _FadeDuration = 0.25f;
        public float _Speed = 1f;
        public TransitionEventsEntry _Events;
        public string[] _Animations;             // GUID strings
        public float[] _Speeds;                  // [] = all default 1.0
        public bool[] _SynchronizeChildren;      // null = not set
        public Vector2[] _Thresholds;
        public Vector2 _DefaultParameter;
        public int _Type;                        // 0=Cartesian, 1=Directional
        public string _ParameterNameX;           // GUID string or null
        public string _ParameterNameY;           // GUID string or null
    }

    [Serializable]
    public class LinearMixerTransitionEntry
    {
        public float _FadeDuration = 0.25f;
        public float _Speed = 1f;
        public TransitionEventsEntry _Events;
        public string[] _Animations;             // GUID strings
        public float[] _Speeds;                  // [] = all default 1.0
        public bool[] _SynchronizeChildren;      // null = not set
        public float[] _Thresholds;
        public float _DefaultParameter;
        public bool _ExtrapolateSpeed = true;
        public string _ParameterName;            // GUID string or null
    }

    [Serializable]
    public class TransitionEventsEntry
    {
        public float[] _NormalizedTimes;
        public string[] _Callbacks;              // always empty — IInvokable[] cannot round-trip
        public string[] _Names;                  // StringAsset GUID strings or empty
    }

    [Serializable]
    public class GripTableEntry
    {
        public string name;
        public string directory;
        public string defaultSet;                // {directory}/{name} path
        public GripEntryItem[] entries;
    }

    [Serializable]
    public class GripEntryItem
    {
        public string gripTag;                   // RdTagDefSO.FullTag
        public string weaponTypeTag;             // RdTagDefSO.FullTag, null=不限
        public string animationSet;              // {directory}/{name} path  (Relax)
        public string combatSet;                 // {directory}/{name} path  (Combat, optional)
    }

    // ═══════════════════════════════════════════════════════════════
    // Static Importer
    // ═══════════════════════════════════════════════════════════════

    public static class AnimationImporter
    {
        internal const string AnimationRoot = "Assets/Data/Animation";

        // Reflection cache for AnimationModeConfigSO private fields
        private static readonly System.Reflection.FieldInfo s_postureField =
            typeof(AnimationModeConfigSO).GetField("posture",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        private static readonly System.Reflection.FieldInfo s_gaitField =
            typeof(AnimationModeConfigSO).GetField("gait",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        private static readonly System.Reflection.FieldInfo s_movingTurnSpeedField =
            typeof(AnimationModeConfigSO).GetField("movingTurnSpeed",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        private static readonly System.Reflection.FieldInfo s_enterAngleField =
            typeof(AnimationModeConfigSO).GetField("enterAngle",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        private static readonly System.Reflection.FieldInfo s_exitAngleField =
            typeof(AnimationModeConfigSO).GetField("exitAngle",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // ═══════════════════════════════════════════════════
        // Export
        // ═══════════════════════════════════════════════════

        public static string ExportToJson()
        {
            var file = new AnimationExportFile
            {
                version = "1.0",
                description = "Animation export",
            };

            var seenSOs = new HashSet<ScriptableObject>();

            // Gather all profiles
            var profileGuids = AssetDatabase.FindAssets("t:CharacterAnimationProfileSO");
            if (profileGuids.Length == 0)
            {
                return JsonUtility.ToJson(file, true);
            }

            var profileEntries = new List<ProfileEntry>();
            var configEntries = new List<LocomotionConfigEntry>();
            var modeEntries = new List<ModeConfigEntry>();
            var setEntries = new List<LocomotionSetEntry>();
            var gripEntries = new List<GripTableEntry>();

            foreach (var guid in profileGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<CharacterAnimationProfileSO>(path);
                if (profile == null) continue;
                if (!seenSOs.Add(profile)) continue;

                var (dir, _) = SplitAssetPath(path);
                var pEntry = new ProfileEntry
                {
                    name = profile.name,
                    directory = dir,
                };

                // locomotionConfig
                if (profile.locomotionConfig != null)
                {
                    pEntry.locomotionConfig = MakePath(profile.locomotionConfig);
                    ExportLocomotionConfig(profile.locomotionConfig, configEntries, modeEntries, seenSOs);
                }

                // modeProfiles
                if (profile.modeProfiles != null)
                {
                    pEntry.modeProfiles = new string[profile.modeProfiles.Length];
                    for (int i = 0; i < profile.modeProfiles.Length; i++)
                    {
                        var m = profile.modeProfiles[i];
                        if (m == null) continue;
                        pEntry.modeProfiles[i] = MakePath(m);
                        ExportModeConfig(m, modeEntries, seenSOs);
                    }
                }

                // defaultLocomotionSet
                if (profile.defaultLocomotionSet != null)
                {
                    pEntry.defaultLocomotionSet = MakePath(profile.defaultLocomotionSet);
                    ExportLocomotionSet(profile.defaultLocomotionSet, setEntries, seenSOs);
                }

                // gripTable
                if (profile.gripTable != null)
                {
                    pEntry.gripTable = MakePath(profile.gripTable);
                    ExportGripTable(profile.gripTable, gripEntries, setEntries, seenSOs);
                }

                profileEntries.Add(pEntry);
            }

            file.profiles = profileEntries.ToArray();
            file.locomotionConfigs = configEntries.ToArray();
            file.modeProfiles = modeEntries.ToArray();
            file.locomotionSets = setEntries.ToArray();
            file.gripTables = gripEntries.ToArray();

            return JsonUtility.ToJson(file, true);
        }

        public static void ExportToFile(string jsonPath)
        {
            File.WriteAllText(jsonPath, ExportToJson());
        }

        // ── Export helpers ──────────────────────────────────

        private static void ExportLocomotionConfig(LocomotionAnimationConfigSO config,
            List<LocomotionConfigEntry> configs, List<ModeConfigEntry> modes,
            HashSet<ScriptableObject> seen)
        {
            if (config == null || !seen.Add(config)) return;

            var entry = new LocomotionConfigEntry
            {
                name = config.name,
                directory = SplitAssetPath(AssetDatabase.GetAssetPath(config)).dir,
            };
            // entry.headLookSmoothingSpeed — Head Look IK 延后
            entry.defaultInPlaceTurnSpeed = config.defaultInPlaceTurnSpeed;
            entry.defaultMovingTurnSpeed = config.defaultMovingTurnSpeed;
            entry.landDistanceThreshold = config.landDistanceThreshold;
            entry.landMinFallDistance = config.landMinFallDistance;
            entry.landLightMaxFallDistance = config.landLightMaxFallDistance;
            entry.landMediumMaxFallDistance = config.landMediumMaxFallDistance;
            entry.landLightTriggerDistance = config.landLightTriggerDistance;
            entry.landMediumTriggerDistance = config.landMediumTriggerDistance;
            entry.landHardTriggerDistance = config.landHardTriggerDistance;

            if (config.modeProfiles != null)
            {
                entry.modeProfiles = new string[config.modeProfiles.Length];
                for (int i = 0; i < config.modeProfiles.Length; i++)
                {
                    var m = config.modeProfiles[i];
                    if (m == null) continue;
                    entry.modeProfiles[i] = MakePath(m);
                    ExportModeConfig(m, modes, seen);
                }
            }

            configs.Add(entry);
        }

        private static void ExportModeConfig(AnimationModeConfigSO mode,
            List<ModeConfigEntry> modes, HashSet<ScriptableObject> seen)
        {
            if (mode == null || !seen.Add(mode)) return;

            modes.Add(new ModeConfigEntry
            {
                name = mode.name,
                directory = SplitAssetPath(AssetDatabase.GetAssetPath(mode)).dir,
                posture = mode.Posture.ToString(),
                gait = mode.Gait.ToString(),
                movingTurnSpeed = mode.MovingTurnSpeed,
                enterAngle = mode.EnterAngle,
                exitAngle = mode.ExitAngle,
            });
        }

        private static void ExportLocomotionSet(LocomotionAnimationSetSO set,
            List<LocomotionSetEntry> sets, HashSet<ScriptableObject> seen)
        {
            if (set == null || !seen.Add(set)) return;

            var entry = new LocomotionSetEntry
            {
                name = set.name,
                directory = SplitAssetPath(AssetDatabase.GetAssetPath(set)).dir,
                walkAnimNativeSpeed = set.walkAnimNativeSpeed,
                runAnimNativeSpeed = set.runAnimNativeSpeed,
                sprintAnimNativeSpeed = set.sprintAnimNativeSpeed,
                crawlAnimNativeSpeed = set.crawlAnimNativeSpeed,
            };

            entry.idleL = ExportClipTransition(set.idleL);
            entry.crouchIdle = ExportClipTransition(set.crouchIdle);
            entry.sprint = ExportClipTransition(set.sprint);
            entry.turnInPlace90L = ExportClipTransition(set.turnInPlace90L);
            entry.turnInPlace90R = ExportClipTransition(set.turnInPlace90R);
            entry.walkMixer = ExportMixer2D(set.walkMixer);
            entry.runMixer = ExportMixer2D(set.runMixer);
            entry.crouchMixer = ExportMixer2D(set.crouchMixer);
            entry.airLight = ExportLinearMixer(set.airLight);
            entry.airHard = ExportLinearMixer(set.airHard);
            entry.landLight = ExportLinearMixer(set.landLight);
            entry.landHard = ExportLinearMixer(set.landHard);
            entry.climbUpHalfMeter = ExportClipTransition(set.climbUpHalfMeter);
            entry.climbUp1meter = ExportClipTransition(set.climbUp1meter);
            entry.climbUp2meter = ExportClipTransition(set.climbUp2meter);
            entry.climbDown1meter = ExportClipTransition(set.climbDown1meter);
            entry.climbDown2meter = ExportClipTransition(set.climbDown2meter);
            entry.landFromWall = ExportClipTransition(set.landFromWall);
            entry.hitReactionFlinch = ExportMixer2D(set.hitReactionFlinch);
            entry.hitReactionStagger = ExportMixer2D(set.hitReactionStagger);
            entry.hitReactionKnockdown = ExportMixer2D(set.hitReactionKnockdown);
            entry.hitReactionGetUp = ExportMixer2D(set.hitReactionGetUp);

            sets.Add(entry);
        }

        private static void ExportGripTable(GripAnimationTableSO table,
            List<GripTableEntry> grips, List<LocomotionSetEntry> sets,
            HashSet<ScriptableObject> seen)
        {
            if (table == null || !seen.Add(table)) return;

            var entry = new GripTableEntry
            {
                name = table.name,
                directory = SplitAssetPath(AssetDatabase.GetAssetPath(table)).dir,
            };

            if (table.defaultSet != null)
            {
                entry.defaultSet = MakePath(table.defaultSet);
                ExportLocomotionSet(table.defaultSet, sets, seen);
            }

            if (table.entries != null)
            {
                entry.entries = new GripEntryItem[table.entries.Length];
                for (int i = 0; i < table.entries.Length; i++)
                {
                    var ge = table.entries[i];
                    entry.entries[i] = new GripEntryItem
                    {
                        gripTag = ge.gripTag?.FullTag,
                        weaponTypeTag = ge.weaponTypeTag?.FullTag,
                        animationSet = MakePath(ge.animationSet),
                        combatSet = ge.combatSet != null ? MakePath(ge.combatSet) : null,
                    };
                    if (ge.animationSet != null)
                        ExportLocomotionSet(ge.animationSet, sets, seen);
                    if (ge.combatSet != null)
                        ExportLocomotionSet(ge.combatSet, sets, seen);
                }
            }

            grips.Add(entry);
        }

        // ── Transition export ───────────────────────────────

        private static ClipTransitionEntry ExportClipTransition(ClipTransition t)
        {
            if (t == null) return null;

            return new ClipTransitionEntry
            {
                _FadeDuration = t.FadeDuration,
                _Speed = t.Speed,
                _Events = ExportEvents(t),
                _Clip = t.Clip != null
                    ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(t.Clip))
                    : null,
                _NormalizedStartTime = float.IsNaN(t.NormalizedStartTime)
                    ? "NaN"
                    : t.NormalizedStartTime.ToString("G9", CultureInfo.InvariantCulture),
            };
        }

        private static Mixer2DTransitionEntry ExportMixer2D(MixerTransition2D t)
        {
            if (t == null) return null;
            var speeds = t.Speeds;
            var hasCustomSpeeds = speeds != null && speeds.Any(s => Math.Abs(s - 1f) > 0.0001f);

            return new Mixer2DTransitionEntry
            {
                _FadeDuration = t.FadeDuration,
                _Speed = t.Speed,
                _Events = ExportEvents(t),
                _Animations = t.Animations?.Select(a =>
                    a is AnimationClip clip
                        ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clip))
                        : null).ToArray(),
                _Speeds = hasCustomSpeeds ? speeds : new float[0],
                _SynchronizeChildren = t.SynchronizeChildren,
                _Thresholds = t.Thresholds,
                _DefaultParameter = t.DefaultParameter,
                _Type = (int)t.Type,
                _ParameterNameX = AssetRefToGuid(t.ParameterNameX),
                _ParameterNameY = AssetRefToGuid(t.ParameterNameY),
            };
        }

        private static LinearMixerTransitionEntry ExportLinearMixer(LinearMixerTransition t)
        {
            if (t == null) return null;
            var speeds = t.Speeds;
            var hasCustomSpeeds = speeds != null && speeds.Any(s => Math.Abs(s - 1f) > 0.0001f);

            return new LinearMixerTransitionEntry
            {
                _FadeDuration = t.FadeDuration,
                _Speed = t.Speed,
                _Events = ExportEvents(t),
                _Animations = t.Animations?.Select(a =>
                    a is AnimationClip clip
                        ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clip))
                        : null).ToArray(),
                _Speeds = hasCustomSpeeds ? speeds : new float[0],
                _SynchronizeChildren = t.SynchronizeChildren,
                _Thresholds = t.Thresholds,
                _DefaultParameter = t.DefaultParameter,
                _ExtrapolateSpeed = t.ExtrapolateSpeed,
                _ParameterName = AssetRefToGuid(t.ParameterName),
            };
        }

        private static TransitionEventsEntry ExportEvents(ITransition t)
        {
            var serialized = t.SerializedEvents;
            if (serialized == null)
                return new TransitionEventsEntry { _NormalizedTimes = new float[0], _Callbacks = new string[0], _Names = new string[0] };

            var callbacks = serialized.Callbacks;
            if (callbacks != null && callbacks.Length > 0)
            {
                Debug.LogWarning($"[AnimationExport] Transition has {callbacks.Length} IInvokable callbacks — cannot round-trip, data will be lost.");
            }

            return new TransitionEventsEntry
            {
                _NormalizedTimes = serialized.NormalizedTimes ?? new float[0],
                _Callbacks = new string[0],
                _Names = serialized.Names?.Select(n =>
                    n != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(n)) : null).ToArray()
                    ?? new string[0],
            };
        }

        // ── Utility ─────────────────────────────────────────

        private static (string dir, string name) SplitAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return ("", "");
            // Strip AnimationRoot prefix
            var rel = assetPath;
            if (rel.StartsWith(AnimationRoot + "/", StringComparison.Ordinal))
                rel = rel.Substring(AnimationRoot.Length + 1);
            var dir = Path.GetDirectoryName(rel)?.Replace('\\', '/') ?? "";
            return (dir, Path.GetFileNameWithoutExtension(rel));
        }

        /// <summary>Build a {directory}/{name} path string for a given SO.</summary>
        private static string MakePath(ScriptableObject so)
        {
            if (so == null) return null;
            var (dir, name) = SplitAssetPath(AssetDatabase.GetAssetPath(so));
            return $"{dir}/{name}";
        }

        private static string AssetRefToGuid(Object obj)
        {
            if (obj == null) return null;
            var path = AssetDatabase.GetAssetPath(obj);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path);
        }

        // ═══════════════════════════════════════════════════
        // Import
        // ═══════════════════════════════════════════════════

        public static (int created, int updated, int skipped, List<string> errors) ImportFromJson(string jsonText)
        {
            var errors = new List<string>();
            int created = 0, updated = 0, skipped = 0;

            AnimationExportFile file;
            try { file = JsonUtility.FromJson<AnimationExportFile>(jsonText); }
            catch (Exception e) { errors.Add($"Parse failed: {e.Message}"); return (0, 0, 0, errors); }
            if (file == null) { errors.Add("JSON deserialized to null."); return (0, 0, 0, errors); }

            if (string.IsNullOrEmpty(file.version) || (file.version != "1.0" && file.version != "2.0"))
            { errors.Add($"Unsupported version '{file.version}'. Only '1.0' / '2.0' is supported."); return (0, 0, 0, errors); }

            if (file.profiles == null || file.profiles.Length == 0)
            { errors.Add("JSON contains no profiles."); return (0, 0, 0, errors); }

            // Build lookups
            var soLookupByName = BuildAssetLookupByName();
            var assetLookupByGuid = BuildAssetLookupByGuid();
            var tagLookup = BuildTagLookup();
            var createdThisSession = new Dictionary<string, Object>();

            // Phase 1: ModeConfigs (leaf — no SO refs)
            if (file.modeProfiles != null)
            {
                foreach (var entry in file.modeProfiles)
                {
                    if (!ValidateEntry(entry, "ModeConfig", errors)) continue;
                    var key = $"{entry.directory}/{entry.name}";
                    if (ImportModeConfig(entry, out var inst, out var skipped1, errors))
                    { createdThisSession[key] = inst; created++; }
                    else updated += skipped1;
                }
            }

            // Phase 2: LocomotionSets (ref Clip/StringAsset by GUID)
            if (file.locomotionSets != null)
            {
                foreach (var entry in file.locomotionSets)
                {
                    if (!ValidateEntry(entry, "LocomotionSet", errors)) continue;
                    var key = $"{entry.directory}/{entry.name}";
                    if (ImportLocomotionSet(entry, assetLookupByGuid, out var inst, out var skipped1, errors))
                    { createdThisSession[key] = inst; created++; }
                    else updated += skipped1;
                }
            }

            // Phase 3: LocomotionConfigs + GripTables (ref Phase 1+2 SOs by {directory}/{name})
            if (file.locomotionConfigs != null)
            {
                foreach (var cfg in file.locomotionConfigs)
                {
                    if (!ValidateEntry(cfg, "LocomotionConfig", errors)) continue;
                    var key = $"{cfg.directory}/{cfg.name}";
                    if (ImportLocomotionConfig(cfg, soLookupByName, createdThisSession,
                            out var inst, out var skipped1, errors))
                    { createdThisSession[key] = inst; created++; }
                    else updated += skipped1;
                }
            }
            if (file.gripTables != null)
            {
                foreach (var gt in file.gripTables)
                {
                    if (!ValidateEntry(gt, "GripTable", errors)) continue;
                    var key = $"{gt.directory}/{gt.name}";
                    if (ImportGripTable(gt, soLookupByName, createdThisSession, tagLookup,
                            out var inst, out var skipped1, errors))
                    { createdThisSession[key] = inst; created++; }
                    else updated += skipped1;
                }
            }

            // Phase 4: Profiles (link everything)
            foreach (var entry in file.profiles)
            {
                if (!ValidateEntry(entry, "Profile", errors)) continue;
                var key = $"{entry.directory}/{entry.name}";
                if (ImportProfile(entry, soLookupByName, createdThisSession,
                        out _, out var skipped1, errors))
                    created++;
                else updated += skipped1;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return (created, updated, skipped, errors);
        }

        public static (int created, int updated, int skipped, List<string> errors) ImportFromFile(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                return (0, 0, 0, new List<string> { $"File not found: {jsonPath}" });
            return ImportFromJson(File.ReadAllText(jsonPath));
        }

        // ── Phase 1: ModeConfig ─────────────────────────────

        private static bool ImportModeConfig(ModeConfigEntry entry,
            out AnimationModeConfigSO instance, out int skipped, List<string> errors)
        {
            instance = null;
            skipped = 0;

            var path = AssemblePath(entry);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var existing = AssetDatabase.LoadAssetAtPath<Object>(path);

            if (existing != null)
            {
                if (existing is not AnimationModeConfigSO mode)
                { errors.Add($"[ModeConfig] '{path}' exists but is not AnimationModeConfigSO (type mismatch)"); skipped = 0; return false; }
                ApplyModeConfig(mode, entry, errors);
                EditorUtility.SetDirty(mode);
                instance = mode;
                skipped = 1;
                return false;
            }

            instance = ScriptableObject.CreateInstance<AnimationModeConfigSO>();
            ApplyModeConfig(instance, entry, errors);
            AssetDatabase.CreateAsset(instance, path);
            DataLabelTools.EnsureBootLabel(path);
            instance.name = entry.name;
            return true;
        }

        private static void ApplyModeConfig(AnimationModeConfigSO mode, ModeConfigEntry entry, List<string> errors)
        {
            // Write private fields via reflection
            if (Enum.TryParse<EPosture>(entry.posture, out var posture))
                s_postureField.SetValue(mode, posture);
            else errors.Add($"[ModeConfig] '{entry.name}': invalid posture '{entry.posture}'");

            if (Enum.TryParse<EMovementGait>(entry.gait, out var gait))
                s_gaitField.SetValue(mode, gait);
            else errors.Add($"[ModeConfig] '{entry.name}': invalid gait '{entry.gait}'");

            s_movingTurnSpeedField.SetValue(mode, entry.movingTurnSpeed);
            s_enterAngleField.SetValue(mode, entry.enterAngle);
            s_exitAngleField.SetValue(mode, entry.exitAngle);
        }

        // ── Phase 2: LocomotionSet ──────────────────────────

        private static bool ImportLocomotionSet(LocomotionSetEntry entry,
            Dictionary<string, Object> guidLookup,
            out LocomotionAnimationSetSO instance, out int skipped, List<string> errors)
        {
            instance = null;
            skipped = 0;

            var path = AssemblePath(entry);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var existing = AssetDatabase.LoadAssetAtPath<Object>(path);

            if (existing != null)
            {
                if (existing is not LocomotionAnimationSetSO set)
                { errors.Add($"[LocomotionSet] '{path}' exists but is not LocomotionAnimationSetSO (type mismatch)"); skipped = 1; return false; }
                ApplyLocomotionSet(set, entry, guidLookup, errors);
                EditorUtility.SetDirty(set);
                instance = set;
                skipped = 1;
                return false;
            }

            instance = ScriptableObject.CreateInstance<LocomotionAnimationSetSO>();
            EnsureTransitionsInstantiated(instance);
            ApplyLocomotionSet(instance, entry, guidLookup, errors);
            AssetDatabase.CreateAsset(instance, path);
            DataLabelTools.EnsureBootLabel(path);
            instance.name = entry.name;
            return true;
        }

        private static void EnsureTransitionsInstantiated(LocomotionAnimationSetSO set)
        {
            // ClipTransition / MixerTransition2D / LinearMixerTransition are inline
            // Serializable classes — Unity does NOT auto-instantiate them on CreateInstance.
            // Each must be created before Apply can populate it.
            set.idleL ??= new ClipTransition();
            set.sprint ??= new ClipTransition();
            set.turnInPlace90L ??= new ClipTransition();
            set.turnInPlace90R ??= new ClipTransition();
            set.walkMixer ??= new MixerTransition2D();
            set.runMixer ??= new MixerTransition2D();
            set.airLight ??= new LinearMixerTransition();
            set.airHard ??= new LinearMixerTransition();
            set.landLight ??= new LinearMixerTransition();
            set.landHard ??= new LinearMixerTransition();
            set.climbUpHalfMeter ??= new ClipTransition();
            set.climbUp1meter ??= new ClipTransition();
            set.climbUp2meter ??= new ClipTransition();
            set.climbDown1meter ??= new ClipTransition();
            set.climbDown2meter ??= new ClipTransition();
            set.landFromWall ??= new ClipTransition();
            set.hitReactionFlinch ??= new MixerTransition2D();
            set.hitReactionStagger ??= new MixerTransition2D();
            set.hitReactionKnockdown ??= new MixerTransition2D();
            set.hitReactionGetUp ??= new MixerTransition2D();
        }

        private static void ApplyLocomotionSet(LocomotionAnimationSetSO set, LocomotionSetEntry entry,
            Dictionary<string, Object> guidLookup, List<string> errors)
        {
            ApplyClipTransitionToField(set.idleL, entry.idleL, guidLookup, errors, $"{entry.name}.idleL");
            ApplyClipTransitionToField(set.crouchIdle, entry.crouchIdle, guidLookup, errors, $"{entry.name}.crouchIdle");
            ApplyClipTransitionToField(set.sprint, entry.sprint, guidLookup, errors, $"{entry.name}.sprint");
            ApplyClipTransitionToField(set.turnInPlace90L, entry.turnInPlace90L, guidLookup, errors, $"{entry.name}.turnInPlace90L");
            ApplyClipTransitionToField(set.turnInPlace90R, entry.turnInPlace90R, guidLookup, errors, $"{entry.name}.turnInPlace90R");
            ApplyMixer2D(set.walkMixer, entry.walkMixer, guidLookup, errors, $"{entry.name}.walkMixer");
            ApplyMixer2D(set.runMixer, entry.runMixer, guidLookup, errors, $"{entry.name}.runMixer");
            ApplyMixer2D(set.crouchMixer, entry.crouchMixer, guidLookup, errors, $"{entry.name}.crouchMixer");
            ApplyLinearMixer(set.airLight, entry.airLight, guidLookup, errors, $"{entry.name}.airLight");
            ApplyLinearMixer(set.airHard, entry.airHard, guidLookup, errors, $"{entry.name}.airHard");
            ApplyLinearMixer(set.landLight, entry.landLight, guidLookup, errors, $"{entry.name}.landLight");
            ApplyLinearMixer(set.landHard, entry.landHard, guidLookup, errors, $"{entry.name}.landHard");

            set.walkAnimNativeSpeed = entry.walkAnimNativeSpeed;
            set.runAnimNativeSpeed = entry.runAnimNativeSpeed;
            set.sprintAnimNativeSpeed = entry.sprintAnimNativeSpeed;
            set.crawlAnimNativeSpeed = entry.crawlAnimNativeSpeed;

            set.climbUpHalfMeter ??= new ClipTransition();
            set.climbUp1meter ??= new ClipTransition();
            set.climbUp2meter ??= new ClipTransition();
            set.climbDown1meter ??= new ClipTransition();
            set.climbDown2meter ??= new ClipTransition();
            set.landFromWall ??= new ClipTransition();
            ApplyClipTransitionToField(set.climbUpHalfMeter, entry.climbUpHalfMeter, guidLookup, errors, $"{entry.name}.climbUpHalfMeter");
            ApplyClipTransitionToField(set.climbUp1meter, entry.climbUp1meter, guidLookup, errors, $"{entry.name}.climbUp1meter");
            ApplyClipTransitionToField(set.climbUp2meter, entry.climbUp2meter, guidLookup, errors, $"{entry.name}.climbUp2meter");
            ApplyClipTransitionToField(set.climbDown1meter, entry.climbDown1meter, guidLookup, errors, $"{entry.name}.climbDown1meter");
            ApplyClipTransitionToField(set.climbDown2meter, entry.climbDown2meter, guidLookup, errors, $"{entry.name}.climbDown2meter");
            ApplyClipTransitionToField(set.landFromWall, entry.landFromWall, guidLookup, errors, $"{entry.name}.landFromWall");
            set.hitReactionFlinch ??= new MixerTransition2D();
            set.hitReactionStagger ??= new MixerTransition2D();
            set.hitReactionKnockdown ??= new MixerTransition2D();
            set.hitReactionGetUp ??= new MixerTransition2D();
            ApplyMixer2D(set.hitReactionFlinch, entry.hitReactionFlinch, guidLookup, errors, $"{entry.name}.hitReactionFlinch");
            ApplyMixer2D(set.hitReactionStagger, entry.hitReactionStagger, guidLookup, errors, $"{entry.name}.hitReactionStagger");
            ApplyMixer2D(set.hitReactionKnockdown, entry.hitReactionKnockdown, guidLookup, errors, $"{entry.name}.hitReactionKnockdown");
            ApplyMixer2D(set.hitReactionGetUp, entry.hitReactionGetUp, guidLookup, errors, $"{entry.name}.hitReactionGetUp");
        }

        // ── Phase 3: LocomotionConfig ───────────────────────

        private static bool ImportLocomotionConfig(LocomotionConfigEntry entry,
            Dictionary<string, Object> nameLookup, Dictionary<string, Object> createdThisSession,
            out LocomotionAnimationConfigSO instance, out int skipped, List<string> errors)
        {
            instance = null;
            skipped = 0;

            var path = AssemblePath(entry);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var existing = AssetDatabase.LoadAssetAtPath<Object>(path);

            if (existing != null)
            {
                if (existing is not LocomotionAnimationConfigSO config)
                { errors.Add($"[LocomotionConfig] '{path}' exists but is not LocomotionAnimationConfigSO (type mismatch)"); skipped = 1; return false; }
                ApplyLocomotionConfig(config, entry, nameLookup, createdThisSession, errors);
                EditorUtility.SetDirty(config);
                instance = config;
                skipped = 1;
                return false;
            }

            instance = ScriptableObject.CreateInstance<LocomotionAnimationConfigSO>();
            ApplyLocomotionConfig(instance, entry, nameLookup, createdThisSession, errors);
            AssetDatabase.CreateAsset(instance, path);
            DataLabelTools.EnsureBootLabel(path);
            instance.name = entry.name;
            return true;
        }

        private static void ApplyLocomotionConfig(LocomotionAnimationConfigSO config, LocomotionConfigEntry entry,
            Dictionary<string, Object> nameLookup, Dictionary<string, Object> createdThisSession, List<string> errors)
        {
            // config.headLookSmoothingSpeed — Head Look IK 延后
            config.defaultInPlaceTurnSpeed = entry.defaultInPlaceTurnSpeed;
            config.defaultMovingTurnSpeed = entry.defaultMovingTurnSpeed;
            config.landDistanceThreshold = entry.landDistanceThreshold;
            config.landMinFallDistance = entry.landMinFallDistance;
            config.landLightMaxFallDistance = entry.landLightMaxFallDistance;
            config.landMediumMaxFallDistance = entry.landMediumMaxFallDistance;
            config.landLightTriggerDistance = entry.landLightTriggerDistance;
            config.landMediumTriggerDistance = entry.landMediumTriggerDistance;
            config.landHardTriggerDistance = entry.landHardTriggerDistance;

            config.modeProfiles = ResolveSORefs<AnimationModeConfigSO>(entry.modeProfiles,
                nameLookup, createdThisSession, errors, entry.name);
        }

        // ── Phase 3: GripTable ──────────────────────────────

        private static bool ImportGripTable(GripTableEntry entry,
            Dictionary<string, Object> nameLookup, Dictionary<string, Object> createdThisSession,
            Dictionary<string, RdTagDefSO> tagLookup,
            out GripAnimationTableSO instance, out int skipped, List<string> errors)
        {
            instance = null;
            skipped = 0;

            var path = AssemblePath(entry);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var existing = AssetDatabase.LoadAssetAtPath<Object>(path);

            if (existing != null)
            {
                if (existing is not GripAnimationTableSO table)
                { errors.Add($"[GripTable] '{path}' exists but is not GripAnimationTableSO (type mismatch)"); skipped = 1; return false; }
                ApplyGripTable(table, entry, nameLookup, createdThisSession, tagLookup, errors);
                EditorUtility.SetDirty(table);
                instance = table;
                skipped = 1;
                return false;
            }

            instance = ScriptableObject.CreateInstance<GripAnimationTableSO>();
            ApplyGripTable(instance, entry, nameLookup, createdThisSession, tagLookup, errors);
            AssetDatabase.CreateAsset(instance, path);
            DataLabelTools.EnsureBootLabel(path);
            instance.name = entry.name;
            return true;
        }

        private static void ApplyGripTable(GripAnimationTableSO table, GripTableEntry entry,
            Dictionary<string, Object> nameLookup, Dictionary<string, Object> createdThisSession,
            Dictionary<string, RdTagDefSO> tagLookup, List<string> errors)
        {
            table.defaultSet = ResolveSORef<LocomotionAnimationSetSO>(entry.defaultSet,
                nameLookup, createdThisSession, errors, entry.name);

            if (entry.entries != null)
            {
                var newEntries = new GripAnimationEntry[entry.entries.Length];
                for (int i = 0; i < entry.entries.Length; i++)
                {
                    var item = entry.entries[i];
                    var ge = new GripAnimationEntry();

                    if (!string.IsNullOrEmpty(item.gripTag) && tagLookup.TryGetValue(item.gripTag, out var tag))
                        ge.gripTag = tag;
                    else if (!string.IsNullOrEmpty(item.gripTag))
                        errors.Add($"[GripTable] '{entry.name}': tag '{item.gripTag}' not found");

                    if (!string.IsNullOrEmpty(item.weaponTypeTag) && tagLookup.TryGetValue(item.weaponTypeTag, out var wtTag))
                        ge.weaponTypeTag = wtTag;

                    ge.animationSet = ResolveSORef<LocomotionAnimationSetSO>(item.animationSet,
                        nameLookup, createdThisSession, errors, entry.name);
                    if (!string.IsNullOrEmpty(item.combatSet))
                        ge.combatSet = ResolveSORef<LocomotionAnimationSetSO>(item.combatSet,
                            nameLookup, createdThisSession, errors, entry.name);

                    newEntries[i] = ge; // struct — assign whole instance
                }
                table.entries = newEntries;
            }
        }

        // ── Phase 4: Profile ────────────────────────────────

        private static bool ImportProfile(ProfileEntry entry,
            Dictionary<string, Object> nameLookup, Dictionary<string, Object> createdThisSession,
            out CharacterAnimationProfileSO instance, out int skipped, List<string> errors)
        {
            instance = null;
            skipped = 0;

            var path = AssemblePath(entry);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var existing = AssetDatabase.LoadAssetAtPath<Object>(path);

            if (existing != null)
            {
                if (existing is not CharacterAnimationProfileSO profile)
                { errors.Add($"[Profile] '{path}' exists but is not CharacterAnimationProfileSO (type mismatch)"); skipped = 1; return false; }
                ApplyProfile(profile, entry, nameLookup, createdThisSession, errors);
                EditorUtility.SetDirty(profile);
                instance = profile;
                skipped = 1;
                return false;
            }

            instance = ScriptableObject.CreateInstance<CharacterAnimationProfileSO>();
            ApplyProfile(instance, entry, nameLookup, createdThisSession, errors);
            AssetDatabase.CreateAsset(instance, path);
            DataLabelTools.EnsureBootLabel(path);
            instance.name = entry.name;
            return true;
        }

        private static void ApplyProfile(CharacterAnimationProfileSO profile, ProfileEntry entry,
            Dictionary<string, Object> nameLookup, Dictionary<string, Object> createdThisSession, List<string> errors)
        {
            profile.locomotionConfig = ResolveSORef<LocomotionAnimationConfigSO>(entry.locomotionConfig,
                nameLookup, createdThisSession, errors, entry.name);
            profile.modeProfiles = ResolveSORefs<AnimationModeConfigSO>(entry.modeProfiles,
                nameLookup, createdThisSession, errors, entry.name);
            profile.defaultLocomotionSet = ResolveSORef<LocomotionAnimationSetSO>(entry.defaultLocomotionSet,
                nameLookup, createdThisSession, errors, entry.name);
            profile.gripTable = ResolveSORef<GripAnimationTableSO>(entry.gripTable,
                nameLookup, createdThisSession, errors, entry.name);
        }

        // ── Transition Apply helpers ────────────────────────

        private static void ApplyClipTransitionToField(ClipTransition target, ClipTransitionEntry entry,
            Dictionary<string, Object> guidLookup, List<string> errors, string fieldPath)
        {
            if (target == null) return;
            if (entry == null) return;

            target.Clip = string.IsNullOrEmpty(entry._Clip) ? null
                : guidLookup.TryGetValue(entry._Clip, out var c) ? (AnimationClip)c : null;
            if (!string.IsNullOrEmpty(entry._Clip) && target.Clip == null)
                errors.Add($"[ClipTransition] '{fieldPath}': AnimationClip GUID '{entry._Clip}' not found");

            target.NormalizedStartTime = float.TryParse(entry._NormalizedStartTime,
                NumberStyles.Float, CultureInfo.InvariantCulture, out var t) ? t : float.NaN;
            target.FadeDuration = entry._FadeDuration;
            target.Speed = entry._Speed;

            ApplyEvents(target, entry._Events, guidLookup, errors, fieldPath);
        }

        private static void ApplyMixer2D(MixerTransition2D target, Mixer2DTransitionEntry entry,
            Dictionary<string, Object> guidLookup, List<string> errors, string fieldPath)
        {
            if (target == null) return;
            if (entry == null) return;

            if (entry._Animations != null && entry._Animations.Length > 0)
            {
                target.Animations = entry._Animations.Select((guid, i) =>
                {
                    if (string.IsNullOrEmpty(guid)) return null;
                    if (guidLookup.TryGetValue(guid, out var c)) return c;
                    errors.Add($"[Mixer2D] '{fieldPath}': AnimationClip GUID '{guid}' not found");
                    return null;
                }).ToArray();
            }
            else if (entry._Animations != null && entry._Animations.Length == 0)
            {
                errors.Add($"[Mixer2D] '{fieldPath}': _Animations is empty — mixer will be invalid");
            }

            target.Thresholds = entry._Thresholds;
            target.DefaultParameter = entry._DefaultParameter;

            if (entry._Type == 0 || entry._Type == 1)
                target.Type = (MixerTransition2D.MixerType)entry._Type;
            else
                errors.Add($"[Mixer2D] '{fieldPath}': _Type={entry._Type} invalid (expected 0 or 1)");

            target.ParameterNameX = ResolveStringAsset(entry._ParameterNameX, guidLookup, errors, $"{fieldPath}._ParameterNameX");
            target.ParameterNameY = ResolveStringAsset(entry._ParameterNameY, guidLookup, errors, $"{fieldPath}._ParameterNameY");
            target.Speeds = entry._Speeds != null && entry._Speeds.Length > 0 ? entry._Speeds : null;
            target.SynchronizeChildren = entry._SynchronizeChildren;
            target.FadeDuration = entry._FadeDuration;
            target.Speed = entry._Speed;

            ApplyEvents(target, entry._Events, guidLookup, errors, fieldPath);
        }

        private static void ApplyLinearMixer(LinearMixerTransition target, LinearMixerTransitionEntry entry,
            Dictionary<string, Object> guidLookup, List<string> errors, string fieldPath)
        {
            if (target == null) return;
            if (entry == null) return;

            if (entry._Animations != null && entry._Animations.Length > 0)
            {
                target.Animations = entry._Animations.Select((guid, i) =>
                {
                    if (string.IsNullOrEmpty(guid)) return null;
                    if (guidLookup.TryGetValue(guid, out var c)) return c;
                    errors.Add($"[LinearMixer] '{fieldPath}': AnimationClip GUID '{guid}' not found");
                    return null;
                }).ToArray();
            }
            else if (entry._Animations != null && entry._Animations.Length == 0)
            {
                errors.Add($"[LinearMixer] '{fieldPath}': _Animations is empty — mixer will be invalid");
            }

            target.Thresholds = entry._Thresholds;
            target.DefaultParameter = entry._DefaultParameter;
            target.ExtrapolateSpeed = entry._ExtrapolateSpeed;
            target.ParameterName = ResolveStringAsset(entry._ParameterName, guidLookup, errors, $"{fieldPath}._ParameterName");
            target.Speeds = entry._Speeds != null && entry._Speeds.Length > 0 ? entry._Speeds : null;
            target.SynchronizeChildren = entry._SynchronizeChildren;
            target.FadeDuration = entry._FadeDuration;
            target.Speed = entry._Speed;

            ApplyEvents(target, entry._Events, guidLookup, errors, fieldPath);
        }

        private static void ApplyEvents(ITransition target, TransitionEventsEntry events,
            Dictionary<string, Object> guidLookup, List<string> errors, string fieldPath)
        {
            // Always set SerializedEvents (clears old events on update)
            if (events == null)
            {
                target.SerializedEvents = new AnimancerEvent.Sequence.Serializable();
                return;
            }

            var serializable = new AnimancerEvent.Sequence.Serializable();
            serializable.NormalizedTimes = events._NormalizedTimes ?? System.Array.Empty<float>();

            if (events._Names != null && events._Names.Length > 0)
            {
                serializable.Names = events._Names.Select(n =>
                {
                    if (string.IsNullOrEmpty(n)) return null;
                    if (guidLookup.TryGetValue(n, out var sa) && sa is StringAsset stringAsset)
                        return stringAsset;
                    errors.Add($"[Events] '{fieldPath}': StringAsset GUID '{n}' not found");
                    return null;
                }).ToArray();
            }
            // _Callbacks cannot be deserialized (SerializeReference) — always empty

            target.SerializedEvents = serializable;
        }

        // ── Resolve helpers ─────────────────────────────────

        private static StringAsset ResolveStringAsset(string guid,
            Dictionary<string, Object> guidLookup, List<string> errors, string fieldPath)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            if (guidLookup.TryGetValue(guid, out var obj) && obj is StringAsset sa) return sa;
            errors.Add($"[StringAsset] '{fieldPath}': GUID '{guid}' not found");
            return null;
        }

        private static T ResolveSORef<T>(string refPath,
            Dictionary<string, Object> nameLookup, Dictionary<string, Object> createdThisSession,
            List<string> errors, string ownerName) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(refPath)) return null;
            if (nameLookup.TryGetValue(refPath, out var existing) && existing is T t) return t;
            if (createdThisSession.TryGetValue(refPath, out var created) && created is T tc) return tc;
            errors.Add($"[{typeof(T).Name}] '{ownerName}': referenced SO '{refPath}' not found");
            return null;
        }

        private static T[] ResolveSORefs<T>(string[] refPaths,
            Dictionary<string, Object> nameLookup, Dictionary<string, Object> createdThisSession,
            List<string> errors, string ownerName) where T : ScriptableObject
        {
            if (refPaths == null) return null;
            var list = new List<T>();
            foreach (var rp in refPaths)
            {
                if (string.IsNullOrEmpty(rp)) continue;
                if (nameLookup.TryGetValue(rp, out var existing) && existing is T t) { list.Add(t); continue; }
                if (createdThisSession.TryGetValue(rp, out var created) && created is T tc) { list.Add(tc); continue; }
                errors.Add($"[{typeof(T).Name}] '{ownerName}': referenced SO '{rp}' not found");
            }
            return list.Count > 0 ? list.ToArray() : null;
        }

        // ── Validation ──────────────────────────────────────

        private static bool ValidateEntry<T>(T entry, string typeLabel, List<string> errors) where T : class
        {
            if (entry == null) return false;

            // Use reflection to read name and directory from any DTO entry type
            var nameProp = typeof(T).GetField("name")?.GetValue(entry) as string;
            var dirProp = typeof(T).GetField("directory")?.GetValue(entry) as string;

            if (string.IsNullOrEmpty(nameProp))
            { errors.Add($"[{typeLabel}] Skipping entry: empty name"); return false; }
            if (nameProp.Contains("/") || nameProp.Contains("\\"))
            { errors.Add($"[{typeLabel}] '{nameProp}': name contains path separator — skipping"); return false; }
            if (string.IsNullOrEmpty(dirProp))
            { errors.Add($"[{typeLabel}] '{nameProp}': empty directory — skipping"); return false; }

            return true;
        }

        // ── Path ────────────────────────────────────────────

        private static string AssemblePath<T>(T entry) where T : class
        {
            var nameProp = typeof(T).GetField("name")?.GetValue(entry) as string ?? "";
            var dirProp = typeof(T).GetField("directory")?.GetValue(entry) as string ?? "";
            return $"{AnimationRoot}/{dirProp}/{nameProp}.asset".Replace('\\', '/');
        }

        // ── Lookup builders ─────────────────────────────────

        private static Dictionary<string, Object> BuildAssetLookupByName()
        {
            // Key: {relativeDirectory}/{name}
            var dict = new Dictionary<string, Object>();
            CollectByName<LocomotionAnimationConfigSO>(dict);
            CollectByName<AnimationModeConfigSO>(dict);
            CollectByName<LocomotionAnimationSetSO>(dict);
            CollectByName<GripAnimationTableSO>(dict);
            CollectByName<CharacterAnimationProfileSO>(dict);
            return dict;
        }

        private static void CollectByName<T>(Dictionary<string, Object> dict) where T : ScriptableObject
        {
            var filter = $"t:{typeof(T).Name}";
            foreach (var guid in AssetDatabase.FindAssets(filter))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith(AnimationRoot + "/", StringComparison.Ordinal)) continue;
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null) continue;
                var key = MakePath(asset);
                if (!dict.ContainsKey(key))
                    dict[key] = asset;
            }
        }

        private static Dictionary<string, Object> BuildAssetLookupByGuid()
        {
            var dict = new Dictionary<string, Object>();
            CollectByGuid<AnimationClip>(dict, "t:AnimationClip");
            CollectByGuid<StringAsset>(dict, "t:StringAsset");
            return dict;
        }

        private static void CollectByGuid<T>(Dictionary<string, Object> dict, string filter) where T : Object
        {
            foreach (var guid in AssetDatabase.FindAssets(filter))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null) continue;
                // Use the asset's own GUID as key
                var assetGuid = AssetDatabase.AssetPathToGUID(path);
                if (!string.IsNullOrEmpty(assetGuid) && !dict.ContainsKey(assetGuid))
                    dict[assetGuid] = asset;
            }
        }

        private static Dictionary<string, RdTagDefSO> BuildTagLookup() => RdTagLookup.Build();
    }

    // ═══════════════════════════════════════════════════════════════
    // EditorWindow
    // ═══════════════════════════════════════════════════════════════

    public class AnimationImportWindow : EditorWindow
    {
        private string _filePath = "Assets/Data/Animation/animation_all.json";
        private string _previewText;
        private (int created, int updated, int skipped, List<string> errors) _result;

        [MenuItem("RedDust/Animation Import-Export", priority = 28)]
        public static void Open()
        {
            var window = GetWindow<AnimationImportWindow>("Animation Import-Export");
            window.minSize = new Vector2(520, 420);
            window.Show();
        }

        private void OnGUI()
        {
            EditorImportExport.Draw(
                title: "Animation Import-Export",
                subtitle: "L3_Character · Animation · JSON <-> .asset",
                defaultDir: "Assets/Data/Animation",
                fileExtension: "json",
                defaultFileName: "animation_export",
                filePath: ref _filePath,
                previewText: ref _previewText,
                result: ref _result,
                buildPreview: BuildPreview,
                onImport: path =>
                {
                    return AnimationImporter.ImportFromFile(path);
                },
                onExport: path => File.WriteAllText(path, AnimationImporter.ExportToJson())
            );
        }

        private static string BuildPreview(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            AnimationExportFile preview;
            try { preview = JsonUtility.FromJson<AnimationExportFile>(File.ReadAllText(filePath)); }
            catch { return null; }
            if (preview == null) return null;

            int profiles = preview.profiles?.Length ?? 0;
            int modes = preview.modeProfiles?.Length ?? 0;
            int sets = preview.locomotionSets?.Length ?? 0;
            int configs = preview.locomotionConfigs?.Length ?? 0;
            int grips = preview.gripTables?.Length ?? 0;

            var summary = $"<b>{profiles}</b> profile(s) · <b>{configs}</b> configs · <b>{modes}</b> modes · <b>{sets}</b> sets" +
                $" · <b>{grips}</b> gripTables";
            return $"{summary}\nv{preview.version} · {preview.description ?? "-"}";
        }
    }
}
#endif
