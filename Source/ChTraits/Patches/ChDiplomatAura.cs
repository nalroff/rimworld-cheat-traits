using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ChTraits.Patches
{
    internal static class ChDiplomatAuraConfig
    {
        internal const int AuraRadius = 16;
        internal const int ScanIntervalTicks = 250;

        // 6 hours linger; you can bump to 8-12h if you want “always on” in base
        internal const int PresenceLingerTicks = 6 * 2500;
    }

    [DefOf]
    internal static class ChDiplomatDefOf
    {
        #pragma warning disable 0649
        public static HediffDef ChDiplomat_Presence;
        #pragma warning restore 0649

        static ChDiplomatDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ChDiplomatDefOf));
        }
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
                if (p == null || !p.Spawned || p.Dead) continue;
                if (p.story?.traits == null) continue;
                if (!ChTraitsUtils.HasTrait(p, ChTraitsNames.DiplomatTrait)) continue;
                diplomats.Add(p);
            }

            if (diplomats.Count == 0) return;

            var hediffDef = ChDiplomatDefOf.ChDiplomat_Presence;
            if (hediffDef == null) return;

            int r2 = ChDiplomatAuraConfig.AuraRadius * ChDiplomatAuraConfig.AuraRadius;

            // Apply presence to nearby friendly humanlikes (you can widen this later if you want)
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn target = pawns[i];
                if (target == null || !target.Spawned || target.Dead) continue;
                if (!target.RaceProps.Humanlike) continue;

                // "Relationships & mood around them" usually means colony-side pawns
                if (target.Faction != Faction.OfPlayer) continue;

                bool inAura = false;
                for (int j = 0; j < diplomats.Count; j++)
                {
                    Pawn d = diplomats[j];
                    if (d.Map != target.Map) continue;

                    // Optional: exclude downed diplomats so aura doesn't work from a hospital bed
                    if (d.Downed) continue;

                    if ((d.Position - target.Position).LengthHorizontalSquared <= r2)
                    {
                        inAura = true;
                        break;
                    }
                }

                if (!inAura) continue;

                Hediff existing = target.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                if (existing == null)
                {
                    existing = HediffMaker.MakeHediff(hediffDef, target);
                    target.health.AddHediff(existing);
                }

                var disappears = existing.TryGetComp<HediffComp_Disappears>();
                if (disappears != null)
                {
                    // Refresh linger whenever they’re in range
                    disappears.ticksToDisappear = ChDiplomatAuraConfig.PresenceLingerTicks;
                }
            }
        }
    }
}
