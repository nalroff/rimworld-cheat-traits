using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using ChTraits.Designators;

namespace ChTraits.Patches
{
    internal static class ChComfyNodeConfig
    {
        public const string NodeDefName = "ChComfyClimateNode";
    }

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

            if (pawn == null || !pawn.Spawned) yield break;
            if (pawn.Faction != Faction.OfPlayer) yield break;
            if (pawn.story?.traits == null) yield break;
            if (!ChTraitsUtils.HasTrait(pawn, ChTraitsNames.ComfyTrait)) yield break;

            ChTraitsMapComponent mapComp = pawn.Map.GetComponent<ChTraitsMapComponent>();
            if (mapComp == null) yield break;

            // Deploy Node Gizmo
            int now = Find.TickManager.TicksGame;

            yield return new Command_Action
            {
                defaultLabel = "Deploy Comfy Node",
                defaultDesc = "Deploy a climate control node.",
                icon = ContentFinder<Texture2D>.Get(
                    "Things/ChComfy_ComfyNode", true
                ),
                action = () =>
                {
                    Find.DesignatorManager.Select(new ComfyNodeDesignator(pawn, ChThingDefOf.ChComfyClimateNode));
                }
            };

            // Fire Suppression Toggle Gizmo
            yield return new Command_Toggle
            {
                defaultLabel = "Fire suppression",
                defaultDesc = "Automatically extinguish nearby fires.\n\nTurn this off if you are using burn boxes or controlled fires.",
                isActive = () => mapComp.ChComfy_IsFireSuppressionEnabled(pawn),
                toggleAction = () =>
                {
                    bool cur = mapComp.ChComfy_IsFireSuppressionEnabled(pawn);
                    mapComp.ChComfy_SetFireSuppressionEnabled(pawn, !cur);
                },
                icon = TexCommand.ForbidOff
            };
        }

        private static void TryPlaceNode(Pawn pawn, ChTraitsMapComponent mapComp, IntVec3 cell)
        {
            Map map = pawn.Map;
            if (map == null) return;

            int now = Find.TickManager.TicksGame;

            if (!cell.InBounds(map))
            {
                Messages.Message("Cannot place there.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!cell.Standable(map))
            {
                Messages.Message("Must be placed on a walkable cell.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(ChComfyNodeConfig.NodeDefName);
            if (def == null)
            {
                Messages.Message("Comfort node def missing.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Prevent stacking multiple nodes in one cell
            var things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i]?.def == def)
                {
                    Messages.Message("A comfort node is already there.", MessageTypeDefOf.RejectInput, false);
                    return;
                }
            }

            Thing node = ThingMaker.MakeThing(def);
            node.SetFaction(Faction.OfPlayer);

            var job = JobMaker.MakeJob(ChJobDefOf.ChDeployComfyNode, cell);
            pawn.jobs.TryTakeOrderedJob(job);

            Messages.Message("Deployed comfort node.", MessageTypeDefOf.PositiveEvent, false);
        }
    }

    [DefOf]
    internal static class ChJobDefOf
    {
        #pragma warning disable 0649
        public static JobDef ChDeployComfyNode;
        #pragma warning restore 0649

        static ChJobDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ChJobDefOf));
    }

    [DefOf]
    public static class ChThingDefOf
    {
        public static ThingDef ChComfyClimateNode; // MUST match the ThingDef defName exactly

        static ChThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ChThingDefOf));
        }
    }
}
