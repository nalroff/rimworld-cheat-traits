# CheatTraits — Multi-Session Implementation Plan

This file tracks the abilities/buildings expansion across multiple Claude Code sessions. The user wants each chunk implemented in its own session to keep context focused.

## How to use this file

For any new "implement the next chunk" session:

1. Read this file. Find the first chunk marked `[ ]`.
2. Read the matching user-facing spec in `README.md` (the spec is authoritative on numbers, names, and behavior).
3. Implement the chunk. Stay within scope — do not start a later chunk.
4. Update the status here from `[ ]` to `[x] YYYY-MM-DD`. Add a "Notes" line if anything deviated from spec.
5. Hand off to the user for manual testing. The user verifies in-game before the next chunk begins.

Do **not** bundle chunks. If a chunk feels small, finish it and stop — the user will choose what comes next.

## Existing patterns to follow

When implementing, mirror the conventions already in the codebase:

- **AbilityDefs:** `Defs/AbilityDefs/ChAbilities.xml`. Use `verbClass=Verb_CastAbility` (NOT psycast — this mod intentionally avoids Royalty deps). Cooldowns via `cooldownTicksRange`; multi-charge via `charges` + `cooldownPerCharge=true`.
- **Ability effect logic:** `Source/CheatTraits/Comps/CompAbilityEffect_Ch*.cs`. Each ability has a paired `CompProperties_AbilityCh<X>` and `CompAbilityEffect_Ch<X>`.
- **Granting an ability via trait — simple case:** `Source/CheatTraits/Patches/ChDiplomatAbilityApplier.cs`. Ticked from `ChTraitsMapComponent`. Use when the trait maps directly to one ability and needs no extra carrier hediff.
- **Granting abilities via trait — hediff case:** `Source/CheatTraits/Patches/ChWizardHediffApplier.cs` + the `ChWizard_Spellbook` hediff with `HediffComp_GiveAbility`. Use when the trait already needs a carrier hediff for stats or aura.
- **MapComponent tick wiring:** `Source/CheatTraits/Patches/ChTraitsMapComponent.cs` is the per-pawn tick hub. New trait→ability appliers register here.
- **Trait-gated buildings:** `Source/CheatTraits/Patches/ChBuildRestrictions.cs`, `Source/CheatTraits/Patches/Patch_BuildFloatMenu_TraitOverrides.cs`, and the `ChThingDefOfs.cs` registry. New buildings need entries in all three plus their `ThingDef` under `Defs/BuildingDefs/`.
- **Building behavior comps:** `Source/CheatTraits/Comps/CompChFloragenCore.cs` (area scan + per-tick effect), `Source/CheatTraits/Comps/CompChComfyClimateNode.cs` (room-scoped effect), `Source/CheatTraits/Comps/CompChTeslaZap.cs` (target-finding + actions).
- **Trait-name string constants:** `Source/CheatTraits/Patches/ChTraitsUtils.cs` (`CheatTraitsNames`). Add any new defName references here, not as scattered string literals.

## Chunks

Status: `[ ]` not started · `[~]` in progress · `[x] YYYY-MM-DD` done.

---

### Chunk 1 — README spec [x] 2026-05-22

`README.md` updated with the full user-facing spec for every new ability and building. This is the authoritative reference for all later chunks.

---

### Chunk 2 — Instant-effect abilities (Knockout Blow, Deadeye, Tunnel) [ ]

**Scope:** Three abilities that fire once and do their thing immediately. No persistent caster hediff, no charges, no targeting state machine beyond the initial pick.

**README sections:** Ch Boxer (Knockout Blow), Ch Tex (Deadeye), Ch Digger (Tunnel).

