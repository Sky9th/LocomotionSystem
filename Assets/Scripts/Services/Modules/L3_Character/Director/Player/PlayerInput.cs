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
        internal bool SencondSkillRequested { get; set; }

        // ── 事件通道 ──
        private CrouchInputEventSO crouchEvent;
        private SprintInputEventSO sprintEvent;
        private ProneInputEventSO proneEvent;
        private StandInputEventSO standEvent;
        private SecondaryInteractInputEventSO secondaryInteractEvent;
        private Skill1InputEventSO firstSkillEvent;
        private Skill2InputEventSO secondSkillEvent;

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

            crouchEvent.OnRaised += OnCrouch;
            sprintEvent.OnRaised += OnSprint;
            proneEvent.OnRaised += OnProne;
            standEvent.OnRaised += OnStand;
            secondaryInteractEvent.OnRaised += OnSecondary;
            firstSkillEvent.OnRaised += OnFirstActivatedSkill;
            secondSkillEvent.OnRaised += OnSecondActivatedSkill;

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
        private void OnSecondActivatedSkill() => SencondSkillRequested = secondSkillEvent.IsRequested;

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
                SencondSkillRequested = false;
        }
    }
}
