namespace CardTower.RuntimeEffects
{
    public sealed class UnstableEngineBattleEffect : RuntimeEffect
    {
        const float TickIntervalSeconds = 2f;
        const float AttackSpeedPercentPerStack = 0.02f;
        const float DamagePercentPerStack = -0.01f;

        float _timer;
        int _stacks;

        public UnstableEngineBattleEffect()
            : base("unstable_engine")
        {
        }

        public override void OnUpdate(RuntimeEffectContext context, float deltaTime)
        {
            base.OnUpdate(context, deltaTime);
            _timer += deltaTime;
            if (_timer < TickIntervalSeconds)
                return;

            _timer -= TickIntervalSeconds;
            _stacks++;
        }

        public override void CollectTowerModifiers(ref TowerStatModifiers modifiers)
        {
            modifiers.AttackSpeedMultiplier *= 1f + _stacks * AttackSpeedPercentPerStack;
            modifiers.DamageMultiplier *= 1f + _stacks * DamagePercentPerStack;
        }
    }
}
