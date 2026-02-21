using RimWorld;
using Verse;

namespace CheatTraits.Patches
{
    [DefOf]
    public static class ChThingDefOf
    {
        public static ThingDef ChComfortNode = null!;
        public static ThingDef ChFloragenCore = null!;
        public static ThingDef ChTeslaCoil = null!;

        static ChThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ChThingDefOf));
        }
    }
}
