using System;
using UnityEngine;

public class VitalsOverlay : UIOverlay
{
    [Header("Stat Bars")]
    [SerializeField] private UIStatBar hpBar;
    [SerializeField] private UIStatBar hungerBar;
    [SerializeField] private UIStatBar thirstBar;
    [SerializeField] private UIStatBar staminaBar;

    [Header("Stat Paths")]
    [SerializeField] private string hpStatPath = "Vitals/HP";
    [SerializeField] private string hungerStatPath = "Vitals/Hunger";
    [SerializeField] private string thirstStatPath = "Vitals/Thirst";
    [SerializeField] private string staminaStatPath = "Vitals/Stamina";

    [Header("Refresh")]
    [SerializeField] private float refreshRate = 0.1f;

    private float refreshTimer;

    protected override void OnInitialize()
    {
        if (hpBar != null) hpBar.SetName("HP");
        if (hungerBar != null) hungerBar.SetName("Hunger");
        if (thirstBar != null) thirstBar.SetName("Thirst");
        if (staminaBar != null) staminaBar.SetName("Stamina");
    }

    private void Update()
    {
        refreshTimer += DeltaTime;
        if (refreshTimer < refreshRate) return;
        refreshTimer = 0f;

        if (uiService == null) return;
        if (!uiService.TryGetPlayerStats(out var stats)) return;

        TryUpdateBar(hpBar, hpStatPath, stats);
        TryUpdateBar(hungerBar, hungerStatPath, stats);
        TryUpdateBar(thirstBar, thirstStatPath, stats);
        TryUpdateBar(staminaBar, staminaStatPath, stats);
    }

    private void TryUpdateBar(UIStatBar bar, string path,
        System.Collections.Generic.Dictionary<string, (float current, float max)> stats)
    {
        if (bar == null) return;
        if (stats.TryGetValue(path, out var stat))
            bar.SetValue(stat.current, stat.max);
    }
}
