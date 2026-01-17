using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ChTraits.Patches
{
    internal static class ChGreenThumbAuraConfig
    {
        public const string TraitDefName = "ChGreenThumb";
        public const float AuraRadius = 12f;
        public const int UpdateIntervalTicks = 250;
        public const float GrowthMultiplier = 10f;
        public const int MaxTrackedPlantsPerMap = 200;
        public const float GrowthRateHardCap = 20f;
    }

    /// <summary>
    /// Green Thumb:
    /// - Periodically scans around pawns with the ChGreenThumb trait.
    /// - Caches affected plant IDs in ChAuraCacheComponent under ChAuraKeys.GreenThumb_Plants.
    /// - Plant getters read the cache in hot paths (no scanning in getters).
    /// </summary>
    internal static class ChGreenThumbAura
    {
        private static TraitDef cachedTraitDef;

        public static void RebuildAffectedPlants(Map map)
        {
            if (map == null) return;

            var cache = map.GetComponent<ChAuraCacheComponent>();
            if (cache == null) return;

            var set = cache.GetSetForWrite(ChAuraKeys.GreenThumb_Plants);
            set.Clear();

            if (cachedTraitDef == null)
                cachedTraitDef = DefDatabase<TraitDef>.GetNamedSilentFail(ChGreenThumbAuraConfig.TraitDefName);

            if (cachedTraitDef == null) return;

            var pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null || pawns.Count == 0) return;

            int tracked = 0;
            int cap = ChGreenThumbAuraConfig.MaxTrackedPlantsPerMap;

            int radiusCellCount = GenRadial.NumCellsInRadius(ChGreenThumbAuraConfig.AuraRadius);

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || !p.Spawned || p.Dead) continue;

                var traits = p.story?.traits;
                if (traits == null || !traits.HasTrait(cachedTraitDef)) continue;

                IntVec3 center = p.Position;

                for (int r = 0; r < radiusCellCount; r++)
                {
                    IntVec3 cell = center + GenRadial.RadialPattern[r];
                    if (!cell.InBounds(map)) continue;

                    var things = map.thingGrid.ThingsListAtFast(cell);
                    if (things == null || things.Count == 0) continue;

                    for (int t = 0; t < things.Count; t++)
                    {
                        if (things[t] is Plant plant && plant.Spawned)
                        {
                            // Count only "new" plants to keep cap meaningful and avoid extra work.
                            if (set.Add(plant.thingIDNumber))
                            {
                                tracked++;
                                if (cap > 0 && tracked >= cap) return;
                            }
                        }
                    }
                }
            }
        }

        public static bool InAura(Plant plant)
        {
            if (plant == null || !plant.Spawned) return false;
            return ChAuraCache.IsAffected(plant, ChAuraKeys.GreenThumb_Plants);
        }
    }

    // ---------------------------
    // Plant stat patches (read cache)
    // ---------------------------

    [HarmonyPatch(typeof(Plant))]
    [HarmonyPatch("GrowthRateFactor_Light", MethodType.Getter)]
    static class Patch_Plant_GrowthRateFactor_Light
    {
        public static void Postfix(Plant __instance, ref float __result)
        {
            if (!ChGreenThumbAura.InAura(__instance)) return;
            __result = 1f;
        }
    }

    [HarmonyPatch(typeof(Plant))]
    [HarmonyPatch("GrowthRateFactor_Temperature", MethodType.Getter)]
    static class Patch_Plant_GrowthRateFactor_Temperature
    {
        public static void Postfix(Plant __instance, ref float __result)
        {
            if (!ChGreenThumbAura.InAura(__instance)) return;
            __result = 1f;
        }
    }

    [HarmonyPatch(typeof(Plant))]
    [HarmonyPatch("GrowthRate", MethodType.Getter)]
    static class Patch_Plant_GrowthRate
    {
        public static void Postfix(Plant __instance, ref float __result)
        {
            if (__result <= 0f) return;
            if (!ChGreenThumbAura.InAura(__instance)) return;

            __result *= ChGreenThumbAuraConfig.GrowthMultiplier;
            __result = Mathf.Min(__result, ChGreenThumbAuraConfig.GrowthRateHardCap);
        }
    }
}
