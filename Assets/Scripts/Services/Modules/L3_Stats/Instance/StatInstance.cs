using System;
using System.Collections.Generic;

namespace RedDust.Stats
{
    public class StatInstance
    {
        public StatDefinitionSO Def { get; }
        public string Path { get; internal set; }
        public float Current { get; private set; }

        private readonly List<StatModifier> modifiers = new();
        private float consumeTimer;
        private float restoreTimer;

        public event Action OnZero;
        public event Action<float> OnChanged;

        internal StatInstance(StatDefinitionSO def, float overrideDefault)
        {
            Def = def;
            Current = overrideDefault >= 0f ? overrideDefault : def.Default;
        }

        public int ModifierCount => modifiers.Count;

        public void AddModifier(StatModifier m) => modifiers.Add(m);
        public void RemoveByOwner(object owner) => modifiers.RemoveAll(m => m.Owner == owner);
        public bool HasModifier(StatModifier m) => modifiers.Contains(m);

        /// <summary>按频率消耗/恢复。consumeRate/restoreRate × dt = 每帧实际变化量。</summary>
        public void ApplyRates(float dt)
        {
            if (Def.IsConsumable)
                TickConsume(dt);

            if (Def.IsRestorable)
                TickRestore(dt);
        }

        private void TickConsume(float dt)
        {
            // TODO: 长间隔统一走帧累加，后续接入 TimeManager 再改为事件驱动
            if (Def.consumeInterval > 0f)
            {
                consumeTimer += dt;
                if (consumeTimer < Def.consumeInterval) return;
                int ticks = (int)(consumeTimer / Def.consumeInterval);
                consumeTimer %= Def.consumeInterval;

                var ctx = CollectModifiers();
                float delta = (-Def.consumeRate + ctx.Addend) * ctx.Multiplier * ticks;
                if (delta != 0f) Modify(delta);
            }
            else
            {
                var ctx = CollectModifiers();
                float delta = (-Def.consumeRate + ctx.Addend) * ctx.Multiplier * dt;
                if (delta != 0f) Modify(delta);
            }
        }

        private void TickRestore(float dt)
        {
            if (Def.restoreInterval > 0f)
            {
                restoreTimer += dt;
                if (restoreTimer < Def.restoreInterval) return;
                int ticks = (int)(restoreTimer / Def.restoreInterval);
                restoreTimer %= Def.restoreInterval;

                var ctx = CollectModifiers();
                float delta = (Def.restoreRate + ctx.Addend) * ctx.Multiplier * ticks;
                if (delta != 0f) Modify(delta);
            }
            else
            {
                var ctx = CollectModifiers();
                float delta = (Def.restoreRate + ctx.Addend) * ctx.Multiplier * dt;
                if (delta != 0f) Modify(delta);
            }
        }

        private ModifierContext CollectModifiers()
        {
            var ctx = new ModifierContext();
            foreach (var m in modifiers)
                m.Apply?.Invoke(this, ctx);
            return ctx;
        }

        public void Modify(float delta)
        {
            float prev = Current;
            Current = Math.Max(Def.Min, Math.Min(Def.Max, Current + delta));
            if (Current <= Def.Min) OnZero?.Invoke();
            if (Math.Abs(Current - prev) > 0.001f) OnChanged?.Invoke(Current);
        }
    }
}
