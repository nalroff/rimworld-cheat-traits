# Cheat Traits

`Cheat Traits` adds 17 overpowered `Ch*` traits for players who want standout specialists, hero pawns, or deliberately broken colony builds. Each trait is built around a clear role: unarmed fighter, revolver ace, speedster, tank, master grower, miracle doctor, beastmaster, diplomat, spellcaster, support hero, master crafter, master builder, and more.

All traits use `commonality = 0` by default, so they are best suited for custom starts, edited pawns, dev-mode setups, or other deliberate trait assignment. If you *want* them to show up randomly, the mod settings let you raise their commonality (see below).

## Requirements

- RimWorld `1.6`
- Harmony

## What The Mod Adds

- `17` high-power role traits
- Several passive aura systems
- `14` custom castable abilities granted by traits
- Five trait-gated buildings:
  - `Floragen Core`
  - `Comfort Node`
  - `Tesla Coil`
  - `Alchemy Cauldron`
  - `Eureka Forge`

## Mod Settings

Open **Options → Mod Settings → Cheat Traits** to control how often the `Ch*` traits appear during pawn generation.

- **Cheat trait commonality** — a global slider (default `0` = off). Raising it makes every cheat trait eligible to roll on newly generated pawns. This applies to **all factions**: your colonists, allies, quest pawns, and raiders alike.
- **Customize individual traits** — an optional, collapsible panel with a per-trait slider. Any trait left untouched follows the global slider; override just the ones you want, and use the reset (`↺`) button or **Reset all to global** to clear overrides.

Guidance: a value near `1.0` makes each cheat trait roughly as common as a typical vanilla trait, which is *very* frequent across 17 traits — small values (`0.05`–`0.20`) are recommended. Changes take effect immediately for newly generated pawns; already-existing pawns are unaffected. The default of `0` keeps the traits deliberate-assignment-only, exactly as before.

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
- Fires every `180` ticks when a valid target is available
- **Chain lightning**: arcs from the coil to the nearest standing hostile within `6` tiles and line of sight, then hops to the next-nearest hostile within `5` tiles, up to `4` targets per pulse
- Every arc **stuns for `120` ticks** — this uses `StunHandler.StunFor`, which bypasses EMP adaptation, so it stays reliable on mechanoids no matter how many times they're hit
- **Damage is split by target type so raiders can still be captured:**
  - Flesh pawns (humanoids, animals): `16` damage at `0.15` armor penetration — tuned to *down* rather than kill. A flesh pawn already below `30%` health takes no damage (stun only), and downed pawns are never re-targeted, so the coil won't finish off a capturable attacker
  - Mechanoids / drones: `55` damage at `1.2` armor penetration — armor-piercing so heavy plating no longer soaks the hit

A wall-mounted variant with the same behavior is also available.

### Alchemy Cauldron

Unlocked by: `Ch Alchemist`

- `1x1` tile footprint
- Costs `50 Steel + 20 Wood`
- `600` work to build
- Requires no power
- Only a Ch Alchemist pawn can construct it
- Only a Ch Alchemist pawn can perform its bills
- Current recipes:
  - `Brew Trail Tonic` — `3 Herbal Medicine + 2 leather (any) + 5 raw plants → 1 Trail Tonic`
- More tonics may be added in future updates

`Trail Tonic` itself:

- Classified as a hard drug, but causes no addiction and no overdose
- The default drug policy excludes it, so colonists never auto-consume it — drinking is always explicit (right-click → "Drink Trail Tonic")
- Drinking applies the `Trail Tonic` hediff for `180000` ticks (3 in-game days):
  - `HungerRateFactor x0.05` (effectively no hunger for the duration)
  - `RestFallRateFactor x0.5`
  - `+5%` Move Speed
- Designed for caravans, long sieges, and extended treks

### Eureka Forge

Unlocked by: `Ch Ascendant`

- Costs `75 Steel + 30 Wood`
- `600` work to build
- Requires no power
- Functions as a crafting workbench with no bills available by default
- Bills become available only while a `Eureka` event has granted recipes (see `Ch Ascendant`)
- All bills use the assigned crafter's normal Crafting skill, work speed, and quality rolls — pair with a Ch Artificer for guaranteed Excellent/Masterwork/Legendary output
- Multiple Forges on one map share the same active recipe list (no per-Forge duplication)
- Inspect string makes the Eureka mechanic explicit so an empty bills tab is not confusing

