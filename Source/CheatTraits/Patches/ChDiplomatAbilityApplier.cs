using RimWorld;
using Verse;

namespace CheatTraits.Patches
{
    /// <summary>
    /// Keeps the ChDiplomat pawn synced with the ChBond ability. Unlike the Wizard,
    /// the Diplomat has no hediff carrying the grant — the trait directly maps to a
    /// single ability, so we just gain/remove it each pawn tick.
    /// </summary>
    internal static class ChDiplomatAbilityApplier
    {
        private static AbilityDef? cachedBondDef;
        private static bool cachedBondDefResolved;

        private static AbilityDef? BondDef
        {
            get
            {
                if (!cachedBondDefResolved)
                {
                    cachedBondDef = DefDatabase<AbilityDef>.GetNamedSilentFail("ChBond");
                    cachedBondDefResolved = true;
                }
                return cachedBondDef;
            }
        }

        public static void TickPawn(Pawn p)
        {
            if (p?.story?.traits == null || p.abilities == null)
                return;
            if (!p.RaceProps.Humanlike)
                return;

            AbilityDef? def = BondDef;
            if (def == null)
                return;

            bool hasDiplomat = CheatTraitsUtils.HasTrait(p, CheatTraitsNames.DiplomatTrait);
            Ability existing = p.abilities.GetAbility(def);

            if (hasDiplomat)
            {
                if (existing == null)
                    p.abilities.GainAbility(def);
            }
            else
            {
                if (existing != null)
                    p.abilities.RemoveAbility(def);
            }
        }
    }
}
