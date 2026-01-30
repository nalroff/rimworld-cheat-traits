using RimWorld;
using System.Collections.Generic;
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

        /// <summary>
        /// Collects all emitters (pawns with the given trait) from the map's spawned pawns.
        /// </summary>
        internal static void CollectEmitters(Map map, string traitName, List<Pawn> outEmitters)
        {
            if (map == null || outEmitters == null) return;

            var pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null || pawns.Count == 0) return;

            outEmitters.Clear();
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p?.story?.traits == null) continue;
                if (HasTrait(p, traitName))
                {
                    outEmitters.Add(p);
                }
            }
        }

        /// <summary>
        /// Applies an aura hediff from emitters to eligible targets within range.
        /// Handles hediff creation, refresh logic, and all ally/range checks.
        /// </summary>
        /// <param name="map">The map to apply auras on</param>
        /// <param name="emitters">List of emitter pawns (sources of aura)</param>
        /// <param name="targetPredicate">Optional predicate to filter targets (e.g., IsAnimal, IsHediffEligible). If null, no additional filter.</param>
        /// <param name="hediffDef">The hediff to apply</param>
        /// <param name="radiusSquared">Range check: (emitterPos - targetPos).LengthHorizontalSquared</param>
        /// <param name="refreshTicks">How many ticks to set on HediffComp_Disappears</param>
        /// <param name="humanlikesOnly">Pass to IsAuraAlly for humanlike filtering</param>
        internal static void ApplyAuraHediff(
            Map map,
            List<Pawn> emitters,
            System.Func<Pawn, bool> targetPredicate,
            HediffDef hediffDef,
            int radiusSquared,
            int refreshTicks,
            bool humanlikesOnly = true)
        {
            if (map == null || emitters == null || emitters.Count == 0 || hediffDef == null) return;

            var pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null || pawns.Count == 0) return;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn target = pawns[i];

                // Apply optional predicate (eligibility check).
                if (targetPredicate != null && !targetPredicate(target)) continue;

                IntVec3 tPos = target.Position;

                for (int j = 0; j < emitters.Count; j++)
                {
                    Pawn source = emitters[j];
                    if (source == null || source.Dead || !source.Spawned) continue;
                    if (source.Map != map) continue;

                    // Range check first (cheap).
                    if ((source.Position - tPos).LengthHorizontalSquared > radiusSquared) continue;

                    // Ally/eligibility check.
                    if (!IsAuraAlly(source, target, humanlikesOnly)) continue;

                    ApplyOrRefreshHediff(target, hediffDef, refreshTicks);
                    break;
                }
            }
        }

        /// <summary>
        /// Ensures a hediff exists on the target and refreshes its disappear timer.
        /// </summary>
        private static void ApplyOrRefreshHediff(Pawn target, HediffDef hediffDef, int refreshTicks)
        {
            if (!IsHediffEligible(target) || hediffDef == null) return;

            Hediff hediff = target.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(hediffDef, target);
                target.health.AddHediff(hediff);
            }

            HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = refreshTicks;
            }
        }
    }

    public static class ChTraitsNames
    {
        public const string ArtificerTrait = "ChArtificer";
        public const string AscendantTrait = "ChAscendant";
        public const string BeastmasterTrait = "ChBeastmaster";
        public const string BoxerTrait = "ChBoxer";
        public const string ComfyTrait = "ChComfy";
        public const string DiggerTrait = "ChDigger";
        public const string DiplomatTrait = "ChDiplomat";
        public const string DocTrait = "ChDoc";
        public const string GreenThumbTrait = "ChGreenThumb";
        public const string TankTrait = "ChTank";
        public const string TexTrait = "ChTex";
        public const string ZippyTrait = "ChZippy";
        public const string TeslaTrait = "ChTesla";
    }
}
