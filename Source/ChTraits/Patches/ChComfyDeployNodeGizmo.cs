using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ChTraits.Patches
{
    internal static class ChComfyNodeConfig
    {
        public const string TraitDefName = "ChComfy";
        public const string NodeDefName = "ChComfyClimateNode";

        // generous cooldown: 12 in-game hours (30,000 ticks)
        public const int DeployCooldownTicks = 30000;
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    internal static class ChComfyDeployNodeGizmoPatch
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
            if (!ChTraitsUtils.HasTrait(pawn, ChComfyNodeConfig.TraitDefName)) yield break;

            ChTraitsMapComponent mapComp = pawn.Map.GetComponent<ChTraitsMapComponent>();
            if (mapComp == null) yield break;

            int now = Find.TickManager.TicksGame;
            bool canDeploy = mapComp.ChComfy_CanDeployNodeNow(pawn, now, out int remaining);

            string cdText = canDeploy ? "Ready." : $"Cooldown: {remaining.ToStringTicksToPeriod()}";

            yield return new Command_Action
            {
                defaultLabel = "Deploy comfort node",
                defaultDesc = "Places a comfort node instantly for free.\n\n" + cdText,
                icon = ContentFinder<Texture2D>.Get(
                    "Things/ChComfy_ComfyNode", true
                ),
                Disabled = !canDeploy,
                disabledReason = canDeploy ? null : cdText,
                action = () =>
                {
                    Find.Targeter.BeginTargeting(
                        new TargetingParameters
                        {
                            canTargetLocations = true,
                            canTargetBuildings = false,
                            canTargetPawns = false,
                            canTargetFires = false,
                            canTargetItems = false,
                            mapObjectTargetsMustBeAutoAttackable = false
                            
                        },
                        target =>
                        {
                            if (!target.IsValid) return;
                            IntVec3 cell = target.Cell;
                            TryPlaceNode(pawn, mapComp, cell);
                        },
                        null, null
                    );
                }
            };
        }

        private static void TryPlaceNode(Pawn pawn, ChTraitsMapComponent mapComp, IntVec3 cell)
        {
            Map map = pawn.Map;
            if (map == null) return;

            int now = Find.TickManager.TicksGame;
            if (!mapComp.ChComfy_CanDeployNodeNow(pawn, now, out int remaining))
            {
                Messages.Message($"Comfort node on cooldown: {remaining.ToStringTicksToPeriod()}",
                    MessageTypeDefOf.RejectInput, false);
                return;
            }

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

            GenPlace.TryPlaceThing(node, cell, map, ThingPlaceMode.Direct);

            mapComp.ChComfy_SetDeployCooldown(pawn, now + ChComfyNodeConfig.DeployCooldownTicks);

            Messages.Message("Deployed comfort node.", MessageTypeDefOf.PositiveEvent, false);
        }
    }
}
