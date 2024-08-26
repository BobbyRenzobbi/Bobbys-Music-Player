using SPT.Reflection.Patching;
using EFT.UI;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using System;
using HarmonyLib;
using Comfort.Common;
using System.Linq;
using System.Threading.Tasks;

namespace BobbyRenzobbi.RaidEndMusic
{
    public class RaidEndMusicPatch : ModulePatch
    {
        internal static List<string> deathMusicList = new List<string>();
        internal static List<string> extractMusicList = new List<string>();
        private static AudioClip raidEndClip;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(UISoundsWrapper), nameof(UISoundsWrapper.GetEndGameClip));
        }

        private void LoadNextTrack(EEndGameSoundType soundType)
        {
            string raidEndTrack;
            if (soundType == EEndGameSoundType.ArenaLose && !deathMusicList.IsNullOrEmpty())
            {
                raidEndTrack = deathMusicList[0];
            }
            else if (soundType == EEndGameSoundType.ArenaWin && !extractMusicList.IsNullOrEmpty())
            {
                raidEndTrack = extractMusicList[0];
                
            }
            else
            {
                return;
            }
            raidEndClip = RequestAudioClip(raidEndTrack);
            string trackPath = Path.GetFileName(raidEndTrack);
            Logger.LogInfo(trackPath + " assigned to Death Music");
        }

        private AudioClip RequestAudioClip(string path)
        {
            string extension = Path.GetExtension(path);
            Dictionary<string, AudioType> audioType = new Dictionary<string, AudioType>
            {
                [".wav"] = AudioType.WAV,
                [".ogg"] = AudioType.OGGVORBIS,
                [".mp3"] = AudioType.MPEG
            };
            UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, audioType[extension]);
            UnityWebRequestAsyncOperation sendWeb = uwr.SendWebRequest();

            while (!sendWeb.isDone)

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Logger.LogError("ChangeDeathMusic: Failed To Fetch Audio Clip");
                return null;
            }
            AudioClip audioclip = DownloadHandlerAudioClip.GetContent(uwr);
            return audioclip;
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