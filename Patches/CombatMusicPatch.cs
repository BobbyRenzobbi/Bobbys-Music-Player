using BepInEx.Logging;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BobbysMusicPlayer.Patches
{
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
        }
    }
    public class PlayerFiringPatch : ModulePatch
    {
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
            if (Plugin.combatTimer < Plugin.CombatFireEntryTime.Value)
            {
                Plugin.combatTimer = Plugin.CombatFireEntryTime.Value;
                Plugin.LogSource.LogInfo("Player fired. Combat timer set to " + Plugin.CombatFireEntryTime.Value);
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
            }
            return true;
        }
    }
}
