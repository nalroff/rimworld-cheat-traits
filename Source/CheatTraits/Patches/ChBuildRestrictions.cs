using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace CheatTraits.Patches
{
    internal static class ChBuildRestrictionUtil
    {
        internal static bool MapHasTraitColonist(Map map, string traitDefName)
        {
            if (map == null)
                return false;

            IReadOnlyList<Pawn>? pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null || pawns.Count == 0)
                return false;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (!CheatTraitsUtils.IsValidPlayerColonistTarget(p))
                    continue;
                if (CheatTraitsUtils.HasTrait(p, traitDefName))
                    return true;
            }

            return false;
        }

        internal static bool IsBuildTarget(Thing t, ThingDef def)
        {
            if (t == null || def == null)
                return false;

            if (t.def == def)
                return true;

            BuildableDef? entDef = t.def?.entityDefToBuild;
            return entDef == def;
        }
    }

    [HarmonyPatch(typeof(Designator_Build), "get_Visible")]
    internal static class Patch_DesignatorBuild_Visible_TraitGates
    {
        public static void Postfix(Designator_Build __instance, ref bool __result)
        {
            if (!__result)
                return;

            BuildableDef? placingDef = __instance?.PlacingDef;
            if (placingDef == null)
                return;

            if (placingDef == ChThingDefOf.ChTeslaCoil)
            {
                if (
                    !ChBuildRestrictionUtil.MapHasTraitColonist(
                        __instance!.Map,
                        CheatTraitsNames.TeslaTrait
                    )
                )
                    __result = false;
                return;
            }

            if (placingDef == ChThingDefOf.ChComfortNode)
            {
                if (
                    !ChBuildRestrictionUtil.MapHasTraitColonist(
                        __instance!.Map,
                        CheatTraitsNames.ComfyTrait
                    )
                )
                    __result = false;
            }

            if (placingDef == ChThingDefOf.ChFloragenCore)
            {
                if (
                    !ChBuildRestrictionUtil.MapHasTraitColonist(
                        __instance!.Map,
                        CheatTraitsNames.GreenThumbTrait
                    )
                )
                    __result = false;
            }
        }
    }

    [HarmonyPatch(
        typeof(WorkGiver_ConstructFinishFrames),
        nameof(WorkGiver_ConstructFinishFrames.JobOnThing)
    )]
    internal static class Patch_ConstructFinishFrames_TraitGates
    {
        public static void Postfix(Pawn pawn, Thing t, bool forced, ref Job? __result)
        {
            if (__result == null)
                return;

            if (ChBuildRestrictionUtil.IsBuildTarget(t, ChThingDefOf.ChTeslaCoil))
            {
                if (!CheatTraitsUtils.HasTrait(pawn, CheatTraitsNames.TeslaTrait))
                    __result = null;
            }

            if (ChBuildRestrictionUtil.IsBuildTarget(t, ChThingDefOf.ChComfortNode))
            {
                if (!CheatTraitsUtils.HasTrait(pawn, CheatTraitsNames.ComfyTrait))
                    __result = null;
            }

            if (ChBuildRestrictionUtil.IsBuildTarget(t, ChThingDefOf.ChFloragenCore))
            {
                if (!CheatTraitsUtils.HasTrait(pawn, CheatTraitsNames.GreenThumbTrait))
                    __result = null;
            }
        }
    }
}
