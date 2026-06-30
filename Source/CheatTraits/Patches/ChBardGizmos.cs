using System.Collections.Generic;
using CheatTraits.Hediffs;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CheatTraits.Patches
{
    /// <summary>
    /// Adds the four stance-switch gizmos to a player-controlled ChBard. The
    /// active stance renders with a checkmark (Command_Toggle); the others grey
    /// out with a countdown while the switch cooldown is running. All state
    /// lives on the pawn's ChBard_Conductor hediff.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    internal static class ChBardGizmoPatch
    {
        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            __result = AddGizmos(__instance, __result);
        }

        private static IEnumerable<Gizmo> AddGizmos(Pawn pawn, IEnumerable<Gizmo> baseGizmos)
        {
            foreach (var g in baseGizmos)
                yield return g;

            if (pawn == null || !pawn.Spawned)
                yield break;
            if (pawn.Faction != Faction.OfPlayer)
                yield break;
            if (pawn.story?.traits == null || pawn.health?.hediffSet == null)
                yield break;
            if (!CheatTraitsUtils.HasTrait(pawn, CheatTraitsNames.BardTrait))
                yield break;

            var conductor =
                pawn.health.hediffSet.GetFirstHediffOfDef(ChBardDefOf.ChBard_Conductor)
                as Hediff_ChBardConductor;
            if (conductor == null)
                yield break;

            for (int i = 0; i < ChBardModes.Count; i++)
            {
                int idx = i; // capture per-iteration for the closures below
                bool isActive = conductor.ModeIndex == idx;
                bool onCooldown = !conductor.CanSwitch;

                var cmd = new Command_Toggle
                {
                    defaultLabel = ChBardModes.Labels[idx],
                    defaultDesc =
                        "Bard aura stance.\n\n"
                        + ChBardModes.Descriptions[idx]
                        + "\n\nAllies build up the buff the longer they stay in the aura, and it fades when they leave. Changing stance has a short cooldown.",
                    icon = IconFor(idx),
                    isActive = () => conductor.ModeIndex == idx,
                    toggleAction = () =>
                    {
                        if (conductor.ModeIndex == idx || !conductor.CanSwitch)
                            return;
                        conductor.SetMode(idx, ChBardAuraConfig.SwitchCooldownTicks);
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    },
                };

                // Greying: lock the inactive stances while the cooldown runs.
                // The active one stays bright so its checkmark reads as selected.
                if (!isActive && onCooldown)
                {
                    cmd.Disabled = true;
                    cmd.disabledReason =
                        "Changing stance again in "
                        + conductor.CooldownRemainingTicks.ToStringTicksToPeriod();
                }

                yield return cmd;
            }
        }

        private static Texture2D IconFor(int index)
        {
            switch ((ChBardMode)ChBardModes.Clamp(index))
            {
                // Infantry: vanilla Core bolt-action rifle.
                case ChBardMode.WarAnthem:
                    return ContentFinder<Texture2D>.Get(
                        "Things/Item/Equipment/WeaponRanged/BoltActionRifle");
                // Bulwark: no DLC-free vanilla shield icon exists; the only shield
                // icons (BulletShield/ShieldMech) are Royalty/Biotech. Pending
                // custom art — keep the draft icon as a placeholder for now.
                case ChBardMode.Bulwark:
                    return TexCommand.Draft;
                // Paragon: custom mod texture (Textures/UI/Commands/Bard_Paragon).
                case ChBardMode.Vigor:
                    return ContentFinder<Texture2D>.Get("UI/Commands/Bard_Paragon");
                // Athlete: vanilla Core move-speed (foot) icon.
                case ChBardMode.HeroicBoon:
                    return ContentFinder<Texture2D>.Get("UI/Icons/MoveSpeedBonus");
                default:
                    return TexCommand.Attack;
            }
        }
    }
}