## Trait Reference

### Ch Boxer

An unarmed melee monster.

- `+10` Melee
- `+100` Melee Hit Chance
- `+1000` Pain Shock Threshold
- While unarmed, `MeleeDamageFactor x10`
- Punches against non-hostile pawns are softened (~vanilla unarmed damage), so social fights and mental breaks won't one-shot allies
- Adds a `Flying Punch` ability: leap through the air (jump-pack style) to land adjacent to a chosen target and deliver a devastating strike
  - Range `15`, requires line of sight, cooldown `12500` ticks
  - Targets any pawn, animal, mech, or building — perfect for closing the gap on a ranged opponent
  - On landing, the Boxer's normal melee verb fires against the target; with the trait's `MeleeDamageFactor x10` unarmed bonus, a bare-handed punch lands as a single overwhelming blow
  - The `Flying Punch` button graphic was drawn by my 10-year-old

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
- Adds a `Deadeye` ability: a single perfect shot at any pawn the caster can see
  - Requires a ranged weapon to be equipped
  - No range limit, but a clear line of sight is required — no shooting through mountains
  - Deals `150` Bullet damage with `2.0` armor penetration directly to the target
  - Targets humanlikes and mechanoids
  - Cooldown `12500` ticks

### Ch Zippy

A movement-focused speedster.

- `+35` Move Speed
- `+35` Crawl Speed
- `+20` Global Work Speed
- Adds a `Blink` ability: instant teleport to a chosen standable tile within `15` tiles
  - No line-of-sight requirement
  - Cooldown `2500` ticks (~1 in-game hour) — intentionally low; this is a tactical positioning tool, not a setpiece

### Ch Tank

A front-line damage sponge with strong armor and pain resistance.

- `+2.5` Sharp armor
- `+2.5` Blunt armor
- Permanent pain dampener while the trait is present
- Pain is reduced to `40%` of normal
- Adds an `Iron Wall` ability: self-cast taunt and damage-reduction surge
  - Self-cast; affects an area `45` tiles around the Tank
  - Every hostile pawn within `45` tiles is forced to prioritize attacking the Tank for the duration
  - For `1500` ticks (~40s): `IncomingDamageFactor x0.10` and immunity to stun
  - Cooldown `12500` ticks

### Ch Green Thumb

A walking plant-growth engine and the unlock for `Floragen Core`.

- `+10` Plants
- Affects plants within `12` tiles
- Reapplies its effect every `250` ticks
- Pushes nearby plants to `10x` total growth speed
- Growth ignores normal light, temperature, season, and rest limitations
- Removes blight from affected plants

### Ch Artificer

A master crafter and artist who works fast and produces top-end quality. The *building* side of things now belongs to the Ch Engineer — Artificer covers crafted items and sculptures.

- `+10` Crafting
- `+10` Artistic
- `GeneralLaborSpeed x5`, capped at `8`
- Quality outcomes are forced for **crafted items and sculptures**:
  - `60%` Excellent
  - `30%` Masterwork
  - `10%` Legendary
  - Items made at work tables (weapons, apparel, components, drugs, etc.) and sculptures built at a frame both qualify
  - Non-art buildings (furniture, benches, walls) are the Ch Engineer's domain
- Adds a `Reforge` ability: rerolls the quality of any carried/equipped item or sculpture
  - Targets any equipped/stored item with a `QualityCategory` (apparel, weapons, art) plus installed sculptures
  - Installed non-art buildings are rejected — use the Ch Engineer's `Retrofit` on those
  - Reroll uses the same `60/30/10` Excellent/Masterwork/Legendary weights as the Artificer's quality patch
  - The reroll always replaces the current quality — even if the new roll is lower than what was there
  - Single use per `12500`-tick cooldown
  - Reroll does not consume the item and does not count as work — other pawns can stay on duty while the Artificer upgrades what they made

### Ch Engineer

A master builder. Splits the construction half off the old Artificer: every structure comes out fast, never fails, and rolls top-end quality.

- `+10` Construction
- `ConstructionSpeed x5`, capped at `8`
- `ConstructSuccessChance` forced to `100%` — construction frames never fail, so resources are never wasted on a botched build
- Quality outcomes are forced for **non-art buildings** built by this pawn (furniture, benches, walls, and other constructed things):
  - `60%` Excellent
  - `30%` Masterwork
  - `10%` Legendary
  - Sculptures are excluded — those roll on the Ch Artificer's quality patch instead
