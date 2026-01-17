using RimWorld;
using Verse;

namespace ChTraits
{
    [DefOf]
    public static class ChJobDefOf
    {
        static ChJobDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ChJobDefOf));

        public static JobDef ChBeastmaster_TameNoFood;
    }
}
