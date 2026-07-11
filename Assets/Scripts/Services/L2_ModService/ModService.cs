using RedDust.Core.GameService;
using RedDust.Core.GameContext;
using RedDust.Core.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using RedDust.Core.Events;
using RedDust.Shared;
using UnityEngine;

namespace RedDust.Services.ModService
{
    /// <summary>
    /// L2 service: Mod loader. Scans mod folders, reads manifest.json,
    /// loads mod DLLs via Assembly.Load (HybridCLR-intercepted in IL2CPP),
    /// discovers [ModEntry] classes implementing IModEntry, and invokes Initialize().
    ///
    /// Manually attached as a child GameObject under GameService in the scene.
    /// ModuleHub.Awake() auto-discovers it.
    /// LoadAllMods() is called by SceneService.EnsureBootReady() after AOT metadata loads.
    ///
    /// TODO S1: dependency topological sort (mod-architecture-framework §4.1)
    /// TODO S1: Mod ID conflict detection + loadPriority (§4.3)
    /// TODO S1: ModManifest add dependencies[], loadPriority, content fields
    /// </summary>
    public class ModService : ModuleChildMono
    {
        private const string ModsDirectoryName = "Mods";
        private const string ManifestFileName = "manifest.json";

        private LogChannel _log;
        private readonly List<ModLoadResult> _results = new();

        public IReadOnlyList<ModLoadResult> Results => _results;
        public int LoadedCount => _results.Count;

        // ── ModuleChildMono lifecycle ──

        public override void OnAssemble()
        {
            _log = LogManager.GetChannel(GetType().Name);
            GameContext.Instance.RegisterService(this);
        }

        public override void OnWire()
        {
            // No cross-service dependencies at this stage.
        }

        // ── Public API ──

        /// <summary>
        /// Scan all mod folders, load manifests, load DLLs, discover entry points,
        /// invoke Initialize(). Must be called AFTER HybridCLR AOT metadata is loaded.
        /// Each mod is loaded independently; failure in one mod does not block others.
        /// </summary>
        public void LoadAllMods()
        {
            string scanPath = ResolveModsPath();

            if (!Directory.Exists(scanPath))
            {
                _log.Info($"Mods directory not found at '{scanPath}'. Creating.");
                Directory.CreateDirectory(scanPath);
                return;
            }

            var modDirs = Directory.GetDirectories(scanPath);

            if (modDirs.Length == 0)
            {
                _log.Info("No mod folders found. Skipping mod loading.");
                return;
            }

            _log.Info($"Scanning {modDirs.Length} mod folder(s) in '{scanPath}'...");

            foreach (var modDir in modDirs)
            {
                var folderName = Path.GetFileName(modDir);
                _log.Debug($"Processing folder: '{folderName}'");
                LoadSingleMod(modDir);
            }

            int successCount = 0;
            foreach (var r in _results)
                if (r.Success) successCount++;

            _log.Info($"Mod loading complete. {successCount}/{_results.Count} loaded successfully.");

            foreach (var r in _results)
            {
                if (!r.Success)
                    _log.Warning($"  Failed: [{r.ModId}] {r.Error}");
            }
        }

        // ── Private ──

        private string ResolveModsPath()
        {
            // Mods live next to the game executable (or project root in Editor).
            // Application.dataPath → Editor: {project}/Assets | Build: {build}/Game_Data
            var gameRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var modsPath = Path.Combine(gameRoot, ModsDirectoryName);
            _log.Info($"Mods root: {modsPath}");
            return modsPath;
        }

