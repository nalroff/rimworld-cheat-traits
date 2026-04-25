using RimWorld;
using Verse;

namespace CheatTraits.Patches
{
    [DefOf]
    public static class ChThingDefOf
    {
        public static ThingDef ChComfortNode = null!;
        public static ThingDef ChComfortNodeWall = null!;
        public static ThingDef ChFloragenCore = null!;
        public static ThingDef ChTeslaCoil = null!;
        public static ThingDef ChTeslaCoilWall = null!;

        static ChThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ChThingDefOf));
        }
    }
}
