using CardTower.RuntimeEffects;

namespace CardTower.Relics
{
    public abstract class RelicBase
    {
        public abstract RelicRuntimeConfig Config { get; }

        public virtual void OnOwned(RuntimeEffectContext context)
        {
        }

        public virtual void CreateRuntimeEffects(RuntimeEffectContext context)
        {
        }
    }

    public sealed class RelicRuntimeConfig
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string IconPath;
        public string Rarity;
        public int Price;
    }
}
