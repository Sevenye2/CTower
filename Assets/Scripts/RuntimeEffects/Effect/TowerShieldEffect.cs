using CardTower.TowerDefense;
using Unity.Entities;

namespace CardTower.RuntimeEffects
{
    public sealed class TowerShieldEffect : RuntimeEffect
    {
        readonly float _shieldAmount;
        Entity _tower;

        public TowerShieldEffect(string sourceId, float durationSeconds, float shieldAmount)
            : base(sourceId, durationSeconds)
        {
            _shieldAmount = shieldAmount;
        }

        public override void OnApply(RuntimeEffectContext context)
        {
            _tower = context.TowerEntity;
            if (_tower == Entity.Null)
                return;

            var em = context.EntityManager;
            if (em.HasComponent<TowerShield>(_tower))
            {
                var s = em.GetComponentData<TowerShield>(_tower);
                s.Value += _shieldAmount;
                em.SetComponentData(_tower, s);
            }
            else
            {
                em.AddComponentData(_tower, new TowerShield { Value = _shieldAmount });
            }
        }

        public override void OnUpdate(RuntimeEffectContext context, float deltaTime)
        {
            base.OnUpdate(context, deltaTime);

            if (_tower == Entity.Null)
                return;

            var em = context.EntityManager;
            if (!em.Exists(_tower) || !em.HasComponent<TowerShield>(_tower))
            {
                Expire();
                return;
            }

            if (em.GetComponentData<TowerShield>(_tower).Value <= 0f)
                Expire();
        }

        public override void OnRemove(RuntimeEffectContext context)
        {
            if (_tower == Entity.Null)
                return;

            var em = context.EntityManager;
            if (em.Exists(_tower) && em.HasComponent<TowerShield>(_tower))
                em.RemoveComponent<TowerShield>(_tower);
        }
    }
}
