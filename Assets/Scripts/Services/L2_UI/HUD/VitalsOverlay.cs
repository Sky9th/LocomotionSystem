using RedDust.Properties;
using UnityEngine;

namespace RedDust.UI
{
    public class VitalsOverlay : UIOverlay
    {
        [Header("Stat Bars")]
        [SerializeField] private UIStatBar hpBar;
        [SerializeField] private UIStatBar hungerBar;
        [SerializeField] private UIStatBar thirstBar;
        [SerializeField] private UIStatBar staminaBar;

        private const string hpStatPath = "Vitals/HP";
        private const string hungerStatPath = "Vitals/Hunger";
        private const string thirstStatPath = "Vitals/Thirst";
        private const string staminaStatPath = "Vitals/Stamina";

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
            if (!uiService.TryGetPlayerProps(out var props)) return;

            TryUpdateBar(hpBar, hpStatPath, props);
            TryUpdateBar(hungerBar, hungerStatPath, props);
            TryUpdateBar(thirstBar, thirstStatPath, props);
            TryUpdateBar(staminaBar, staminaStatPath, props);
        }

        private static void TryUpdateBar(UIStatBar bar, string path, IPropertyReader props)
        {
            if (bar == null) return;
            bar.SetValue(props.GetFloat(path), props.GetMax(path));
        }
    }
}


