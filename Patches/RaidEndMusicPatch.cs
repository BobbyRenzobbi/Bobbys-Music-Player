using SPT.Reflection.Patching;
using EFT.UI;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace BobbysMusicPlayer.Patches
{
    public class RaidEndMusicPatch : ModulePatch
    {
        internal static List<string> deathMusicList = new List<string>();
        internal static List<string> extractMusicList = new List<string>();
        private Dictionary<EEndGameSoundType, List<string>> raidEndDictionary = new Dictionary<EEndGameSoundType, List<string>> 
        {
            [EEndGameSoundType.ArenaLose] = deathMusicList,
            [EEndGameSoundType.ArenaWin] = extractMusicList
        };
        private static AudioClip raidEndClip;
        Plugin plugin = new Plugin();

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(UISoundsWrapper), nameof(UISoundsWrapper.GetEndGameClip));
        }

        private void LoadNextTrack(EEndGameSoundType soundType)
        {
            string raidEndTrack;
            if (!raidEndDictionary[soundType].IsNullOrEmpty())
            {
                raidEndTrack = raidEndDictionary[soundType][0];
            }
            else
            {
                return;
            }
            raidEndClip = plugin.RequestAudioClip(raidEndTrack);
            string trackPath = Path.GetFileName(raidEndTrack);
            Logger.LogInfo(trackPath + " assigned to " + soundType);
        }

        [PatchPrefix]
        static bool Prefix(ref AudioClip __result, EEndGameSoundType soundType)
        {
            RaidEndMusicPatch patch = new RaidEndMusicPatch();
            patch.LoadNextTrack(soundType);
            if (raidEndClip != null)
            {
                __result = raidEndClip;
                return false;
            }
            return true;
        }
    }
}