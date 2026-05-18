using CardTower.TowerDefense;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CardTower.RuntimeEffects
{
    public sealed class LifeSpringEffect : RuntimeEffect
    {
        const float HealInterval = 10f;
        const float HealPercent = 0.1f;

        float _timer;

        public LifeSpringEffect()
            : base("life_spring")
        {
        }

        public override void OnUpdate(RuntimeEffectContext context, float deltaTime)
        {
            base.OnUpdate(context, deltaTime);
            _timer += deltaTime;
            if (_timer < HealInterval)
                return;

            _timer -= HealInterval;

            var em = context.EntityManager;
            var tower = context.TowerEntity;
            if (tower == Entity.Null || !em.Exists(tower) || !em.HasComponent<Health>(tower))
                return;

            var hp = em.GetComponentData<Health>(tower);
            var heal = hp.Max * HealPercent;
            hp.Current = math.min(hp.Max, hp.Current + heal);
            em.SetComponentData(tower, hp);
        }
    }
}
