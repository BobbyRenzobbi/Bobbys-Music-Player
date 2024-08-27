using SPT.Reflection.Patching;
using EFT.UI;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using Comfort.Common;
using System.Linq;

namespace BobbysMusicPlayer.Patches
{
    public class CustomMusicPatch : ModulePatch
    {
        private static List<AudioClip> audioClips = new List<AudioClip>();
        private static System.Random rand = new System.Random();
        internal static List<string> menuTrackList = new List<string>();
        private static List<AudioClip> trackArray = new List<AudioClip>();
        private static List<AudioClip> storedTrackArray = new List<AudioClip>();
        private static List<string> trackListToPlay = new List<string>();
        private static float targetLength = 3600f;
        private static float totalLength = 0f;
        private static List<string> trackNamesArray = new List<string>();
        private static List<string> storedTrackNamesArray = new List<string>();
        internal static bool HasReloadedAudio = false;
        Plugin plugin = new Plugin();

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GUISounds), nameof(GUISounds.method_3));
        }

        internal async void LoadAudioClips()
        {
            HasReloadedAudio = true;
            if (menuTrackList.IsNullOrEmpty())
            {
                return;
            }
            trackArray.Clear();
            storedTrackArray.Clear();
            trackNamesArray.Clear();
            storedTrackNamesArray.Clear();
            trackListToPlay.Clear();
            trackListToPlay.AddRange(menuTrackList);
            do
            {
                int nextRandom = rand.Next(trackListToPlay.Count);
                string track = trackListToPlay[nextRandom];
                string trackPath = Path.GetFileName(track);
                AudioClip unityAudioClip = await plugin.AsyncRequestAudioClip(track);
                trackArray.Add(unityAudioClip);
                trackNamesArray.Add(trackPath);
                trackListToPlay.Remove(track);
                totalLength += trackArray.Last().length;
                Plugin.LogSource.LogInfo(trackPath + " has been loaded and added to playlist");
            } while ((totalLength < targetLength) && (!trackListToPlay.IsNullOrEmpty()));
            storedTrackArray.AddRange(trackArray);
            storedTrackNamesArray.AddRange(trackNamesArray);
            Plugin.LogSource.LogInfo("trackArray stored in storeTrackArray");
            totalLength = 0;
        }

        [PatchPrefix]
        static bool Prefix()
        {
            if (menuTrackList.IsNullOrEmpty())
            {
                return true;
            }
            audioClips.Clear();
            audioClips.Add(trackArray[0]);
            audioClips.Add(trackArray[0]);
            trackArray.RemoveAt(0);
            //Credit to SamSWAT for discovering that the game loads infinitely if the audioClip_0 array has only one element
            if (trackArray.IsNullOrEmpty())
            {
                trackArray.AddRange(storedTrackArray);
            }
            Traverse.Create(Singleton<GUISounds>.Instance).Field("audioClip_0").SetValue(audioClips.ToArray());
            Plugin.LogSource.LogInfo("Playing " + trackNamesArray[0]);
            trackNamesArray.RemoveAt(0);
            if (trackNamesArray.IsNullOrEmpty())
            {
                trackNamesArray.AddRange(storedTrackNamesArray);
            }
            return true;
        }
    }
}