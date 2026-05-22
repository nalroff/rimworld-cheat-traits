using CheatTraits.Patches;
using RimWorld;
using Verse;
using Verse.Sound;

namespace CheatTraits.Comps
{
    public class CompProperties_AbilityChFlyingPunch : CompProperties_AbilityEffect
    {
        // Base blunt damage of the strike before the Boxer multiplier.
        // Roughly matches a heavy vanilla unarmed punch.
        public float baseDamage = 15f;

        // Multiplier applied when the caster has the ChBoxer trait. Mirrors
        // the trait's MeleeDamageFactor x10 unarmed passive so the Flying
        // Punch always lands at signature damage — even if the Boxer is
        // holding a weapon (the unarmed-only stat patch would otherwise
        // suppress the x10 in that case).
        public float boxerMultiplier = 10f;

        public float armorPenetration = 1.5f;

        public CompProperties_AbilityChFlyingPunch()
        {
            compClass = typeof(CompAbilityEffect_ChFlyingPunch);
        }
    }

    /// <summary>
    /// Two-phase ability:
    ///   1. Apply() — fires on cast. No effect here; the jump is started by
    ///      Verb_CastAbilityChFlyingPunch.TryCastShot via JumpUtility.DoJump.
    ///   2. OnJumpCompleted() — fires when the PawnFlyer lands. Applies a
    ///      direct DamageInfo to the original target.
    ///
    /// Damage is applied directly rather than routed through the Boxer's
    /// melee verb because the unarmed-only stat patch in
    /// ChTraitsGetStatValuePatch.IsBoxer suppresses the x10 factor when the
    /// pawn has any primary weapon equipped, and a post-landing stance race
    /// can also drop the TryMeleeAttack call entirely.
    /// </summary>
    public class CompAbilityEffect_ChFlyingPunch
        : CompAbilityEffect,
            ICompAbilityEffectOnJumpCompleted
    {
        private new CompProperties_AbilityChFlyingPunch Props =>
            (CompProperties_AbilityChFlyingPunch)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            // Strike is deferred to OnJumpCompleted — pawn is mid-flight here.
        }

        public void OnJumpCompleted(IntVec3 origin, LocalTargetInfo target)
        {
            Pawn? caster = parent?.pawn;
            if (caster == null || !caster.Spawned)
                return;

            Thing? thing = target.Thing;
            if (thing == null || thing.Destroyed || !thing.Spawned)
                return;
            if (thing.Map != caster.Map)
                return;

            float damage = Props.baseDamage;
            if (CheatTraitsUtils.HasTrait(caster, CheatTraitsNames.BoxerTrait))
                damage *= Props.boxerMultiplier;

            DamageInfo dinfo = new DamageInfo(
                DamageDefOf.Blunt,
                damage,
                Props.armorPenetration,
                angle: -1f,
                instigator: caster,
                hitPart: null,
                weapon: null,
                category: DamageInfo.SourceCategory.ThingOrUnknown,
                intendedTarget: thing
            );
            thing.TakeDamage(dinfo);

            SoundDefOf.Pawn_Melee_Punch_HitPawn.PlayOneShot(
                new TargetInfo(thing.Position, caster.Map, false)
            );
        }
    }
}