- Adds a `Retrofit` ability: rerolls the quality of an installed, non-art building in place
  - Targets installed furniture, benches, and other non-art buildings with a `QualityCategory`
  - Items and sculptures are rejected — use the Ch Artificer's `Reforge` on those
  - Reroll uses the same `60/30/10` Excellent/Masterwork/Legendary weights as the Artificer's
  - The reroll always replaces the current quality — even if the new roll is lower than what was there
  - Single use per `12500`-tick cooldown

### Ch Alchemist

A cook whose meals become buffs instead of just food.

- `+10` Cooking
- Unlocks the `Alchemy Cauldron` (see the trait-gated buildings section above)
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
- Adds a `Miracle Heal` ability: target one pawn anywhere on the map
  - Range `45`, no line-of-sight requirement
  - Cures one disease or infection (worst first)
  - Closes every non-permanent injury on the target
  - Restores `1` missing or destroyed body part (worst non-vital first)
  - Cooldown `12500` ticks

### Ch Ascendant

A genius intellect whose insights periodically unlock impossible recipes.

- `+10` Intellectual
- `ResearchSpeed x10`
- `HackingSpeed x10`
- Unlocks the `Eureka Forge` (see the trait-gated buildings section above)
- Every `15` in-game days (one quadrum) a `Eureka` event fires, but only while a Ch Ascendant is on the map:
  - `2` random recipes are added to the Eureka Forge for `3` in-game days
  - Pool is drawn from a curated list of normally-unattainable items: `Luciferium`, `Hyperweave`, `Components`, `Advanced Components`, `Glitterworld Medicine`
  - DLC items added to the pool when their DLC is installed: `Bioferrite` (Anomaly), `Archite Capsule` (Biotech)
  - Currently-active recipes are excluded from each new pick
  - A letter announces the event
  - Multiple Ascendants on one map do not multiply Eurekas
  - If the Ascendant is off-map when the timer elapses, the event fires the moment they return

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

Active ability:

- Adds a `Call of the Wild` ability: map-wide rally of every wild and neutral animal
  - No range limit and no line-of-sight requirement
  - Affects every animal on the map whose faction is *not* the player's — wild animals, manhunter packs, and insectoid hive defenders are all swept in
  - Forces affected animals into a custom `Wild Hunt` mental state for `5000` ticks
  - Animals seek out and attack the nearest pawn hostile to the player (and not in their own faction, so insectoids do not infight)
  - Player-faction animals (tamed pets, haulers, mounts) are unaffected
  - Cooldown `120000` ticks (2 in-game days)

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
- Adds a `Bond pawns` ability: cast on one humanlike, then pick a second to lock their relationship to near-maximum
  - Range `45`, requires line of sight, cooldown `12500` ticks (~3.5 in-game hours); the cooldown is refunded if the second pick is cancelled
  - Compatibility is forced to `2.0` (near-max): `~3x` deep-talk weight, `~0.5x` insult/fight chance
  - Opinion is forced to `100` in both directions (still suppressed if the pawn is dead or non-humanlike)
  - Romance chance is forced to `2.0` (clamps to 100%) — but only when the engine would already permit romance: orientation, age, species, incest, and Biotech missing-gene blocks are all respected
  - Bonds persist across maps and save/load
  - Casting on a bonded pair again removes the bond

### Ch Digger

An extreme miner and driller.

- `+10` Mining
- `MiningSpeed x3`
- `SmoothingSpeed x6`
- `MiningYield x2.5`
- `DeepDrillingSpeed x3`
- Adds a `Tunnel` ability: instantly clears a straight passage through rock and ore
  - Target a tile up to `15` tiles away
  - Excavates a `3`-wide line of cells from the Digger to the target tile
  - Yields resources at the trait's `MiningYield x2.5`
  - Cooldown `12500` ticks

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

### Ch Wizard

A born spellcaster with a custom spellbook of overpowered abilities. Works without `Royalty` — the abilities are standalone `AbilityDef`s with their own cooldowns rather than psycasts, so no psylink or psyfocus management is needed.

