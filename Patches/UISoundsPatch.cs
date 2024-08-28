using SPT.Reflection.Patching;
using EFT.UI;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace BobbysMusicPlayer.Patches
{
    public class UISoundsPatch : ModulePatch
    {
        internal static string[] questSounds = new string[8];
        internal static string[] questSoundsDir = new string[8]
        {
            "QuestCompleted", "QuestFailed", "QuestFinished", "QuestStarted", "QuestSubtaskComplete", "DeathSting", "ErrorSound", "TradeSound"
        };
        private static AudioClip replacementClip;
        Plugin plugin = new Plugin();
        private Dictionary<EUISoundType, int> UISoundDictionary = new Dictionary<EUISoundType, int>
        {
            [EUISoundType.QuestCompleted] = 0,
            [EUISoundType.QuestFailed] = 1,
            [EUISoundType.QuestFinished] = 2,
            [EUISoundType.QuestStarted] = 3,
            [EUISoundType.QuestSubTrackComplete] = 4,
            [EUISoundType.PlayerIsDead] = 5,
            [EUISoundType.ErrorMessage] = 6,
            [EUISoundType.TradeOperationComplete] = 7
        };

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(UISoundsWrapper), nameof(UISoundsWrapper.GetUIClip));
        }

        private void LoadNextTrack(EUISoundType soundType)
        {
            string replacementTrack;
            if (UISoundDictionary.ContainsKey(soundType) && questSounds[UISoundDictionary[soundType]] != null)
            {
                replacementTrack = questSounds[UISoundDictionary[soundType]];
            }
            else
            {
                return;
            }
            replacementClip = plugin.RequestAudioClip(replacementTrack);
            string trackPath = Path.GetFileName(replacementTrack);
            Logger.LogInfo(trackPath + " assigned to " + soundType);
        }

        [PatchPrefix]
        static bool Prefix(ref AudioClip __result, EUISoundType soundType)
        {
            replacementClip = null;
            Logger.LogInfo("UISoundsPatch.Prefix called");
            UISoundsPatch patch = new UISoundsPatch();
            patch.LoadNextTrack(soundType);
            Logger.LogInfo("UISoundsPatch.LoadNextTrack called");
            if (replacementClip != null)
            {
                __result = replacementClip;
                return false;
            }
            return true;
        }
    }
}