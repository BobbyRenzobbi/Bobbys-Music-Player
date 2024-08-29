using SPT.Reflection.Patching;
using EFT.UI;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using Comfort.Common;
using System.Linq;
using EFT;
using System;

namespace BobbysMusicPlayer.Patches
{
    
    public class CustomMusicPatch : ModulePatch
    {
        internal static int trackCounter;
        private static System.Random rand = new System.Random();
        internal static List<string> menuTrackList = new List<string>();
        internal static List<AudioClip> trackArray = new List<AudioClip>();
        private static List<string> trackListToPlay = new List<string>();
        private static List<string> trackNamesArray = new List<string>();
        internal static bool HasReloadedAudio = false;
        Plugin plugin = new Plugin();

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GUISounds), nameof(GUISounds.method_3));
        }

        internal async void LoadAudioClips()
        {
            float totalLength = 0;
            HasReloadedAudio = true;
            if (menuTrackList.IsNullOrEmpty())
            {
                return;
            }
            trackArray.Clear();
            trackNamesArray.Clear();
            trackListToPlay.Clear();
            trackListToPlay.AddRange(menuTrackList);
            float targetLength = Plugin.CustomMenuMusicLength.Value * 60f;
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
            Plugin.LogSource.LogInfo("trackArray stored in storeTrackArray");
        }

        [PatchPrefix]
        static bool Prefix(AudioSource ___audioSource_3)
        {
            if (menuTrackList.IsNullOrEmpty())
            {
                return true;
            }
            Audio.menuMusicAudioSource = ___audioSource_3;
            if (trackArray.Count == 1)
            {
                trackCounter = 0;
            }
            Audio.menuMusicAudioSource.clip = trackArray[trackCounter];
            Audio.menuMusicAudioSource.Play();
            Plugin.LogSource.LogInfo("Playing " + trackNamesArray[trackCounter]);
            trackCounter++;
            CustomMusicJukebox.menuMusicCoroutine = StaticManager.Instance.WaitSeconds(Audio.menuMusicAudioSource.clip.length, new Action(Singleton<GUISounds>.Instance.method_3));
            if (trackCounter >= trackArray.Count)
            {
                trackCounter = 0;
            }
            return false;
            
            
        }
    }
}