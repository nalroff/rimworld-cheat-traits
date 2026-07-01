using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CheatTraits.Patches
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    internal static class ChComfyGizmoPatch
    {
        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            __result = AddGizmos(__instance, __result);
        }

        private static IEnumerable<Gizmo> AddGizmos(Pawn pawn, IEnumerable<Gizmo> baseGizmos)
        {
            foreach (var g in baseGizmos)
                yield return g;

            if (pawn == null || !pawn.Spawned)
                yield break;
            if (pawn.Faction != Faction.OfPlayer)
                yield break;
            if (pawn.story?.traits == null)
                yield break;
            if (!CheatTraitsUtils.HasTrait(pawn, CheatTraitsNames.ComfyTrait))
                yield break;

            CheatTraitsMapComponent mapComp = pawn.Map.GetComponent<CheatTraitsMapComponent>();
            if (mapComp == null)
                yield break;

            // Fire Suppression Toggle Gizmo
            yield return new Command_Toggle
            {
                defaultLabel = "Fire suppression",
                defaultDesc =
                    "Automatically extinguish nearby fires.\n\nTurn this off if you are using burn boxes or controlled fires.",
                isActive = () => mapComp.ChComfy_IsFireSuppressionEnabled(pawn),
                toggleAction = () =>
                {
                    bool cur = mapComp.ChComfy_IsFireSuppressionEnabled(pawn);
                    mapComp.ChComfy_SetFireSuppressionEnabled(pawn, !cur);
                },
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Comfy_FireSuppression"),
            };
        }
    }
}
