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

        internal static bool IsValidPlayerColonistTarget(Pawn p)
        {
            if (p == null || p.Dead || !p.Spawned) return false;

            // Only player-controlled pawns (colonists + typically slaves), excludes prisoners/guests.
            if (!p.IsColonistPlayerControlled) return false;

            // Optional: humanlikes only (if you don't want auras hitting animals by accident)
            if (!p.RaceProps.Humanlike) return false;

            return true;
        }

        internal static bool IsAnimal(Pawn pawn)
            => pawn != null && pawn.RaceProps != null && pawn.RaceProps.Animal;

        internal static bool IsAuraAlly(Pawn source, Pawn target, bool humanlikesOnly = true)
        {
            if (source == null || target == null || source == target) return false;
            if (humanlikesOnly && (target.RaceProps == null || !target.RaceProps.Humanlike)) return false;
            if (source.Dead || target.Dead || !source.Spawned || !target.Spawned) return false;
            if (source.Map != target.Map) return false;

            // Treat same faction as ally
            return source.Faction != null && source.Faction == target.Faction;
        }

        internal static bool IsHediffEligible(Pawn target)
            => !(target == null || target.Dead || !target.Spawned || target.health?.hediffSet == null);
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
