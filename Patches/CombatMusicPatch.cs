using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BobbysMusicPlayer.Patches
{
    // The following patches each represent a different trigger for the combat state,
    // but they can never decrease the value of the combat timer.
    public class ShotAtPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(FlyingBulletSoundPlayer), nameof(FlyingBulletSoundPlayer.method_3));
        }

        [PatchPostfix]
        private static void Postfix()
        {
            if (Plugin.combatTimer < Plugin.CombatAttackedEntryTime.Value)
            {
                Plugin.combatTimer = Plugin.CombatAttackedEntryTime.Value;
                Plugin.LogSource.LogInfo("Player shot at. Combat Timer set to " + Plugin.combatTimer);
            }
            else
            {
                Plugin.LogSource.LogInfo("Player shot at");
            }
        }
    }
    
    public class PlayerFiringPatch : ModulePatch
    {
        internal static bool playerFired = false;
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.OnMakingShot));
        }

        [PatchPrefix]
        private static bool Prefix(Player __instance)
        {
            Player player = Singleton<GameWorld>.Instance.MainPlayer;
            if (__instance != player)
            {
                return true;
            }
            playerFired = true;
            if (Plugin.combatTimer < Plugin.CombatFireEntryTime.Value)
            {
                Plugin.combatTimer = Plugin.CombatFireEntryTime.Value;
                Plugin.LogSource.LogInfo("Player fired. Combat timer set to " + Plugin.CombatFireEntryTime.Value);
            }
            else
            {
                Plugin.LogSource.LogInfo("Player fired");
            }
            return true;
        }
    }
    
    public class DamageTakenPatch : ModulePatch
    {
        private static List<string> damageTypeList = new List<string>()
        {
            "Explosion", "Blunt", "Sniper", "Bullet", "Melee", "Landmine"
        };
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.ReceiveDamage));
        }
        [PatchPrefix]
        private static bool Prefix(Player __instance, EDamageType type)
        {
            Player player = Singleton<GameWorld>.Instance.MainPlayer;
            if (__instance != player)
            {
                return true;
            }
            if (damageTypeList.Contains(type.ToString()))
            {
                if (Plugin.combatTimer < Plugin.CombatHitEntryTime.Value)
                {
                    Plugin.combatTimer = Plugin.CombatHitEntryTime.Value;
                    Plugin.LogSource.LogInfo("Player hit. Combat timer set to " + Plugin.CombatHitEntryTime.Value);
                }
                else
                {
                    Plugin.LogSource.LogInfo("Player hit");
                }
            }
            return true;
        }
    }
    
    public class ShotFiredNearPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponSoundPlayer), nameof(WeaponSoundPlayer.FireBullet));
        }
        [PatchPrefix]
        private static bool Prefix(Vector3 shotPosition)
        {
            Player player = Singleton<GameWorld>.Instance.MainPlayer;
            float distance = Vector3.Distance(player.PlayerBones.BodyTransform.position, shotPosition);
            if (distance < Plugin.ShotNearCutoff.Value)
            {
                if (PlayerFiringPatch.playerFired == true)
                {
                    PlayerFiringPatch.playerFired = false;
                    return true;
                }
                if (Plugin.combatTimer < Plugin.CombatDangerEntryTime.Value)
                {
                    Plugin.combatTimer = Plugin.CombatDangerEntryTime.Value;
                    Plugin.LogSource.LogInfo("Player shot near. Combat Timer set to " + Plugin.combatTimer);
                }
            }
            else
            {
                Plugin.LogSource.LogInfo("Enemy shot fired past cutoff distance");
            }
            return true;
        }
    }
    
    public class GrenadePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Grenade), nameof(Grenade.Explosion));
        }
        [PatchPrefix]
        private static bool Prefix(Vector3 grenadePosition)
        {
            Player player = Singleton<GameWorld>.Instance.MainPlayer;
            float distance = Vector3.Distance(player.PlayerBones.BodyTransform.position, grenadePosition);
            if (distance < Plugin.GrenadeNearCutoff.Value)
            {
                if (PlayerFiringPatch.playerFired == true)
                {
                    PlayerFiringPatch.playerFired = false;
                    return true;
                }
                if (Plugin.combatTimer < Plugin.CombatGrenadeEntryTime.Value)
                {
                    Plugin.combatTimer = Plugin.CombatGrenadeEntryTime.Value;
                    Plugin.LogSource.LogInfo("Grenade explosion near. Combat Timer set to " + Plugin.combatTimer);
                }
            }
            else
            {
                Plugin.LogSource.LogInfo("Grenade explosion past cutoff distance");
            }
            return true;
        }
    }
}
