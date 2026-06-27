using System.Collections.Generic;
using CheatTraits.Hediffs;
using RimWorld;
using Verse;

namespace CheatTraits.Patches
{
    /// <summary>
    /// The four aura stances a ChBard can switch between. Values are the
    /// canonical index used everywhere (conductor state, gizmos, buff lookup).
    /// </summary>
    internal enum ChBardMode
    {
        WarAnthem = 0,
        Bulwark = 1,
        Vigor = 2,
        HeroicBoon = 3,
    }

    /// <summary>
    /// Mode metadata + lookups shared by the conductor hediff, the gizmos, and
    /// the aura system. Intentionally texture-free so the conductor's health-tab
    /// label can read <see cref="Labels"/> without dragging in graphics state;
    /// gizmo icons live in ChBardGizmoPatch.
    /// </summary>
    internal static class ChBardModes
    {
        public const int Count = 4;

        public static readonly string[] Labels =
        {
            "War Anthem",
            "Bulwark",
            "Vigor",
            "Heroic Boon",
        };

        public static readonly string[] Descriptions =
        {
            "Offense. Empowers nearby colonists' melee and ranged attacks — hit chance, damage, and faster swings and shots.",
            "Defense. Thickens nearby colonists' armor, dulls pain, and cuts incoming damage.",
            "Sustain. Slows nearby colonists' hunger and fatigue, speeds healing and immunity, and hardens them against toxins.",
            "Reflexes and toughness. Grants nearby colonists Nimble-like dodge and Tough-like damage reduction, plus a little speed.",
        };

        public static int Clamp(int index)
        {
            if (index < 0)
                return 0;
            if (index >= Count)
                return Count - 1;
            return index;
        }

        public static HediffDef BuffDef(int index)
        {
            switch ((ChBardMode)Clamp(index))
            {
                case ChBardMode.WarAnthem:
                    return ChBardDefOf.ChBard_WarAnthem;
                case ChBardMode.Bulwark:
                    return ChBardDefOf.ChBard_Bulwark;
                case ChBardMode.Vigor:
                    return ChBardDefOf.ChBard_Vigor;
                case ChBardMode.HeroicBoon:
                    return ChBardDefOf.ChBard_HeroicBoon;
                default:
                    return null!;
            }
        }
    }

    internal static class ChBardAuraConfig
    {
        internal const int AuraRadius = 12;
        internal const int AuraRadiusSquared = AuraRadius * AuraRadius;
        internal const int ScanIntervalTicks = 250;

        // Severity added to each in-range ally's buff per scan. Paired with the
        // buff hediffs' -8.0 severityPerDay decay, this nets a ramp to full over
        // ~3 in-game hours in the aura, and a ~3h fade to nothing after leaving.
        internal const float RampUpPerScan = 0.07f;
        internal const float MaxSeverity = 1.0f;

        // 3 in-game hours (2500 ticks/hour) between stance changes.
        internal const int SwitchCooldownTicks = 3 * 2500;
    }

    internal static class ChBardAuraSystem
    {
        private static readonly List<Pawn> bards = new List<Pawn>(4);

        public static void TickMap(Map map)
        {
            if (map == null)
                return;

            CheatTraitsUtils.CollectEmitters(map, CheatTraitsNames.BardTrait, bards);
            if (bards.Count == 0)
                return;

            var pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null || pawns.Count == 0)
                return;

            for (int w = 0; w < bards.Count; w++)
            {
                Pawn bard = bards[w];
                if (bard == null || bard.Dead || !bard.Spawned || bard.Map != map)
                    continue;

                // Each Bard emits whichever stance its conductor currently holds.
                var conductor =
                    bard.health?.hediffSet?.GetFirstHediffOfDef(ChBardDefOf.ChBard_Conductor)
                    as Hediff_ChBardConductor;
                if (conductor == null)
                    continue;

                HediffDef buffDef = ChBardModes.BuffDef(conductor.ModeIndex);
                if (buffDef == null)
                    continue;

                IntVec3 wPos = bard.Position;
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn target = pawns[i];

                    // Cheap range check first.
                    if (
                        (wPos - target.Position).LengthHorizontalSquared
                        > ChBardAuraConfig.AuraRadiusSquared
                    )
                        continue;

                    // Same-faction humanlike allies only (excludes the Bard itself).
                    if (!CheatTraitsUtils.IsAuraAlly(bard, target, humanlikesOnly: true))
                        continue;

                    RampBuff(target, buffDef);
                }
            }
        }

        /// <summary>
        /// Applies the buff hediff if missing, then bumps its severity one step
        /// (capped). The hediff's negative SeverityPerDay handles fade-out when
        /// the target stops being refreshed.
        /// </summary>
        private static void RampBuff(Pawn target, HediffDef buffDef)
        {
            if (!CheatTraitsUtils.IsHediffEligible(target))
                return;

            Hediff hediff = target.health.hediffSet.GetFirstHediffOfDef(buffDef);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(buffDef, target);
                target.health.AddHediff(hediff);
            }

            float newSeverity = hediff.Severity + ChBardAuraConfig.RampUpPerScan;
            if (newSeverity > ChBardAuraConfig.MaxSeverity)
                newSeverity = ChBardAuraConfig.MaxSeverity;
            hediff.Severity = newSeverity;
        }
    }

    [DefOf]
    internal static class ChBardDefOf
    {
#pragma warning disable 0649
        public static HediffDef ChBard_Conductor = null!;
        public static HediffDef ChBard_WarAnthem = null!;
        public static HediffDef ChBard_Bulwark = null!;
        public static HediffDef ChBard_Vigor = null!;
        public static HediffDef ChBard_HeroicBoon = null!;
#pragma warning restore 0649

        static ChBardDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ChBardDefOf));
    }
}
