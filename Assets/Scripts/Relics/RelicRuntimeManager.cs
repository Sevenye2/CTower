using CardTower.RuntimeEffects;

namespace CardTower.Relics
{
    public sealed class RelicRuntimeManager
    {
        static RelicRuntimeManager _instance;

        public static RelicRuntimeManager Instance => _instance ??= new RelicRuntimeManager();

        public RelicRuntimeContainer Relics { get; } = new RelicRuntimeContainer();

        public void RebuildFromSave(RuntimeEffectContext context)
        {
            Relics.RebuildFromSave(context);
        }
    }
}
