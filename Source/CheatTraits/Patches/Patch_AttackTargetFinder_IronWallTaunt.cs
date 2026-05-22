using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace CheatTraits.Patches
{
    /// <summary>
    /// While a Ch Tank has the Iron Wall hediff active, every hostile target search
    /// within 45 tiles of the Tank returns the Tank as the chosen target. Patches
    /// <see cref="AttackTargetFinder.BestAttackTarget"/> — the single chokepoint
    /// for melee + ranged + manhunter + mech + berserk + Anomaly target picks
    /// (BestShootTargetFromCurrentPosition forwards through it).
    /// </summary>
    [HarmonyPatch(typeof(AttackTargetFinder), nameof(AttackTargetFinder.BestAttackTarget))]
    internal static class Patch_AttackTargetFinder_IronWallTaunt
    {
        private const float TauntRadius = 45f;
        private const float TauntRadiusSquared = TauntRadius * TauntRadius;

        private static HediffDef? cachedHediff;
        private static bool cachedResolved;

        private static HediffDef? IronWallHediff
        {
            get
            {
                if (!cachedResolved)
                {
                    cachedHediff = DefDatabase<HediffDef>.GetNamedSilentFail("ChTank_IronWall");
                    cachedResolved = true;
                }
                return cachedHediff;
            }
        }

        private static void Postfix(IAttackTargetSearcher searcher, ref IAttackTarget __result)
        {
            if (__result == null)
                return;

            HediffDef? hediff = IronWallHediff;
            if (hediff == null)
                return;

            Thing? searcherThing = searcher?.Thing;
            if (searcherThing == null || searcherThing.Map == null)
                return;

            // Don't redirect already-correct picks.
            Pawn? currentTarget = __result.Thing as Pawn;

            Map map = searcherThing.Map;
            IntVec3 searcherPos = searcherThing.Position;

            // Find any spawned pawn on this map carrying the Iron Wall hediff.
            var pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
                return;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn taunter = pawns[i];
                if (taunter == null || taunter.Dead || !taunter.Spawned)
                    continue;
                if (taunter.health?.hediffSet?.GetFirstHediffOfDef(hediff) == null)
                    continue;
                if ((taunter.Position - searcherPos).LengthHorizontalSquared > TauntRadiusSquared)
                    continue;
                if (taunter == currentTarget)
                    return;
                if (taunter == searcherThing)
                    continue;
                // Only redirect when the searcher is actually hostile to the taunter —
                // otherwise berserk colonists / mental-break friendlies would be
                // shunted into the Tank.
                if (!searcherThing.HostileTo(taunter))
                    continue;

                __result = taunter;
                return;
            }
        }
    }
}
