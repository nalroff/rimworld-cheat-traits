using RimWorld;
using Verse;

namespace CheatTraits.Patches
{
    [DefOf]
    public static class ChTeslaThingDefOf
    {
        public static ThingDef ChTeslaCoil = null!;

        static ChTeslaThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ChTeslaThingDefOf));
        }
    }
}
