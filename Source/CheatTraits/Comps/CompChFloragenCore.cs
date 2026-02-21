using Verse;

namespace CheatTraits.Comps
{
    /// <summary>
    /// Passive plant growth amplifier.
    ///
    /// Intentionally does not tick on its own; the centralized CheatTraitsMapComponent
    /// drives scanning/applying via ChFloragenCoreSystem to avoid duplicate work.
    /// </summary>
    public class CompProperties_ChFloragenCore : CompProperties
    {
        public float radius = 12f;

        // Much lower cadence than pawn auras for performance.
        // 2000 ticks = the same cadence as Plant.TickLong.
        public int scanIntervalTicks = 2000;

        // Total multiplier target.
        // Implemented as: add (growthMultiplier - 1) * baseline growth directly.
        public float growthMultiplier = 3f;

        // Soft cap on how many plants we will process per map per scan.
        public int maxTrackedPlantsPerMap = 1200;

        public CompProperties_ChFloragenCore()
        {
            compClass = typeof(CompChFloragenCore);
        }
    }

    public class CompChFloragenCore : ThingComp
    {
        public CompProperties_ChFloragenCore PropsEx => (CompProperties_ChFloragenCore)props;
    }
}
