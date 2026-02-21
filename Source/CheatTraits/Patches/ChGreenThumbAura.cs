using RimWorld;
using UnityEngine;
using Verse;

namespace CheatTraits.Patches
{
    internal static class ChGreenThumbAuraConfig
    {
        public const float AuraRadius = 12f;

        // Keep naming consistent with other auras (Ascendant/Diplomat/Beastmaster)
        public const int ScanIntervalTicks = 250;

        // 10x total growth => add +9x baseline growth directly
        public const float GrowthMultiplier = 10f;

        public const int MaxTrackedPlantsPerMap = 200;
    }

    internal static class ChGreenThumbAura
    {
        public static void RebuildAffectedPlants(Map map)
        {
            if (map == null)
                return;

            var cache = map.GetComponent<ChAuraCacheComponent>();
            if (cache == null)
                return;

            var set = cache.GetSetForWrite(ChAuraKeys.GreenThumb_Plants);
            set.Clear();

            var pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null || pawns.Count == 0)
                return;

            int tracked = 0;
            int cap = ChGreenThumbAuraConfig.MaxTrackedPlantsPerMap;

            int radiusCellCount = GenRadial.NumCellsInRadius(ChGreenThumbAuraConfig.AuraRadius);

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || !p.Spawned || p.Dead)
                    continue;

                if (!CheatTraitsUtils.HasTrait(p, CheatTraitsNames.GreenThumbTrait))
                    continue;

                IntVec3 center = p.Position;

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

                        if (!set.Add(plant.thingIDNumber))
                            continue;

                        CureBlightIfPresent(plant);
                        ApplyGrowthDirect(
                            plant,
                            ChGreenThumbAuraConfig.GrowthMultiplier,
                            ChGreenThumbAuraConfig.ScanIntervalTicks
                        );

                        tracked++;
                        if (cap > 0 && tracked >= cap)
                            return;
                    }
                }
            }
        }

        public static bool InAura(Plant plant)
        {
            if (plant == null || !plant.Spawned)
                return false;

            return ChAuraCache.IsAffected(plant, ChAuraKeys.GreenThumb_Plants);
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

            // Baseline at ideal: +1.0 growth over `growDays` days.
            // totalMultiplier=10 => add +9x baseline, scaled to our scan interval.
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

        private static void CureBlightIfPresent(Plant plant)
        {
            // Blight is a separate Thing; removing it restores normal growth behavior (and stops spread).
            var blight = plant.Blight;
            if (blight != null && !blight.Destroyed)
                blight.Destroy(DestroyMode.Vanish);
        }
    }
}