**Files to create:**
- Three `AbilityDef` entries in `Defs/AbilityDefs/ChAbilities.xml`: `ChKnockoutBlow`, `ChDeadeye`, `ChTunnel`.
- `Source/CheatTraits/Comps/CompProperties_AbilityChKnockoutBlow.cs` + `CompAbilityEffect_ChKnockoutBlow.cs`
- `Source/CheatTraits/Comps/CompProperties_AbilityChDeadeye.cs` + `CompAbilityEffect_ChDeadeye.cs`
- `Source/CheatTraits/Comps/CompProperties_AbilityChTunnel.cs` + `CompAbilityEffect_ChTunnel.cs`
- Three appliers under `Source/CheatTraits/Patches/`: `ChBoxerAbilityApplier.cs`, `ChTexAbilityApplier.cs`, `ChDiggerAbilityApplier.cs` (mirror `ChDiplomatAbilityApplier.cs`).

**Files to modify:**
- `Source/CheatTraits/Patches/ChTraitsMapComponent.cs` — call the three new appliers from the pawn-tick hub.
- `Source/CheatTraits/Patches/ChTraitsUtils.cs` (`CheatTraitsNames`) — add trait def-name constants if not already present.

**Implementation notes:**
- **Knockout Blow:** target `Pawn` within range `8`, exclude `RaceProps.IsMechanoid`. On apply, give the target an anesthetic-style hediff for `2500` ticks (the same downed mechanic as vanilla anesthetic — see `HediffDef.Anesthetic` or apply a small Hediff with `isBad=true` and high severity to force-down). Consider just calling `target.health.AddHediff(HediffDefOf.Anesthetic)` and removing it on a delayed schedule, or define a custom `ChKnockoutHediff` that downs the pawn for the duration. Custom hediff is cleaner.
- **Deadeye:** no LOS, no range; target is any `Pawn` on the map. On cast, build a `DamageInfo` with `DamageDefOf.Bullet`, `amount=150`, `armorPenetration=2.0f`, `instigator=caster` and call `target.TakeDamage(dInfo)`. Add a brief visual: see how `ChLightningBolt` does its strike for an effect spawn pattern.
- **Tunnel:** target a cell within `12` tiles. Compute a 3-wide line: take the vector from caster to target, derive a perpendicular, iterate cells from caster to target offsetting by `-1, 0, +1` perpendicular. For each cell, if `cell.GetEdifice(map)?.def.mineable == true` OR a natural rock filth/mountain cell, call `cell.GetEdifice(map).DestroyMined(caster)` or directly resolve the yield via `MineUtility.PickAResource(...)` and spawn at `2.5x`. Reference vanilla `Mineable.DestroyMined` for the yield path and apply the `2.5x` multiplier consistently with the existing `MiningYield` trait passive (the existing patch in `ChTraitsGetStatValuePatch.cs` won't apply since this isn't going through `Mineable.TrySpawnYield` — the comp needs to multiply explicitly).

**Acceptance criteria:**
- Trait-tagged pawns gain the ability on spawn / on trait grant; lose it when the trait is removed.
- Each ability respects its range and cooldown.
- Cast verbs do not require Royalty (existing pattern — Verb_CastAbility, no psycast machinery).

**Manual test steps for the user:**
- Dev-spawn Ch Boxer, target a raider with Knockout Blow → raider is downed.
- Dev-spawn Ch Tex, fire Deadeye at a hostile across the map (no LOS) → target takes ~150 damage instantly.
- Dev-spawn Ch Digger inside a mountain. Tunnel toward the outside → 3-wide passage opens, chunks/ore drop at the doubled yield.

---

### Chunk 3 — Hediff-applying abilities (Iron Wall, Miracle Heal) [ ]

**Scope:** Two abilities that apply effects to a target pawn (caster for Iron Wall, an ally for Miracle Heal). Iron Wall also requires a taunt patch on hostile target selection.

**README sections:** Ch Tank (Iron Wall), Ch Doc (Miracle Heal).

