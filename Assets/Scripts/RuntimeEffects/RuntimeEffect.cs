namespace CardTower.RuntimeEffects
{
    public abstract class RuntimeEffect
    {
        public string SourceId { get; }
        public float RemainingSeconds { get; private set; }
        public bool IsExpired { get; private set; }

        protected RuntimeEffect(string sourceId, float durationSeconds = -1f)
        {
            SourceId = sourceId;
            RemainingSeconds = durationSeconds;
        }

        public virtual void OnApply(RuntimeEffectContext context)
        {
        }

        public virtual void OnUpdate(RuntimeEffectContext context, float deltaTime)
        {
            if (RemainingSeconds < 0f)
                return;

            RemainingSeconds -= deltaTime;
            if (RemainingSeconds <= 0f)
                IsExpired = true;
        }

        public virtual void OnRemove(RuntimeEffectContext context)
        {
        }

        public virtual void OnBattleStart(RuntimeEffectContext context)
        {
        }

        public virtual void OnBattleEnd(RuntimeEffectContext context)
        {
        }

        public virtual void OnCardPlay(RuntimeEffectContext context)
        {
        }

        public virtual void CollectTowerModifiers(ref TowerStatModifiers modifiers)
        {
        }

        public void Expire()
        {
            IsExpired = true;
        }
    }
}
