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

            crouchEvent.Register(OnCrouch);
            sprintEvent.Register(OnSprint);
            proneEvent.Register(OnProne);
            standEvent.Register(OnStand);
            secondaryInteractEvent.Register(OnSecondary);
            firstSkillEvent.Register(OnFirstActivatedSkill);
            secondSkillEvent.Register(OnSecondActivatedSkill);
            equip1Event.Register(OnEquip1);
            equip2Event.Register(OnEquip2);
            equip3Event.Register(OnEquip3);

            // TODO: migrate to EventHub
            if (GameContext.Instance != null &&
                GameContext.Instance.TryResolveService(out dispatcher))
                dispatcher.Subscribe<SCameraSnapshot>(OnCameraSnapshot);
        }

        public void UnbindEvents()
        {
            crouchEvent.Unregister(OnCrouch);
            sprintEvent.Unregister(OnSprint);
            proneEvent.Unregister(OnProne);
            standEvent.Unregister(OnStand);
            secondaryInteractEvent.Unregister(OnSecondary);
            firstSkillEvent.Unregister(OnFirstActivatedSkill);
            secondSkillEvent.Unregister(OnSecondActivatedSkill);
            equip1Event.Unregister(OnEquip1);
            equip2Event.Unregister(OnEquip2);
            equip3Event.Unregister(OnEquip3);

            dispatcher?.Unsubscribe<SCameraSnapshot>(OnCameraSnapshot);
            dispatcher = null;
        }

        // ── Handlers ──

        private void OnCrouch(SButtonInputPayload p) => CrouchRequested = p.IsRequested;
        private void OnSprint(SButtonInputPayload p) => SprintRequested = p.IsRequested;
        private void OnProne(SButtonInputPayload p) => ProneRequested = p.IsRequested;
        private void OnStand(SButtonInputPayload p) => StandRequested = p.IsRequested;
        private void OnSecondary(SButtonInputPayload p) => SecondaryRequested = p.IsRequested;
        private void OnFirstActivatedSkill(SButtonInputPayload p) => FirstSkillRequested = p.IsRequested;
        private void OnSecondActivatedSkill(SButtonInputPayload p) => SecondSkillRequested = p.IsRequested;
        private void OnEquip1(SButtonInputPayload p) => Equip1Requested = p.IsRequested;
        private void OnEquip2(SButtonInputPayload p) => Equip2Requested = p.IsRequested;
        private void OnEquip3(SButtonInputPayload p) => Equip3Requested = p.IsRequested;

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
