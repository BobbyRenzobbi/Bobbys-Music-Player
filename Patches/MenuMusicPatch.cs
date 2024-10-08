﻿using SPT.Reflection.Patching;
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

    public class MenuMusicPatch : ModulePatch
    {
        internal static int trackCounter;
        internal static List<string> menuTrackList = new List<string>();
        internal static List<AudioClip> trackArray = new List<AudioClip>();
        private static List<string> trackListToPlay = new List<string>();
        private static List<string> trackNamesArray = new List<string>();
        internal static bool HasReloadedAudio = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GUISounds), nameof(GUISounds.method_3));
        }

        internal static async void LoadAudioClips()
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
                int nextRandom = Plugin.rand.Next(trackListToPlay.Count);
                string track = trackListToPlay[nextRandom];
                string trackPath = Path.GetFileName(track);
                Plugin plugin = new Plugin();
                AudioClip unityAudioClip = await plugin.AsyncRequestAudioClip(track);
                trackArray.Add(unityAudioClip);
                trackNamesArray.Add(trackPath);
                trackListToPlay.Remove(track);
                totalLength += trackArray.Last().length;
                Plugin.LogSource.LogInfo(trackPath + " has been loaded and added to playlist");
            } while ((totalLength < targetLength) && (!trackListToPlay.IsNullOrEmpty()));
        }

        [PatchPrefix]
        static bool Prefix(AudioSource ___audioSource_3, AudioClip[] ___audioClip_0)
        {
            Audio.menuMusicAudioSource = ___audioSource_3;
            if (menuTrackList.IsNullOrEmpty())
            {
                if (trackArray.IsNullOrEmpty())
                {
                    int[] randomArray = new int[___audioClip_0.Length];
                    Plugin.LogSource.LogInfo("Starting 'for loop'");
                    for (int i = 0; i < ___audioClip_0.Length - 1; i++)
                    {
                        Plugin.LogSource.LogInfo("for loop iteration " + i);
                        int randomInt;
                        do
                        {
                            Plugin.LogSource.LogInfo("choosing randomInt");
                            randomInt = Plugin.rand.Next(___audioClip_0.Length);
                        } while (randomArray.Contains(randomInt));
                        randomArray[i] = randomInt;
                        trackArray.Add(___audioClip_0[randomInt]);
                    }
                }
                Singleton<GUISounds>.Instance.method_4();
                Audio.menuMusicAudioSource.clip = trackArray[trackCounter];
                Audio.menuMusicAudioSource.Play();
                trackCounter++;
                MenuMusicJukebox.menuMusicCoroutine = StaticManager.Instance.WaitSeconds(Audio.menuMusicAudioSource.clip.length, new Action(Singleton<GUISounds>.Instance.method_3));
                if (trackCounter >= trackArray.Count)
                {
                    trackCounter = 0;
                }
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
                MenuMusicJukebox.menuMusicCoroutine = StaticManager.Instance.WaitSeconds(Audio.menuMusicAudioSource.clip.length, new Action(Singleton<GUISounds>.Instance.method_3));
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
            if (MenuMusicJukebox.menuMusicCoroutine == null)
            {
                return false;
            }
            StaticManager.Instance.StopCoroutine(MenuMusicJukebox.menuMusicCoroutine);
            MenuMusicJukebox.menuMusicCoroutine = null;
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
            MenuMusicJukebox.menuMusicCoroutine = StaticManager.Instance.WaitSeconds(transitionTime, new Action(Singleton<GUISounds>.Instance.method_4));
            return false;
        }

    }
}