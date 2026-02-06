using System.Collections.Generic;
using Verse;

namespace CheatTraits.Patches
{
    public class CheatTraitsMapComponent : MapComponent
    {
        private int nextPawnTick;
        private int nextGreenThumbTick;
        private int nextAscendantTick;
        private int nextBeastmasterTick;
        private int nextDiplomatTick;
        private int nextComfyTick;

        private HashSet<int> chComfyFireSuppressionDisabledPawnIds = new HashSet<int>();
        private Dictionary<int, int> chComfyNextDeployTickByPawnId = new Dictionary<int, int>();

        public CheatTraitsMapComponent(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            int tick = Find.TickManager.TicksGame;

            Map m = map;
            if (m == null) return;

            // Pawn-facing systems cadence
            if (tick >= nextPawnTick)
            {
                nextPawnTick = tick + 120;

                var pawns = m.mapPawns?.AllPawnsSpawned;
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
                ChAscendantAuraSystem.TickMap(m);
            }

            // Beastmaster cadence (250)
            if (tick >= nextBeastmasterTick)
            {
                nextBeastmasterTick = tick + ChBeastmasterAuraConfig.ScanIntervalTicks;
                ChBeastmasterAuraSystem.TickMap(m);
            }

            // Diplomat cadence (250)
            if (tick >= nextDiplomatTick)
            {
                nextDiplomatTick = tick + ChDiplomatAuraConfig.ScanIntervalTicks;
                ChDiplomatAuraSystem.TickMap(m);
            }

            // Green Thumb cadence
            if (tick >= nextGreenThumbTick)
            {
                nextGreenThumbTick = tick + ChGreenThumbAuraConfig.ScanIntervalTicks;
                ChGreenThumbAura.RebuildAffectedPlants(m);
            }

            // ChComfy cadence
            if (tick >= nextComfyTick)
            {
                nextComfyTick = tick + ChComfyAuraConfig.UpdateIntervalTicks;
                ChComfyAuraSystem.TickMap(m);
            }
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref chComfyFireSuppressionDisabledPawnIds,
                "chComfy_fireSuppressionDisabledPawnIds", LookMode.Value);

            if (chComfyFireSuppressionDisabledPawnIds == null)
                chComfyFireSuppressionDisabledPawnIds = new HashSet<int>();

            List<int>? tmpKeys = null;
            List<int>? tmpVals = null;

            Scribe_Collections.Look(ref chComfyNextDeployTickByPawnId,
                "chComfy_nextDeployTickByPawnId",
                LookMode.Value, LookMode.Value,
                ref tmpKeys, ref tmpVals);

            if (chComfyNextDeployTickByPawnId == null)
                chComfyNextDeployTickByPawnId = new Dictionary<int, int>();
        }

        public bool ChComfy_IsFireSuppressionEnabled(Pawn pawn)
        {
            if (pawn == null) return false;
            return !chComfyFireSuppressionDisabledPawnIds.Contains(pawn.thingIDNumber);
        }

        public void ChComfy_SetFireSuppressionEnabled(Pawn pawn, bool enabled)
        {
            if (pawn == null) return;

            int id = pawn.thingIDNumber;
            if (enabled)
                chComfyFireSuppressionDisabledPawnIds.Remove(id);
            else
                chComfyFireSuppressionDisabledPawnIds.Add(id);
        }
    }
}
