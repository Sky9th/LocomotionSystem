using System;
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

            // Movement
            eventHub.Get<SprintInputEventSO>().Register(OnSprint);
            eventHub.Get<CrouchInputEventSO>().Register(OnCrouch);
            eventHub.Get<ProneInputEventSO>().Register(OnProne);
            eventHub.Get<StandInputEventSO>().Register(OnStand);

            //Intereaction
            eventHub.Get<SecondaryInteractEventSO>().Register(OnSecondary);

            //Combat
            eventHub.Get<FirstSkillInputEventSO>().Register(OnFirstActivatedSkill);
            eventHub.Get<SecondSkillInputEventSO>().Register(OnSecondActivatedSkill);

            // TEMP
            if (GameContext.Instance != null &&
                GameContext.Instance.TryResolveService(out dispatcher))
            {
                dispatcher.Subscribe<SCameraSnapshot>(OnCameraSnapshot);
            }
        }

        public void UnbindEvents()
        {
            eventHub.Get<SecondaryInteractEventSO>()?.Unregister(OnSecondary);
            eventHub.Get<SprintInputEventSO>()?.Unregister(OnSprint);
            eventHub.Get<CrouchInputEventSO>()?.Unregister(OnCrouch);
            eventHub.Get<ProneInputEventSO>()?.Unregister(OnProne);
            eventHub.Get<StandInputEventSO>()?.Unregister(OnStand);

            eventHub.Get<FirstSkillInputEventSO>()?.Unregister(OnFirstActivatedSkill);
            eventHub.Get<SecondSkillInputEventSO>()?.Unregister(OnSecondActivatedSkill);

            dispatcher?.Unsubscribe<SCameraSnapshot>(OnCameraSnapshot);
            dispatcher = null;
        }

        // ── Handlers ──

        private void OnSecondary(bool p) { if (p) SecondaryRequested = true; }
        private void OnSprint(bool p) { if (p) SprintRequested = true; }
        private void OnCrouch(bool p) { if (p) CrouchRequested = true; }
        private void OnProne(bool p) { if (p) ProneRequested = true; }
        private void OnStand(bool p) { if (p) StandRequested = true; }
        private void OnFirstActivatedSkill(bool p) { if (p) FirstSkillRequested = true; }
        private void OnSecondActivatedSkill(bool p) { if (p) SencondSkillRequested = true; }
        
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