        private void LoadSingleMod(string modDir)
        {
            var folderName = Path.GetFileName(modDir);

            // ── Step 1: Read manifest.json ──
            _log.Debug($"[{folderName}] Reading {ManifestFileName}...");

            var manifestPath = Path.Combine(modDir, ManifestFileName);
            ModManifest manifest;

            try
            {
                if (!File.Exists(manifestPath))
                {
                    _log.Warning($"[{folderName}] {ManifestFileName} not found. Skipping.");
                    return;
                }

                var json = File.ReadAllText(manifestPath);

                _log.Debug($"[{folderName}] Parsing manifest...");
                manifest = JsonUtility.FromJson<ModManifest>(json);

                if (manifest == null || string.IsNullOrWhiteSpace(manifest.modId))
                {
                    _log.Warning($"[{folderName}] Invalid manifest: modId is null or empty. Skipping.");
                    return;
                }
            }
            catch (Exception ex)
            {
                _log.Warning($"[{folderName}] Failed to read/parse {ManifestFileName}: {ex.GetType().Name} — {ex.Message}");
                return;
            }

            _log.Info($"[{manifest.modId}] v{manifest.version} by {manifest.author} — {manifest.name}");

            // ── Step 2: Find DLL files ──
            var dllFiles = Directory.GetFiles(modDir, "*.dll");

            if (dllFiles.Length == 0)
            {
                _log.Info($"[{manifest.modId}] No .dll found (data-only mod). Skipping.");
                _results.Add(new ModLoadResult
                {
                    ModId = manifest.modId,
                    FolderName = folderName,
                    Success = false,
                    Error = "No .dll files found."
                });
                return;
            }

            var fileNames = new List<string>();
            foreach (var f in dllFiles)
                fileNames.Add(Path.GetFileName(f));
            _log.Debug($"[{manifest.modId}] Found {dllFiles.Length} .dll file(s): {string.Join(", ", fileNames)}");

            // ── Step 3: Load DLLs via Assembly.Load ──
            foreach (var dllPath in dllFiles)
            {
                var dllName = Path.GetFileName(dllPath);
                Assembly modAssembly = null;

                try
                {
                    long fileSize = new FileInfo(dllPath).Length;
                    _log.Debug($"[{manifest.modId}] Loading: {dllName} ({fileSize} bytes)...");

                    var dllBytes = File.ReadAllBytes(dllPath);
                    modAssembly = Assembly.Load(dllBytes);

                    _log.Info($"[{manifest.modId}] Assembly loaded: {modAssembly.GetName().Name} v{modAssembly.GetName().Version}");

                    // ── Step 4: Discover [ModEntry] classes ──
                    DiscoverAndInvokeEntries(manifest.modId, folderName, modAssembly);
                }
                catch (BadImageFormatException ex)
                {
                    _log.Error($"[{manifest.modId}] Failed to load '{dllName}': {ex.GetType().Name} — {ex.Message}");
                }
                catch (FileLoadException ex)
                {
                    _log.Error($"[{manifest.modId}] Failed to load '{dllName}': {ex.GetType().Name} — {ex.Message}");
                }
                catch (Exception ex)
                {
                    _log.Error($"[{manifest.modId}] Unexpected error loading '{dllName}': {ex.GetType().Name} — {ex.Message}");
                }
            }
        }

        private void DiscoverAndInvokeEntries(string modId, string folderName, Assembly modAssembly)
        {
            Type[] exportedTypes;

            try
            {
                exportedTypes = modAssembly.GetExportedTypes();
            }
            catch (Exception ex)
            {
                _log.Error($"[{modId}] Failed to enumerate exported types: {ex.GetType().Name} — {ex.Message}");
                return;
            }

            _log.Debug($"[{modId}] Scanning {exportedTypes.Length} exported type(s) for [ModEntry]...");

            int entryCount = 0;
            foreach (var type in exportedTypes)
            {
                if (type.GetCustomAttribute<ModEntryAttribute>() == null)
                    continue;

                _log.Debug($"[{modId}] [ModEntry] found: {type.FullName}");

                // Check IModEntry implementation
                _log.Debug($"[{modId}]   Checking IModEntry...");
                if (!typeof(IModEntry).IsAssignableFrom(type))
                {
                    _log.Warning($"[{modId}] {type.Name} has [ModEntry] but doesn't implement IModEntry. Skipping.");
                    continue;
                }

                // Create instance (requires parameterless constructor)
                object instance;
                try
                {
                    instance = Activator.CreateInstance(type);
                }
                catch (MissingMethodException)
                {
                    _log.Error($"[{modId}] {type.Name} has no parameterless constructor. Skipping.");
                    continue;
                }
                catch (Exception ex)
                {
                    _log.Error($"[{modId}] Failed to create instance of {type.Name}: {ex.GetType().Name} — {ex.Message}");
                    continue;
                }

                var entry = (IModEntry)instance;

                // Invoke Initialize()
                try
                {
                    _log.Debug($"[{modId}]   Calling Initialize()...");
                    entry.Initialize();
                    _log.Info($"[{modId}] Mod entry initialized: {type.Name}");

                    _results.Add(new ModLoadResult
                    {
                        ModId = modId,
                        FolderName = folderName,
                        AssemblyName = modAssembly.GetName().Name,
                        Success = true
                    });
                    entryCount++;
                }
                catch (Exception ex)
                {
                    _log.Error($"[{modId}] Initialize() threw: {ex.GetType().Name} — {ex.Message}");

                    _results.Add(new ModLoadResult
                    {
                        ModId = modId,
                        FolderName = folderName,
                        AssemblyName = modAssembly.GetName().Name,
                        Success = false,
                        Error = $"Initialize() threw: {ex.GetType().Name} — {ex.Message}"
                    });
                }
            }

            if (entryCount == 0)
            {
                _log.Warning($"[{modId}] No valid [ModEntry] class found in assembly.");

                // Avoid duplicate entries if some failed during discovery
                bool alreadyRecorded = false;
                foreach (var r in _results)
                {
                    if (r.ModId == modId)
                    {
                        alreadyRecorded = true;
                        break;
                    }
                }
                if (!alreadyRecorded)
                {
                    _results.Add(new ModLoadResult
                    {
                        ModId = modId,
                        FolderName = folderName,
                        AssemblyName = modAssembly.GetName().Name,
                        Success = false,
                        Error = "No valid [ModEntry] class found."
                    });
                }
            }
        }
    }

    /// <summary>
    /// Per-mod load result. Stored for diagnostics and future Mod Management UI.
    /// </summary>
    public class ModLoadResult
    {
        public string ModId;
        public string FolderName;
        public string AssemblyName;
        public bool Success;
        public string Error;
    }
}
