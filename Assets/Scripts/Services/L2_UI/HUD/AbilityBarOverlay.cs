using System;
using RedDust.Gameplay.Ability;
using RedDust.Services.EntityService;
using UnityEngine;

namespace RedDust.Services.UI
{
    /// <summary>
    /// 技能栏 Overlay。动态槽位 + 事件驱动激活 + hover 弹出 SkillCard。
    /// 纯 Entity.Query.Ability 读数据，不再穿透 Actor/BuildContext。
    /// </summary>
    public class AbilityBarOverlay : UIOverlay
    {
        // TODO: 快捷键配置化 — 目前硬编码 Q~U 七键
        private static readonly string[] Keybinds = { "Q", "W", "E", "R", "T", "Y", "U" };

        [Header("Slot")]
        [SerializeField] private UIIconSlot slotPrefab;
        [SerializeField] private Transform slotContainer;

        [Header("Skill Card")]
        [SerializeField] private SkillCard skillCardPrefab;
        [SerializeField] private Vector2 cardOffset = new(0, 8);

        [Header("Refresh")]
        [SerializeField] private float refreshRate = 0.15f;

        private readonly System.Collections.Generic.List<UIIconSlot> _slots = new();
        private ActiveAbilitySO[] _actives = Array.Empty<ActiveAbilitySO>();
        private float _refreshTimer;
        private SkillCard _skillCard;
        private int _hoveredIndex = -1;

        private void Start()
        {
            if (skillCardPrefab != null)
            {
                _skillCard = Instantiate(skillCardPrefab, transform);
                _skillCard.SetVisible(false);
            }
        }

        private void Update()
        {
            _refreshTimer += DeltaTime;
            if (_refreshTimer < refreshRate) return;
            _refreshTimer = 0f;

            if (uiService == null) return;
            var entity = uiService.PlayerEntity;
            if (entity == null) return;

            var ability = entity.Query.Ability;
            if (ability == null) return;

            var actives = ability.ActiveAbilities;
            _actives = actives;
            EnsureSlots(actives.Length);
            RefreshSlots(ability);
        }

        private void EnsureSlots(int count)
        {
            while (_slots.Count < count)
            {
                var slot = slotContainer != null
                    ? Instantiate(slotPrefab, slotContainer)
                    : Instantiate(slotPrefab);
                slot.name = $"SkillSlot_{_slots.Count}";
                int idx = _slots.Count;
                slot.SetKeybind(idx < Keybinds.Length ? Keybinds[idx] : $"{idx + 1}");

                // Subscribe hover
                var capturedIdx = idx;
                slot.onHoverChanged += (s, hovered) => OnSlotHover(capturedIdx, hovered);

                _slots.Add(slot);
            }

            for (int i = 0; i < _slots.Count; i++)
                _slots[i].gameObject.SetActive(i < count);
        }

        private void RefreshSlots(AbilityQuery ability)
        {
            for (int i = 0; i < _actives.Length && i < _slots.Count; i++)
            {
                var slot = _slots[i];
                var active = _actives[i];
                if (active == null)
                {
                    slot.SetEmpty();
                    continue;
                }

                slot.SetIcon(active.icon);
                slot.SetSlotLabel(active.displayName ?? active.internalName);

                float remaining = ability.GetCooldownRemaining(active);
                slot.SetCooldown(remaining, active.cooldownDuration);
                slot.SetSelected(ability.IsActive(active));
            }
        }

        // ── Hover Tooltip ───────────────────────────────────────────

        private void OnSlotHover(int index, bool hovered)
        {
            if (_skillCard == null) return;

            if (hovered)
            {
                _hoveredIndex = index;
                if (index < _actives.Length && _actives[index] != null)
                {
                    var data = SkillCardData.FromActiveAbility(_actives[index]);
                    _skillCard.SetData(data);

                    // Position card above the slot
                    var slotRT = _slots[index].GetComponent<RectTransform>();
                    if (slotRT != null)
                    {
                        var cardRT = _skillCard.GetComponent<RectTransform>();
                        cardRT.pivot = new Vector2(0.5f, 1f);
                        cardRT.position = slotRT.position + new Vector3(cardOffset.x, cardOffset.y, 0);
                    }

                    _skillCard.SetVisible(true);
                }
            }
            else
            {
                if (index == _hoveredIndex)
                {
                    _hoveredIndex = -1;
                    _skillCard.SetVisible(false);
                }
            }
        }
    }
}
