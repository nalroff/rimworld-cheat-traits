using System.Collections.Generic;
using System.Text;
using CheatTraits.Patches;
using RimWorld;
using Verse;

namespace CheatTraits.Comps
{
    public class CompProperties_ChEurekaForgeInspect : CompProperties
    {
        public CompProperties_ChEurekaForgeInspect()
        {
            compClass = typeof(CompChEurekaForgeInspect);
        }
    }

    public class CompChEurekaForgeInspect : ThingComp
    {
        public override string? CompInspectStringExtra()
        {
            Map? map = parent?.Map;
            if (map == null)
                return null;

            ChEurekaTracker? tracker = map.GetComponent<CheatTraitsMapComponent>()?.EurekaTracker;
            if (tracker == null)
                return null;

            int now = Find.TickManager.TicksGame;
            IReadOnlyList<ChEurekaTracker.EurekaActive> actives = tracker.Actives;

            StringBuilder sb = new StringBuilder();

            if (actives.Count > 0)
            {
                sb.Append("Eureka recipes available:");
                for (int i = 0; i < actives.Count; i++)
                {
                    var entry = actives[i];
                    RecipeDef? def = DefDatabase<RecipeDef>.GetNamedSilentFail(entry.recipeDefName);
                    string label = def?.LabelCap ?? entry.recipeDefName;
                    int remaining = entry.expiresAtTick - now;
                    if (remaining < 0)
                        remaining = 0;
                    sb.Append("\n  - ");
                    sb.Append(label);
                    sb.Append(" (");
                    sb.Append(remaining.ToStringTicksToPeriod());
                    sb.Append(" remaining)");
                }
            }
            else
            {
                int next = tracker.NextEurekaTick;
                if (next < 0)
                {
                    sb.Append("No Eureka recipes active.");
                }
                else
                {
                    int wait = next - now;
                    if (wait < 0)
                        wait = 0;
                    sb.Append("No Eureka recipes active. Next Eureka in ");
                    sb.Append(wait.ToStringTicksToPeriod());
                    sb.Append('.');
                }
            }

            if (!MapHasAscendant(map))
                sb.Append("\n(Eureka requires a Ch Ascendant on this map.)");

            return sb.ToString();
        }

        private static bool MapHasAscendant(Map map)
        {
            var pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
                return false;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == null || p.Dead || !p.Spawned)
                    continue;
                if (CheatTraitsUtils.HasTrait(p, CheatTraitsNames.AscendantTrait))
                    return true;
            }
            return false;
        }
    }
}
