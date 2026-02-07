using System;
using System.Collections.Generic;
using UnityEngine;
using Animancer.FSM;
using Game.Locomotion.Adapter.Conditions;

namespace Game.Locomotion.Adapter
{
    public partial class LocomotionAnimancerAdapter : MonoBehaviour
    {
        #region Animation State Base

        private abstract class State : IState
        {
            protected readonly LocomotionAnimancerAdapter Adapter;

            private readonly List<Transition> transitions = new List<Transition>(4);

            protected State(LocomotionAnimancerAdapter adapter)
            {
                Adapter = adapter;
            }

            public virtual bool CanEnterState => true;
            public virtual bool CanExitState => true;

            public virtual void OnEnterState() { }
            public virtual void OnExitState() { }
            public virtual void Update() { }

            protected void AddTransition(ILocomotionCondition condition, Func<State> getTarget)
            {
                if (condition == null) throw new ArgumentNullException(nameof(condition));
                if (getTarget == null) throw new ArgumentNullException(nameof(getTarget));

                transitions.Add(new Transition(condition, getTarget));
            }

            /// <summary>
            /// Evaluates this state's configured transitions in order and applies
            /// the first matching transition. Returns true if a transition occurred.
            /// </summary>
            protected bool TryApplyTransitions()
            {
                for (int i = 0; i < transitions.Count; i++)
                {
                    var transition = transitions[i];
                    if (!transition.Condition.Evaluate(Adapter))
                    {
                        continue;
                    }

                    var target = transition.GetTarget();
                    if (target != null)
                    {
                        Adapter.stateMachine.TrySetState(target);
                        return true;
                    }
                }

                return false;
            }

            private readonly struct Transition
            {
                public readonly ILocomotionCondition Condition;
                public readonly Func<State> GetTarget;

                public Transition(ILocomotionCondition condition, Func<State> getTarget)
                {
                    Condition = condition;
                    GetTarget = getTarget;
                }
            }
        }

        #endregion
    }
}
