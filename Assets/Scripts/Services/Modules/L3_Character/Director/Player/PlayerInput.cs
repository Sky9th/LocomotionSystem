using RedDust.Core;
using RedDust.GameInput;
using UnityEngine;

namespace RedDust.Character.Director
{
    /// <summary>
    /// 玩家输入聚合器。纯类，通过 EventHub 订阅事件，缓存帧状态。
    /// 由 CharacterActor 驱动 BindEvents / UnbindEvents。
    /// </summary>
    internal sealed class PlayerInput : IEventListener
    {
        private readonly EventHub eventHub;

        // ── 帧状态 ──
        internal bool SecondaryRequested { get; set; }
        internal bool SprintRequested { get; set; }
        internal bool CrouchRequested { get; set; }
        internal bool ProneRequested { get; set; }
        internal bool StandRequested { get; set; }
        internal bool FirstSkillRequested { get; set; }
        internal bool SecondSkillRequested { get; set; }
        internal bool Equip1Requested { get; set; }
        internal bool Equip2Requested { get; set; }
        internal bool Equip3Requested { get; set; }

        // ── 事件通道 ──
        private CrouchInputEventSO crouchEvent;
        private SprintInputEventSO sprintEvent;
        private ProneInputEventSO proneEvent;
        private StandInputEventSO standEvent;
        private SecondaryInteractInputEventSO secondaryInteractEvent;
        private Skill1InputEventSO firstSkillEvent;
        private Skill2InputEventSO secondSkillEvent;
        private Equip1InputEventSO equip1Event;
        private Equip2InputEventSO equip2Event;
        private Equip3InputEventSO equip3Event;

        // ── TEMP ──
        private EventDispatcherService dispatcher;
        private Vector3 mouseGroundPosition;
        private bool hasMouseGround;
        internal Vector3 MouseGroundPosition => mouseGroundPosition;
        internal bool HasMouseGround => hasMouseGround;

        internal PlayerInput(EventHub eventHub)
        {
            this.eventHub = eventHub;
        }

        public void BindEvents()
        {
            crouchEvent = eventHub.Get<CrouchInputEventSO>();
            sprintEvent = eventHub.Get<SprintInputEventSO>();
            proneEvent = eventHub.Get<ProneInputEventSO>();
            standEvent = eventHub.Get<StandInputEventSO>();
            secondaryInteractEvent = eventHub.Get<SecondaryInteractInputEventSO>();
            firstSkillEvent = eventHub.Get<Skill1InputEventSO>();
            secondSkillEvent = eventHub.Get<Skill2InputEventSO>();
            equip1Event = eventHub.Get<Equip1InputEventSO>();
            equip2Event = eventHub.Get<Equip2InputEventSO>();
            equip3Event = eventHub.Get<Equip3InputEventSO>();

            crouchEvent.OnRaised += OnCrouch;
            sprintEvent.OnRaised += OnSprint;
            proneEvent.OnRaised += OnProne;
            standEvent.OnRaised += OnStand;
            secondaryInteractEvent.OnRaised += OnSecondary;
            firstSkillEvent.OnRaised += OnFirstActivatedSkill;
            secondSkillEvent.OnRaised += OnSecondActivatedSkill;
            equip1Event.OnRaised += OnEquip1;
            equip2Event.OnRaised += OnEquip2;
            equip3Event.OnRaised += OnEquip3;

            // TODO: migrate to EventHub
            if (GameContext.Instance != null &&
                GameContext.Instance.TryResolveService(out dispatcher))
                dispatcher.Subscribe<SCameraSnapshot>(OnCameraSnapshot);
        }

        public void UnbindEvents()
        {
            crouchEvent.OnRaised -= OnCrouch;
            sprintEvent.OnRaised -= OnSprint;
            proneEvent.OnRaised -= OnProne;
            standEvent.OnRaised -= OnStand;
            secondaryInteractEvent.OnRaised -= OnSecondary;
            firstSkillEvent.OnRaised -= OnFirstActivatedSkill;
            secondSkillEvent.OnRaised -= OnSecondActivatedSkill;
            equip1Event.OnRaised -= OnEquip1;
            equip2Event.OnRaised -= OnEquip2;
            equip3Event.OnRaised -= OnEquip3;

            dispatcher?.Unsubscribe<SCameraSnapshot>(OnCameraSnapshot);
            dispatcher = null;
        }

        // ── Handlers ──

        private void OnCrouch() => CrouchRequested = crouchEvent.IsRequested;
        private void OnSprint() => SprintRequested = sprintEvent.IsRequested;
        private void OnProne() => ProneRequested = proneEvent.IsRequested;
        private void OnStand() => StandRequested = standEvent.IsRequested;
        private void OnSecondary() => SecondaryRequested = secondaryInteractEvent.IsRequested;
        private void OnFirstActivatedSkill() => FirstSkillRequested = firstSkillEvent.IsRequested;
        private void OnSecondActivatedSkill() => SecondSkillRequested = secondSkillEvent.IsRequested;
        private void OnEquip1() => Equip1Requested = equip1Event.IsRequested;
        private void OnEquip2() => Equip2Requested = equip2Event.IsRequested;
        private void OnEquip3() => Equip3Requested = equip3Event.IsRequested;

        // TEMP
        private void OnCameraSnapshot(SCameraSnapshot snapshot, MetaStruct _)
        {
            mouseGroundPosition = snapshot.MouseGroundPosition;
            hasMouseGround = snapshot.IsMouseGroundValid;
        }

        internal void ClearFrameSignals()
        {
            SecondaryRequested = SprintRequested = CrouchRequested =
                ProneRequested = StandRequested = FirstSkillRequested =
                SecondSkillRequested = Equip1Requested = Equip2Requested =
                Equip3Requested = false;
        }
    }
}
