using RimWorld;
using Verse;

namespace ChTraits.Patches
{
    public class ChTraitsMapComponent : MapComponent
    {
        private int nextPawnTick;
        private int nextGreenThumbTick;
        private int nextAscendantTick;
        private int nextBeastmasterTick;
        private int nextDiplomatTick;

        public ChTraitsMapComponent(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            int tick = Find.TickManager.TicksGame;

            // Pawn-facing systems cadence
            if (tick >= nextPawnTick)
            {
                nextPawnTick = tick + 120;

                var pawns = map?.mapPawns?.AllPawnsSpawned;
                if (pawns != null)
                {
                    for (int i = 0; i < pawns.Count; i++)
                    {
                        Pawn p = pawns[i];
                        if (p?.story?.traits == null || p.health?.hediffSet == null) continue;

                        // Keep Tank as a per-pawn applier
                        ChTankHediffApplier.TickPawn(p);
                    }
                }
            }

            // Ascendant cadence (250)
            if (tick >= nextAscendantTick)
            {
                nextAscendantTick = tick + ChAscendantAuraConfig.ScanIntervalTicks;
                ChAscendantAuraSystem.TickMap(map);
            }

            // Beastmaster cadence (250)
            if (tick >= nextBeastmasterTick)
            {
                nextBeastmasterTick = tick + ChBeastmasterAuraConfig.ScanIntervalTicks;
                ChBeastmasterAuraSystem.TickMap(map);
            }

            // Diplomat cadence (250)
            if (tick >= nextDiplomatTick)
            {
                nextDiplomatTick = tick + ChDiplomatAuraConfig.ScanIntervalTicks;
                ChDiplomatAuraSystem.TickMap(map);
            }

            // Plant-facing systems cadence (separate so you can tune later)
            if (tick >= nextGreenThumbTick)
            {
                nextGreenThumbTick = tick + ChGreenThumbAuraConfig.UpdateIntervalTicks; // assumes this exists in your project
                ChGreenThumbAura.RebuildAffectedPlants(map);
            }
        }
    }
}
