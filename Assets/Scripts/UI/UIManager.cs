using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : BaseService
{
    [SerializeField] private UIPanelConfigSO panelConfig;
    [SerializeField] private Transform screenContainer;
    [SerializeField] private Transform overlayContainer;
    [SerializeField] private Transform modalContainer;

    private readonly Dictionary<UIPanelId, PanelState> panelStates = new();
    private UIScreen currentScreen;
    private UIPanelId currentScreenId;
    private bool hasCurrentScreen;
    private readonly List<UIOverlay> activeOverlays = new();

    public bool IsInputBlocked { get; private set; }

    protected override bool OnRegister(GameContext context)
    {
        context.RegisterService(this);
        if (panelConfig != null) panelConfig.BuildLookup();
        Logger.Log("[UIManager] Registered.", name);
        return true;
    }

    protected override void OnSubscriptionsActivated()
    {
        if (Dispatcher != null)
            Dispatcher.Subscribe<SGameState>(HandleGameState);
    }

    protected override void OnServicesReady()
    {
        if (GameContext != null && GameContext.TryGetSnapshot(out SGameState state))
            UpdateUIForState(state.CurrentState);
    }

    private void OnDestroy()
    {
        if (Dispatcher != null)
            Dispatcher.Unsubscribe<SGameState>(HandleGameState);
    }

    // ---- Public API ----

    public void ShowScreen(UIPanelId id, object args = null)
    {
        if (!TryGetPanel(id, EUIPanelType.Screen, out UIScreen screen)) return;
        if (screen == currentScreen) return;

        if (currentScreen != null)
        {
            var old = currentScreen;
            var oldId = currentScreenId;
            currentScreen = null;
            hasCurrentScreen = false;

            old.PlayExitSequence().OnComplete(() =>
            {
                Destroy(old.gameObject);
                panelStates.Remove(oldId);
                ActivateScreen(screen, id, args);
            });
        }
        else
        {
            ActivateScreen(screen, id, args);
        }
    }

    private void ActivateScreen(UIScreen screen, UIPanelId id, object args)
    {
        currentScreen = screen;
        currentScreenId = id;
        screen.gameObject.SetActive(true);
        screen.PlayEnterSequence(args);
    }

    public void HideScreen(UIPanelId id)
    {
        if (!TryGetPanel(id, EUIPanelType.Screen, out UIScreen screen)) return;
        if (screen == currentScreen)
        {
            currentScreen = null;
            hasCurrentScreen = false;
        }

        panelStates.Remove(id);
        screen.PlayExitSequence().OnComplete(() =>
        {
            if (screen != null) Destroy(screen.gameObject);
        });
    }

    public void ShowOverlay(UIPanelId id, object args = null)
    {
        if (!TryGetPanel(id, EUIPanelType.Overlay, out UIOverlay overlay)) return;

        var exists = activeOverlays.Find(o => o.gameObject == overlay.gameObject);
        if (exists != null) return;

        activeOverlays.Add(overlay);
        overlay.gameObject.SetActive(true);
        overlay.PlayEnterSequence(args);
    }

    public void HideOverlay(UIPanelId id)
    {
        if (!TryGetPanel(id, EUIPanelType.Overlay, out UIOverlay overlay)) return;
        if (!activeOverlays.Remove(overlay)) return;

        overlay.PlayExitSequence().OnComplete(() =>
        {
            if (overlay != null) Destroy(overlay.gameObject);
        });
    }

    public bool TryGetSnapshot<T>(out T snapshot) where T : struct
    {
        snapshot = default;
        return GameContext != null && GameContext.TryGetSnapshot(out snapshot);
    }

    public void RequestNewGame()
    {
        if (IsInputBlocked) return;
        StartCoroutine(TransitionToGameplay());
    }

    public void RequestQuit()
    {
        Application.Quit();
    }

    // ---- Internal ----

    private bool TryGetPanel<T>(UIPanelId id, EUIPanelType type, out T panel) where T : MonoBehaviour
    {
        panel = null;

        if (panelStates.TryGetValue(id, out var state))
        {
            panel = state.Instance as T;
            return panel != null;
        }

        if (!panelConfig.TryGetEntry(id, out var entry))
        {
            Debug.LogError($"[UIManager] Panel '{id}' not found in PanelConfig. Add it to the config SO.", this);
            return false;
        }

        if (entry.type != type)
        {
            Debug.LogError($"[UIManager] Panel '{id}' type is {entry.type}, expected {type}.", this);
            return false;
        }

        if (entry.prefab == null)
        {
            Debug.LogError($"[UIManager] Panel '{id}' prefab is null.", this);
            return false;
        }

        var container = type switch
        {
            EUIPanelType.Screen => screenContainer,
            EUIPanelType.Overlay => overlayContainer,
            EUIPanelType.Modal => modalContainer,
            _ => transform
        };

        var instance = Instantiate(entry.prefab, container != null ? container : transform);
        instance.name = id.ToString();

        if (instance.TryGetComponent<T>(out var component))
        {
            panel = component;
            panelStates[id] = new PanelState { Instance = panel };

            if (panel is UIScreen screen)
                screen.Initialize(this);
            else if (panel is UIOverlay overlay)
                overlay.Initialize(this);

            return true;
        }

        Destroy(instance);
        return false;
    }

    private IEnumerator TransitionToGameplay()
    {
        IsInputBlocked = true;

        if (currentScreen != null)
        {
            var exitSeq = currentScreen.PlayExitSequence();
            yield return exitSeq.WaitForCompletion();
        }

        var asyncOp = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
        while (!asyncOp.isDone)
            yield return null;

        yield return null;

        if (TryResolveService(out GameState gameState))
            gameState.RequestState(EGameState.Playing);

        IsInputBlocked = false;
    }

    private void HandleGameState(SGameState state, MetaStruct meta)
    {
        UpdateUIForState(state.CurrentState);
    }

    private void UpdateUIForState(EGameState state)
    {
        switch (state)
        {
            case EGameState.MainMenu:
                ShowScreen(UIPanelId.MainMenu);
                break;
            case EGameState.Playing:
                if (hasCurrentScreen)
                    HideScreen(currentScreenId);
                ShowOverlay(UIPanelId.VitalsOverlay);
                break;
        }
    }

    private class PanelState
    {
        public MonoBehaviour Instance;
    }
}
