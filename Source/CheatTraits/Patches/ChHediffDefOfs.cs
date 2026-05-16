using RimWorld;
using Verse;

namespace CheatTraits.Patches
{
    [DefOf]
    public static class ChHediffDefOf
    {
        public static HediffDef ChSuperSoldier = null!;

        static ChHediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ChHediffDefOf));
        }
    }
}
