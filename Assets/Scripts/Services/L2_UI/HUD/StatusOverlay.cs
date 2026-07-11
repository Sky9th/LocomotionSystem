using RedDust.Core.GameContext;
using UnityEngine;

namespace RedDust.Services.UI
{

    public class StatusOverlay : UIOverlay
    {
        [Header("Status Container")]
        [SerializeField] private RectTransform statusContainer;

        [Header("Status Entry Prefab")]
        [SerializeField] private GameObject statusEntryPrefab;

        [Header("Layout")]
        [SerializeField] private float refreshRate = 1f;

        private float refreshTimer;

        protected override void OnInitialize()
        {
            // TODO: subscribe to character condition/buff snapshot when system is ready
        }

        private void Update()
        {
            refreshTimer += DeltaTime;
            if (refreshTimer < refreshRate) return;
            refreshTimer = 0f;

            RefreshStatuses();
        }

        private void RefreshStatuses()
        {
            // TODO: read condition data from GameContext snapshot
            // For each active status effect:
            //   - instantiate or reuse statusEntryPrefab
            //   - set icon, duration ring, tooltip text
        }
    }
}
