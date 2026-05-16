using RimWorld;
using Verse;

namespace CheatTraits.Comps
{
    public class CompProperties_AbilityChMassBerserk : CompProperties_AbilityEffect
    {
        public float effectRadius = 12f;

        public CompProperties_AbilityChMassBerserk()
        {
            compClass = typeof(CompAbilityEffect_ChMassBerserk);
        }
    }

    public class CompAbilityEffect_ChMassBerserk : CompAbilityEffect
    {
        private new CompProperties_AbilityChMassBerserk Props =>
            (CompProperties_AbilityChMassBerserk)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn? caster = parent?.pawn;
            Map? map = caster?.Map;
            if (parent == null || caster == null || map == null)
                return;

            IntVec3 center = target.Cell;
            if (!center.IsValid || !center.InBounds(map))
                return;

            // Central pulse visual at the impact cell. Reuses Royalty psycast flecks
            // for tonal consistency with the rest of the ChWizard kit.
            FleckMaker.Static(center.ToVector3Shifted(), map, FleckDefOf.PsycastSkipFlashEntry, 3f);

            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(center, map, Props.effectRadius, useCenter: true))
            {
                if (!(thing is Pawn p))
                    continue;
                if (p == caster || p.Dead || !p.Spawned || p.Downed)
                    continue;
                if (p.InMentalState)
                    continue;
                if (!p.HostileTo(caster))
                    continue;

                // Mirrors vanilla Neuroquake: humanlikes/animals get Berserk, mechs get
                // BerserkMechanoid (the variant that lets a mech turn on its own).
                MentalStateDef stateDef = p.RaceProps.IsMechanoid
                    ? MentalStateDefOf.BerserkMechanoid
                    : MentalStateDefOf.Berserk;

                // Vanilla helper handles forceWake, sets forceRecoverAfterTicks from
                // Ability_Duration, and sets sourceFaction. PsychicSensitivity scales
                // duration the same way vanilla psycasts do.
                CompAbilityEffect_GiveMentalState.TryGiveMentalState(
                    stateDef,
                    p,
                    parent.def,
                    StatDefOf.PsychicSensitivity,
                    caster,
                    forced: true
                );

                FleckMaker.Static(p.DrawPos, map, FleckDefOf.PsycastSkipInnerExit, 1.5f);
            }
        }
    }
}
