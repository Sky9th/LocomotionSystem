using System;

namespace Game.Stats
{
    public class StatInstance
    {
        public StatDefSO Def { get; }
        public float Current { get; protected set; }
        public StatBehaviorSO[] Behaviors { get; private set; }

        public event Action OnZero;
        public event Action<float> OnChanged;

        internal StatInstance(StatDefSO def, float overrideDefault, StatBehaviorSO[] behaviors)
        {
            Def = def;
            Current = overrideDefault >= 0f ? overrideDefault : def.Default;
            Behaviors = behaviors;
            if (Behaviors != null)
                foreach (var b in Behaviors)
                    b.Bind(this);
        }

        public virtual void Tick(float dt)
        {
            if (Behaviors == null) return;
            foreach (var b in Behaviors)
                b.Tick(dt);
        }

        public virtual void Modify(float delta)
        {
            float prev = Current;
            Current = Math.Max(Def.Min, Math.Min(Def.Max, Current + delta));
            if (Current <= Def.Min) OnZero?.Invoke();
            if (Math.Abs(Current - prev) > 0.001f) OnChanged?.Invoke(Current);
        }

        public virtual (string id, float value) Snap() => (Def.Id, Current);
    }
}
