using System.Collections.Generic;

namespace CardTower.RuntimeEffects
{
    public sealed class RuntimeEffectContainer
    {
        readonly List<RuntimeEffect> _effects = new List<RuntimeEffect>();

        public IReadOnlyList<RuntimeEffect> Effects => _effects;

        public void Add(RuntimeEffect effect, RuntimeEffectContext context)
        {
            if (effect == null)
                return;

            _effects.Add(effect);
            effect.OnApply(context);
        }

        public void Update(RuntimeEffectContext context, float deltaTime)
        {
            for (var i = _effects.Count - 1; i >= 0; i--)
            {
                var effect = _effects[i];
                effect.OnUpdate(context, deltaTime);
                if (!effect.IsExpired)
                    continue;

                effect.OnRemove(context);
                _effects.RemoveAt(i);
            }
        }

        public void RemoveBySource(string sourceId, RuntimeEffectContext context)
        {
            for (var i = _effects.Count - 1; i >= 0; i--)
            {
                if (_effects[i].SourceId != sourceId)
                    continue;

                _effects[i].OnRemove(context);
                _effects.RemoveAt(i);
            }
        }

        public void Clear(RuntimeEffectContext context)
        {
            for (var i = _effects.Count - 1; i >= 0; i--)
                _effects[i].OnRemove(context);
            _effects.Clear();
        }

        public void DispatchBattleStart(RuntimeEffectContext context)
        {
            foreach (var effect in _effects)
                effect.OnBattleStart(context);
        }

        public void DispatchBattleEnd(RuntimeEffectContext context)
        {
            foreach (var effect in _effects)
                effect.OnBattleEnd(context);
        }

        public void DispatchCardPlay(RuntimeEffectContext context)
        {
            foreach (var effect in _effects)
                effect.OnCardPlay(context);
        }

        public TowerStatModifiers CollectTowerModifiers()
        {
            var modifiers = TowerStatModifiers.Identity;
            foreach (var effect in _effects)
                effect.CollectTowerModifiers(ref modifiers);
            return modifiers;
        }
    }
}
