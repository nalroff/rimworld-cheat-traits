using HarmonyLib;
using RimWorld;
using Verse;

namespace CheatTraits.Patches
{
    // Softens a Ch Boxer's unarmed punches against non-hostile targets so social
    // fights and mental breaks don't one-shot friendlies. The base Boxer stat patch
    // grants a 10x MeleeDamageFactor with no target awareness; this clamps it back
    // to roughly vanilla unarmed levels at the damage-application boundary.
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
    public static class ChBoxerFriendlyDamageSoften
    {
        // ~undoes the 10x MeleeDamageFactor boost so friendly punches land near vanilla.
        internal const float BoxerFriendlySoftFactor = 0.1f;

        public static void Prefix(Pawn __instance, ref DamageInfo dinfo)
        {
            if (__instance == null || __instance.Dead)
                return;

            Pawn? attacker = dinfo.Instigator as Pawn;
            if (attacker == null || attacker == __instance)
                return;

            if (!CheatTraitsGetStatValuePatch.IsBoxer(attacker))
                return;

            // Verb_MeleeAttackDamage sets dinfo.Weapon to the caster's ThingDef for unarmed
            // attacks; gate on that so abilities / other damage sources are unaffected.
            if (dinfo.Weapon != attacker.def)
                return;

            // HostileTo covers factions, prisoners, manhunters, and mental-state aggression,
            // so social fights, berserk, and accidental friendly-fire all soften here,
            // while raider/enemy-animal targets keep the full 10x.
            if (attacker.HostileTo(__instance))
                return;

            dinfo.SetAmount(dinfo.Amount * BoxerFriendlySoftFactor);
        }
    }
}
