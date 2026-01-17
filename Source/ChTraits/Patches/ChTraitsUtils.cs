using RimWorld;
using Verse;

namespace ChTraits.Patches
{
    public static class ChTraitsUtils
    {
        internal static bool HasTrait(Pawn pawn, string defName)
            => pawn?.story?.traits?.HasTrait(DefDatabase<TraitDef>.GetNamedSilentFail(defName)) ?? false;
    }

    public static class ChTraitsNames
    {
        public const string ArtificerTrait = "ChArtificer";
        public const string AscendantTrait = "ChAscendant";
        public const string BeastmasterTrait = "ChBeastmaster";
        public const string BoxerTrait = "ChBoxer";
        public const string DocTrait = "ChDoc";
        public const string GreenThumbTrait = "ChGreenThumb";
        public const string TankTrait = "ChTank";
        public const string TexTrait = "ChTex";
    }
}