- `+8` Intellectual
- `+1.0` Psychic Sensitivity (flavor — used by Mass Berserk's duration scaling)
- Grants four custom castable abilities:

`Lightning Bolt`

- `3` charges, each on an independent `12500`-tick (5 in-game hours) cooldown — cast up to 3 bolts in quick succession, then one charge returns every 5 hours
- `45` tile range
- No line-of-sight requirement
- Calls down a vanilla lightning strike plus a `1.5`-tile EMP burst
- Deals `120` Burn damage to the primary target with `2.0` armor penetration

`Teleport Other`

- `2` charges, each on an independent `12500`-tick (5 in-game hours) cooldown — cast up to 2 teleports back-to-back, then one charge returns every 5 hours
- No range limit and no line-of-sight requirement (works across the entire map)
- Pulls any humanlike pawn to the nearest standable cell beside the caster
- Stuns the teleported pawn for `30`–`60` ticks

`Mass Berserk`

- `12500`-tick (5 in-game hours) cooldown, `45` tile range
- Requires line of sight to the target point
- Affects every hostile pawn within `12` tiles of the target — humanlikes, animals, and mechanoids
- Drives affected targets into a berserk frenzy for `600` ticks, attacking the nearest target including their own allies

`Super Soldier`

- `12500`-tick (5 in-game hours) cooldown
- No range limit and no line-of-sight requirement
- Target any friendly humanlike pawn on the map (including self)
- For `5000` ticks (`2` in-game hours):
  - Maxes the target's `Shooting` and `Melee` skills to `20` (original levels and XP are restored when the buff ends)
  - Applies `IncomingDamageFactor x0.25` (4x effective durability)
  - Applies `Nimble` trait parity: `+15` Melee Dodge Chance, `PawnTrapSpringChance x0.1`
  - Closes `1` HP of the worst non-permanent injury every `150` ticks (~`10` HP/min of active wound regen)
  - Suspends `Incapable of Violence` for the duration — a `Pacifist` target can draft, equip the conjured rifle, and attack while the buff is active (the trait re-engages when the buff ends)
  - Spawns Legendary `Cataphract Armor`, `Cataphract Helmet`, and a custom `Super Charge Rifle` onto the target
  - The target's existing primary weapon and any replaced apparel drop to the ground rather than being destroyed
- Once the buff ends, the spawned gear is destroyed regardless of where it ended up (still worn, dropped on the map, or picked up by another pawn)
- The `Super Charge Rifle` is exclusive to this spell — it has `2x` range (`55.8`), `2x` damage (`32`), and `2x` armor penetration (`0.70`) of a vanilla charge rifle, and never spawns through raids, trade, quests, scenarios, or crafting

### Ch Bard

A battlefield support hero. The Bard projects a `12`-tile aura that empowers nearby colonists, and the buff **ramps up the longer an ally stays inside it** — reaching full strength after about `3` in-game hours and fading away over a similar time once they leave.

- `+6` Social
- Aura affects same-faction humanlikes within `12` tiles (the Bard does not buff itself)
- Each affected ally builds the buff up through three tiers (faint → rising → peak); standing in the aura is what charges it
- Switchable between four stances using gizmos on the Bard. Changing stance has a `3` in-game hour cooldown; the active stance shows a checkmark, the others grey out with a countdown while on cooldown:

`Infantry` (offense)

- Up to `+0.18` Melee Hit Chance, `+0.10` Melee Dodge Chance, `+3` Shooting Accuracy
- Up to `x1.30` Melee Damage, `x0.80` Melee Cooldown, `x0.70` Aiming Delay

`Bulwark` (defense)

- Up to `+0.30`/`+0.25`/`+0.20` Sharp/Blunt/Heat Armor and `+0.30` Pain Shock Threshold
- Up to `x0.65` Incoming Damage

`Paragon` (sustain)

- Up to `-0.35` Hunger Rate, `x1.50` Immunity Gain Speed, `x1.50` Injury Healing, `x1.30` Rest Rate, `x1.30` Toxic Resistance

`Athlete` (reflexes and toughness)

- Mimics `Nimble` + `Tough`: up to `+0.18` Melee Dodge Chance, `x0.75` Incoming Damage, `x1.10` Move Speed

## Suggested Use

Cheat Traits is best for:

- custom starts
- hero-pawn playthroughs
- themed colonies
- challenge runs with a few extremely strong specialists
- players who want powerful utility pawns without micromanaging gear or implants

If you want a colony of normal pawns with one or two absurd standouts, this mod is built for exactly that.
