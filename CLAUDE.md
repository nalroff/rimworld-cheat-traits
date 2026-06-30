# CheatTraits

RimWorld 1.6 mod. Adds 17 overpowered `Ch*` traits for custom colonists and hero pawns. All traits use `commonality = 0`, so they never appear in normal gameplay — assignment is always deliberate (dev mode, scenario rules, character editor, etc.).

## Working on this mod

**Always use the `rimworld-analysis` skill before reasoning about RimWorld internals.** Any time a task touches vanilla RimWorld types, methods, or behavior — writing a new Harmony patch, modifying an existing one, debugging why a patch isn't firing, choosing the right hook point, reviewing aura/stat/Comp/Hediff logic, answering "how does X work in RimWorld", or naming a class/method to target — invoke the `rimworld-analysis` skill first so guidance is grounded in the curated notes and decompiled source rather than guessed. The only exception is pure XML Def authoring that doesn't touch C# behavior.

**Always use the `rimworld-sprite` skill when creating or refining a sprite/texture.** It owns the SVG → Inkscape → ImageMagick pipeline, the vanilla-style rules (camera angle, outline weight, palette, tone count), and the Graphic_Single vs Graphic_Multi decision. Per-building SVG sources live in `Source/art/`; see [`Source/art/cauldron.svg`](Source/art/cauldron.svg) as the canonical template.

**Keep `About/About.xml` in sync with `README.md`.** `README.md` is the source of truth for trait/ability/building names, counts, and behavior. Whenever you add, change, or remove a `Ch*` trait, ability, or trait-gated building, re-check the Steam-facing `<description>` in [`About/About.xml`](About/About.xml) for accuracy: the trait count, the building list, the ability count and names, and the high-level summaries must still match the README. The About description is a curated marketing blurb (not an exhaustive dump), so update it for correctness rather than copying the README verbatim.

## Active multi-session work

`PLAN.md` at the repo root tracks an in-progress abilities/buildings expansion broken into chunks. If the user asks to "implement the next chunk", "continue the plan", or names one of the chunked features (Knockout Blow, Deadeye, Tunnel, Iron Wall, Miracle Heal, Reforge, Blink, Vitae Pillar, Call of the Wild, Alchemy Cauldron), read `PLAN.md` first. The user-facing spec for each chunk lives in `README.md` and is authoritative on names/numbers/behavior.

## Prerequisites

- .NET SDK 6+
- RimWorld 1.6 installed
- Harmony mod (loaded before CheatTraits)

## Build Setup

1. Copy `.env.example` to `.env` and fill in your local paths:
   ```
   RIMWORLD_PATH=D:\Steam\steamapps\common\RimWorld
   RIMWORLD_DECOMP_PATH=F:\Development\rimworld-decomp
   ```
2. Run the build script (validates paths and runs dotnet build):
   ```
   ./build.ps1
   ```
3. Output lands in `Assemblies/CheatTraits.dll`.

Note: `Directory.Build.props` resolves RimWorld DLL references via relative paths (the mod lives inside `RimWorld/Mods/`), so `RIMWORLD_PATH` in `.env` is used for validation only, not injected into the build.

## Architecture

```
Source/CheatTraits/
  Bootstrap.cs                        — Harmony patch entry point
  ChTraitsMapComponent.cs             — Per-map state: aura ticks, fire suppression
  ChTraitsUtils.cs                    — Shared helpers (stat factor math, pawn checks)
  ChThingDefOfs.cs                    — [DefOf] references to trait-gated building defs
  ChAuraCacheComponent.cs             — Caches active aura emitters per map tick
  ChAuraKeys.cs                       — Hediff/stat def string constants

  Patches/
    Bootstrap.cs                      — Harmony init; applies all patches
    ChAlchemistMeals.cs               — Post-cook hook: applies buff meal variants
    ChArtificerQuality.cs             — Quality forcing split: Artificer=items+sculptures, Engineer=non-art buildings (Frame patch)
    ChAscendantAura.cs                — Learning + healing aura for humanlikes
    ChBeastmasterAura.cs              — Herd-blessing aura for animals
    ChBeastmasterInteractAnimalIgnoreSkill.cs — Bypasses skill minimums for tame/train
    ChBuildRestrictions.cs            — Restricts trait-gated buildings to trait holders
    ChComfyAura.cs                    — Fire-suppression tick; Comfort Node temperature
    ChComfyGizmos.cs                  — Toggle gizmo for fire suppression per pawn
    ChDiplomatAura.cs                 — Mood + opinion aura for humanlikes
    ChDiplomatThoughtWorkers.cs       — ThoughtWorker for diplomatic calm/easy rapport
    ChDocMedical.cs                   — Surgery no-fail + 100% tend quality patches
    ChFloragenCoreSystem.cs           — Plant-growth override within Floragen Core radius
    ChGreenThumbAura.cs               — Green Thumb pawn aura (plant growth boost)
    ChTankHediffApplier.cs            — Pain reduction hediff management for Ch Tank
    ChTraitsGetStatValuePatch.cs      — Stat factor patches (Boxer unarm bonus, Tex revolver, etc.)
    Patch_BuildFloatMenu_TraitOverrides.cs — Context menu: force-build for trait-gated things
    PlantInspect.cs                   — Adds growth-rate info to plant inspect string

  Comps/
    CompChComfyClimateNode.cs         — Comfort Node: temperature stabilizer + light shift
    CompChComfyGlow.cs                — Dynamic glow color for Comfort Node
    CompChFloragenCore.cs             — Floragen Core: area growth override ThingComp
    CompChTeslaZap.cs                 — Tesla Coil: power generation + hostile zap logic
```

