using RimWorld;
using Verse;

namespace CheatTraits.Patches
{
    /// <summary>
    /// Keeps the ChWizard pawn synced with the engine-side state needed to use
    /// their abilities: the ChWizard_Spellbook hediff (which carries the stat
    /// boosts and grants the four ability defs via HediffComp_GiveAbility).
    ///
    /// Mirrors ChTankHediffApplier's pattern — invoked from CheatTraitsMapComponent
    /// on the shared pawn-tick cadence, so spawn/load/dev-mode all converge on the
    /// same state within a couple of seconds without needing Harmony hooks.
    /// </summary>
    internal static class ChWizardHediffApplier
    {
        private const string SpellbookHediffDefName = "ChWizard_Spellbook";

        private static HediffDef? cachedSpellbook;
        private static bool cachedSpellbookResolved;

        private static HediffDef? Spellbook
        {
            get
            {
                if (!cachedSpellbookResolved)
                {
                    cachedSpellbook = DefDatabase<HediffDef>.GetNamedSilentFail(
                        SpellbookHediffDefName
                    );
                    cachedSpellbookResolved = true;
                }
                return cachedSpellbook;
            }
        }

        public static void TickPawn(Pawn p)
        {
            if (p?.story?.traits == null || p.health?.hediffSet == null)
                return;
            if (!p.RaceProps.Humanlike)
                return;

            bool hasWizard = CheatTraitsUtils.HasTrait(p, CheatTraitsNames.WizardTrait);
            HediffDef? spellbook = Spellbook;

            if (hasWizard)
            {
                if (spellbook != null)
                {
                    var existingSpellbook = p.health.hediffSet.GetFirstHediffOfDef(spellbook);
                    if (existingSpellbook == null)
                        p.health.AddHediff(spellbook);
                }

                // Self-healing grant. The HediffCompProperties_GiveAbility on the
                // spellbook hediff is a one-shot — it runs at hediff-add and never
                // again. If the spellbook was added at a moment where the grant
                // didn't stick (ability tracker not initialised, save file from
                // before this trait existed, etc.), this catches it and restores
                // the ability on the next pawn tick. Harmless when the ability is
                // already present (GainAbility is idempotent).
                EnsureLightningBoltAbility(p);
                EnsureTeleportOtherAbility(p);
                EnsureMassBerserkAbility(p);
                EnsureSuperSoldierAbility(p);
            }
            else
            {
                // Trait was removed (dev mode, character editor): drop the spellbook
                // to revoke the abilities.
                if (spellbook != null)
                {
                    var existingSpellbook = p.health.hediffSet.GetFirstHediffOfDef(spellbook);
                    if (existingSpellbook != null)
                        p.health.RemoveHediff(existingSpellbook);
                }
            }
        }

        private static AbilityDef? cachedLightningBoltDef;
        private static bool cachedLightningBoltDefResolved;

        private static AbilityDef? LightningBoltDef
        {
            get
            {
                if (!cachedLightningBoltDefResolved)
                {
                    cachedLightningBoltDef = DefDatabase<AbilityDef>.GetNamedSilentFail(
                        "ChLightningBolt"
                    );
                    cachedLightningBoltDefResolved = true;
                }
                return cachedLightningBoltDef;
            }
        }

        private static void EnsureLightningBoltAbility(Pawn p)
        {
            if (p.abilities == null)
                return;

            AbilityDef? def = LightningBoltDef;
            if (def == null)
                return;

            if (p.abilities.GetAbility(def) == null)
                p.abilities.GainAbility(def);
        }

        private static AbilityDef? cachedTeleportOtherDef;
        private static bool cachedTeleportOtherDefResolved;

        private static AbilityDef? TeleportOtherDef
        {
            get
            {
                if (!cachedTeleportOtherDefResolved)
                {
                    cachedTeleportOtherDef = DefDatabase<AbilityDef>.GetNamedSilentFail(
                        "ChTeleportOther"
                    );
                    cachedTeleportOtherDefResolved = true;
                }
                return cachedTeleportOtherDef;
            }
        }

        private static void EnsureTeleportOtherAbility(Pawn p)
        {
            if (p.abilities == null)
                return;

            AbilityDef? def = TeleportOtherDef;
            if (def == null)
                return;

            if (p.abilities.GetAbility(def) == null)
                p.abilities.GainAbility(def);
        }

        private static AbilityDef? cachedMassBerserkDef;
        private static bool cachedMassBerserkDefResolved;

        private static AbilityDef? MassBerserkDef
        {
            get
            {
                if (!cachedMassBerserkDefResolved)
                {
                    cachedMassBerserkDef = DefDatabase<AbilityDef>.GetNamedSilentFail(
                        "ChMassBerserk"
                    );
                    cachedMassBerserkDefResolved = true;
                }
                return cachedMassBerserkDef;
            }
        }

        private static void EnsureMassBerserkAbility(Pawn p)
        {
            if (p.abilities == null)
                return;

            AbilityDef? def = MassBerserkDef;
            if (def == null)
                return;

            if (p.abilities.GetAbility(def) == null)
                p.abilities.GainAbility(def);
        }

        private static AbilityDef? cachedSuperSoldierDef;
        private static bool cachedSuperSoldierDefResolved;

        private static AbilityDef? SuperSoldierDef
        {
            get
            {
                if (!cachedSuperSoldierDefResolved)
                {
                    cachedSuperSoldierDef = DefDatabase<AbilityDef>.GetNamedSilentFail(
                        "ChSuperSoldier"
                    );
                    cachedSuperSoldierDefResolved = true;
                }
                return cachedSuperSoldierDef;
            }
        }

        private static void EnsureSuperSoldierAbility(Pawn p)
        {
            if (p.abilities == null)
                return;

            AbilityDef? def = SuperSoldierDef;
            if (def == null)
                return;

            if (p.abilities.GetAbility(def) == null)
                p.abilities.GainAbility(def);
        }
    }
}
