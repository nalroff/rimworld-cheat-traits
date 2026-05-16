using RimWorld;
using UnityEngine;
using Verse;

namespace CheatTraits.Comps
{
    public class CompProperties_AbilityChLightningBolt : CompProperties_AbilityEffect
    {
        public float shockDamage = 120f;
        public float shockRadius = 1.5f;
        public float shockArmorPenetration = 2.0f;

        public CompProperties_AbilityChLightningBolt()
        {
            compClass = typeof(CompAbilityEffect_ChLightningBolt);
        }
    }

    public class CompAbilityEffect_ChLightningBolt : CompAbilityEffect
    {
        private new CompProperties_AbilityChLightningBolt Props =>
            (CompProperties_AbilityChLightningBolt)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn? caster = parent?.pawn;
            Map? map = caster?.Map;
            if (map == null)
                return;

            IntVec3 cell = target.Cell;
            if (!cell.IsValid || !cell.InBounds(map))
                return;

            // Vanilla strike: handles visual, thunder sound, screen shake, flame
            // explosion at radius 1.9, and auto-fire ignition.
            Mesh? unusedBoltMesh = null;
            try
            {
                WeatherEvent_LightningStrike.DoStrike(cell, map, ref unusedBoltMesh);
            }
            catch
            {
                // If the vanilla strike throws for any reason, we still want to land
                // the shock damage below.
            }

            // Layered shock: EMP explosion to stun pawns and disable mechs, plus
            // direct damage on the primary target. EMP is the only electricity-themed
            // DamageDef in core 1.6 that doesn't require Anomaly.
            DamageDef shockDef = DamageDefOf.EMP;

            GenExplosion.DoExplosion(
                center: cell,
                map: map,
                radius: Props.shockRadius,
                damType: shockDef,
                instigator: caster,
                damAmount: Mathf.RoundToInt(Props.shockDamage * 0.5f),
                armorPenetration: Props.shockArmorPenetration,
                explosionSound: null,
                weapon: null,
                projectile: null,
                intendedTarget: target.Thing,
                postExplosionSpawnThingDef: null,
                postExplosionSpawnChance: 0f,
                postExplosionSpawnThingCount: 1,
                postExplosionGasType: null,
                applyDamageToExplosionCellsNeighbors: false,
                preExplosionSpawnThingDef: null,
                preExplosionSpawnChance: 0f,
                preExplosionSpawnThingCount: 1,
                chanceToStartFire: 0f,
                damageFalloff: false,
                doVisualEffects: true,
                doSoundEffects: false
            );

            // Direct damage on the primary target if it's a thing we can hit, so
            // the lightning bolt actually feels lethal against a single hardened
            // target (vanilla DoStrike's 1.9-radius Flame explosion + a 0.5x EMP
            // wash alone won't reliably down armored mechs).
            Thing? primaryTarget = target.Thing;
            if (primaryTarget != null && !primaryTarget.Destroyed)
            {
                DamageInfo dinfo = new DamageInfo(
                    DamageDefOf.Burn,
                    Props.shockDamage,
                    Props.shockArmorPenetration,
                    -1f,
                    caster
                );
                primaryTarget.TakeDamage(dinfo);
            }
        }
    }
}
