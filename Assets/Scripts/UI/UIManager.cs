using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central runtime entry point for all game UI.
/// Manages UIScreenBase (full-screen) and UIOverlayBase (overlay) instances
/// and exposes a simple API for showing, hiding and toggling them.
///
/// Lifecycle is coordinated by GameManager via BaseService hooks:
/// - Register(GameContext): cache context, build lookup tables, record initial active states.
/// - AttachDispatcher(EventDispatcher): cache dispatcher reference (no subscriptions yet).
/// - ActivateSubscriptions(): future extension point for input / UI events.
/// - OnInitialized(): restore default visible screens/overlays based on initial activeSelf.
/// </summary>
public class UIManager : BaseService
{
    [Header("UI Roots")]
    [SerializeField] private Transform uiRoot;
    [SerializeField] private Transform screensRoot;
    [SerializeField] private Transform overlaysRoot;

    [Header("Screen Config")]
    [SerializeField] private UIScreenConfig screenConfig;

    [Header("Overlay Config")]
    [SerializeField] private UIOverlayConfig overlayConfig;

    private readonly Dictionary<string, UIScreenBase> screensById = new();
    private readonly Dictionary<string, UIOverlayBase> overlaysById = new();

    private readonly HashSet<string> activeOverlayIds = new();

    private UIScreenBase currentScreen;
    private string currentScreenId;

    /// <summary>
    /// Called by GameManager during bootstrap to bind this manager to the GameContext
    /// and prepare internal lookup structures.
    /// </summary>
    protected override bool OnRegister(GameContext context)
    {
        if (context == null)
        {
            Debug.LogError("UIManager requires a valid GameContext reference.", this);
            return false;
        }

        BuildScreenLookup();
        BuildOverlayLookup();

        return true;
    }

    private void BuildScreenLookup()
    {
        screensById.Clear();
        currentScreen = null;
        currentScreenId = null;

        var entries = screenConfig != null ? screenConfig.Screens : null;
        if (entries == null || entries.Length == 0)
        {
            Debug.LogWarning("UIManager has no UIScreenConfig or it contains no entries.", this);
            return;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            if (string.IsNullOrEmpty(entry.id))
            {
                Debug.LogWarning($"UIManager has a UIScreenEntry with an empty id at index {i}.", this);
                continue;
            }

            if (entry.screen == null)
            {
                Debug.LogWarning($"UIManager UIScreenEntry '{entry.id}' has no screen reference.", this);
                continue;
            }

            if (screensById.ContainsKey(entry.id))
            {
                Debug.LogWarning($"UIManager detected duplicate screen id '{entry.id}'. Only the first instance will be used.", this);
                continue;
            }

            // Entries in the ScriptableObject are treated as prefabs.
            // Instantiate them under the configured screens root (or uiRoot/this.transform)
            // and keep the instances disabled until initialization completes.
            var parent = screensRoot != null ? screensRoot : (uiRoot != null ? uiRoot : transform);
            var instance = Instantiate(entry.screen, parent);
            instance.gameObject.SetActive(false);

            // Inject GameContext into the instantiated screen so it can
            // access shared runtime data if needed.
            instance.Registered(GameContext);

            screensById.Add(entry.id, instance);
        }
    }

    private void BuildOverlayLookup()
    {
        overlaysById.Clear();
        activeOverlayIds.Clear();

        var entries = overlayConfig != null ? overlayConfig.Overlays : null;
        if (entries == null || entries.Length == 0)
        {
            Debug.LogWarning("UIManager has no UIOverlayConfig or it contains no entries.", this);
            return;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            if (string.IsNullOrEmpty(entry.id))
            {
                Debug.LogWarning($"UIManager has a UIOverlayEntry with an empty id at index {i}.", this);
                continue;
            }

            if (entry.overlay == null)
            {
                Debug.LogWarning($"UIManager UIOverlayEntry '{entry.id}' has no overlay reference.", this);
                continue;
            }

            if (overlaysById.ContainsKey(entry.id))
            {
                Debug.LogWarning($"UIManager detected duplicate overlay id '{entry.id}'. Only the first instance will be used.", this);
                continue;
            }

            // Entries in the ScriptableObject are treated as prefabs.
            // Instantiate them under the configured overlays root (or uiRoot/this.transform)
            // and keep the instances disabled until initialization completes.
            var parent = overlaysRoot != null ? overlaysRoot : (uiRoot != null ? uiRoot : transform);
            var instance = Instantiate(entry.overlay, parent);
            instance.gameObject.SetActive(false);

            // Inject GameContext into the instantiated overlay so it can
            // access shared runtime data if needed.
            instance.Registered(GameContext);

            overlaysById.Add(entry.id, instance);
        }
    }

