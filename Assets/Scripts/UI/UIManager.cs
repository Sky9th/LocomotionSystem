using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central coordinator for all runtime UI. Manages fullscreen screens and overlays
/// (including debug panels) and provides a simple API for navigation.
/// </summary>
[DisallowMultipleComponent]
public class UIManager : BaseService
{
    [Header("Root Canvas")]
    [SerializeField] private Canvas rootCanvas;

    [Header("Screens (optional in first iteration)")]
    [SerializeField] private UIScreenEntry[] screens = Array.Empty<UIScreenEntry>();

    [Header("Overlays")]
    [SerializeField] private UIOverlayEntry[] overlays = Array.Empty<UIOverlayEntry>();

    // Configuration lookup by id.
    private readonly Dictionary<string, UIScreenBase> screenLookup = new();
    private readonly Dictionary<string, UIOverlayBase> overlayLookup = new();

    // Runtime instances (scene objects or instantiated prefabs).
    private readonly Dictionary<string, UIScreenBase> screenInstances = new();
    private readonly Dictionary<string, UIOverlayBase> overlayInstances = new();

    // Initial active state of scene-based UI elements, recorded at registration.
    private readonly Dictionary<string, bool> screenInitialActive = new();
    private readonly Dictionary<string, bool> overlayInitialActive = new();

    private UIScreenBase activeScreen;

    protected override bool OnRegister(GameContext context)
    {
        context.RegisterService(this);

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInChildren<Canvas>();
            if (rootCanvas == null)
            {
                Debug.LogWarning($"{name} is missing a Canvas reference. UI may not be visible.", this);
            }
        }

        BuildLookupsAndResetInstances();
        return true;
    }

    private void BuildLookupsAndResetInstances()
    {
        screenLookup.Clear();
        overlayLookup.Clear();
        screenInstances.Clear();
        overlayInstances.Clear();
        screenInitialActive.Clear();
        overlayInitialActive.Clear();

        foreach (var entry in screens)
        {
            if (string.IsNullOrWhiteSpace(entry.id) || entry.screen == null)
            {
                continue;
            }

            if (screenLookup.ContainsKey(entry.id))
            {
                Debug.LogWarning($"Duplicate screen id '{entry.id}' in {name}.", this);
                continue;
            }

            screenLookup.Add(entry.id, entry.screen);

            var screen = entry.screen;
            if (screen != null && screen.gameObject.scene.IsValid())
            {
                bool wasActive = screen.gameObject.activeSelf;
                screenInitialActive[entry.id] = wasActive;
                screen.SetVisible(false);
                screenInstances[entry.id] = screen;
            }
        }

        foreach (var entry in overlays)
        {
            if (string.IsNullOrWhiteSpace(entry.id) || entry.overlay == null)
            {
                continue;
            }

            if (overlayLookup.ContainsKey(entry.id))
            {
                Debug.LogWarning($"Duplicate overlay id '{entry.id}' in {name}.", this);
                continue;
            }

            overlayLookup.Add(entry.id, entry.overlay);

            var overlay = entry.overlay;
            if (overlay != null && overlay.gameObject.scene.IsValid())
            {
                bool wasActive = overlay.gameObject.activeSelf;
                overlayInitialActive[entry.id] = wasActive;
                overlay.SetVisible(false);
                overlayInstances[entry.id] = overlay;
            }
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Restore default visibility for any scene-based screens/overlays that were
        // authored as active in the editor.
        foreach (var pair in screenInitialActive)
        {
            if (pair.Value)
            {
                ShowScreen(pair.Key);
            }
        }

        foreach (var pair in overlayInitialActive)
        {
            if (pair.Value)
            {
                ShowOverlay(pair.Key);
            }
        }
    }

    #region Screen API

    public void ShowScreen(string screenId, object payload = null)
    {
        if (!TryGetScreenInstance(screenId, out var target))
        {
            return;
        }

        if (activeScreen == target)
        {
            return;
        }

        if (activeScreen != null)
        {
            activeScreen.OnExit();
        }

        activeScreen = target;
        activeScreen.OnEnter(payload);
    }

    #endregion

    #region Overlay API

    public void ShowOverlay(string overlayId, object payload = null)
    {
        if (!TryGetOverlayInstance(overlayId, out var overlay))
        {
            return;
        }

        overlay.OnShow(payload);
    }

    public void HideOverlay(string overlayId)
    {
        if (!TryGetOverlayInstance(overlayId, out var overlay))
        {
            return;
        }

        overlay.OnHide();
    }

    public void ToggleOverlay(string overlayId, object payload = null)
    {
        if (!TryGetOverlayInstance(overlayId, out var overlay))
        {
            return;
        }

        if (overlay.IsVisible)
        {
            overlay.OnHide();
        }
        else
        {
            overlay.OnShow(payload);
        }
    }

    public void ToggleDebugOverlay(string overlayId)
    {
        ToggleOverlay(overlayId);
    }

    #endregion

    #region Instance Resolution

    private bool TryGetScreenInstance(string screenId, out UIScreenBase screen)
    {
        screen = null;
        if (string.IsNullOrWhiteSpace(screenId))
        {
            return false;
        }

        if (!screenLookup.TryGetValue(screenId, out var config) || config == null)
        {
            Debug.LogWarning($"{name} could not find screen with id '{screenId}'.", this);
            return false;
        }

        if (screenInstances.TryGetValue(screenId, out screen) && screen != null)
        {
            return true;
        }

        // If the referenced screen lives in the active scene, just use it.
        if (config.gameObject.scene.IsValid())
        {
            screen = config;
        }
        else
        {
            if (rootCanvas == null)
            {
                Debug.LogWarning($"{name} cannot instantiate screen '{screenId}' because rootCanvas is missing.", this);
                return false;
            }

            screen = Instantiate(config, rootCanvas.transform);
            screen.SetVisible(false);
        }

        screenInstances[screenId] = screen;
        return screen != null;
    }

    private bool TryGetOverlayInstance(string overlayId, out UIOverlayBase overlay)
    {
        overlay = null;
        if (string.IsNullOrWhiteSpace(overlayId))
        {
            return false;
        }

        if (!overlayLookup.TryGetValue(overlayId, out var config) || config == null)
        {
            Debug.LogWarning($"{name} could not find overlay with id '{overlayId}'.", this);
            return false;
        }

        if (overlayInstances.TryGetValue(overlayId, out overlay) && overlay != null)
        {
            return true;
        }

        if (config.gameObject.scene.IsValid())
        {
            overlay = config;
        }
        else
        {
            if (rootCanvas == null)
            {
                Debug.LogWarning($"{name} cannot instantiate overlay '{overlayId}' because rootCanvas is missing.", this);
                return false;
            }

            overlay = Instantiate(config, rootCanvas.transform);
            overlay.SetVisible(false);
        }

        overlayInstances[overlayId] = overlay;
        return overlay != null;
    }

    #endregion
}