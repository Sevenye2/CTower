using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CardTower.Cards;
using CardTower.Relics;
using CardTower.TowerDefense;

namespace CardTower.Config
{
    public sealed class GameContentRegistry
    {
        static GameContentRegistry _instance;

        readonly Dictionary<string, Type> _relicTypesById = new();
        readonly Dictionary<string, CardBase> _cardEffectsById = new();
        readonly Dictionary<string, TowerAttackBase> _towerAttacksById = new();

        public static GameContentRegistry Instance => _instance ??= BuildFromAssemblies();

        public static void Reload()
        {
            _instance = BuildFromAssemblies();
        }

        public bool TryCreateRelic(string id, out RelicBase relic)
        {
            if (_relicTypesById.TryGetValue(id, out var type))
            {
                relic = (RelicBase)Activator.CreateInstance(type);
                return relic != null;
            }

            relic = null;
            return false;
        }

        public bool TryGetCardEffect(string cardId, out CardBase effect)
        {
            return _cardEffectsById.TryGetValue(cardId, out effect);
        }

        public bool TryGetCardConfig(string id, out CardConfig config)
        {
            if (_cardEffectsById.TryGetValue(id, out var effect))
            {
                config = effect.Config;
                return true;
            }

            config = null;
            return false;
        }

        public bool TryGetTowerAttack(string id, out TowerAttackBase attack)
        {
            return _towerAttacksById.TryGetValue(id, out attack);
        }

        public IEnumerable<RelicBase> GetAllRelics()
        {
            foreach (var kv in _relicTypesById)
            {
                if (TryCreateRelic(kv.Key, out var relic))
                    yield return relic;
            }
        }

        public IEnumerable<CardBase> GetAllCards()
        {
            return _cardEffectsById.Values;
        }

        public IEnumerable<TowerAttackBase> GetAllTowerAttacks()
        {
            return _towerAttacksById.Values;
        }

        static GameContentRegistry BuildFromAssemblies()
        {
            var registry = new GameContentRegistry();
            foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(GetLoadableTypes))
            {
                if (type == null || type.IsAbstract || type.IsInterface)
                    continue;

                registry.TryRegisterRelic(type);
                registry.TryRegisterCardEffect(type);
                registry.TryRegisterTowerAttack(type);
            }

            return registry;
        }

        static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }

        void TryRegisterRelic(Type type)
        {
            if (!typeof(RelicBase).IsAssignableFrom(type) || type.GetConstructor(Type.EmptyTypes) == null)
                return;

            if (Activator.CreateInstance(type) is not RelicBase relic || relic.Config == null || string.IsNullOrWhiteSpace(relic.Config.Id))
                return;

            _relicTypesById[relic.Config.Id] = type;
        }

        void TryRegisterCardEffect(Type type)
        {
            if (!typeof(CardBase).IsAssignableFrom(type) || type.GetConstructor(Type.EmptyTypes) == null)
                return;

            if (Activator.CreateInstance(type) is not CardBase effect)
                return;

            _cardEffectsById[effect.Config.Id] = effect;
        }

        void TryRegisterTowerAttack(Type type)
        {
            if (!typeof(TowerAttackBase).IsAssignableFrom(type) || type.GetConstructor(Type.EmptyTypes) == null)
                return;

            if (Activator.CreateInstance(type) is not TowerAttackBase attack)
                return;

            _towerAttacksById[attack.Config.Id] = attack;
        }
    }
}
