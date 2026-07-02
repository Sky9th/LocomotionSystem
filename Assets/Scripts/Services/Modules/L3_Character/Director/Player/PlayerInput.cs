using RedDust.Core;
using RedDust.Core.Events;
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
        private InputCrouchEvent crouchEvent;
        private InputSprintEvent sprintEvent;
        private InputProneEvent proneEvent;
        private InputStandEvent standEvent;
        private InputSecondaryInteractEvent secondaryInteractEvent;
        private InputSkill1Event firstSkillEvent;
        private InputSkill2Event secondSkillEvent;
        private InputEquip1Event equip1Event;
        private InputEquip2Event equip2Event;
        private InputEquip3Event equip3Event;

        // ── TEMP ──
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
            crouchEvent = eventHub.Get<InputCrouchEvent>();
            sprintEvent = eventHub.Get<InputSprintEvent>();
            proneEvent = eventHub.Get<InputProneEvent>();
            standEvent = eventHub.Get<InputStandEvent>();
            secondaryInteractEvent = eventHub.Get<InputSecondaryInteractEvent>();
            firstSkillEvent = eventHub.Get<InputSkill1Event>();
            secondSkillEvent = eventHub.Get<InputSkill2Event>();
            equip1Event = eventHub.Get<InputEquip1Event>();
            equip2Event = eventHub.Get<InputEquip2Event>();
            equip3Event = eventHub.Get<InputEquip3Event>();

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

            eventHub?.Get<CameraSnapshotEvent>()?.Register(OnCameraSnapshot);
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

            eventHub?.Get<CameraSnapshotEvent>()?.Unregister(OnCameraSnapshot);
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
        private void OnCameraSnapshot(SCameraSnapshot snapshot)
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