**Files to create:**
- Two `AbilityDef` entries in `Defs/AbilityDefs/ChAbilities.xml`: `ChIronWall`, `ChMiracleHeal`.
- `Source/CheatTraits/Comps/CompProperties_AbilityChIronWall.cs` + `CompAbilityEffect_ChIronWall.cs`
- `Source/CheatTraits/Comps/CompProperties_AbilityChMiracleHeal.cs` + `CompAbilityEffect_ChMiracleHeal.cs`
- `Defs/Hediffs/ChIronWall.xml` (or extend `ChHediffs.xml`) — the `ChIronWallBuff` hediff carrying the `IncomingDamageFactor x0.10`, stun immunity, and a marker for the taunt patch to detect.
- `Source/CheatTraits/Patches/Patch_AttackTargetFinder_IronWallTaunt.cs` — Harmony patch (postfix or prefix) on `AttackTargetFinder.BestAttackTarget` (or whichever method hostile pawns use to pick targets) that, for any hostile considering targets within `45` tiles of an Iron-Wall-buffed pawn, returns the buffed pawn instead.
- Two appliers: `ChTankAbilityApplier.cs`, `ChDocAbilityApplier.cs`.

**Files to modify:**
- `Source/CheatTraits/Patches/ChTraitsMapComponent.cs` — wire the new appliers.
- `Source/CheatTraits/Patches/ChTraitsUtils.cs` — add constants if needed.

**Implementation notes:**
- **Iron Wall:** self-cast (`canTargetSelf=true`, all other target flags false). On cast, apply `ChIronWallBuff` hediff to caster for `1500` ticks. Hediff has stages with `statOffsets/statFactors` for `IncomingDamageFactor x0.10` and stun resistance. Use `HediffComp_Disappears` for the duration. The taunt patch checks `if (any pawn within 45 tiles has ChIronWallBuff) return that pawn` before falling through to vanilla target selection. Be careful to only redirect hostiles, not friendly fire scenarios.
- **Miracle Heal:** target any `Pawn` within `45` tiles, no LOS. On cast:
  1. Find the worst non-immune disease/infection: iterate `target.health.hediffSet.hediffs`, prefer those with `def.makesSickThought` or `def.tendable` and high severity → remove via `target.health.RemoveHediff(...)`.
  2. Close non-permanent injuries: iterate `Hediff_Injury` instances where `IsPermanent() == false` → `injury.Severity = 0` then `target.health.RemoveHediff(injury)`.
  3. Restore one missing/destroyed body part: iterate `Hediff_MissingPart` instances, pick the worst non-vital (skip Brain, Heart, etc.) → remove the missing-part hediff and any prosthetic that occupied that part should drop (consult vanilla `Recipe_RemoveImplant` for the drop path if needed).
- Suggest a sound/effect on cast for both — pull from the same library as existing abilities.

**Acceptance criteria:**
- Iron Wall applies a visible hediff with the right stage effects; hostile pawns inside 45 tiles audibly switch targets onto the Tank.
- Miracle Heal cures, closes injuries, and restores a body part in one cast; if the target has none of those, the cast still consumes the cooldown (player feedback: a message saying nothing to heal would be nice but not required).
- Cooldowns match spec.

**Manual test steps for the user:**
- Dev-spawn Ch Tank with raiders nearby. Cast Iron Wall → raiders pile onto the Tank, damage taken visibly reduced.
- Dev-spawn Ch Doc, target a colonist missing a leg with plague active → leg restored, plague cleared, injuries closed.

---

### Chunk 4 — Reforge (Artificer) [ ]

**Scope:** A 3-charge ability that targets any building or item with a `QualityCategory` and rerolls its quality.

**README section:** Ch Artificer (Reforge).

**Files to create:**
- `AbilityDef` `ChReforge` in `ChAbilities.xml` with `charges=3`, `cooldownPerCharge=true`, `cooldownTicksRange=12500`.
- `Source/CheatTraits/Comps/CompProperties_AbilityChReforge.cs` + `CompAbilityEffect_ChReforge.cs`
- `Source/CheatTraits/Patches/ChArtificerAbilityApplier.cs`

**Files to modify:**
- `Source/CheatTraits/Patches/ChTraitsMapComponent.cs` — wire the applier.

