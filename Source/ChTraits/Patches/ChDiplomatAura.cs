using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ChTraits.Patches
{
    internal static class ChDiplomatAuraConfig
    {
        internal const int AuraRadius = 16;
        internal const int AuraRadiusSquared = AuraRadius * AuraRadius;
        internal const int ScanIntervalTicks = 250;

        // 6 hours linger; you can bump to 8-12h if you want “always on” in base
        internal const int PresenceLingerTicks = 6 * 2500;
    }

    internal static class ChDiplomatAuraSystem
    {
        private static readonly List<Pawn> diplomats = new List<Pawn>(4);

        public static void TickMap(Map map)
        {
            if (map == null) return;

            var pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null || pawns.Count == 0) return;

            diplomats.Clear();

            // Collect diplomats
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p.story?.traits == null) continue;
                if (!ChTraitsUtils.HasTrait(p, ChTraitsNames.DiplomatTrait)) continue;
                diplomats.Add(p);
            }

            if (diplomats.Count == 0) return;

            // Apply presence to nearby friendlies
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn target = pawns[i];
                if (!ChTraitsUtils.IsHediffEligible(target)) continue;
                for (int j = 0; j < diplomats.Count; j++)
                {
                    Pawn source = diplomats[j];
                    if ((source.Position - target.Position).LengthHorizontalSquared > ChDiplomatAuraConfig.AuraRadiusSquared) continue;
                    if (!ChTraitsUtils.IsAuraAlly(source, target)) continue;

                    EnsurePresenceHediff(target, ChDiplomatDefOf.ChDiplomat_Presence);
                    break;
                }
            }
        }

        private static void EnsurePresenceHediff(Pawn target, HediffDef hediffDef)
        {
            Hediff existing = target.health.hediffSet.GetFirstHediffOfDef(ChDiplomatDefOf.ChDiplomat_Presence);
            if (existing == null)
            {
                existing = HediffMaker.MakeHediff(ChDiplomatDefOf.ChDiplomat_Presence, target);
                target.health.AddHediff(existing);
            }

            var disappears = existing.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = ChDiplomatAuraConfig.PresenceLingerTicks;
            }
        }
    }

    [DefOf]
    internal static class ChDiplomatDefOf
    {
        #pragma warning disable 0649
        public static HediffDef ChDiplomat_Presence;
        #pragma warning restore 0649

        static ChDiplomatDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ChDiplomatDefOf));
    }
}
