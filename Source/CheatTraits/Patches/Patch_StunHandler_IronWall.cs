using HarmonyLib;
using RimWorld;
using Verse;

namespace CheatTraits.Patches
{
    /// <summary>
    /// Iron Wall grants stun immunity. Patches <see cref="StunHandler.StunFor"/>
    /// to no-op when the owning pawn is carrying the Iron Wall hediff.
    /// </summary>
    [HarmonyPatch(typeof(StunHandler), nameof(StunHandler.StunFor))]
    internal static class Patch_StunHandler_IronWall
    {
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

        private static bool Prefix(StunHandler __instance)
        {
            HediffDef? hediff = IronWallHediff;
            if (hediff == null)
                return true;

            Pawn? pawn = __instance?.parent as Pawn;
            if (pawn == null || pawn.health?.hediffSet == null)
                return true;

            if (pawn.health.hediffSet.GetFirstHediffOfDef(hediff) != null)
                return false;

            return true;
        }
    }
}
