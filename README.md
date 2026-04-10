# Cheat Traits

`Cheat Traits` adds 14 overpowered `Ch*` traits for players who want standout specialists, hero pawns, or deliberately broken colony builds. Each trait is built around a clear role: unarmed fighter, revolver ace, speedster, tank, master grower, miracle doctor, beastmaster, diplomat, and more.

All traits use `commonality = 0`, so they are best suited for custom starts, edited pawns, dev-mode setups, or other deliberate trait assignment.

## Requirements

- RimWorld `1.6`
- Harmony

## What The Mod Adds

- `14` high-power role traits
- Several passive aura systems
- Three trait-gated buildings:
  - `Floragen Core`
  - `Comfort Node`
  - `Tesla Coil`

## Trait-Gated Buildings

Some traits do more than modify stats. They also unlock special buildings that only appear when your colony has the matching trait, and only pawns with that trait can build or force-build them.

### Floragen Core

Unlocked by: `Ch Green Thumb`

- Costs `15 Steel`
- `250` work to build
- `12` tile radius
- Scans every `2000` ticks
- Applies `3x` total plant growth in its field
- Multiple cores do not stack with each other
- Stacks with the Green Thumb pawn aura

### Comfort Node

Unlocked by: `Ch Comfy`

- Costs `15 Steel`
- `250` work to build
- Requires no power
- Indoor room stabilizer with a target-temperature gizmo
- Keeps its room near the chosen temperature
- Emits light that shifts cooler or warmer based on room temperature

### Tesla Coil

Unlocked by: `Ch Tesla`

- Costs `15 Steel`
- `250` work to build
- Generates `750W`
- Attacks standing hostile pawns within `6` tiles and line of sight
- Deals `45` damage with `0.15` armor penetration
- Stuns for `120` ticks
- Fires every `180` ticks when a valid target is available

## Trait Reference

### Ch Boxer

An unarmed melee monster.

- `+10` Melee
- `+100` Melee Hit Chance
- `+1000` Pain Shock Threshold
- While unarmed, `MeleeDamageFactor x10`

### Ch Tex

A gunfighter with a heavy revolver specialty.

- `+10` Shooting
- Always gains these ranged bonuses from the trait itself:
  - `+0.45` Shooting Accuracy Pawn
  - `AimingDelayFactor x0.55`
  - `RangedCooldownFactor x0.85`
- Gains an additional bonus while wielding a revolver
- Works with `Gun_Revolver` and weapons whose `defName` contains `Revolver`
- Total ranged bonus with a revolver equipped:
  - Accuracy is pushed extremely high and then capped at `0.99`
  - `AimingDelayFactor x0.10`
  - `RangedCooldownFactor x0.25`

### Ch Zippy

A movement-focused speedster.

- `+35` Move Speed
- `+35` Crawl Speed
- `+20` Global Work Speed

### Ch Tank

A front-line damage sponge with strong armor and pain resistance.

- `+2.5` Sharp armor
- `+2.5` Blunt armor
- Permanent pain dampener while the trait is present
- Pain is reduced to `40%` of normal

### Ch Green Thumb

A walking plant-growth engine and the unlock for `Floragen Core`.

- `+10` Plants
- Affects plants within `12` tiles
- Reapplies its effect every `250` ticks
- Pushes nearby plants to `10x` total growth speed
- Growth ignores normal light, temperature, season, and rest limitations
- Removes blight from affected plants

### Ch Artificer

A master builder, crafter, and artist who works fast and produces top-end quality.

- `+10` Construction
- `+10` Crafting
- `+10` Artistic
- `GeneralLaborSpeed x5`, capped at `8`
- `ConstructionSpeed x5`, capped at `8`
- Quality outcomes are forced to:
  - `60%` Excellent
  - `30%` Masterwork
  - `10%` Legendary

### Ch Alchemist

A cook whose meals become buffs instead of just food.

- `+10` Cooking
- Works on simple, fine, and lavish meals
- Alchemist meals never cause food poisoning
- Meals can become "perfect" versions:
  - Simple: `15%`
  - Fine: `25%`
  - Lavish: `35%`

Meal effects:

