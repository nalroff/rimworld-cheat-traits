using RimWorld;
using Verse;

namespace CheatTraits.Patches
{
    internal static class ChBoxerAbilityApplier
    {
        private static AbilityDef? cachedDef;
        private static bool cachedResolved;

        private static AbilityDef? Def
        {
            get
            {
                if (!cachedResolved)
                {
                    cachedDef = DefDatabase<AbilityDef>.GetNamedSilentFail("ChFlyingPunch");
                    cachedResolved = true;
                }
                return cachedDef;
            }
        }

        public static void TickPawn(Pawn p)
        {
            if (p?.story?.traits == null || p.abilities == null)
                return;
            if (!p.RaceProps.Humanlike)
                return;

            AbilityDef? def = Def;
            if (def == null)
                return;

            bool hasTrait = CheatTraitsUtils.HasTrait(p, CheatTraitsNames.BoxerTrait);
            Ability existing = p.abilities.GetAbility(def);

            if (hasTrait)
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
