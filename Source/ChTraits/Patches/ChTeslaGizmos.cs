using ChTraits.Designators;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ChTraits.Patches
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    internal static class ChTeslaGizmoPatch
    {
        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            __result = AddGizmos(__instance, __result);
        }

        private static IEnumerable<Gizmo> AddGizmos(Pawn pawn, IEnumerable<Gizmo> baseGizmos)
        {
            foreach (var g in baseGizmos)
                yield return g;

            if (pawn == null || !pawn.Spawned) yield break;
            if (pawn.Faction != Faction.OfPlayer) yield break;
            if (pawn.story?.traits == null) yield break;
            if (!ChTraitsUtils.HasTrait(pawn, ChTraitsNames.TeslaTrait)) yield break;

            yield return new Command_Action
            {
                defaultLabel = "Deploy Tesla Coil",
                defaultDesc = "Deploy a tesla coil that generates power and zaps nearby enemies.",
                icon = ContentFinder<Texture2D>.Get("Things/ChTesla_TeslaCoil", true),
                action = () =>
                {
                    Find.DesignatorManager.Select(new TeslaCoilDesignator(pawn, ChTeslaThingDefOf.ChTeslaCoil));
                }
            };
        }
    }

    [DefOf]
    public static class ChTeslaThingDefOf
    {
        public static ThingDef ChTeslaCoil;

        static ChTeslaThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ChTeslaThingDefOf));
        }
    }
}
