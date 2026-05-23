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

### Chunk 2 — Instant-effect abilities (Knockout Blow, Deadeye, Tunnel) [x] 2026-05-22

Notes:
- Knockout Blow was reworked into Flying Punch after first test (user design call — the original "instant down" felt wrong). New behavior: jump-pack-style leap (Verse `PawnFlyer`) to land adjacent to any pawn/animal/mech/building target, then strike via the Boxer's normal melee verb. Implementation: custom `Verb_CastAbilityChFlyingPunch : Verb_CastAbility` that resolves a walkable cell adjacent to the picked thing's footprint (using `GenAdj.CellsAdjacent8Way`) and calls `JumpUtility.DoJump` with the original target stored in the flyer; `CompAbilityEffect_ChFlyingPunch : ICompAbilityEffectOnJumpCompleted` fires `caster.meleeVerbs.TryMeleeAttack(target)` on landing. The trait's existing `MeleeDamageFactor x10` unarmed bonus makes a single hit "devastating" without any custom damage code. The original `ChKnockoutBlow` hediff was deleted.
- Deadeye fires `DamageInfo(Bullet, 150, AP=2.0)` through `TakeDamage`; passes the caster's primary weapon def as the `weapon` field so the kill log reads as a shot. Visual is a `FleckDefOf.ShotFlash`; no extra sound (TakeDamage handles the impact audio).
- Deadeye design change after first test: `requireLineOfSight=true` (was `false` per original spec). Reasoning: shooting through mountains felt wrong. Range stays at `9999` so cross-map shots are still possible when the line is clear. README updated to match.
- Bugfix during testing: a lethal Deadeye despawns the target before the fleck call, nulling Position/Map. Position and Map are now captured into locals before `TakeDamage`.
- Tunnel uses `GenSight.PointsOnLineOfSight` for the central line and widens by a rounded perpendicular IntVec3 offset (-1, 0, +1). Each cell calls `Mineable.Notify_TookMiningDamage(HitPoints, caster)` so vanilla `TrySpawnYield` reads the caster's `MiningYield` stat and drops at the trait's 2.5x naturally — no manual multiplier.
- Icon paths used: `UI/Abilities/Stun` (Knockout), `Things/Item/Equipment/WeaponRanged/Revolver` (Deadeye), `UI/Abilities/Chunkskip` (Tunnel). All vanilla, no custom textures needed.

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

### Chunk 3 — Hediff-applying abilities (Iron Wall, Miracle Heal) [x] 2026-05-22

Notes:
- **Iron Wall taunt** was implemented as a Harmony postfix on `Verse.AI.AttackTargetFinder.BestAttackTarget`. Confirmed via decomp + analysis that this is the single chokepoint for melee + ranged + manhunter + mech + berserk + Anomaly target picks (`BestShootTargetFromCurrentPosition` forwards through it). Postfix checks: skip when `__result == null` (preserve "no target found"), look up any spawned pawn on the searcher's map carrying the `ChTank_IronWall` hediff, redirect only if within 45 tiles, hostile to the searcher, and not the searcher itself. New file: `Patches/Patch_AttackTargetFinder_IronWallTaunt.cs`.
- **Iron Wall stun immunity** was implemented as a Harmony prefix on `RimWorld.StunHandler.StunFor` (returns false to skip the original when the owning pawn has the hediff). Cleaner than juggling stat factors. New file: `Patches/Patch_StunHandler_IronWall.cs`.
- **Iron Wall hediff** lives in `ChHediffs.xml`: `IncomingDamageFactor 0.10`, `HediffComp_Disappears` 1500 ticks, `showRemainingTime=true`. No `keepOnBodyPartRestoration` issues — it's torso-applied so RestorePart wouldn't sweep it from a Miracle Heal target anyway.
- **Iron Wall ability** is `canTargetSelf=true, range=0, targetRequired=false` — instant self-cast with no designator prompt. `targetRequired=false` makes `Command_Ability.ProcessInput` skip the targeter and immediately queue the cast on the caster with `LocalTargetInfo.Invalid`. `CompAbilityEffect_ChIronWall` ignores the target arg and applies the hediff to `parent.pawn`. First pass used the default `targetRequired=true` and brought up a designator that wouldn't accept any click — fixed after user report. If a previous Iron Wall is active, the comp removes the old hediff before adding the new one so the duration refreshes cleanly.
- **Miracle Heal** uses `Pawn_HealthTracker.RestorePart` for the body-part restoration — confirmed via decomp that this single call handles missing-part removal, all attached injuries on that part, and recursive child-part cleanup. We pick the target part via `hediffSet.GetMissingPartsCommonAncestors()` so a missing arm doesn't compete with its also-missing hand. Vital-part filter is by defName (Brain, Heart, Liver, Stomach) since `BodyPartDefOf` doesn't expose these constants in 1.6.
- **Miracle Heal disease cure** filters `hediffSet.hediffs` for non-Injury/non-MissingPart/non-AddedPart hediffs with `makesSickThought || tendable`, sorted by makesSickThought-first then severity, takes the first. Removes via `RemoveHediff`.
- **Miracle Heal injury close** iterates `Hediff_Injury` instances filtering out `IsPermanent()`, removes each via `RemoveHediff`. Spec said "set severity to 0 then remove" — removing directly is equivalent and avoids an unnecessary tick of state-change recompute.
- **Miracle Heal "nothing to heal" feedback**: if all three stages no-op, a neutral message fires (`"<pawn> had nothing to heal."`). Cooldown still consumed per spec.
- Icon paths used: `UI/Abilities/BulletShield` (Iron Wall, Royalty) and `UI/Abilities/UnnaturalHealing` (Miracle Heal, Anomaly). First-pass picks (`UI/Abilities/Adrenaline`, `UI/Abilities/PsychicHeartfreeze`) didn't exist in any DLC — corrected after the user reported the texture-load errors. The mod already uses other Royalty psycast icons (`Flashstorm`, `BerserkPulse`, etc.), so the DLC-icon dependency is consistent with existing assumptions.
- Two new analysis notes written for future reference: `features/hostile-target-selection.md` and `features/restore-body-part.md`.



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

