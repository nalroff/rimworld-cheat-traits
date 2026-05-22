using RimWorld;
using Verse;

namespace CheatTraits.Comps
{
    public class CompProperties_AbilityChIronWall : CompProperties_AbilityEffect
    {
        public string hediffDefName = "ChTank_IronWall";

        public CompProperties_AbilityChIronWall()
        {
            compClass = typeof(CompAbilityEffect_ChIronWall);
        }
    }

    public class CompAbilityEffect_ChIronWall : CompAbilityEffect
    {
        private new CompProperties_AbilityChIronWall Props =>
            (CompProperties_AbilityChIronWall)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn? caster = parent?.pawn;
            if (caster == null || caster.Dead || !caster.Spawned || caster.health == null)
                return;

            HediffDef? def = DefDatabase<HediffDef>.GetNamedSilentFail(Props.hediffDefName);
            if (def == null)
                return;

            Hediff existing = caster.health.hediffSet.GetFirstHediffOfDef(def);
            if (existing != null)
                caster.health.RemoveHediff(existing);

            Hediff hediff = HediffMaker.MakeHediff(def, caster);
            caster.health.AddHediff(hediff);

            FleckMaker.Static(caster.DrawPos, caster.Map, FleckDefOf.PsycastSkipFlashEntry, 2.0f);
        }
    }
}
