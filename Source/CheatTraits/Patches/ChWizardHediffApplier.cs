using RimWorld;
using Verse;

namespace CheatTraits.Patches
{
    /// <summary>
    /// Keeps the ChWizard pawn synced with the engine-side state needed to cast
    /// psycasts: a baseline Hediff_Psylink (so they meet psycast level requirements)
    /// and the ChWizard_Spellbook hediff (which carries the stat boosts and grants
    /// custom ability defs via HediffComp_GiveAbility).
    ///
    /// Mirrors ChTankHediffApplier's pattern — invoked from CheatTraitsMapComponent
    /// on the shared pawn-tick cadence, so spawn/load/dev-mode all converge on the
    /// same state within a couple of seconds without needing Harmony hooks.
    /// </summary>
    internal static class ChWizardHediffApplier
    {
        private const string SpellbookHediffDefName = "ChWizard_Spellbook";

        // The vanilla "PsychicAmplifier" HediffDef uses hediffClass=Hediff_Psylink
        // and is what the empire questline / anima tree gives. Adding it at severity
        // 1 makes the pawn psylink level 1 immediately, and vanilla progression
        // (anima, quest rewards) continues to raise its level normally afterward.
        private const string PsychicAmplifierDefName = "PsychicAmplifier";

        // Passive psyfocus regen per pawn-tick interval (120 ticks ≈ 2s real time).
        // Vanilla band-2 drain is 0.075/day = ~0.0000375 per 120-tick interval, so this
        // outpaces decay by ~130x and fills 0 → 100% in roughly 7 minutes of real time.
        // OffsetPsyfocusDirectly clamps to [0, 1] so we can call it unconditionally.
        private const float PsyfocusRegenPerInterval = 0.005f;

        private static HediffDef? cachedSpellbook;
        private static bool cachedSpellbookResolved;

        private static HediffDef? cachedAmplifier;
        private static bool cachedAmplifierResolved;

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

        private static HediffDef? Amplifier
        {
            get
            {
                if (!cachedAmplifierResolved)
                {
                    cachedAmplifier = DefDatabase<HediffDef>.GetNamedSilentFail(
                        PsychicAmplifierDefName
                    );
                    cachedAmplifierResolved = true;
                }
                return cachedAmplifier;
            }
        }

        public static void TickPawn(Pawn p)
        {
            if (!ModLister.RoyaltyInstalled)
                return;
            if (p?.story?.traits == null || p.health?.hediffSet == null)
                return;
            if (!p.RaceProps.Humanlike)
                return;

            bool hasWizard = CheatTraitsUtils.HasTrait(p, CheatTraitsNames.WizardTrait);
            HediffDef? spellbook = Spellbook;
            HediffDef? amplifier = Amplifier;

            if (hasWizard)
            {
                // Psylink FIRST. Two reasons:
                // (1) HediffComp_GiveAbility fires CompPostPostAdd exactly once when the
                //     spellbook hediff is added. For a psycast to instantiate cleanly
                //     into pawn.abilities, the pawn needs a working psylink already.
                //     Adding the spellbook before the psylink leaves us in a state
                //     where the grant misfires and never retries — the user has to
                //     remove+re-add the trait to recreate the hediff and re-fire
                //     CompPostPostAdd.
                // (2) Hediff_Psylink.PostAdd grants a free random level-1 vanilla
                //     psycast, but skips that grant if the pawn already has one of
                //     that level. Putting psylink first means the wizard gets a free
                //     vanilla psycast in addition to ChLightningBolt rather than
                //     having ChLightningBolt block the vanilla pick.
                if (amplifier != null && p.GetPsylinkLevel() <= 0)
                {
                    // Hediff_Level.PostAdd requires a non-null body part — vanilla
                    // pins the psylink to the brain (see DebugToolsPawns.GivePsylink).
                    // The 2-arg MakeHediff overload leaves Part null and triggers a
                    // "PsychicAmplifier has null Part" error inside PostAdd.
                    BodyPartRecord? brain = p.health.hediffSet.GetBrain();
                    if (brain != null)
                    {
                        Hediff psylink = HediffMaker.MakeHediff(amplifier, p, brain);
                        p.health.AddHediff(psylink);
                    }
                }

                if (spellbook != null)
                {
                    var existingSpellbook = p.health.hediffSet.GetFirstHediffOfDef(spellbook);
                    if (existingSpellbook == null)
                        p.health.AddHediff(spellbook);
                }

                // Self-healing grant. The HediffCompProperties_GiveAbility on the
                // spellbook hediff is a one-shot — it runs at hediff-add and never
                // again. If the spellbook was added at a moment where the grant
                // didn't stick (no psylink yet, ability tracker not initialised, save
                // file from before this trait existed, etc.), this catches it and
                // restores the ability on the next pawn tick. Harmless when the
                // ability is already present (GainAbility is idempotent).
                EnsureLightningBoltAbility(p);
                EnsureTeleportOtherAbility(p);

                // Passive psyfocus regeneration. Reverses the vanilla drain so the
                // wizard's psyfocus trickles up while the trait is present. The
                // entropy cap is unaffected — chain-casting is still bounded.
                if (p.psychicEntropy != null && !p.Dead)
                    p.psychicEntropy.OffsetPsyfocusDirectly(PsyfocusRegenPerInterval);
            }
            else
            {
                // Trait was removed (dev mode, character editor): drop the spellbook
                // to revoke the abilities. Leave any psylink hediff intact — the pawn
                // may have earned it through vanilla progression and we don't want to
                // strip earned content.
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
    }
}
