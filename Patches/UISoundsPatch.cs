using SPT.Reflection.Patching;
using EFT.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
        private static List<AudioClipData>[] UISoundsClips = new List<AudioClipData>[8];
        
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
            __result = audioClipArray[Range(0, audioClipArray.Count)].Get();
            return false;
        }
        
        /// <summary>
        /// Each element of uiSoundsClips is a List of AudioClips since we want players to be able to import as many sounds as they want per folder.
        /// </summary>
        internal static async void LoadUIClips()
        {
            // Check if already cached
            if (BobbysMusicPlayerPlugin.Instance.GetCache().IsPlaylistCached("ui"))
            {
                LoadCachedUIClips();
                return;
            }
            
            BobbysMusicPlayerPlugin.LogSource.LogInfo("[UI SOUNDS] Loading UI sounds from files...");
            
            int counter = 0;
            foreach (var list in UISounds)
            {
                UISoundsClips[counter] = new List<AudioClipData>();
                foreach (var track in list)
                {
                    // Use cached clip instead of reloading
                    var audioClip = await BobbysMusicPlayerPlugin.Instance.GetCache().GetOrCacheAudioClipData(track);
                    if (audioClip != null)
                    {
                        UISoundsClips[counter].Add(audioClip);
                        BobbysMusicPlayerPlugin.LogSource.LogInfo(Path.GetFileName(track) + " assigned to " + GlobalData.UISoundsDir[counter]);
                    }
                }
                counter++;
            }
            
            // Cache all UI sounds for future use
            var allUISounds = new List<AudioClipData>();
            foreach (var clipList in UISoundsClips)
            {
                if (clipList != null)
                {
                    allUISounds.AddRange(clipList);
                }
            }
            BobbysMusicPlayerPlugin.Instance.GetCache().CachePlaylist("ui", allUISounds);
            
            BobbysMusicPlayerPlugin.LogSource.LogInfo($"[UI SOUNDS] Loaded and cached {allUISounds.Count} UI sound clips");
        }
        
        /// <summary>
        /// Load UI sounds from cache without reloading files
        /// </summary>
        internal static void LoadCachedUIClips()
        {
            BobbysMusicPlayerPlugin.LogSource.LogInfo("[UI SOUNDS] Loading UI sounds from cache...");
            
            // Get all cached UI sounds
            var allCachedSounds = BobbysMusicPlayerPlugin.Instance.GetCache().GetCachedPlaylist("ui");
            if (allCachedSounds.IsNullOrEmpty())
            {
                BobbysMusicPlayerPlugin.LogSource.LogWarning("[UI SOUNDS] No cached UI sounds found, falling back to file loading");
                BobbysMusicPlayerPlugin.Instance.GetCache().ClearPlaylist("ui");
                LoadUIClips();
                return;
            }
            
            var cachedSoundsLookup = new Dictionary<string, AudioClipData>();
            foreach (var clip in allCachedSounds)
            {
                if (clip != null && !string.IsNullOrEmpty(clip.Get().name))
                {
                    cachedSoundsLookup[clip.Get().name] = clip;
                }
            }
            
            UISoundsClips = new List<AudioClipData>[8];
            int counter = 0;
            
            foreach (var list in UISounds)
            {
                UISoundsClips[counter] = new List<AudioClipData>();
                
                foreach (var track in list)
                {
                    var fileName = Path.GetFileName(track);
                    if (cachedSoundsLookup.TryGetValue(fileName, out var cachedClip))
                    {
                        UISoundsClips[counter].Add(cachedClip);
                    }
                    else
                    {
                        BobbysMusicPlayerPlugin.LogSource.LogWarning($"[UI SOUNDS] Cached clip not found for {fileName}, will load from file");
                        
                        var tempInt = counter;
                        // Fallback: load from file if not in cache
                        _ = Task.Run(async () =>
                        {
                            var audioClip = await BobbysMusicPlayerPlugin.Instance.GetCache().GetOrCacheAudioClipData(track);
                            if (audioClip != null)
                            {
                                UISoundsClips[tempInt].Add(audioClip);
                            }
                        });
                    }
                }
                
                counter++;
            }
            
            BobbysMusicPlayerPlugin.LogSource.LogInfo($"[UI SOUNDS] Loaded {UISoundsClips.Sum(list => list?.Count ?? 0)} UI sound clips from cache");
        }
    }
}