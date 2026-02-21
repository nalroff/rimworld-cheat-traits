using System.Collections.Generic;
using CheatTraits.Comps;
using RimWorld;
using UnityEngine;
using Verse;

namespace CheatTraits.Patches
{
    internal static class ChFloragenCoreConfig
    {
        // Fallbacks if the ThingDef/Comp isn't available for any reason.
        public const int DefaultScanIntervalTicks = 2000;
        public const float DefaultRadius = 12f;
        public const float DefaultGrowthMultiplier = 3f;
        public const int DefaultMaxTrackedPlantsPerMap = 1200;
    }

    /// <summary>
    /// Floragen Core:
    /// - Building-driven plant aura (trees + wild plants included).
    /// - Separate cache key from Green Thumb.
    /// - Low-frequency scan for performance.
    /// - Multiple Floragen Cores do NOT stack with each other (apply once per plant per scan).
    /// - Stacks naturally with Green Thumb (no cross-check).
    ///
    /// Driven by CheatTraitsMapComponent.
    /// </summary>
    internal static class ChFloragenCoreSystem
    {
        /// <summary>
        /// Rebuild cache + apply growth for this map.
        /// Returns the interval (ticks) the map component should wait until next run.
        /// </summary>
        public static int TickMap(Map map)
        {
            if (map == null)
                return ChFloragenCoreConfig.DefaultScanIntervalTicks;

            var cache = map.GetComponent<ChAuraCacheComponent>();
            if (cache == null)
                return ChFloragenCoreConfig.DefaultScanIntervalTicks;

            var set = cache.GetSetForWrite(ChAuraKeys.Floragen_Plants);
            set.Clear();

            ThingDef coreDef = ChThingDefOf.ChFloragenCore;
            if (coreDef == null)
                return ChFloragenCoreConfig.DefaultScanIntervalTicks;

            List<Thing>? cores = map.listerThings?.ThingsOfDef(coreDef);
            if (cores == null || cores.Count == 0)
                return ResolveIntervalFallback(ChFloragenCoreConfig.DefaultScanIntervalTicks);

            int minInterval = int.MaxValue;
            int cap = int.MaxValue;
            int tracked = 0;

            for (int i = 0; i < cores.Count; i++)
            {
                Thing core = cores[i];
                if (core == null || !core.Spawned)
                    continue;

                var comp = core.TryGetComp<CompChFloragenCore>();

                float radius = comp?.PropsEx.radius ?? ChFloragenCoreConfig.DefaultRadius;
                int interval =
                    comp?.PropsEx.scanIntervalTicks
                    ?? ChFloragenCoreConfig.DefaultScanIntervalTicks;
                float growthMultiplier =
                    comp?.PropsEx.growthMultiplier ?? ChFloragenCoreConfig.DefaultGrowthMultiplier;
                int localCap =
                    comp?.PropsEx.maxTrackedPlantsPerMap
                    ?? ChFloragenCoreConfig.DefaultMaxTrackedPlantsPerMap;

                if (interval > 0)
                    minInterval = Mathf.Min(minInterval, interval);
                if (localCap > 0)
                    cap = Mathf.Min(cap, localCap);

                int radiusCellCount = GenRadial.NumCellsInRadius(radius);
                IntVec3 center = core.Position;

                for (int r = 0; r < radiusCellCount; r++)
                {
                    IntVec3 cell = center + GenRadial.RadialPattern[r];
                    if (!cell.InBounds(map))
                        continue;

                    var things = map.thingGrid.ThingsListAtFast(cell);
                    if (things == null || things.Count == 0)
                        continue;

                    for (int t = 0; t < things.Count; t++)
                    {
                        if (things[t] is not Plant plant || !plant.Spawned)
                            continue;

                        // Non-stacking across multiple Floragen Cores.
                        if (!set.Add(plant.thingIDNumber))
                            continue;

                        ApplyGrowthDirect(plant, growthMultiplier, interval);

                        tracked++;
                        if (cap != int.MaxValue && tracked >= cap)
                            return ResolveIntervalFallback(minInterval);
                    }
                }
            }

            return ResolveIntervalFallback(minInterval);
        }

        private static int ResolveIntervalFallback(int interval)
        {
            if (interval <= 0 || interval == int.MaxValue)
                return ChFloragenCoreConfig.DefaultScanIntervalTicks;
            return interval;
        }

        private static void ApplyGrowthDirect(Plant plant, float totalMultiplier, int intervalTicks)
        {
            if (plant == null || !plant.Spawned || plant.Destroyed)
                return;

            if (plant.LifeStage == PlantLifeStage.Sowing)
                return;

            float cur = plant.Growth;
            if (cur >= 1f)
                return;

            var plantProps = plant.def?.plant;
            if (plantProps == null)
                return;

            float growDays = plantProps.growDays;
            if (growDays <= 0f)
                return;

            if (totalMultiplier <= 1f)
                return;

            // Baseline: +1.0 Growth over `growDays` days.
            // Add (totalMultiplier - 1) * baseline, scaled to our scan interval.
            // This ignores light/temp/season/resting/etc. by design.
            float baseGrowthPerDay = 1f / growDays;
            float extraPerDay = baseGrowthPerDay * (totalMultiplier - 1f);
            float extraThisInterval = extraPerDay * (intervalTicks / (float)GenDate.TicksPerDay);

            if (extraThisInterval <= 0f)
                return;

            float newGrowth = Mathf.Min(1f, cur + extraThisInterval);
            if (newGrowth <= cur)
                return;

            plant.Growth = newGrowth;
            plant.Map?.mapDrawer?.MapMeshDirty(
                plant.Position,
                MapMeshFlagDefOf.Things,
                true,
                false
            );
        }
    }
}
