using CardTower.RuntimeEffects;

namespace CardTower.Cards
{
    public abstract class CardBase
    {
        public abstract CardConfig Config { get; }

        public abstract void Play(RuntimeEffectContext context);
    }

    public sealed class CardConfig
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public int ManaCost;
        public int Price;
    }
}
