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
        internal static List<string>[] uiSounds = new List<string>[8];
        internal static string[] uiSoundsDir = new string[8]
        {
            "QuestCompleted", "QuestFailed", "QuestFinished", "QuestStarted", "QuestSubtaskComplete", "DeathSting", "ErrorSound", "TradeSound"
        };
        private static AudioClip replacementClip;
        private static Plugin plugin = new Plugin();
        private static Dictionary<EUISoundType, int> UISoundDictionary = new Dictionary<EUISoundType, int>
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
        internal static List<AudioClip>[] uiSoundsClips = new List<AudioClip>[8];
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(UISoundsWrapper), nameof(UISoundsWrapper.GetUIClip));
        }

        internal static async void LoadUIClips()
        {
            int counter = 0;
            foreach (List<string> list in uiSounds)
            {
                uiSoundsClips[counter] = new List<AudioClip>();
                foreach (string track in list)
                {
                    uiSoundsClips[counter].Add(await plugin.AsyncRequestAudioClip(track));
                    Plugin.LogSource.LogInfo(Path.GetFileName(track) + " assigned to " + uiSoundsDir[counter]);
                }
                counter++;
            }
        }
        [PatchPrefix]
        static bool Prefix(ref AudioClip __result, EUISoundType soundType)
        {
            if (!UISoundDictionary.ContainsKey(soundType))
            {
                return true;
            }
            var audioClipArray = uiSoundsClips[UISoundDictionary[soundType]];
            if (audioClipArray.IsNullOrEmpty())
            {
                return true;
            }
            __result = audioClipArray[Plugin.rand.Next(audioClipArray.Count)];
            return false;
        }
    }
}