using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ChTraits.Patches
{
    public static class ChTraitsUtils
    {
        private static readonly Dictionary<string, TraitDef> traitDefCache = new Dictionary<string, TraitDef>();

        internal static bool HasTrait(Pawn pawn, string defName)
        {
            if (pawn?.story?.traits == null) return false;
            if (string.IsNullOrEmpty(defName)) return false;

            if (!traitDefCache.TryGetValue(defName, out TraitDef traitDef))
            {
                traitDef = DefDatabase<TraitDef>.GetNamedSilentFail(defName);
                traitDefCache[defName] = traitDef; // cache null too
            }

            return traitDef != null && pawn.story.traits.HasTrait(traitDef);
        }
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
        public const string DiplomatTrait = "ChDiplomat";
    }
}
