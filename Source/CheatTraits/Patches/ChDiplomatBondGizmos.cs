using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

namespace CheatTraits.Patches
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    internal static class ChDiplomatBondGizmoPatch
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
            if (!CheatTraitsUtils.HasTrait(pawn, CheatTraitsNames.DiplomatTrait))
                yield break;

            yield return new Command_Action
            {
                defaultLabel = "Bond pawns",
                defaultDesc =
                    "Pick two pawns to lock their relationship to near-maximum:\n"
                    + " - Compatibility set to 2.0 (boosts deep talks, drastically cuts fights)\n"
                    + " - Opinion forced to 100 in both directions\n"
                    + " - Romance chance pushed to 100% IF the engine would already allow romance "
                    + "(orientation, age, species, incest, and missing-gene blocks all still apply)\n\n"
                    + "Picking the same pair again removes the bond.",
                icon = TexCommand.GatherSpotActive,
                action = () => BeginPickFirst(),
            };
        }

        private static void BeginPickFirst()
        {
            Find.Targeter.BeginTargeting(
                BondTargetingParameters(null),
                (LocalTargetInfo first) =>
                {
                    Pawn? firstPawn = first.Pawn;
                    if (firstPawn == null)
                        return;

                    Find.Targeter.BeginTargeting(
                        BondTargetingParameters(firstPawn),
                        (LocalTargetInfo second) =>
                        {
                            Pawn? secondPawn = second.Pawn;
                            if (secondPawn == null)
                                return;
                            ApplyBond(firstPawn, secondPawn);
                        }
                    );
                }
            );
        }

        private static TargetingParameters BondTargetingParameters(Pawn? excludePawn)
        {
            return new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                canTargetItems = false,
                canTargetSelf = true,
                validator = (TargetInfo info) =>
                {
                    if (!info.HasThing)
                        return false;
                    Pawn? p = info.Thing as Pawn;
                    if (p == null || p.Dead || !p.Spawned)
                        return false;
                    if (!p.RaceProps.Humanlike)
                        return false;
                    if (excludePawn != null && p == excludePawn)
                        return false;
                    return true;
                },
            };
        }

        private static void ApplyBond(Pawn a, Pawn b)
        {
            ChDiplomatBondsGameComponent? bonds = ChDiplomatBondsGameComponent.Instance;
            if (bonds == null || a == null || b == null || a == b)
                return;

            if (bonds.IsBonded(a, b))
            {
                bonds.RemoveBond(a, b);
                Messages.Message(
                    $"{a.LabelShortCap} and {b.LabelShortCap} are no longer bonded.",
                    new LookTargets(new Pawn[] { a, b }),
                    MessageTypeDefOf.NeutralEvent,
                    historical: false
                );
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
            else
            {
                bonds.AddBond(a, b);
                Messages.Message(
                    $"{a.LabelShortCap} and {b.LabelShortCap} are now bonded — their compatibility is near-maximum.",
                    new LookTargets(new Pawn[] { a, b }),
                    MessageTypeDefOf.PositiveEvent,
                    historical: false
                );
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
        }
    }
}
