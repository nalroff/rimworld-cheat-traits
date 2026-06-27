using Verse;

namespace CheatTraits.Patches
{
    /// <summary>
    /// Keeps a ChBard pawn carrying its ChBard_Conductor hediff — the state
    /// anchor that holds the active aura stance and the switch cooldown. Mirrors
    /// ChWizardHediffApplier: invoked from CheatTraitsMapComponent on the shared
    /// pawn-tick cadence so spawn / load / dev-mode trait grants all converge on
    /// the conductor being present within a couple of seconds, without needing a
    /// dedicated Harmony hook. Removing the trait drops the conductor (which
    /// stops the aura on the next scan).
    /// </summary>
    internal static class ChBardHediffApplier
    {
        public static void TickPawn(Pawn p)
        {
            if (p?.story?.traits == null || p.health?.hediffSet == null)
                return;
            if (!p.RaceProps.Humanlike)
                return;

            bool hasBard = CheatTraitsUtils.HasTrait(p, CheatTraitsNames.BardTrait);
            HediffDef conductorDef = ChBardDefOf.ChBard_Conductor;
            if (conductorDef == null)
                return;

            Hediff existing = p.health.hediffSet.GetFirstHediffOfDef(conductorDef);

            if (hasBard)
            {
                if (existing == null)
                    p.health.AddHediff(conductorDef);
            }
            else if (existing != null)
            {
                // Trait removed (dev mode, character editor): drop the conductor.
                p.health.RemoveHediff(existing);
            }
        }
    }
}