    /// <summary>
    /// Called by GameManager after all services registered and subscriptions are active.
    /// At this stage UIManager is allowed to begin showing UI.
    /// It will scan all known elements and automatically show any whose
    /// logical Visible flag is true (for example LocomotionDebugOverlay).
    /// </summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Activate the first screen which is marked as logically visible.
        foreach (var pair in screensById)
        {
            var screen = pair.Value;
            if (screen != null && screen.Visible)
            {
                ShowScreen(pair.Key);
                break;
            }
        }

        // Activate all overlays which are marked as logically visible.
        foreach (var pair in overlaysById)
        {
            var overlay = pair.Value;
            if (overlay != null && overlay.Visible)
            {
                ShowOverlay(pair.Key);
            }
        }
    }

    /// <summary>
    /// Switch the active full-screen UI to the specified screen id.
    /// Only one screen can be active at a time.
    /// </summary>
    public void ShowScreen(string screenId, object payload = null)
    {
        if (string.IsNullOrEmpty(screenId))
        {
            Debug.LogWarning("UIManager.ShowScreen was called with an empty id.", this);
            return;
        }

        if (!screensById.TryGetValue(screenId, out var targetScreen) || targetScreen == null)
        {
            Debug.LogWarning($"UIManager could not find UIScreen with id '{screenId}'.", this);
            return;
        }

        if (currentScreen == targetScreen)
        {
            // Already showing the requested screen; allow re-enter with new payload if desired.
            currentScreen.OnEnter(payload);
            return;
        }

        if (currentScreen != null)
        {
            currentScreen.OnExit();
            currentScreen.SetVisible(false);
        }

        currentScreen = targetScreen;
        currentScreenId = screenId;

        currentScreen.gameObject.SetActive(true);
        currentScreen.SetVisible(true);
        currentScreen.OnEnter(payload);
    }

    /// <summary>
    /// Show (or refresh) an overlay by id. Multiple overlays can be visible at once.
    /// </summary>
    public void ShowOverlay(string overlayId, object payload = null)
    {
        if (string.IsNullOrEmpty(overlayId))
        {
            Debug.LogWarning("UIManager.ShowOverlay was called with an empty id.", this);
            return;
        }

        if (!overlaysById.TryGetValue(overlayId, out var overlay) || overlay == null)
        {
            Debug.LogWarning($"UIManager could not find UIOverlay with id '{overlayId}'.", this);
            return;
        }

        overlay.gameObject.SetActive(true);
        overlay.SetVisible(true);
        overlay.OnShow(payload);
        activeOverlayIds.Add(overlayId);
    }

    /// <summary>
    /// Hide an overlay by id if it is currently visible.
    /// </summary>
    public void HideOverlay(string overlayId)
    {
        if (string.IsNullOrEmpty(overlayId))
        {
            Debug.LogWarning("UIManager.HideOverlay was called with an empty id.", this);
            return;
        }

        if (!overlaysById.TryGetValue(overlayId, out var overlay) || overlay == null)
        {
            return;
        }

        if (!activeOverlayIds.Contains(overlayId))
        {
            // If we are not tracking this as active, still attempt to hide its GameObject.
            overlay.SetVisible(false);
            return;
        }

        overlay.gameObject.SetActive(false);
        overlay.OnHide();
        overlay.SetVisible(false);
        activeOverlayIds.Remove(overlayId);
    }

    /// <summary>
    /// Toggle an overlay's visibility state.
    /// Intended for Debug UI panels or temporary overlays.
    /// </summary>
    public void ToggleDebugOverlay(string overlayId, object payload = null)
    {
        if (string.IsNullOrEmpty(overlayId))
        {
            Debug.LogWarning("UIManager.ToggleDebugOverlay was called with an empty id.", this);
            return;
        }

        if (activeOverlayIds.Contains(overlayId))
        {
            HideOverlay(overlayId);
        }
        else
        {
            ShowOverlay(overlayId, payload);
        }
    }
}