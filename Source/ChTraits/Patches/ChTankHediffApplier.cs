using Verse;

namespace ChTraits.Patches
{
    internal static class ChTankHediffApplier
    {
        private const string DampenerHediffDefName = "ChTank_PainDampener";

        private static HediffDef Dampener => DefDatabase<HediffDef>.GetNamedSilentFail(DampenerHediffDefName);

        public static void TickPawn(Pawn p)
        {
            if (p?.story?.traits == null || p.health?.hediffSet == null) return;

            var dampener = Dampener;
            if (dampener == null) return;

            bool hasTank = ChTraitsUtils.HasTrait(p, ChTraitsNames.TankTrait);
            var existing = p.health.hediffSet.GetFirstHediffOfDef(dampener);

            if (hasTank)
            {
                if (existing == null) p.health.AddHediff(dampener);
            }
            else
            {
                if (existing != null) p.health.RemoveHediff(existing);
            }
        }
    }
}
