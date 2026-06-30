using System.Collections.Generic;
using Verse;

namespace CheatTraits.Patches
{
    public class CheatTraitsMapComponent : MapComponent
    {
        private int nextPawnTick;
        private int nextGreenThumbTick;
        private int nextBeastmasterTick;
        private int nextDiplomatTick;
        private int nextComfyTick;
        private int nextFloragenTick;
        private int nextEurekaTick;
        private int nextBardTick;

        private HashSet<int> chComfyFireSuppressionDisabledPawnIds = new HashSet<int>();
        private Dictionary<int, int> chComfyNextDeployTickByPawnId = new Dictionary<int, int>();

        private ChEurekaTracker eurekaTracker = new ChEurekaTracker();

        public ChEurekaTracker EurekaTracker => eurekaTracker;

        public CheatTraitsMapComponent(Map map)
            : base(map) { }

        public override void MapComponentTick()
        {
            int tick = Find.TickManager.TicksGame;

            Map m = map;
            if (m == null)
                return;

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
                        if (p?.story?.traits == null || p.health?.hediffSet == null)
                            continue;

                        // Keep Tank as a per-pawn applier
                        ChTankHediffApplier.TickPawn(p);
                        ChWizardHediffApplier.TickPawn(p);
                        ChDiplomatAbilityApplier.TickPawn(p);
                        ChBoxerAbilityApplier.TickPawn(p);
                        ChTexAbilityApplier.TickPawn(p);
                        ChDiggerAbilityApplier.TickPawn(p);
                        ChTankAbilityApplier.TickPawn(p);
                        ChDocAbilityApplier.TickPawn(p);
                        ChArtificerAbilityApplier.TickPawn(p);
                        ChEngineerAbilityApplier.TickPawn(p);
                        ChZippyAbilityApplier.TickPawn(p);
                        ChBeastmasterAbilityApplier.TickPawn(p);
                        ChBardHediffApplier.TickPawn(p);
                    }
                }
            }

            // Eureka tracker cadence
            if (tick >= nextEurekaTick)
            {
                nextEurekaTick = tick + ChEurekaTracker.TickGateTicks;
                eurekaTracker.Tick(m);
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

            // Bard cadence (250) — ramping support-hero aura
            if (tick >= nextBardTick)
            {
                nextBardTick = tick + ChBardAuraConfig.ScanIntervalTicks;
                ChBardAuraSystem.TickMap(m);
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

            // Floragen Core cadence (building-driven, low frequency)
            if (tick >= nextFloragenTick)
            {
                int interval = ChFloragenCoreSystem.TickMap(m);
                nextFloragenTick = tick + interval;
            }
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(
                ref chComfyFireSuppressionDisabledPawnIds,
                "chComfy_fireSuppressionDisabledPawnIds",
                LookMode.Value
            );

            if (chComfyFireSuppressionDisabledPawnIds == null)
                chComfyFireSuppressionDisabledPawnIds = new HashSet<int>();

            List<int>? tmpKeys = null;
            List<int>? tmpVals = null;

            Scribe_Collections.Look(
                ref chComfyNextDeployTickByPawnId,
                "chComfy_nextDeployTickByPawnId",
                LookMode.Value,
                LookMode.Value,
                ref tmpKeys,
                ref tmpVals
            );

            if (chComfyNextDeployTickByPawnId == null)
                chComfyNextDeployTickByPawnId = new Dictionary<int, int>();

            Scribe_Deep.Look(ref eurekaTracker, "chEurekaTracker");
            if (eurekaTracker == null)
                eurekaTracker = new ChEurekaTracker();
        }

        public bool ChComfy_IsFireSuppressionEnabled(Pawn pawn)
        {
            if (pawn == null)
                return false;
            return !chComfyFireSuppressionDisabledPawnIds.Contains(pawn.thingIDNumber);
        }

        public void ChComfy_SetFireSuppressionEnabled(Pawn pawn, bool enabled)
        {
            if (pawn == null)
                return;

            int id = pawn.thingIDNumber;
            if (enabled)
                chComfyFireSuppressionDisabledPawnIds.Remove(id);
            else
                chComfyFireSuppressionDisabledPawnIds.Add(id);
        }
    }
}
