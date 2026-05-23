using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace CheatTraits.Patches
{
    public class ChEurekaTracker : IExposable
    {
        public const int EurekaIntervalTicks = 900000;
        public const int EurekaDurationTicks = 180000;
        public const int EurekaRecipesPerEvent = 2;
        public const int TickGateTicks = 2500;

        private const string RecipePrefix = "ChEureka_";

        public class EurekaActive : IExposable
        {
            public string recipeDefName = string.Empty;
            public int expiresAtTick;

            public void ExposeData()
            {
                Scribe_Values.Look(ref recipeDefName, "recipeDefName", string.Empty);
                Scribe_Values.Look(ref expiresAtTick, "expiresAtTick", 0);
            }
        }

        private int nextEurekaTick = -1;
        private List<EurekaActive> actives = new List<EurekaActive>();

        public IReadOnlyList<EurekaActive> Actives => actives;

        public int NextEurekaTick => nextEurekaTick;

        public void ExposeData()
        {
            Scribe_Values.Look(ref nextEurekaTick, "nextEurekaTick", -1);
            Scribe_Collections.Look(ref actives, "actives", LookMode.Deep);
            if (actives == null)
                actives = new List<EurekaActive>();
        }

        public bool IsActive(string recipeDefName)
        {
            if (string.IsNullOrEmpty(recipeDefName))
                return false;
            for (int i = 0; i < actives.Count; i++)
            {
                if (actives[i].recipeDefName == recipeDefName)
                    return true;
            }
            return false;
        }

        public IEnumerable<RecipeDef> GetActiveRecipes()
        {
            for (int i = 0; i < actives.Count; i++)
            {
                RecipeDef def = DefDatabase<RecipeDef>.GetNamedSilentFail(actives[i].recipeDefName);
                if (def != null)
                    yield return def;
            }
        }

        public void Tick(Map map)
        {
            if (map == null)
                return;

            int now = Find.TickManager.TicksGame;

            for (int i = actives.Count - 1; i >= 0; i--)
            {
                if (now >= actives[i].expiresAtTick)
                    actives.RemoveAt(i);
            }

            if (!MapHasAscendant(map))
                return;

            if (nextEurekaTick < 0)
            {
                nextEurekaTick = now + EurekaIntervalTicks;
                return;
            }

            if (now >= nextEurekaTick)
                FireEureka(map, now);
        }

        private static bool MapHasAscendant(Map map)
        {
            var pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null || pawns.Count == 0)
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

        private void FireEureka(Map map, int now)
        {
            var eligible = new List<RecipeDef>();
            var all = DefDatabase<RecipeDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                RecipeDef d = all[i];
                if (d?.defName == null)
                    continue;
                if (!d.defName.StartsWith(RecipePrefix))
                    continue;
                if (IsActive(d.defName))
                    continue;
                eligible.Add(d);
            }

            int picks = System.Math.Min(EurekaRecipesPerEvent, eligible.Count);
            var picked = new List<RecipeDef>(picks);
            for (int i = 0; i < picks; i++)
            {
                RecipeDef chosen = eligible.RandomElement();
                eligible.Remove(chosen);
                picked.Add(chosen);
                actives.Add(
                    new EurekaActive
                    {
                        recipeDefName = chosen.defName,
                        expiresAtTick = now + EurekaDurationTicks,
                    }
                );
            }

            nextEurekaTick = now + EurekaIntervalTicks;

            if (picked.Count > 0)
                SendEurekaLetter(map, picked);
        }

        private static void SendEurekaLetter(Map map, List<RecipeDef> picked)
        {
            Pawn? ascendant = FindAscendant(map);
            string who = ascendant?.LabelShortCap ?? "The Ascendant";

            StringBuilder body = new StringBuilder();
            body.Append(who);
            body.Append(" has had a breakthrough. The Eureka Forge can now produce:\n");
            for (int i = 0; i < picked.Count; i++)
            {
                body.Append("  - ");
                body.Append(picked[i].LabelCap);
                body.Append('\n');
            }
            body.Append("These recipes will remain available for 3 days.");

            if (ascendant != null)
            {
                Find.LetterStack.ReceiveLetter(
                    "Eureka!",
                    body.ToString(),
                    LetterDefOf.PositiveEvent,
                    new LookTargets(ascendant)
                );
            }
            else
            {
                Find.LetterStack.ReceiveLetter(
                    "Eureka!",
                    body.ToString(),
                    LetterDefOf.PositiveEvent
                );
            }
        }

        private static Pawn? FindAscendant(Map map)
        {
            var pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
                return null;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (CheatTraitsUtils.HasTrait(p, CheatTraitsNames.AscendantTrait))
                    return p;
            }
            return null;
        }
    }

    public class ChRecipeWorker_Eureka : RecipeWorker
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord? part = null)
        {
            if (!base.AvailableOnNow(thing, part))
                return false;
            if (thing?.Map == null)
                return false;

            CheatTraitsMapComponent? mc = thing.Map.GetComponent<CheatTraitsMapComponent>();
            ChEurekaTracker? tracker = mc?.EurekaTracker;
            if (tracker == null)
                return false;

            return tracker.IsActive(this.recipe.defName);
        }
    }
}
