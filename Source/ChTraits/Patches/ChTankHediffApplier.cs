using RimWorld;
using Verse;

namespace ChTraits.Patches
{
    internal static class ChTankHediffApplier
    {
        private const string TankTraitDefName = "ChTank";
        private const string DampenerHediffDefName = "ChTank_PainDampener"; // <-- confirm this matches your HediffDef defName

        private static TraitDef TankTrait => DefDatabase<TraitDef>.GetNamedSilentFail(TankTraitDefName);
        private static HediffDef Dampener => DefDatabase<HediffDef>.GetNamedSilentFail(DampenerHediffDefName);

        public static void TickPawn(Pawn p)
        {
            if (p?.story?.traits == null || p.health?.hediffSet == null) return;

            var tankTrait = TankTrait;
            var dampener = Dampener;
            if (tankTrait == null || dampener == null) return;

            bool hasTank = p.story.traits.HasTrait(tankTrait);
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