- Simple meal: `+5%` Move Speed, `+5%` Work Speed Global, `+10%` Immunity Gain Speed, `+10%` Rest Rate, `-10%` Hunger Rate for `6000` ticks
- Fine meal: `+10%` Move Speed, `+10%` Work Speed Global, `+20%` Immunity Gain Speed, `+20%` Rest Rate, `-20%` Hunger Rate for `9000` ticks
- Lavish meal: `+15%` Move Speed, `+15%` Work Speed Global, `+30%` Immunity Gain Speed, `+30%` Rest Rate, `-30%` Hunger Rate for `12000` ticks
- Perfect simple meal: `+10%` Move Speed, `+10%` Work Speed Global, `+20%` Immunity Gain Speed, `+20%` Rest Rate, `-20%` Hunger Rate for `9000` ticks
- Perfect fine meal: `+20%` Move Speed, `+20%` Work Speed Global, `+35%` Immunity Gain Speed, `+35%` Rest Rate, `-35%` Hunger Rate for `12000` ticks
- Perfect lavish meal: `+30%` Move Speed, `+30%` Work Speed Global, `+50%` Immunity Gain Speed, `+50%` Rest Rate, `-50%` Hunger Rate for `15000` ticks

### Ch Doc

A guaranteed surgeon and elite doctor.

- `+10` Medicine
- Surgeries performed by this pawn do not fail
- Tend quality is forced to `100%`
- Includes self-tend

### Ch Ascendant

A high-end leader/researcher with a powerful support aura.

- `+10` Intellectual
- `ResearchSpeed x10`
- `Fertility` is forced to `1.0`
- Emits an aura to same-faction humanlikes within `20` tiles
- Aura refreshes every `250` ticks and lingers for `7500` ticks
- Aura grants:
  - `GlobalLearningFactor x3`
  - `InjuryHealingFactor x4`

### Ch Beastmaster

A supreme animal handler with a strong aura for colony animals.

- `+10` Animals
- `+1000` Tame Animal Chance
- `+1000` Train Animal Chance
- `AnimalGatherSpeed x2`
- `AnimalGatherYield x2`
- Ignores animal-interaction skill minimums for tame/train-style work
- Emits an aura to same-faction animals within `20` tiles
- Aura refreshes every `250` ticks and lingers for `5000` ticks

Herd blessing effects on animals:

- `+0.12` Melee Hit Chance
- `+0.08` Melee Dodge Chance
- `+0.15` Sharp armor
- `+0.12` Blunt armor
- `+0.20` Pain Shock Threshold
- `MoveSpeed x1.15`
- `CarryingCapacity x1.50`
- `MeleeDamageFactor x1.25`
- `MeleeCooldownFactor x0.95`
- `InjuryHealingFactor x1.25`
- `ImmunityGainSpeed x1.25`
- `ToxicResistance x1.25`
- `ToxicEnvironmentResistance x1.25`
- `IncomingDamageFactor x0.85`
- `Fertility x1.25`
- `BodyResourceGrowthSpeed x7`

### Ch Diplomat

A social powerhouse with mood and relationship support.

- `+10` Social
- `+10.0` Social Impact
- `+10.0` Negotiation Ability
- `+2.0` Trade Price Improvement
- `+10.0` Conversion Power
- Emits an aura to same-faction humanlikes within `16` tiles
- Aura refreshes every `250` ticks and lingers for `15000` ticks
- Affected player pawns gain `+8` mood from `diplomatic calm`
- Affected player pawns gain `+12` opinion of each other from `easy rapport`

### Ch Digger

An extreme miner and driller.

- `+10` Mining
- `MiningSpeed x3`
- `SmoothingSpeed x6`
- `MiningYield x2.5`
- `DeepDrillingSpeed x3`

### Ch Comfy

A fire-control and temperature utility specialist that unlocks the `Comfort Node`.

- `+2.5` Heat armor
- `-40` Comfy Temperature Min
- `+40` Comfy Temperature Max
- Automatically extinguishes fires within `10` tiles
- Checks for fires every `120` ticks
- Fire suppression can be toggled on or off per pawn

### Ch Tesla

The unlock trait for the `Tesla Coil`.

- No direct stat bonuses
- Gives access to the Tesla Coil build chain
- Best used for colonies that want free power plus short-range automated defense

## Suggested Use

Cheat Traits is best for:

- custom starts
- hero-pawn playthroughs
- themed colonies
- challenge runs with a few extremely strong specialists
- players who want powerful utility pawns without micromanaging gear or implants

If you want a colony of normal pawns with one or two absurd standouts, this mod is built for exactly that.
