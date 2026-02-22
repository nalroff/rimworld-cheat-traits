using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace CheatTraits.Patches
{
    internal static class ChForcedBuildFloatMenuUtil
    {
        private const string BlueprintWorkGiverDefName = "ConstructDeliverResourcesToBlueprints";
        private const string ForcedBuildDescription =
            "Forces this pawn to work on the construction regardless of work priorities.";

        internal static bool TryCreateFrameOption(
            Frame frame,
            Pawn pawn,
            out FloatMenuOption? option
        )
        {
            option = null;
            if (frame == null || pawn == null)
                return false;

            if (!TryGetBuildTargetInfo(frame, out string requiredTrait, out string labelPrefix))
                return false;

            if (!CanPawnForceBuild(pawn, frame, requiredTrait))
                return false;

            string label = labelPrefix + ": Build - " + ForcedBuildDescription;
            FloatMenuOption baseOption = new FloatMenuOption(
                label,
                delegate
                {
                    if (!CanPawnForceBuild(pawn, frame, requiredTrait))
                        return;

                    Job job = JobMaker.MakeJob(JobDefOf.FinishFrame, frame);
                    job.playerForced = true;
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
            );

            option = FloatMenuUtility.DecoratePrioritizedTask(baseOption, pawn, frame);
            return true;
        }

        internal static bool TryCreateBlueprintOption(
            Blueprint_Build blueprint,
            Pawn pawn,
            out FloatMenuOption? option
        )
        {
            option = null;
            if (blueprint == null || pawn == null)
                return false;

            if (!TryGetBuildTargetInfo(blueprint, out string requiredTrait, out string labelPrefix))
                return false;

            if (!CanPawnForceBuild(pawn, blueprint, requiredTrait))
                return false;

            string label = labelPrefix + ": Build - " + ForcedBuildDescription;
            FloatMenuOption baseOption = new FloatMenuOption(
                label,
                delegate
                {
                    if (!CanPawnForceBuild(pawn, blueprint, requiredTrait))
                        return;

                    Job? job = MakeBlueprintJob(pawn, blueprint);
                    if (job == null)
                        return;

                    job.playerForced = true;
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
            );

            option = FloatMenuUtility.DecoratePrioritizedTask(baseOption, pawn, blueprint);
            return true;
        }

        internal static IEnumerable<FloatMenuOption> AppendOption(
            IEnumerable<FloatMenuOption>? options,
            FloatMenuOption addedOption
        )
        {
            if (options != null)
            {
                foreach (FloatMenuOption option in options)
                    yield return option;
            }

            yield return addedOption;
        }

        private static Job? MakeBlueprintJob(Pawn pawn, Blueprint_Build blueprint)
        {
            WorkGiverDef? workGiverDef = DefDatabase<WorkGiverDef>.GetNamedSilentFail(
                BlueprintWorkGiverDefName
            );
            if (workGiverDef?.Worker is not WorkGiver_ConstructDeliverResourcesToBlueprints worker)
                return null;

            return worker.JobOnThing(pawn, blueprint, true);
        }

        private static bool CanPawnForceBuild(Pawn pawn, Thing target, string requiredTrait)
        {
            if (!CheatTraitsUtils.IsValidPlayerColonistTarget(pawn))
                return false;
            if (!CheatTraitsUtils.HasTrait(pawn, requiredTrait))
                return false;
            if (pawn.Drafted || pawn.Downed)
                return false;
            if (target == null || target.Destroyed || !target.Spawned)
                return false;
            if (pawn.Map != target.Map)
                return false;
            if (target.IsForbidden(pawn))
                return false;
            if (pawn.health?.capacities == null)
                return false;
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return false;
            if (!pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly))
                return false;
            if (!pawn.CanReserve(target, 1, -1, null, false))
                return false;

            return true;
        }

        private static bool TryGetBuildTargetInfo(
            Thing target,
            out string requiredTrait,
            out string labelPrefix
        )
        {
            if (ChBuildRestrictionUtil.IsBuildTarget(target, ChThingDefOf.ChComfortNode))
            {
                requiredTrait = CheatTraitsNames.ComfyTrait;
                labelPrefix = "Ch Comfy";
                return true;
            }

            if (ChBuildRestrictionUtil.IsBuildTarget(target, ChThingDefOf.ChTeslaCoil))
            {
                requiredTrait = CheatTraitsNames.TeslaTrait;
                labelPrefix = "Ch Tesla";
                return true;
            }

            if (ChBuildRestrictionUtil.IsBuildTarget(target, ChThingDefOf.ChFloragenCore))
            {
                requiredTrait = CheatTraitsNames.GreenThumbTrait;
                labelPrefix = "Ch Green Thumb";
                return true;
            }

            requiredTrait = string.Empty;
            labelPrefix = string.Empty;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class Patch_Frame_FloatMenu_TraitOverrides
    {
        public static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(ThingWithComps), nameof(ThingWithComps.GetFloatMenuOptions));

        public static void Postfix(
            ThingWithComps __instance,
            Pawn selPawn,
            ref IEnumerable<FloatMenuOption> __result
        )
        {
            if (__instance is not Frame frame)
                return;

            if (
                !ChForcedBuildFloatMenuUtil.TryCreateFrameOption(
                    frame,
                    selPawn,
                    out FloatMenuOption? option
                )
                || option == null
            )
                return;

            __result = ChForcedBuildFloatMenuUtil.AppendOption(__result, option);
        }
    }

    [HarmonyPatch]
    internal static class Patch_BlueprintBuild_FloatMenu_TraitOverrides
    {
        public static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(ThingWithComps), nameof(ThingWithComps.GetFloatMenuOptions));

        public static void Postfix(
            ThingWithComps __instance,
            Pawn selPawn,
            ref IEnumerable<FloatMenuOption> __result
        )
        {
            if (__instance is not Blueprint_Build blueprint)
                return;

            if (
                !ChForcedBuildFloatMenuUtil.TryCreateBlueprintOption(
                    blueprint,
                    selPawn,
                    out FloatMenuOption? option
                )
                || option == null
            )
                return;

            __result = ChForcedBuildFloatMenuUtil.AppendOption(__result, option);
        }
    }
}
