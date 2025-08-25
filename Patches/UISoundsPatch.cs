using SPT.Reflection.Patching;
using EFT.UI;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BobbysMusicPlayer.Data;
using BobbysMusicPlayer.Utils;
using UnityEngine;
using HarmonyLib;
using static UnityEngine.Random;

namespace BobbysMusicPlayer.Patches
{
    public class UISoundsPatch : ModulePatch
    {
        internal static List<string>[] UISounds = new List<string>[8];
        private static List<AudioClip>[] UISoundsClips = new List<AudioClip>[8];
        
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(UISoundsWrapper), nameof(UISoundsWrapper.GetUIClip));
        }
        
        [PatchPrefix]
        static bool Prefix(ref AudioClip __result, EUISoundType soundType)
        {
            if (!GlobalData.UISoundDictionary.ContainsKey(soundType))
            {
                return true;
            }
            
            var audioClipArray = UISoundsClips[GlobalData.UISoundDictionary[soundType]];
            if (audioClipArray.IsNullOrEmpty())
            {
                return true;
            }
            
            // The sound that plays in game will be a randomly selected sound from the corresponding folder
            __result = audioClipArray[Range(0, audioClipArray.Count)];
            return false;
        }
        
        /// <summary>
        /// Each element of uiSoundsClips is a List of AudioClips since we want players to be able to import as many sounds as they want per folder.
        /// </summary>
        internal static async void LoadUIClips()
        {
            int counter = 0;
            foreach (var list in UISounds)
            {
                UISoundsClips[counter] = new List<AudioClip>();
                foreach (var track in list)
                {
                    UISoundsClips[counter].Add(await AudioManager.AsyncRequestAudioClip(track));
                    BobbysMusicPlayerPlugin.LogSource.LogInfo(Path.GetFileName(track) + " assigned to " + GlobalData.UISoundsDir[counter]);
                }
                counter++;
            }
        }
    }
}