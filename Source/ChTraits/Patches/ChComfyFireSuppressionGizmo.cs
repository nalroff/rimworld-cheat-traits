using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ChTraits.Patches
{
    internal static class ChComfyConfig
    {
        public const string TraitDefName = "ChComfy";
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    internal static class ChComfyFireSuppressionGizmoPatch
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
            if (!ChTraitsUtils.HasTrait(pawn, ChComfyConfig.TraitDefName)) yield break;

            var comp = pawn.Map.GetComponent<ChTraitsMapComponent>();
            if (comp == null) yield break;

            yield return new Command_Toggle
            {
                defaultLabel = "Fire suppression",
                defaultDesc = "Automatically extinguish nearby fires.\n\nTurn this off if you are using burn boxes or controlled fires.",
                isActive = () => comp.ChComfy_IsFireSuppressionEnabled(pawn),
                toggleAction = () =>
                {
                    bool cur = comp.ChComfy_IsFireSuppressionEnabled(pawn);
                    comp.ChComfy_SetFireSuppressionEnabled(pawn, !cur);
                },
                icon = TexCommand.ForbidOff
            };
        }
    }
}
