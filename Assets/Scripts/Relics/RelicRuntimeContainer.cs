using System.Collections.Generic;
using CardTower.Config;
using CardTower.RuntimeEffects;

namespace CardTower.Relics
{
    public sealed class RelicRuntimeContainer
    {
        readonly List<RelicBase> _relics = new List<RelicBase>();

        public IReadOnlyList<RelicBase> Relics => _relics;

        public void RebuildFromSave(RuntimeEffectContext context)
        {
            _relics.Clear();
            foreach (var relicSave in SaveDataManager.instance.Relics)
            {
                GameContentRegistry.Instance.TryCreateRelic(relicSave.id, out var relic);
                relic ??= new UnknownRelic();
                _relics.Add(relic);
                relic.OnOwned(context);
            }
        }

        public void CreateAllEffects(RuntimeEffectContext context)
        {
            foreach (var relic in _relics)
                relic.CreateRuntimeEffects(context);
        }
    }
}