## Key Patterns

**Stat patching** (`ChTraitsGetStatValuePatch.cs`): Harmony postfix on `StatWorker.GetValueUnfinalized`. Trait-specific bonuses are applied as factors/offsets after the base value. Tex revolver bonus is applied on top of the always-on ranged bonuses.

**Aura system** (`ChAuraCacheComponent.cs`, `ChTraitsMapComponent.cs`): On each tick interval, aura emitters are found via `ChAuraCacheComponent`, then apply hediffs to nearby pawns. Hediffs linger for the configured duration — this avoids re-scanning every tick. Auras check faction alignment (`Faction.IsPlayer`) before applying.

**Trait-gated buildings** (`ChBuildRestrictions.cs`, `Patch_BuildFloatMenu_TraitOverrides.cs`, `ChThingDefOfs.cs`): Buildings are hidden from the Architect menu unless the colony has the unlock trait. The float menu patch lets a trait-holding pawn force-build a gated thing via right-click even if it wouldn't normally be assignable.

**Quality forcing** (`ChArtificerQuality.cs`): Splits quality forcing across two traits using the same 60/30/10 weights (Excellent/Masterwork/Legendary). The `QualityUtility.GenerateQualityCreatedByPawn` leaf patch handles **items** (recipe `workSkill` ≠ Construction) for the Artificer. A `Frame.CompleteConstruction` patch handles **buildings**, branching on `CompArt`: sculptures (art) → Artificer, everything else → Engineer. Construction-skill rolls are deliberately skipped in the leaf patch so the Frame patch can tell art from non-art.

**Surgery no-fail** (`ChDocMedical.cs`): Patches `Recipe_Surgery.CheckSurgeryFail` — returns false (no fail) when the doctor pawn has Ch Doc. Tend quality patch forces `Pawn_HealthTracker.Notify_Tended` to 1.0f.

## Defs Layout

```
Defs/
  BuildingDefs/       — FloragenCore, ComfortNode, TeslaCoil ThingDefs + BuildableDef
  HediffDefs/         — Aura hediffs (AscendantAura, BeastmasterAura, DiplomatAura, TankPainDamp, AlchemistMealBuff_*)
  StatDefs/           — Any custom StatDefs (none currently — all patches use base game stats)
  ThoughtDefs/        — DiplomaticCalm, EasyRapport ThoughtDefs
  TraitDefs/          — All 17 Ch* TraitDefs with stat offsets
```

## Naming Conventions

- All traits: `ChBoxer`, `ChTex`, `ChZippy`, `ChTank`, `ChGreenThumb`, `ChArtificer`, `ChEngineer`, `ChAlchemist`, `ChDoc`, `ChAscendant`, `ChBeastmaster`, `ChDiplomat`, `ChDigger`, `ChComfy`, `ChTesla`, `ChWizard`, `ChBard`
- All C# classes: `Ch` prefix (e.g., `CompChTeslaZap`, `ChDiplomatAura`)
- All Def names: `Ch` prefix (e.g., `ChFloragenCore`, `ChDiplomaticCalm`)
- Harmony patch classes follow the pattern `Patch_<TargetType>_<Method>` or `Ch<Feature>` for multi-method patches

## Verification

1. `./build.ps1` exits 0; `Assemblies/` contains `CheatTraits.dll`
2. In-game: Mod Settings → CheatTraits entry visible (if settings added)
3. Dev mode: spawn a pawn, grant a `Ch*` trait, verify stat changes in the inspect window
4. Trait-gated buildings: Architect menu only shows the building after the unlock trait is on a colony pawn
5. Auras: check with dev mode pawn inspector — hediff should appear/disappear as the emitter pawn moves in/out of range
