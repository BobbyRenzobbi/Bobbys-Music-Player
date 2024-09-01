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
using static System.TimeZoneInfo;

namespace BobbysMusicPlayer.Patches
{

    public class MenuMusicPatch : ModulePatch
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
        static bool Prefix(AudioSource ___audioSource_3, AudioClip[] ___audioClip_0, int ___int_0)
        {
            Audio.menuMusicAudioSource = ___audioSource_3;
            if (menuTrackList.IsNullOrEmpty())
            {
                int num;
                do
                {
                    num = global::UnityEngine.Random.Range(0, ___audioClip_0.Length);
                }
                while (___int_0 == num);
                ___int_0 = num;
                AudioClip audioClip = ___audioClip_0[___int_0];
                Singleton<GUISounds>.Instance.method_4();
                ___audioSource_3.clip = audioClip;
                ___audioSource_3.Play();
                CustomMusicJukebox.menuMusicCoroutine = StaticManager.Instance.WaitSeconds(audioClip.length, new Action(Singleton<GUISounds>.Instance.method_3));
            }
            else
            {
                if (trackArray.Count == 1)
                {
                    trackCounter = 0;
                }
                Singleton<GUISounds>.Instance.method_4();
                Audio.menuMusicAudioSource.clip = trackArray[trackCounter];
                Audio.menuMusicAudioSource.Play();
                Plugin.LogSource.LogInfo("Playing " + trackNamesArray[trackCounter]);
                trackCounter++;
                CustomMusicJukebox.menuMusicCoroutine = StaticManager.Instance.WaitSeconds(Audio.menuMusicAudioSource.clip.length, new Action(Singleton<GUISounds>.Instance.method_3));
                if (trackCounter >= trackArray.Count)
                {
                    trackCounter = 0;
                }
            }
            return false;
        }
    }
    public class MenuMusicMethod5Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GUISounds), nameof(GUISounds.method_5));
        }
        [PatchPrefix]
        static bool Prefix()
        {
            Plugin.LogSource.LogInfo("GUISounds.method_5 called");
            if (CustomMusicJukebox.menuMusicCoroutine == null)
            {
                return false;
            }
            StaticManager.Instance.StopCoroutine(CustomMusicJukebox.menuMusicCoroutine);
            CustomMusicJukebox.menuMusicCoroutine = null;
            return false;
        }
    }
    public class StopMenuMusicPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GUISounds), nameof(GUISounds.StopMenuBackgroundMusicWithDelay));
        }
        [PatchPrefix]
        static bool Prefix(float transitionTime)
        {
            Plugin.LogSource.LogInfo("GUISounds.StopMenuBackgroundMusicWithDelay called");
            Singleton<GUISounds>.Instance.method_5();
            CustomMusicJukebox.menuMusicCoroutine = StaticManager.Instance.WaitSeconds(transitionTime, new Action(Singleton<GUISounds>.Instance.method_4));
            return false;
        }

    }
}