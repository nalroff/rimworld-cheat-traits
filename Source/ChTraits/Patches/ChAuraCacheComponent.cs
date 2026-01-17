using System;
using System.Collections.Generic;
using Verse;

namespace ChTraits.Patches
{
    public class ChAuraCacheComponent : MapComponent
    {
        private readonly Dictionary<string, HashSet<int>> affectedByKey =
            new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);

        public ChAuraCacheComponent(Map map) : base(map) { }

        public bool IsAffected(string key, Thing thing)
        {
            if (thing == null || !thing.Spawned) return false;
            return IsAffected(key, thing.thingIDNumber);
        }

        public bool IsAffected(string key, int thingId)
        {
            if (string.IsNullOrEmpty(key) || thingId <= 0) return false;
            return affectedByKey.TryGetValue(key, out var set) && set.Contains(thingId);
        }

        public HashSet<int> GetSetForWrite(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Aura cache key cannot be null/empty.", nameof(key));

            if (!affectedByKey.TryGetValue(key, out var set))
            {
                set = new HashSet<int>();
                affectedByKey[key] = set;
            }

            return set;
        }

        public void Clear(string key) => GetSetForWrite(key).Clear();
    }

    internal static class ChAuraCache
    {
        public static ChAuraCacheComponent Get(Map map) => map?.GetComponent<ChAuraCacheComponent>();

        public static bool IsAffected(Thing thing, string key)
            => thing?.Map?.GetComponent<ChAuraCacheComponent>()?.IsAffected(key, thing) ?? false;

        public static HashSet<int> GetSetForWrite(Map map, string key)
            => Get(map)?.GetSetForWrite(key);
    }
}