### Chunk 4 — Reforge (Artificer) [x] 2026-05-22

Notes:
- Quality roll reuses `ArtificerQualityUtil.GetArtificerQualityLevel()` (already 60/30/10 Excellent/MW/Legendary). Per spec, replacement is unconditional — no downgrade guard.
- Validation lives entirely in `CompAbilityEffect_ChReforge.Valid`: rejects non-Thing targets and Things without `CompQuality` with cursor messages. `ExtraLabelMouseAttachment` adds an inline "No quality" tag for non-quality targets so the rejection is visible before clicking.
- `CompQuality.SetQuality(q, ArtGenerationContext.Colony)` does the actual rework: it sets the int, re-rolls art via `CompArt.InitializeArt(source)` when present, and triggers `Thing.PostQualitySet` (e.g. books regenerate). Verified against [CompQuality.cs](F:/Development/rimworld-decomp/Assembly-CSharp/RimWorld/CompQuality.cs). After the call, the comp also runs `thing.DirtyMapMesh(map)` so quality-tinted graphics (sculptures, etc.) repaint on the same tick.
- Icon `UI/Abilities/TransmuteSteel` (Anomaly) — thematically right for "rework an item" and consistent with the mod's existing pattern of using DLC psycast icons (`Flashstorm`, `UnnaturalHealing`, `BulletShield`).
- Default `targetRequired=true` is kept — Reforge needs an explicit pick. Range `15` and `requireLineOfSight=false`.
- **Bugfix during first test**: cursor showed the no-go symbol on every target. Cause: `TargetingParameters.mapObjectTargetsMustBeAutoAttackable` defaults to `true`, which filters non-auto-attackable buildings (chairs, art, beds, etc.) and non-auto-attackable items (weapons, apparel). Verified against [TargetingParameters.cs:192-211](F:/Development/rimworld-decomp/Assembly-CSharp/RimWorld/TargetingParameters.cs#L192). Fix: set `<mapObjectTargetsMustBeAutoAttackable>false</mapObjectTargetsMustBeAutoAttackable>` in the AbilityDef's `targetParams`. This mirrors vanilla `TargetingParameters.ForThing()`.

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

### Chunk 5 — Blink (Zippy) [x] 2026-05-23

Notes:
- Implementation followed the spec exactly. `AbilityDef ChBlink`: range 15, `requireLineOfSight=false`, `canTargetLocations=true` only, `cooldownTicksRange=2500`, `warmupTime=0.1`.
- `CompAbilityEffect_ChBlink` uses the same fleck/sound stack as `ChTeleportOther` (`PsycastSkipFlashEntry` at origin, `PsycastSkipInnerExit` + `PsycastSkipOuterRingExit` at landing; `Psycast_Skip_Entry` and `Psycast_Skip_Exit` sounds). Move via `caster.Position = landing; caster.Notify_Teleported(endCurrentJob: true, resetTweenedPos: true)`.
- `Valid()` rejects: out-of-bounds, non-standable, and any cell already occupied by another pawn — each with a `RejectInput` message so the cursor explains the rejection.
- Icon `UI/Abilities/Skip` (Royalty psycast icon). Consistent with the mod's existing DLC-icon usage (`Flashstorm`, `UnnaturalHealing`, `BulletShield`, `Beckon`, etc.).
- Applier `ChZippyAbilityApplier` mirrors `ChDiggerAbilityApplier` exactly — humanlike-only, gain/remove per pawn tick. Wired into `CheatTraitsMapComponent.MapComponentTick` alongside the other ability appliers.

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

### Chunk 6 — Eureka Forge (Ascendant) [x] 2026-05-23

Notes:
- `ChEurekaForge` ThingDef lives in `Defs/Things/ChEurekaForge.xml` (matching the codebase's existing `Defs/Things/` layout — the plan's `Defs/BuildingDefs/` was the spec path, not what the repo uses). Uses vanilla `Things/Building/Production/FueledSmithy` art as a placeholder; rotatable, 3x1, no power required. The `<recipes>` list statically attaches all `ChEureka_*` recipes (Bioferrite/ArchiteCapsule entries wrapped with `MayRequire`).
- `ChEureka_*` recipes live in `Defs/RecipeDefs/ChEurekaRecipes.xml`. Each uses `<workerClass>CheatTraits.Patches.ChRecipeWorker_Eureka</workerClass>`. No `recipeUsers` — the Forge's `<recipes>` list owns them. Both the Bioferrite (Anomaly) and Archite Capsule (Biotech) recipes use the canonical `MayRequire="ludeon.rimworld.anomaly"` / `MayRequire="ludeon.rimworld.biotech"` attributes on the top-level `<RecipeDef>` element.
- `ChEurekaTracker` (in `ChEurekaSystem.cs`) keeps per-map state — `nextEurekaTick` + a list of `EurekaActive { recipeDefName, expiresAtTick }`. `IsActive` is a linear scan over the typically-≤2 actives. `nextEurekaTick` starts at `-1` so the first tick after the Ascendant arrives sets it to `now + EurekaIntervalTicks` (a quadrum-long wait before the first Eureka, matching spec). Caravan-pause behavior comes naturally: `Tick(map)` returns without advancing the schedule if `MapHasAscendant(map)` is false, so the Ascendant being off-map freezes the timer. Save scribe uses `Scribe_Deep` on the tracker and `LookMode.Deep` on the actives list.
- `ChRecipeWorker_Eureka.AvailableOnNow(Thing, BodyPartRecord)` consults `thing.Map.GetComponent<CheatTraitsMapComponent>().EurekaTracker.IsActive(recipe.defName)`. Confirmed via [RecipeWorker.cs:12](F:/Development/rimworld-decomp/Assembly-CSharp/Verse/RecipeWorker.cs#L12) that the signature is `AvailableOnNow(Thing thing, BodyPartRecord part = null)` and the base returns `true` — keeps the recipe inert by default outside an active Eureka with no Harmony patch required.
- `CompChEurekaForgeInspect` renders the inspect string. Shows the active recipes with `ToStringTicksToPeriod()` for each, or "Next Eureka in Xd Yh." when none active, plus the Ascendant-absence reminder when no Ascendant is on the map.
- `ChAscendantAura.cs` was deleted entirely; `ChAscendant_InspirationAura` HediffDef removed from `ChHediffs.xml`. Trait description rewritten to match the Eureka mechanic. Trait already lacked the spec'd `<Fertility>1.0</Fertility>` statFactor (only `ResearchSpeed x10` was present), so no deletion needed there.
- Tick wiring: removed `nextAscendantTick` + `ChAscendantAuraSystem.TickMap` from `ChTraitsMapComponent`; replaced with `nextEurekaTick` gated by `ChEurekaTracker.TickGateTicks` (2500). Tracker state scribes via `Scribe_Deep.Look` in the MapComponent's `ExposeData`.
- Build clean: 0 errors, 0 warnings. README already pre-updated.



**Scope:** Full reshape of the Ch Ascendant trait. The previous Vitae Pillar / aura / Fertility-forcing design is replaced with the **Eureka Forge**: a trait-gated crafting workbench whose bills are populated dynamically by a passive `Eureka` event. The event fires every `900,000` ticks (15 in-game days, one quadrum) while a Ch Ascendant is present on the map, granting `2` random recipes from a curated pool for `180,000` ticks (3 in-game days). The trait keeps its `+10 Intellectual` and `ResearchSpeed x10` passives; loses the aura, the Fertility forcing, and the Vitae Pillar.

This chunk both **adds new content** (Forge, Eureka system, discoverable recipes) and **deletes old content** (aura code, aura hediff, Fertility statFactor). The Vitae Pillar that the original Chunk 6 spec'd was never implemented — there is no in-game state to migrate.

**Implementation strategy — research-style availability gating (do NOT patch `AllRecipes`).**

Earlier drafts of this chunk planned to dynamically inject recipes into `Building_WorkTable.AllRecipes` via Harmony. That approach is wrong. Confirmed via [features/worktable-recipes-and-bills.md](F:/Development/rimworld-analysis/features/worktable-recipes-and-bills.md):

- `Building_WorkTable` does **not** define `AllRecipes`. The only `AllRecipes` getter lives on `ThingDef` (per-def, statically cached in `allRecipesCached`).
- Mutating `__result` from a postfix permanently pollutes the def cache for the game session (defs are global, not save state).
- The bill UI ([ITab_Bills.cs:101](F:/Development/rimworld-decomp/Assembly-CSharp/RimWorld/ITab_Bills.cs#L101)) calls **two** gates per recipe: `recipe.AvailableNow` (def-wide) AND `recipe.AvailableOnNow(thing, part)` (per-instance, gets the workbench as `thing` → has `thing.Map`). This is exactly how research-gated recipes work — the recipe is permanently in `AllRecipes`, and `AvailableNow` returns `false` until `researchPrerequisite.IsFinished` (see [RecipeDef.cs:88](F:/Development/rimworld-decomp/Assembly-CSharp/Verse/RecipeDef.cs#L88)).

**The clean approach for this chunk:**
1. Statically attach every `ChEureka_*` recipe to the Forge via the Forge ThingDef's `<recipes>` list. The recipes are permanently in `AllRecipes` for the Forge.
2. Each `ChEureka_*` recipe uses a custom `<workerClass>` — `CheatTraits.Patches.ChRecipeWorker_Eureka` — that overrides `AvailableOnNow(Thing thing, BodyPartRecord part)` to consult the tracker on `thing.Map`. When the recipe isn't in the map's active set, the worker returns `false` and the recipe is hidden in the bills tab / inert for queued bills.
3. **No Harmony patch is needed for the Eureka availability mechanic at all.** Bills that were queued while a recipe was active and outlive it go inert automatically: `WorkGiver_DoBill` calls `bill.ShouldDoNow`, which checks `AvailableOnNow`. No orphan-bill walker, no `AllRecipes` cache invalidation, no `Find.CurrentMap` guesswork — the workbench instance gives us the map directly.

Trade-off worth knowing: a few non-UI consumers (`PlayerItemAccessibilityUtility`, `QuestNode_GetThingPlayerCanProduce`, `RoomRoleWorker_Kitchen`) iterate `def.AllRecipes` without checking availability. They will treat Eureka recipes as "the player can produce this" even between Eurekas. For single-colony play this is invisible; it's not worth a second gate unless a leak shows up in testing.

**README sections:** `Ch Ascendant`, `Eureka Forge` (under Trait-Gated Buildings).

**Files to create:**
- `Defs/BuildingDefs/ChEurekaForge.xml` — `ThingDef` using `thingClass="RimWorld.Building_WorkTable"`. Costs `75 Steel + 30 Wood`, work `600`, no power, designation category matches existing trait-gated buildings. **Include a `<recipes>` list naming every `ChEureka_*` recipe by defName** (the static attachment is what lets the worker-based availability gate work). Wrap individual recipe references that require DLC with `<li MayRequire="ludeon.rimworld.anomaly">ChEureka_Bioferrite</li>` etc. so the reference itself is dropped when the recipe def is absent. Add the comp from below to the def's `<comps>` so the inspect string explains the mechanic.
- `Defs/RecipeDefs/ChEurekaRecipes.xml` — All discoverable recipes. Each `RecipeDef` is prefixed `ChEureka_`, sets `<workerClass>CheatTraits.Patches.ChRecipeWorker_Eureka</workerClass>`, and uses `MayRequire="..."` on the top-level entry to opt out when its required DLC isn't loaded. **Do not set `recipeUsers`** — the Forge picks them up via its `<recipes>` list so they never leak to other workbenches. Suggested initial pool (tune ingredient/work numbers during implementation):
  - `ChEureka_Luciferium` (base) — `3 Neutroamine + 1 Gold`, `2000` work → `5 Luciferium`
  - `ChEureka_Hyperweave` (base) — `40 Cloth + 20 Synthread`, `2000` work → `40 Hyperweave`
  - `ChEureka_Components` (base) — `30 Steel`, `800` work → `5 Components`
  - `ChEureka_AdvancedComponents` (base) — `5 Components + 10 Plasteel`, `3000` work → `2 Advanced Components`
  - `ChEureka_GlitterworldMedicine` (base) — `5 Medicine + 2 Neutroamine`, `2500` work → `5 Glitterworld Medicine`
  - `ChEureka_Bioferrite` — `<RecipeDef MayRequire="ludeon.rimworld.anomaly">` — `10 Steel + 2 Chemfuel`, `1500` work → `5 Bioferrite`
  - `ChEureka_ArchiteCapsule` — `<RecipeDef MayRequire="ludeon.rimworld.biotech">` — `2 Luciferium + 5 Gold`, `5000` work → `1 Archite Capsule`
- `Source/CheatTraits/Patches/ChEurekaSystem.cs` — Core system. Houses both the tracker and the recipe worker.
  - `ChEurekaTracker : IExposable` — per-map state.
    - Fields: `int nextEurekaTick`, `List<EurekaActive> actives` where `EurekaActive` is `{ string recipeDefName; int expiresAtTick; }` (store as string for resilience if a recipe def is later renamed or its DLC unloaded).
    - Constants at top of file: `EurekaIntervalTicks = 900000`, `EurekaDurationTicks = 180000`, `EurekaRecipesPerEvent = 2`, `TickGateTicks = 2500`.
    - `bool IsActive(string recipeDefName)` — fast lookup used by the worker. Be efficient: this is called from `AvailableOnNow`, which runs per-recipe per-frame while the bills tab is open.
  - `Tick(Map map)` — called from the existing tick hub, gated to every `TickGateTicks`. Steps:
    1. Prune expired entries from `actives`.
    2. If no Ch Ascendant is currently spawned on the map (`CheatTraitsUtils.HasTrait` over `map.mapPawns.AllPawnsSpawned`), return without advancing the schedule (caravan-friendly: timer doesn't burn when the Ascendant is gone).
    3. If `Find.TickManager.TicksGame >= nextEurekaTick`, call `FireEureka(map)`. (`>=` not `==` so a return-from-caravan fires the missed Eureka immediately — matches README spec.)
  - `FireEureka(Map map)`:
    1. Build the eligible pool: `DefDatabase<RecipeDef>.AllDefs.Where(d => d.defName.StartsWith("ChEureka_"))` minus the currently-active set.
    2. Pick `EurekaRecipesPerEvent` distinct entries (or all of them if the pool has fewer).
    3. Append picks to `actives` with `expiresAtTick = TicksGame + EurekaDurationTicks`.
    4. `nextEurekaTick = TicksGame + EurekaIntervalTicks` (advance from now — keeps the cadence honest after a long Ascendant absence).
    5. Fire a `Letter` of `LetterDefOf.PositiveEvent` titled "Eureka!" with body listing the discovered recipes' labels and the expiration time.
    6. (No bill-UI refresh needed: `ITab_Bills` re-reads `AvailableOnNow` every frame — new recipes appear automatically the next time the tab redraws.)
  - `GetActiveRecipes()` → `IEnumerable<RecipeDef>` — resolve defNames lazily via `DefDatabase<RecipeDef>.GetNamedSilentFail` so DLC-unloaded saves don't NRE. Used by the Forge inspect string comp.
  - `ChRecipeWorker_Eureka : RecipeWorker` (same file, separate class):
    - Override `AvailableOnNow(Thing thing, BodyPartRecord part = null)`.
    - Resolve the tracker via `thing?.Map?.GetComponent<CheatTraitsMapComponent>()?.EurekaTracker`.
    - Return `base.AvailableOnNow(thing, part) && tracker != null && tracker.IsActive(this.recipe.defName)`. (The base call preserves any default `RecipeWorker` checks.)
    - Defensive null guards: if `thing == null` or `thing.Map == null`, fall back to `false` (recipe inactive when we can't know the map).
- `Source/CheatTraits/Comps/CompChEurekaForgeInspect.cs` — `ThingComp` whose `CompInspectStringExtra()` formats the Eureka status. Reads the tracker via `parent.Map.GetComponent<CheatTraitsMapComponent>()?.EurekaTracker`. Format:
  - When `actives` is non-empty:
    `Eureka recipes available:`
    `  - <label> (<Xd Yh> remaining)`
    `  - ...`
    Use `(activeEntry.expiresAtTick - Find.TickManager.TicksGame).ToStringTicksToPeriod()`.
  - When empty: `No Eureka recipes active. Next Eureka in <Xd Yh>.`
  - When no Ascendant on map: append `\n(Eureka requires a Ch Ascendant on this map.)`.
  - The corresponding `CompProperties_ChEurekaForgeInspect` is trivial — just `compClass = typeof(CompChEurekaForgeInspect)`.

**Files to modify:**
- `Source/CheatTraits/Patches/ChTraitsMapComponent.cs` — Add a `ChEurekaTracker EurekaTracker` field (public read, owned by the MapComponent). Initialize in constructor, chain through `ExposeData` via `Scribe_Deep.Look`, call `EurekaTracker.Tick(map)` from the existing tick hub gated to `ChEurekaTracker.TickGateTicks`. Remove the call to `ChAscendantAuraSystem.TickMap(map)` and the `nextAscendantTick` field.
- `Source/CheatTraits/Patches/ChThingDefOfs.cs` — Add `ChEurekaForge` entry.
- `Source/CheatTraits/Patches/ChBuildRestrictions.cs` — Gate `ChEurekaForge` to the Ch Ascendant trait (mirror the existing Comfort Node / Floragen entries; one `if (placingDef == ChThingDefOf.ChEurekaForge)` in `Patch_DesignatorBuild_Visible_TraitGates.Postfix` and one in `Patch_ConstructFinishFrames_TraitGates.Postfix`).
- `Source/CheatTraits/Patches/Patch_BuildFloatMenu_TraitOverrides.cs` — Add a `ChEurekaForge` branch to `TryGetBuildTargetInfo` with `requiredTrait = CheatTraitsNames.AscendantTrait`, `labelPrefix = "Ch Ascendant"`.
- `Source/CheatTraits/Patches/ChTraitsUtils.cs` — `CheatTraitsNames.AscendantTrait` already exists; verify only.
- `Defs/TraitDefs/ChTraits.xml` — In the `ChAscendant` entry: **delete the `<statFactors><Fertility>1.0</Fertility></statFactors>` block** (or just remove the `Fertility` line if other factors live there); update the `<description>` text to reflect the Eureka mechanic instead of the old support-aura framing.
- `Defs/Hediffs/ChHediffs.xml` — **Delete the entire `ChAscendant_InspirationAura` HediffDef block**.
- `README.md` — Already updated as part of this plan revision.

**Files to delete:**
- `Source/CheatTraits/Patches/ChAscendantAura.cs` — the entire aura system (`ChAscendantAuraConfig`, `ChAscendantUtil`, `ChAscendantAuraSystem`, `ChAscendantDefOf`). After updating `ChTraitsMapComponent.cs` to drop the `ChAscendantAuraSystem.TickMap(map)` call, nothing else references these types.

**Implementation notes:**
- **Cadence & uptime:** 3 days active out of every 15 = 20% uptime. Both numbers are single constants at the top of `ChEurekaTracker` — keep them visible for tuning.
- **DLC safety — recipe defs:** `<RecipeDef MayRequire="ludeon.rimworld.anomaly">` on the top-level entry is the vanilla XML pattern; RimWorld's Def loader silently skips the def when the DLC isn't installed. The pool builder uses `defName.StartsWith("ChEureka_")` so it naturally sees only the available recipes — no runtime DLC checks needed in C#.
- **DLC safety — Forge `<recipes>` list:** Wrap individual `<li>` entries with `MayRequire="..."` so the cross-ref loader doesn't error on a missing recipe when the DLC is absent. Pattern: `<li MayRequire="ludeon.rimworld.anomaly">ChEureka_Bioferrite</li>`.
- **DLC package IDs in `MayRequire`:** Canonical lowercase forms are `ludeon.rimworld.anomaly`, `ludeon.rimworld.biotech` (the engine compares case-insensitively, but lowercase is the in-source canonical — see [ModLister.cs:350](F:/Development/rimworld-decomp/Assembly-CSharp/Verse/ModLister.cs#L350) and [ModLister.cs:227](F:/Development/rimworld-decomp/Assembly-CSharp/Verse/ModLister.cs#L227)).
- **Why the worker subclass, not a Harmony patch:** `RecipeDef.AvailableOnNow` delegates to `this.Worker.AvailableOnNow(thing, part)` ([RecipeDef.cs:243](F:/Development/rimworld-decomp/Assembly-CSharp/Verse/RecipeDef.cs#L243)). Overriding `RecipeWorker.AvailableOnNow` in a subclass is the vanilla extension point — it's how Royalty/Anomaly recipes do conditional availability. No Harmony required, no static-cache fragility, and the `thing` argument hands us the workbench so we have `thing.Map` directly.
- **Multi-Ascendant rule:** the tracker is per-map, not per-pawn, so multiple Ascendants on one map don't multiply Eurekas. State naturally enforces this — no extra code.
- **Multi-Forge rule:** the tracker is per-map, the worker reads `thing.Map`, so multiple Forges on the same map share the active recipe list. Parallel throughput only. Desirable.
- **Off-map behavior:** If the Ascendant is on a caravan when `nextEurekaTick` elapses, the timer pauses (step 2 of `Tick`). When they return, the next tick check passes the `>=` comparison and fires immediately. This matches the README spec.
- **Save compat:** None needed — the prior aura/Pillar design was never shipped beyond the existing aura hediff. Any in-progress save with the aura hediff will gracefully drop it (vanilla handles missing HediffDefs by removing the hediff at load). No migration code required.
- **Bill auto-disable on recipe expiry:** Bills outlive `AvailableOnNow` returning `false` — they remain on the BillStack with their `recipe` reference, but `WorkGiver_DoBill` calls `bill.ShouldDoNow → recipe.AvailableOnNow(billGiver)` and the bill goes inert automatically. The player can leave a `Brew Luciferium` bill queued; it sits paused between Eurekas and reactivates on the next time that recipe rolls. No bill cleanup code needed. **Verify this behavior in testing.** If the bill UI does something visually broken (e.g. shows the bill as available when the worker says no), add a cosmetic patch then — not before.
- **Non-UI leakage:** `PlayerItemAccessibilityUtility`, `QuestNode_GetThingPlayerCanProduce`, `RoomRoleWorker_Kitchen` iterate `def.AllRecipes` without consulting `AvailableNow` / `AvailableOnNow`. They will treat Eureka recipes as always-producible. Single-colony impact is invisible. Don't add a second gate unless a real leak surfaces.
- **Letter wording suggestion:** Title `"Eureka!"`. Body: `"<Ascendant's name> has had a breakthrough. The Eureka Forge can now produce:\n  - <recipe 1>\n  - <recipe 2>\nThese recipes will remain available for 3 days."`.
- **Worker call frequency:** `AvailableOnNow` runs per-recipe per-frame for every Forge whose bills tab is open. Keep `ChEurekaTracker.IsActive` to an O(n) scan over a tiny `actives` list (max 2 entries plus carryover from any partial overlap). A `HashSet<string>` rebuilt only when `actives` mutates is overkill; a linear scan over ≤4 entries is fine.

**Acceptance criteria:**
- Eureka Forge appears in the Architect menu only when the colony has a Ch Ascendant; force-buildable by a Ch Ascendant via the float-menu override.
- Built Forge shows every `ChEureka_*` recipe in the bills tab's "Add Bill" menu **only while that recipe is active**. Inactive recipes are hidden.
- Roughly 15 in-game days after build (or game start with an Ascendant present), a letter announces the Eureka and exactly `2` random recipes become available in the bills tab.
- After 3 in-game days, those recipes are hidden again from the "Add Bill" menu and any queued bills referencing them stop being picked up by workers (the bill row in the UI may still be visible — confirm whether it shows a "no recipe available" state or stays cosmetically green).
- Bills use the assigned crafter's normal Crafting skill / work speed / quality (Artificer pairing produces MW/Legendary as expected).
- DLC-gated recipes do not appear without the corresponding DLC installed and do not produce Def load errors when missing.
- Ascendant absence (caravan, death) pauses the Eureka timer; presence resumes it.
- `ChAscendant_InspirationAura` is fully removed: no hediff def, no code references, the file `ChAscendantAura.cs` is gone, and the trait def no longer carries `Fertility x1.0`.

**Manual test steps for the user:**
- Spawn Ch Ascendant. Confirm the Eureka Forge appears in the Architect menu and is force-buildable. Confirm there is no Vitae Pillar entry anywhere.
- Build the Forge. Open the bills tab and confirm "Add Bill" shows no `ChEureka_*` recipes (worker gates them out). Confirm the inspect string explains the Eureka mechanic with a countdown.
- Dev-mode skip ~15 days. Confirm letter fires and exactly two recipes appear in the bills tab without reopening it.
- Queue a discovered recipe with a non-Artificer pawn; confirm the pawn's Crafting skill drives work speed and quality.
- Queue a discovered recipe with a Ch Artificer; confirm Excellent/MW/Legendary quality rolls per their existing patch.
- Skip another 3 days; confirm the recipes drop out of "Add Bill" and that any in-flight bill stops being worked. Document whether the queued bill row stays cosmetically present or shows a "recipe not available" state.
- Toggle Anomaly and Biotech on/off in separate test sessions — confirm Bioferrite / Archite Capsule recipes appear / don't appear accordingly and no Def load errors fire.
- Caravan the Ascendant off-map for ~20 days; confirm no Eureka fires while gone, and a Eureka fires within `TickGateTicks` of their return.
- Inspect the Ch Ascendant pawn — confirm no aura hediff appears on nearby colonists and no Fertility override.

---

### Chunk 7 — Call of the Wild (Beastmaster) [x] 2026-05-23

Notes:
- **Key shortcut found via analysis:** the vanilla animal think tree (`Core/Defs/ThinkTreeDefs/SubTrees_Misc.xml`, "Manhunter" branch in the `AnimalMentalStates` subtree) uses `ThinkNode_ConditionalMentalStateClass` with `stateClass=MentalState_Manhunter`. That node's `Satisfied` check uses `Type.IsInstanceOfType`, so **any subclass of `MentalState_Manhunter` automatically inherits the entire `JobGiver_Manhunter` branch**. No think tree patch, no custom JobGiver, no custom MentalStateWorker — just subclass and override `ForceHostileTo`. New analysis note written at [features/mental-states.md](F:/Development/rimworld-analysis/features/mental-states.md).
- `MentalState_ChWildHunt : MentalState_Manhunter` lives in `Source/CheatTraits/MentalStates/`. Overrides both `ForceHostileTo(Thing)` and `ForceHostileTo(Faction)` to narrow the manhunter targeting to "things hostile to the player faction, excluding the pawn's own faction and the player faction itself." `Pawn.HostileTo` consults these overrides during `AttackTargetFinder.BestAttackTarget`'s `innerValidator`, so `JobGiver_Manhunter.FindPawnTarget` naturally picks only player-hostile humanlikes/mechs (the `Intelligence >= ToolUser` predicate already filters out other animals, preventing animal-on-animal infighting).
- `MentalStateDef ChWildHunt` sets `min/maxTicksBeforeRecovery=5000` for an exact 5000-tick lifetime (per `MentalState.MentalStateTick`'s `age >= maxTicksBeforeRecovery` check); `recoveryMtbDays=9999` is defensive — MTB recovery is only considered when `age >= minTicksBeforeRecovery`, so with min==max it never fires, but the high MTB makes the intent obvious. `beginLetter` / `recoveryMessage` are left null so the map-wide cast doesn't spam per-pawn letters — the comp sends one summary letter at cast time.
- `CompAbilityEffect_ChCallOfTheWild` snapshots `mapPawns.AllPawnsSpawned` before iterating (calling `TryStartMentalState` mutates the pawn's state and can invalidate the live list). For each animal not in the player's faction and not currently in `ChWildHunt`, it calls `TryStartMentalState(stateDef, forced: true, forceWake: true, transitionSilently: true)`. `forced=true` bypasses the `MentalBreaksBlocked()` check (some hediffs block mental breaks). `transitionSilently=true` skips per-pawn letter / tale-recording / records-increment. Same-def guard up front avoids the `CurStateDef == stateDef` no-op spam.
- Cast UX matches Iron Wall — `targetRequired=false, range=0, canTargetSelf=true` (only) — fires instantly on click with no designator.
- Icon: `UI/Abilities/AnimalWarcall` (Royalty psycast for animal mental breaks). Consistent with the mod's existing DLC-icon usage pattern. First-pass pick `UI/Abilities/AnimalRecruitmentAura` was a guess that doesn't exist in vanilla — corrected by grepping iconPath values across DLC AbilityDef XML before commit.
- README spec was already authored in the prior README sync; no changes needed this chunk.

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

**Scope:** Trait-gated building with one bill (Trail Tonic). Adds a custom drug-class ingestible `Trail Tonic`. Surface area: building + ThingDef + drug behavior + 1 RecipeDef + a hediff + a trait-gate on recipes. The trait-gate infrastructure (`ChRequiredTrait` DefModExtension + Harmony postfix) should be built generically so additional Alchemist tonics can be added in later chunks with just a RecipeDef + Hediff + ThingDef.

**README section:** Alchemy Cauldron (under Trait-Gated Buildings).

**Files to create:**
- `Defs/BuildingDefs/ChAlchemyCauldron.xml` — `ThingDef` for the cauldron. Costs `50 Steel + 20 Wood`, work `600`, no power. Include a `compIngredients` and the standard bill workbench machinery (mirror vanilla `ButcherTable` or `DrugLab` for the bill config).
- `Defs/ThingDefs/ChTrailTonic.xml` — `ThingDef` for the tonic item. Parent off a drug base (e.g. `MakeableDrugBase` or `OrganicProductBase` with drug comps). Set `drugCategory` to `Hard` so the default policy excludes it. `comps`: `CompProperties_Drug` with `addictiveness=0`, `overdoseSeverityOffset=0`, `listOrder` and an ingest job. `ingestEffect=Drink`, `ingestSound=Ingest_Drink`. The hediff giver on the comp applies `ChTrailTonicBuff`.
- `Defs/Hediffs/ChTrailTonicBuff.xml` — hediff for the drink effect: `HungerRateFactor x0.05`, `RestFallRateFactor x0.5`, `+5%` Move Speed. Use `HediffComp_Disappears` with duration `180000` ticks.
- `Defs/RecipeDefs/ChAlchemyRecipes.xml` — one recipe: `BrewChTrailTonic`. Hook to the cauldron via `recipeUsers` on the recipe OR `recipes` on the cauldron ThingDef (either pattern works, pick one).
- `Source/CheatTraits/Patches/ChAlchemistRecipeGate.cs` — Harmony patch that gates Alchemist-tagged recipes to Ch Alchemist pawns only. Hook point candidates: `RecipeDef.PawnSatisfiesSkillRequirements` (returns false for non-Alchemist when the recipe carries the trait tag) or `WorkGiver_DoBill.JobOnThing`. Use a `DefModExtension` (e.g. `ChRequiredTrait`) on the recipe so the patch is generic and future-proof — additional tonics added later just need the same extension, no patch changes.

**Files to modify:**
- `Source/CheatTraits/Patches/ChThingDefOfs.cs` — register the cauldron and the trail tonic defs.
- `Source/CheatTraits/Patches/ChBuildRestrictions.cs` — gate cauldron build to Ch Alchemist.
- `Source/CheatTraits/Patches/Patch_BuildFloatMenu_TraitOverrides.cs` — allow Alchemist force-build.
- `Source/CheatTraits/Patches/ChTraitsUtils.cs` (`CheatTraitsNames`) — add the trait constant.

**Implementation notes:**
- **Witch's brew ingredients (per spec, no human-derived materials):**
  - Trail Tonic: `3 Herbal Medicine + 2 leather (any non-human) + 5 raw plants → 1 Trail Tonic`. For "leather (any)", use a `ThingFilter` referencing the `Leathery` category and explicitly exclude `Leather_Human`. Raw plants: `PlantMatter` category covers raw veggies, healroot, etc.
  - `workAmount` for Trail Tonic ~`8000`. Tune to feel.
- **Drug category vs auto-consume:** `drugCategory: Hard` plus the default drug policy (`PolicyDefOf.DefaultDrug` excludes Hard drugs unless scheduled) gives the "explicit consumption only" behavior. Confirm at test time by giving a colonist trail tonic in inventory and watching that they don't drink it spontaneously.
- **Recipe trait gate:** the cleanest approach is a small `DefModExtension`:
  ```
  ChRequiredTrait { traitDefName = "ChAlchemist" }
  ```
  attached to the recipe def, plus a Harmony postfix on `RecipeDef.PawnSatisfiesSkillRequirements` that also checks the ext and returns false for non-matching pawns. Build this generically so future tonic recipes only need to add the extension.
- **Building bill UI:** vanilla `Building_WorkTable` already gives you the bill stack and the "Add Bill" menu — the cauldron should derive from it. Workbench art can reuse a vanilla texture initially; placeholder is fine for first pass.
- This chunk's natural split if it gets too long:
  1. Cauldron ThingDef + trait-gated build + the generic recipe-gate machinery (sanity-check the building exists in-game and an empty bill list works).
  2. Trail Tonic item + hediff + ingest behavior + the Trail Tonic recipe (sanity-check drinking works from inventory and Alchemist can craft).
  Aim for a single session if possible.

**Acceptance criteria:**
- Architect entry appears only with Ch Alchemist in the colony.
- Bills tab on the cauldron lists the Trail Tonic recipe.
- Non-Alchemist pawns cannot be assigned and will skip bills on it.
- Trail Tonic items in stockpile are *not* auto-consumed under the default drug policy.
- Right-click → Drink Trail Tonic works and applies the hediff for 3 in-game days.

**Manual test steps for the user:**
- Spawn Ch Alchemist; confirm cauldron unlocks. Build it.
- Queue the Trail Tonic bill. Confirm Alchemist works the bench. Switch Alchemist off-duty; queue the bill again; confirm a non-Alchemist Cook does *not* perform the bill.
- Move Trail Tonic into a pawn's inventory; observe over a couple of in-game days that they don't drink it on their own.
- Manually drink it on the pawn → hediff applied, hunger flatlines for 3 days.

---

## Deferred / not in this plan

- Additional Alchemy Cauldron tonics beyond Trail Tonic — deferred to future chunks. The Chunk 8 trait-gate machinery is built generically so adding new tonics later is just RecipeDef + Hediff + ThingDef, no patch changes.
- Any rebalancing of existing traits.
- Multi-target or upgrade variants of the new abilities.

When the user requests additions, add a new chunk at the bottom rather than amending closed chunks.
