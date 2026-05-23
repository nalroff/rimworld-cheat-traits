using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace CheatTraits.Comps
{
    public class CompProperties_AbilityChCallOfTheWild : CompProperties_AbilityEffect
    {
        public string mentalStateDefName = "ChWildHunt";

        public CompProperties_AbilityChCallOfTheWild()
        {
            compClass = typeof(CompAbilityEffect_ChCallOfTheWild);
        }
    }

    public class CompAbilityEffect_ChCallOfTheWild : CompAbilityEffect
    {
        private new CompProperties_AbilityChCallOfTheWild Props =>
            (CompProperties_AbilityChCallOfTheWild)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn? caster = parent?.pawn;
            Map? map = caster?.Map;
            if (caster == null || map == null)
                return;

            MentalStateDef? stateDef = DefDatabase<MentalStateDef>.GetNamedSilentFail(
                Props.mentalStateDefName
            );
            if (stateDef == null)
                return;

            var pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null || pawns.Count == 0)
                return;

            int affected = 0;
            List<Pawn> snapshot = new List<Pawn>(pawns);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Pawn p = snapshot[i];
                if (p == null || p.Dead || !p.Spawned)
                    continue;
                if (p.RaceProps == null || !p.RaceProps.Animal)
                    continue;
                if (p.Faction == Faction.OfPlayerSilentFail)
                    continue;
                if (p.Downed)
                    continue;
                if (p.mindState?.mentalStateHandler == null)
                    continue;
                if (p.mindState.mentalStateHandler.CurStateDef == stateDef)
                    continue;

                bool started = p.mindState.mentalStateHandler.TryStartMentalState(
                    stateDef,
                    reason: null,
                    forced: true,
                    forceWake: true,
                    causedByMood: false,
                    otherPawn: null,
                    transitionSilently: true
                );
                if (started)
                    affected++;
            }

            FleckMaker.Static(caster.DrawPos, map, FleckDefOf.PsycastSkipFlashEntry, 2.5f);

            if (affected > 0 && PawnUtility.ShouldSendNotificationAbout(caster))
            {
                StringBuilder body = new StringBuilder();
                body.Append(caster.LabelShortCap);
                body.Append(" has called the wild hunt. ");
                body.Append(affected);
                body.Append(
                    affected == 1
                        ? " animal answers, seeking out enemies of the colony."
                        : " animals answer, seeking out enemies of the colony."
                );
                Find.LetterStack.ReceiveLetter(
                    "The wilds have answered.",
                    body.ToString(),
                    LetterDefOf.PositiveEvent,
                    caster
                );
            }
            else
            {
                Messages.Message(
                    "No wild animals on the map answered the call.",
                    caster,
                    MessageTypeDefOf.NeutralEvent,
                    historical: false
                );
            }
        }
    }
}
