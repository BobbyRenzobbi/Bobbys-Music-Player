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
    public class RaidEndMusicPatch : ModulePatch
    {
        internal static List<string> DeathMusicList = new();
        internal static List<string> ExtractMusicList = new();
        private static Dictionary<EEndGameSoundType, List<string>> raidEndDictionary = new() {
            [EEndGameSoundType.ArenaWin] = ExtractMusicList,
            [EEndGameSoundType.ArenaLose] = DeathMusicList
        };
        
        private static AudioClipData raidEndClip;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(UISoundsWrapper), nameof(UISoundsWrapper.GetEndGameClip));
        }

        [PatchPrefix]
        static bool Prefix(ref AudioClip __result, EEndGameSoundType soundType)
        {
            if (raidEndDictionary[soundType].IsNullOrEmpty())
            {
                return true;
            }
            
            LoadNextTrack(soundType);
            
            if (raidEndClip != null)
            {
                __result = raidEndClip.Get();
                return false;
            }
            
            return true;
        }
        
        private static void LoadNextTrack(EEndGameSoundType soundType)
        {
            string raidEndTrack = raidEndDictionary[soundType][Range(0, raidEndDictionary[soundType].Count)];
            raidEndClip = new AudioClipData(AudioManager.RequestAudioClip(raidEndTrack));
            
            string trackName = Path.GetFileName(raidEndTrack);
            BobbysMusicPlayerPlugin.LogSource.LogInfo(trackName + " assigned to " + soundType);
        }
    }
}