**Implementation notes:**
- Target params: `canTargetBuildings=true`, `canTargetItems=true`, `canTargetPawns=false`, `canTargetLocations=false`. Range can be modest (e.g. `15`) since this is fiddly precision targeting.
- On cast, resolve the targeted `Thing`. Look for a `CompQuality` (`thing.TryGetComp<CompQuality>()`); if null, the cast is invalid (validator should already reject it via `Verb_CastAbility.ValidateTarget` — implement validation via the comp's `Valid` override).
- Roll new quality using the same `60/30/10` weights as `ChArtificerQuality.cs`. **Always replace** the current quality with the new roll, even if the new quality is lower — this matches the README spec and is the user's explicit call. Do not add a downgrade-guard.
- For built buildings, `CompQuality.SetQuality(newQ, ArtGenerationContext.Colony)` then trigger any visual refresh (`thing.DirtyMapMesh(map)` if needed for art/sculpture).
- For equipped items, the same `SetQuality` works on the inner `Thing` — `Pawn.equipment.Primary` / `apparel.WornApparel` direct references.

**Acceptance criteria:**
- Reforge ability appears with `3` charge pips on the toolbar.
- Targeting a non-quality-bearing thing is rejected with a clear cursor message.
- Successful cast visibly changes the item's quality label in the inspect window.
- Each charge ticks down independently.

**Manual test steps for the user:**
- Dev-spawn Ch Artificer. Build a chair (Poor quality forced via dev). Cast Reforge on it → quality changes to one of Excellent/MW/Legendary.
- Cast 3 times in succession to exhaust charges; confirm fourth cast is locked behind cooldown.

---

### Chunk 5 — Blink (Zippy) [ ]

**Scope:** Short-range teleport for the Zippy pawn.

**README section:** Ch Zippy (Blink).

**Files to create:**
- `AbilityDef` `ChBlink` in `ChAbilities.xml`: `cooldownTicksRange=2500`, range `15`, `requireLineOfSight=false`, target params `canTargetLocations=true`, `canTargetSelf=false`.
- `Source/CheatTraits/Comps/CompProperties_AbilityChBlink.cs` + `CompAbilityEffect_ChBlink.cs`
- `Source/CheatTraits/Patches/ChZippyAbilityApplier.cs`

**Files to modify:**
- `Source/CheatTraits/Patches/ChTraitsMapComponent.cs` — wire the applier.

**Implementation notes:**
- On cast, validate target cell is standable for the caster: `target.Cell.Standable(map) && !target.Cell.GetThingList(map).Any(t => t is Pawn)`.
- Move pawn: `caster.Position = target.Cell; caster.Notify_Teleported(endCurrentJob: true, resetTweenedPos: true)`.
- Optional polish: spawn a small flecks/poof effect at both origin and destination (`FleckMaker.ThrowDustPuff` or similar).
- No persistent state, no hediff.

**Acceptance criteria:**
- Cast moves the pawn instantly to the chosen cell.
- Invalid cells (occupied, impassable) are rejected at targeting time.
- Low cooldown (~`2500` ticks) is honored.

**Manual test steps for the user:**
- Dev-spawn Ch Zippy. Cast Blink across a wall → pawn appears on the other side.
- Try to target an impassable cell → cursor rejects.

---

### Chunk 6 — Vitae Pillar (Ascendant) [ ]

**Scope:** Trait-gated building that affects every pawn in the same room. Includes a Biotech-conditional patch for birth complications.

**README section:** Vitae Pillar (under Trait-Gated Buildings).

**Files to create:**
- `Defs/BuildingDefs/ChVitaePillar.xml` — `ThingDef` for the building. Costs `25 Steel`, work `400`, no power, designation category mirrors the existing trait-gated buildings.
- `Defs/Hediffs/ChVitaeBlessing.xml` (or append to `ChHediffs.xml`) — the per-pawn buff hediff with `InjuryHealingFactor x4`, `ImmunityGainSpeed x2`. Lingers like the Ascendant aura — pawn is reapplied while in-room, hediff has a short `HediffComp_Disappears` so it falls off when they leave.
- `Source/CheatTraits/Comps/CompProperties_ChVitaePillar.cs` + `CompChVitaePillar.cs` — per-tick (every `250` ticks) scan: for each pawn in `this.parent.GetRoom()`, apply blessing hediff; tick down severity of any `Hediff_Addiction` on those pawns.
- `Source/CheatTraits/Patches/Patch_PregnancyComplications_VitaePillar.cs` — Biotech-conditional Harmony patch. Wrap `[HarmonyPatch]` registration in a `ModsConfig.BiotechActive` check; the patch suppresses pregnancy/labor complication rolls when the carrying pawn is inside a room with a Vitae Pillar.

**Files to modify:**
- `Source/CheatTraits/Patches/ChThingDefOfs.cs` — add the pillar's defName.
- `Source/CheatTraits/Patches/ChBuildRestrictions.cs` — gate to Ch Ascendant.
- `Source/CheatTraits/Patches/Patch_BuildFloatMenu_TraitOverrides.cs` — allow Ascendant to force-build.
- `README.md` "Defs Layout" mentions of the new BuildingDef are already correct (no list change needed).

**Implementation notes:**
- Mirror `CompChFloragenCore.cs` for the per-tick scan cadence, but scope to `parent.GetRoom()` instead of a tile-radius scan. Filter out `OutdoorsRoom` / `PsychologicallyOutdoors` rooms so a pillar dropped outside doesn't blanket the map.
- "Multiple pillars in one room do not stack" → before applying the blessing hediff, check if the pawn already has it from this tick. Use the existing single-hediff-instance approach (the hediff is unique on a pawn; reapplying just refreshes severity).
- Addiction decay: iterate `pawn.health.hediffSet.hediffs.OfType<Hediff_Addiction>()` → severity reduction per scan such that ~5000 ticks (20 scans at 250 each) brings any normal addiction to zero. Tune to taste.
- Biotech patch target: `PregnancyUtility.GetBirthComplicationsChance` (or the actual labor-roll method — confirm via the rimworld-analysis skill before patching). The simplest patch returns 0 chance when the mother is in a Vitae Pillar room.
- Heads-up to the implementer: **invoke the `rimworld-analysis` skill before touching the Biotech birth code** — that surface changes between patches and you want to be sure of the exact method name and signature.

**Acceptance criteria:**
- Building shows up in the Architect menu only when the colony has a Ch Ascendant.
- Placed inside a room, pawns in the room gain the blessing hediff within one scan tick.
- Addictions visibly tick down over a few in-game minutes.
- (Biotech) Pregnant pawn in a Vitae Pillar room never rolls a complication.

**Manual test steps for the user:**
- Spawn Ch Ascendant, confirm Architect entry appears. Build pillar in a hospital.
- Dev-give a colonist a deep wound and an alcohol addiction. Watch healing speed and addiction severity in the inspect window while they're in the room.
- (Biotech) Force-trigger labor on a pawn in vs. out of the room; confirm complications are absent in-room.

---

### Chunk 7 — Call of the Wild (Beastmaster) [ ]

**Scope:** Map-wide ability that puts every non-player animal into a custom mental state that drives them to attack the nearest hostile-to-player pawn.

**README section:** Ch Beastmaster (Active ability: Call of the Wild).

**Files to create:**
- `AbilityDef` `ChCallOfTheWild` in `ChAbilities.xml`: `cooldownTicksRange=120000`, range `9999`, `canTargetSelf=true` (self-cast, no other target needed), warmupTime modest.
- `Defs/MentalStates/ChWildHunt.xml` — custom `MentalStateDef` referencing a custom worker class.
- `Source/CheatTraits/MentalStates/MentalStateWorker_ChWildHunt.cs` — entry-point worker (mirror vanilla `MentalStateWorker_Manhunter` shape).
- `Source/CheatTraits/MentalStates/MentalState_ChWildHunt.cs` — the runtime state. Override target selection so the animal pathfinds toward and attacks the nearest pawn that is `HostileTo(Faction.OfPlayer) && Faction != this.pawn.Faction`. Falls back to idle if no eligible target.
- `Source/CheatTraits/Comps/CompProperties_AbilityChCallOfTheWild.cs` + `CompAbilityEffect_ChCallOfTheWild.cs`
- `Source/CheatTraits/Patches/ChBeastmasterAbilityApplier.cs`

**Files to modify:**
- `Source/CheatTraits/Patches/ChTraitsMapComponent.cs` — wire the applier.

**Implementation notes:**
- On cast, iterate `caster.Map.mapPawns.AllPawns`. Filter to `p.RaceProps.Animal && p.Faction != Faction.OfPlayer && !p.Dead && !p.Downed`. For each, call `p.mindState.mentalStateHandler.TryStartMentalState(ChWildHuntDef, reason: null, forceWake: true, causedByMood: false, transitionSilently: true)`. Duration `5000` ticks (set on the MentalStateDef).
- For target selection inside the state: each `JobGiver` tick (the state's job giver), pick `caster.Map.mapPawns.AllPawns.Where(t => t.HostileTo(Faction.OfPlayer) && t.Faction != self.Faction).MinBy(t => self.Position.DistanceToSquared(t.Position))`. Issue a `JobMaker.MakeJob(JobDefOf.AttackMelee, target)` (or `AttackStatic` for ranged animals).
- Insectoids: `Faction.OfInsects` is not the player's faction, so they're already swept in. Good.
- Manhunter packs: already in a manhunter mental state. The new state should *replace* it. `TryStartMentalState` typically does that, but verify — the vanilla manhunter logic may resist override. **Use the rimworld-analysis skill** to check how mental-state replacement works.
- Cooldown of `120000` ticks = ~2 in-game days.
- Worth a polish pass: a sound/effect at cast time, plus a brief letter so the player notices what just happened ("The wilds have answered.").

**Acceptance criteria:**
- Cast affects every non-player animal on the map.
- Affected animals attack the nearest hostile-to-player pawn, not colonists.
- Bugs included.
- Tamed pets unaffected.
- 2 in-game day cooldown enforced.

**Manual test steps for the user:**
- Trigger a raid. Trigger an infestation. Dev-spawn a few thrumbos. Cast Call of the Wild. Expectation: insectoids, thrumbos, and any wild megasloths converge on the raiders.
- Confirm a tamed husky stays on its current job.

---

### Chunk 8 — Alchemy Cauldron (Alchemist) [ ]

**Scope:** Trait-gated building with two bills. Adds a custom drug-class ingestible `Trail Tonic`. Most ambitious chunk by surface area — building + ThingDef + drug behavior + 2 RecipeDefs + a hediff + a trait-gate on recipes.

**README section:** Alchemy Cauldron (under Trait-Gated Buildings).

**Files to create:**
- `Defs/BuildingDefs/ChAlchemyCauldron.xml` — `ThingDef` for the cauldron. Costs `50 Steel + 20 Wood`, work `600`, no power. Include a `compIngredients` and the standard bill workbench machinery (mirror vanilla `ButcherTable` or `DrugLab` for the bill config).
- `Defs/ThingDefs/ChTrailTonic.xml` — `ThingDef` for the tonic item. Parent off a drug base (e.g. `MakeableDrugBase` or `OrganicProductBase` with drug comps). Set `drugCategory` to `Hard` so the default policy excludes it. `comps`: `CompProperties_Drug` with `addictiveness=0`, `overdoseSeverityOffset=0`, `listOrder` and an ingest job. `ingestEffect=Drink`, `ingestSound=Ingest_Drink`. The hediff giver on the comp applies `ChTrailTonicBuff`.
- `Defs/Hediffs/ChTrailTonicBuff.xml` — hediff for the drink effect: `HungerRateFactor x0.05`, `RestFallRateFactor x0.5`, `+5%` Move Speed. Use `HediffComp_Disappears` with duration `180000` ticks.
- `Defs/RecipeDefs/ChAlchemyRecipes.xml` — two recipes: `BrewChTrailTonic` and `SynthesizeChNeutroamine`. Hook to the cauldron via `recipeUsers` on the recipe OR `recipes` on the cauldron ThingDef (either pattern works, pick one).
- `Source/CheatTraits/Patches/ChAlchemistRecipeGate.cs` — Harmony patch that gates these two recipes to Ch Alchemist pawns only. Hook point candidates: `RecipeDef.PawnSatisfiesSkillRequirements` (returns false for non-Alchemist on these recipes) or `WorkGiver_DoBill.JobOnThing`. Use a `DefModExtension` (e.g. `ChRequiredTrait`) on the recipes so the patch can be generic and future-proof.

**Files to modify:**
- `Source/CheatTraits/Patches/ChThingDefOfs.cs` — register the cauldron and the trail tonic defs.
- `Source/CheatTraits/Patches/ChBuildRestrictions.cs` — gate cauldron build to Ch Alchemist.
- `Source/CheatTraits/Patches/Patch_BuildFloatMenu_TraitOverrides.cs` — allow Alchemist force-build.
- `Source/CheatTraits/Patches/ChTraitsUtils.cs` (`CheatTraitsNames`) — add the trait constant.

**Implementation notes:**
- **Witch's brew ingredients (per spec, no human-derived materials):**
  - Trail Tonic: `3 Herbal Medicine + 2 leather (any non-human) + 5 raw plants → 1 Trail Tonic`. For "leather (any)", use a `ThingFilter` referencing the `Leathery` category and explicitly exclude `Leather_Human`. Raw plants: `PlantMatter` category covers raw veggies, healroot, etc.
  - Neutroamine: `5 Herbal Medicine + 2 leather (any non-human) + 1 Chemfuel → 5 Neutroamine`. Same exclusions.
  - Cooldowns on `workAmount` should be roughly: Trail Tonic ~`8000` work, Neutroamine ~`6000` work. Tune to feel.
- **Drug category vs auto-consume:** `drugCategory: Hard` plus the default drug policy (`PolicyDefOf.DefaultDrug` excludes Hard drugs unless scheduled) gives the "explicit consumption only" behavior. Confirm at test time by giving a colonist trail tonic in inventory and watching that they don't drink it spontaneously.
- **Recipe trait gate:** the cleanest approach is a small `DefModExtension`:
  ```
  ChRequiredTrait { traitDefName = "ChAlchemist" }
  ```
  attached to each recipe def, plus a Harmony postfix on `RecipeDef.PawnSatisfiesSkillRequirements` that also checks the ext and returns false for non-matching pawns.
- **Building bill UI:** vanilla `Building_WorkTable` already gives you the bill stack and the "Add Bill" menu — the cauldron should derive from it. Workbench art can reuse a vanilla texture initially; placeholder is fine for first pass.
- This chunk is bigger than the others. If it splits naturally, the rough split is:
  1. Cauldron ThingDef + trait-gated build + a no-op bill list (sanity-check the building exists in-game).
  2. Trail Tonic item + hediff + ingest behavior (sanity-check drinking works from inventory).
  3. Two recipes + trait gate (sanity-check Alchemist can craft and non-Alchemist cannot).
  But aim for a single session if possible.

**Acceptance criteria:**
- Architect entry appears only with Ch Alchemist in the colony.
- Bills tab on the cauldron lists both recipes.
- Non-Alchemist pawns cannot be assigned and will skip bills on it.
- Trail Tonic items in stockpile are *not* auto-consumed under the default drug policy.
- Right-click → Drink Trail Tonic works and applies the hediff for 3 in-game days.
- Neutroamine recipe produces 5 neutroamine of standard quality.

**Manual test steps for the user:**
- Spawn Ch Alchemist; confirm cauldron unlocks. Build it.
- Queue both bills. Confirm Alchemist works the bench. Switch Alchemist off-duty; queue a bill again; confirm a non-Alchemist Cook does *not* perform the bill.
- Move Trail Tonic into a pawn's inventory; observe over a couple of in-game days that they don't drink it on their own.
- Manually drink it on the pawn → hediff applied, hunger flatlines for 3 days.

---

## Deferred / not in this plan

- Any further Alchemy Cauldron recipes beyond Trail Tonic and Neutroamine (user wants to keep scope tight).
- Any rebalancing of existing traits.
- Multi-target or upgrade variants of the new abilities.

When the user requests additions, add a new chunk at the bottom rather than amending closed chunks.
