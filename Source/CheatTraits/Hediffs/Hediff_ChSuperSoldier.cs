using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CheatTraits.Hediffs
{
    /// <summary>
    /// Bookkeeping hediff for the Super Soldier ability. Spawns a Legendary loadout
    /// (Cataphract Armor + Helmet, fully-charged Shield Belt, Super Charge Rifle),
    /// pegs Shooting and Melee skills to 20, and tears everything down when
    /// HediffComp_Disappears removes the hediff after 2 in-game hours.
    ///
    /// Original gear is dropped (not destroyed), and original skill levels are
    /// restored on removal. Spawned gear is destroyed regardless of whether it
    /// stayed on the original wearer, was dropped, or was picked up by someone
    /// else — the tracked Thing references are followed by save/load too.
    /// </summary>
    public class Hediff_ChSuperSoldier : HediffWithComps
    {
        private List<Thing> spawnedGear = new List<Thing>();
        private int origShootingLevel = -1;
        private int origMeleeLevel = -1;
        private float origShootingXp;
        private float origMeleeXp;
        private bool initialized;

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            // PostAdd fires on first application only; load-from-save uses
            // ExposeData and skips PostAdd, so the persisted state is what's used.
            // The 'initialized' guard is defensive — keeps the grant idempotent
            // if anything causes a second PostAdd.
            if (initialized || pawn == null)
                return;
            initialized = true;

            BoostSkills();
            GrantGear();
            // Pacifist-override patch reads CombinedDisabledWorkTags, but
            // Pawn_WorkSettings/Pawn_SkillTracker cache disable state.
            // Refresh those caches so the work tab and draftability reflect
            // the new Violent-enabled state immediately.
            pawn.Notify_DisabledWorkTypesChanged();
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            RestoreSkills();
            DestroySpawnedGear();
            // Same reason as PostAdd: rebuild the cached disable state so
            // Violent (and the Pacifist trait it came from) re-disables work
            // and drafting now that the override hediff is gone.
            pawn?.Notify_DisabledWorkTypesChanged();
        }

        // Replaces the shield belt's burst absorption with steady regen:
        // closes 1 HP of the worst non-permanent injury every 150 ticks
        // (~10 HP/min). Over the full 2-hour buff this caps around 1200 HP
        // of healing, more than a Legendary shield belt's pool, but spread
        // out so single high-damage hits still matter.
        public override void Tick()
        {
            base.Tick();
            if (pawn == null || pawn.Dead)
                return;
            if (!pawn.IsHashIntervalTick(150))
                return;
            HealWorstInjury();
        }

        private void HealWorstInjury()
        {
            HediffSet? hediffSet = pawn.health?.hediffSet;
            if (hediffSet == null)
                return;

            Hediff_Injury? worst = null;
            float worstSeverity = 0f;
            List<Hediff> hediffs = hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is Hediff_Injury injury
                    && !injury.IsPermanent()
                    && injury.Severity > worstSeverity)
                {
                    worst = injury;
                    worstSeverity = injury.Severity;
                }
            }

            worst?.Heal(1f);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref spawnedGear, "spawnedGear", LookMode.Reference);
            Scribe_Values.Look(ref origShootingLevel, "origShootingLevel", -1);
            Scribe_Values.Look(ref origMeleeLevel, "origMeleeLevel", -1);
            Scribe_Values.Look(ref origShootingXp, "origShootingXp", 0f);
            Scribe_Values.Look(ref origMeleeXp, "origMeleeXp", 0f);
            Scribe_Values.Look(ref initialized, "initialized", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && spawnedGear == null)
                spawnedGear = new List<Thing>();
        }

        private void BoostSkills()
        {
            if (pawn.skills == null)
                return;

            SkillRecord? shooting = pawn.skills.GetSkill(SkillDefOf.Shooting);
            if (shooting != null)
            {
                origShootingLevel = shooting.levelInt;
                origShootingXp = shooting.xpSinceLastLevel;
                // Skill.Level setter clamps to [0,20], so +20 effectively maxes
                // the skill for the duration. Original value is restored on
                // PostRemoved.
                shooting.Level = 20;
            }

            SkillRecord? melee = pawn.skills.GetSkill(SkillDefOf.Melee);
            if (melee != null)
            {
                origMeleeLevel = melee.levelInt;
                origMeleeXp = melee.xpSinceLastLevel;
                melee.Level = 20;
            }
        }

        private void RestoreSkills()
        {
            if (pawn?.skills == null)
                return;

            if (origShootingLevel >= 0)
            {
                SkillRecord? shooting = pawn.skills.GetSkill(SkillDefOf.Shooting);
                if (shooting != null)
                {
                    shooting.Level = origShootingLevel;
                    shooting.xpSinceLastLevel = origShootingXp;
                }
            }

            if (origMeleeLevel >= 0)
            {
                SkillRecord? melee = pawn.skills.GetSkill(SkillDefOf.Melee);
                if (melee != null)
                {
                    melee.Level = origMeleeLevel;
                    melee.xpSinceLastLevel = origMeleeXp;
                }
            }
        }

        private void GrantGear()
        {
            if (pawn?.apparel == null || pawn.equipment == null)
                return;

            // Apparel: each piece is spawned, made Legendary, and worn with
            // dropReplacedApparel:true so the wearer's existing gear falls to
            // the ground (not destroyed). Shield belt is intentionally omitted
            // — it blocks the wearer's own ranged attacks, which would defeat
            // the charge rifle. Survivability is delivered via the hediff's
            // IncomingDamageFactor and the Tick-driven regen below.
            TryWearLegendary("Apparel_ArmorCataphract");
            TryWearLegendary("Apparel_ArmorHelmetCataphract");

            // Weapon: MakeRoomFor drops the existing primary (if any) onto the
            // ground; AddEquipment slots in the new rifle.
            ThingDef? rifleDef = DefDatabase<ThingDef>.GetNamedSilentFail("Gun_ChSuperChargeRifle");
            if (rifleDef != null)
            {
                ThingWithComps rifle = (ThingWithComps)ThingMaker.MakeThing(rifleDef);
                SetLegendary(rifle);
                pawn.equipment.MakeRoomFor(rifle);
                pawn.equipment.AddEquipment(rifle);
                spawnedGear.Add(rifle);
            }
        }

        private Apparel? TryWearLegendary(string defName)
        {
            ThingDef? def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null)
                return null;
            Apparel apparel = (Apparel)ThingMaker.MakeThing(def);
            SetLegendary(apparel);
            pawn.apparel.Wear(apparel, dropReplacedApparel: true, locked: false);
            spawnedGear.Add(apparel);
            return apparel;
        }

        private static void SetLegendary(Thing thing)
        {
            CompQuality? compQuality = thing.TryGetComp<CompQuality>();
            compQuality?.SetQuality(QualityCategory.Legendary, ArtGenerationContext.Outsider);
        }

        private void DestroySpawnedGear()
        {
            if (spawnedGear == null)
                return;

            for (int i = 0; i < spawnedGear.Count; i++)
            {
                Thing item = spawnedGear[i];
                if (item == null || item.Destroyed)
                    continue;

                // Detach from any holder first so the tracker doesn't keep a
                // stale reference. After Remove, the apparel/weapon is not held
                // by anyone — we then Destroy it directly.
                if (item is Apparel apparel && apparel.Wearer != null)
                {
                    apparel.Wearer.apparel.Remove(apparel);
                }
                else if (item is ThingWithComps weapon
                         && item.ParentHolder is Pawn_EquipmentTracker eqTracker)
                {
                    eqTracker.Remove(weapon);
                }

                if (!item.Destroyed)
                    item.Destroy(DestroyMode.Vanish);
            }

            spawnedGear.Clear();
        }
    }
}
