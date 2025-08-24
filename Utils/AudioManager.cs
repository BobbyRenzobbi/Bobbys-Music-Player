using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BobbysMusicPlayer.Data;
using BobbysMusicPlayer.Extensions;
using BobbysMusicPlayer.Models;
using BobbysMusicPlayer.Patches;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using UnityEngine;
using UnityEngine.Networking;
using static UnityEngine.Random;

namespace BobbysMusicPlayer.Utils
{
    public class AudioManager
    {
        public AudioSource SoundtrackAudioSource;
        public AudioSource SpawnAudioSource;
        public AudioSource CombatAudioSource;
        public AudioSource MenuMusicAudioSource;
        
        public bool HasStartedLoadingAudio;
        public bool HasFinishedLoadingAudio;
        public bool SpawnTrackHasPlayed;
        
        private List<string> _combatMusicTrackList = new();
        private List<string> _ambientTrackListToPlay = new();
        private List<string> _spawnTrackList = new();
        private List<string> _defaultTrackList = new();
        
        private List<AudioClip> _combatMusicClipList = new();
        private List<AudioClip> _spawnTrackClipList = new();
        public List<string> AmbientTrackNamesArray = new();
        public List<AudioClip> AmbientTrackArray = new();
        
        public float Lerp;
        public float CurrentEnvironmentMultiplier;
        public float CombatTimer;
        public float SoundtrackVolume;
        public float SpawnMusicVolume;
        public float CombatMusicVolume;
        public float HeadsetMultiplier = 1f;
        
        private float _targetEnvironmentMultiplier;
        private float _targetHeadsetMultiplier;
        private EnvironmentType _lastEnvironment;

        public void Init(GameObject mainObj)
        {
            if (SoundtrackAudioSource == null)
            {
                SoundtrackAudioSource = mainObj.AddComponent<AudioSource>();
                SpawnAudioSource = mainObj.AddComponent<AudioSource>();
                CombatAudioSource = mainObj.AddComponent<AudioSource>();
                BobbysMusicPlayerPlugin.LogSource.LogWarning("AudioSources added to game");
            }

            LoadMusic();
        }
        
        public void SetClip(AudioSource audiosource, AudioClip clip)
        {
            audiosource.clip = clip;
        }

        private void AdjustVolume(AudioSource audiosource, float volume)
        {
            audiosource.volume = volume;
        }
        
        #region UpdateMethods
        
        /// <summary>
        /// Dynamic adjust volume
        /// </summary>
        public void VolumeSetter()
        {
            // Next two lines are taken from Fontaine's Realism Mod. Credit to him
            CompoundItem headwear = Singleton<GameWorld>.Instance.MainPlayer.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as CompoundItem;
            HeadphonesItemClass headset = Singleton<GameWorld>.Instance.MainPlayer.Equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem as HeadphonesItemClass ?? headwear?.GetAllItemsFromCollection().OfType<HeadphonesItemClass>().FirstOrDefault();
            
            _targetHeadsetMultiplier = headset != null ? SettingsModel.Instance.HeadsetMultiplier.Value : 1f;
            
            // Fix sharp switching by headset volume
            HeadsetMultiplier = HeadsetMultiplier.SmoothTowards(_targetHeadsetMultiplier,
                Time.deltaTime, SettingsModel.Instance.TransitionHeadsetSpeed.Value);
            
            var currentEnvironment = Singleton<GameWorld>.Instance.MainPlayer.Environment;

            _lastEnvironment = currentEnvironment;
            _targetEnvironmentMultiplier = GlobalData.EnvironmentDict[currentEnvironment];
            
            // Fix sharp switching of Environment
            CurrentEnvironmentMultiplier = CurrentEnvironmentMultiplier.SmoothTowards(_targetEnvironmentMultiplier,
                Time.deltaTime, SettingsModel.Instance.TransitionEnvSpeed.Value);
            
            // Each of the in-raid AudioSources' volumes are calculated by multiplying their configurable volumes with the indoor multiplier and the headset multiplier.
            SoundtrackVolume = SettingsModel.Instance.SoundtrackVolume.Value * CurrentEnvironmentMultiplier * HeadsetMultiplier;
            SpawnMusicVolume = SettingsModel.Instance.SpawnMusicVolume.Value * CurrentEnvironmentMultiplier * HeadsetMultiplier;
            CombatMusicVolume = SettingsModel.Instance.CombatMusicVolume.Value * CurrentEnvironmentMultiplier * HeadsetMultiplier;

            // We check if the combat AudioSource is playing so that the CombatLerp method can do its job adjusting the ambient soundtrack and spawn music AudioSources
            if (!CombatAudioSource.isPlaying)
            {
                AdjustVolume(SoundtrackAudioSource, SoundtrackVolume); 
                AdjustVolume(SpawnAudioSource, SpawnMusicVolume);
            }
            if (Lerp >= 1)
            {
                AdjustVolume(CombatAudioSource, CombatMusicVolume);
            }   
        }
        
        /// <summary>
        /// Process combat music side 
        /// </summary>
        public void CombatMusic()
        {
            if (!_combatMusicTrackList.IsNullOrEmpty())
            {
                if (CombatTimer > 0)
                {
                    if (!CombatAudioSource.isPlaying && CombatAudioSource.loop == false)
                    {
                        CombatAudioSource.loop = true;
                        BobbysMusicPlayerPlugin.LogSource.LogInfo("[COMBAT] Play");
                        CombatAudioSource.Play();
                    }
                    if (Lerp <= 1)
                    {
                        CombatLerp();
                        Lerp += Time.deltaTime / SettingsModel.Instance.CombatInFader.Value;
                    }
                    CombatTimer -= Time.deltaTime;
                }
                // When the combat timer runs out, the AudioSource will only stop playing once it's done fading out.
                // If the player re-enters combat in the middle of fading out, the music will smoothly fade back in from the volume it faded out to.
                else if (CombatTimer <= 0)
                {
                    if (CombatAudioSource.isPlaying)
                    {
                        CombatTimer = 0f;
                        CombatLerp();
                        Lerp -= Time.deltaTime / SettingsModel.Instance.CombatOutFader.Value;
                        if (Lerp <= 0)
                        {
                            CombatAudioSource.loop = false;
                            BobbysMusicPlayerPlugin.LogSource.LogInfo("[COMBAT] Stop");
                            CombatAudioSource.Stop();
                            // The combat AudioSource's clip will be randomly selected each time the combat music stops
                            CombatAudioSource.clip = _combatMusicClipList[Range(0, _combatMusicClipList.Count)];
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Play sound when spawn
        /// </summary>
        public void PlaySpawnMusic(bool check = true)
        {
            // Early return if no tracks available
            if (_spawnTrackClipList.IsNullOrEmpty())
            {
                BobbysMusicPlayerPlugin.LogSource.LogInfo("Empty spawn track list");
                SpawnTrackHasPlayed = true;
                return;
            }
            
            // Additional safety check for debug mode
            if (!check && _spawnTrackClipList.IsNullOrEmpty())
            {
                BobbysMusicPlayerPlugin.LogSource.LogInfo("Empty spawn track list");
                SpawnTrackHasPlayed = true;
                return;
            }
            
            AudioClip clip = _spawnTrackClipList[Range(0, _spawnTrackClipList.Count)];
            if (clip == null)
            {
                BobbysMusicPlayerPlugin.LogSource.LogInfo("[SPAWN] WTF?");
            }
            SpawnAudioSource.clip = clip;
            SpawnAudioSource.Play();
            BobbysMusicPlayerPlugin.LogSource.LogInfo("[SPAWN] Play");
            SpawnTrackHasPlayed = true;
        }
        
        /// <summary>
        /// Process combat music lerp side 
        /// </summary>
        private void CombatLerp()
        {
            AdjustVolume(CombatAudioSource, Mathf.Lerp(0f, CombatMusicVolume, Lerp));
            AdjustVolume(SoundtrackAudioSource, Mathf.Lerp(SoundtrackVolume, SettingsModel.Instance.AmbientCombatMultiplier.Value*SoundtrackVolume, Lerp));
            AdjustVolume(SpawnAudioSource, Mathf.Lerp(SpawnMusicVolume, SettingsModel.Instance.AmbientCombatMultiplier.Value*SpawnMusicVolume, Lerp));
        }
        
        #endregion

        #region Loading

        /// <summary>
        /// Load paths for all music 
        /// </summary>
        private void LoadMusic()
        {
            MenuMusicPatch.menuTrackList.AddRange(Directory.GetFiles(PathData.CustomMenuMusicSounds));
            
            //This if statement exists just in case some people install outdated music packs by mistake
            if (MenuMusicPatch.menuTrackList.IsNullOrEmpty() && Directory.Exists(PathData.CustomMenuMusicSoundsMissing))
            {
                MenuMusicPatch.menuTrackList.AddRange(Directory.GetFiles(PathData.CustomMenuMusicSoundsMissing));
            }
            
            _defaultTrackList.AddRange(Directory.GetFiles(PathData.SoundtrackDefault));
            if (_defaultTrackList.IsNullOrEmpty() && Directory.Exists(PathData.SoundtrackSoundsMissing))
            {
                _defaultTrackList.AddRange(Directory.GetFiles(PathData.SoundtrackSoundsMissing));
            }
            
            _combatMusicTrackList.AddRange(Directory.GetFiles(PathData.SoundtrackCombat));
            
            _spawnTrackList.AddRange(Directory.GetFiles(PathData.SoundtrackSpawn));
            
            RaidEndMusicPatch.DeathMusicList.AddRange(Directory.GetFiles(PathData.SoundtrackDeath));
            RaidEndMusicPatch.ExtractMusicList.AddRange(Directory.GetFiles(PathData.SoundtrackExtract));
            
            var counter = 0;
            foreach (var dir in GlobalData.UISoundsDir)
            {
                // Each element of uiSounds is a List of strings so that users can add as few or as many sounds as they want to a given folder
                UISoundsPatch.UISounds[counter] = new List<string>();
                UISoundsPatch.UISounds[counter].AddRange(Directory.GetFiles(PathData.SoundtrackUI + dir));
                counter++;
            }
        }
        
        /// <summary>
        /// This method gets called once when loading into a raid. It makes sure every AudioClip is ready to play in the raid
        /// </summary>
        public async void PrepareRaidAudioClips()
        {
            try
            {
                if (!HasStartedLoadingAudio)
                {
                    HasStartedLoadingAudio = true;
                
                    if (!_defaultTrackList.IsNullOrEmpty())
                    {
                        LoadAmbientSoundtrackClips();
                    }
                
                    if (!_spawnTrackList.IsNullOrEmpty())
                    {
                        // Check if spawn music is cached
                        if (BobbysMusicPlayerPlugin.Instance.GetCache().IsPlaylistCached("spawn"))
                        {
                            LoadCachedSpawnMusic();
                        }
                        else
                        {
                            await LoadSpawnMusicFromFiles();
                        }
                    }
                
                    if (!_combatMusicTrackList.IsNullOrEmpty())
                    {
                        // Check if combat music is cached
                        if (BobbysMusicPlayerPlugin.Instance.GetCache().IsPlaylistCached("combat"))
                        {
                            LoadCachedCombatMusic();
                        }
                        else
                        {
                            await LoadCombatMusicFromFiles();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                BobbysMusicPlayerPlugin.LogSource.LogError($"[PrepareRaidAudioClips] Throw error {e}");
            }
        }
        
        /// <summary>
        /// Load spawn music from cache
        /// </summary>
        private void LoadCachedSpawnMusic()
        {
            var cachedSpawnMusic = BobbysMusicPlayerPlugin.Instance.GetCache().GetCachedPlaylist("spawn");
            _spawnTrackClipList = new List<AudioClip>(cachedSpawnMusic);
            SpawnTrackHasPlayed = false;
            BobbysMusicPlayerPlugin.LogSource.LogInfo($"[AUDIO MANAGER] Loaded {_spawnTrackClipList.Count} spawn music tracks from cache");
        }
        
        /// <summary>
        /// Load spawn music from files and cache them
        /// </summary>
        private async Task LoadSpawnMusicFromFiles()
        {
            _spawnTrackClipList.Clear();
            foreach (var track in _spawnTrackList)
            {
                var audioClip = await BobbysMusicPlayerPlugin.Instance.GetCache().GetOrCacheAudioClip(track);
                if (audioClip != null)
                {
                    _spawnTrackClipList.Add(audioClip);
                    BobbysMusicPlayerPlugin.LogSource.LogInfo("[AUDIO MANAGER] Using cached spawnTrackClip: " + Path.GetFileName(track));
                }
            }
            SpawnTrackHasPlayed = false;
            
            BobbysMusicPlayerPlugin.Instance.GetCache().CachePlaylist("spawn", new List<AudioClip>(_spawnTrackClipList));
        }
        
        /// <summary>
        /// Load combat music from cache
        /// </summary>
        private void LoadCachedCombatMusic()
        {
            var cachedCombatMusic = BobbysMusicPlayerPlugin.Instance.GetCache().GetCachedPlaylist("combat");
            _combatMusicClipList = new List<AudioClip>(cachedCombatMusic);
            
            // Reset combat state
            CombatTimer = 0f;
            Lerp = 0;
            CombatAudioSource.Stop();
            CombatAudioSource.loop = false;
            
            if (_combatMusicClipList.Count > 0)
            {
                CombatAudioSource.clip = _combatMusicClipList[Range(0, _combatMusicClipList.Count)];
                BobbysMusicPlayerPlugin.LogSource.LogInfo($"[AUDIO MANAGER] Loaded {_combatMusicClipList.Count} combat music tracks from cache");
            }
        }
        
        /// <summary>
        /// Load combat music from files and cache them
        /// </summary>
        private async Task LoadCombatMusicFromFiles()
        {
            BobbysMusicPlayerPlugin.LogSource.LogInfo("[AUDIO MANAGER] Loading combat music from cache");
            
            // Reset combat state
            CombatTimer = 0f;
            Lerp = 0;
            CombatAudioSource.Stop();
            CombatAudioSource.loop = false;
            
            _combatMusicClipList.Clear();
            foreach (var track in _combatMusicTrackList)
            {
                var audioClip = await BobbysMusicPlayerPlugin.Instance.GetCache().GetOrCacheAudioClip(track);
                if (audioClip != null)
                {
                    _combatMusicClipList.Add(audioClip);
                }
            }
            
            if (_combatMusicClipList.Count > 0)
            {
                CombatAudioSource.clip = _combatMusicClipList[Range(0, _combatMusicClipList.Count)];
                BobbysMusicPlayerPlugin.LogSource.LogInfo($"[AUDIO MANAGER] Combat music loaded from cache! {CombatAudioSource.clip.length}");
            }
            
            BobbysMusicPlayerPlugin.Instance.GetCache().CachePlaylist("combat", new List<AudioClip>(_combatMusicClipList));
        }
        
        /// <summary>
        /// Load ambient OST
        /// </summary>
        private async void LoadAmbientSoundtrackClips()
        {
            float totalLength = 0f;
            float targetLength = 60f * SettingsModel.Instance.SoundtrackLength.Value;
            
            HasFinishedLoadingAudio = false;
            
            AmbientTrackArray.Clear();
            AmbientTrackNamesArray.Clear();
            _ambientTrackListToPlay.Clear();
            
            BobbysMusicPlayerPlugin.LogSource.LogInfo("Map is " + Singleton<GameWorld>.Instance.MainPlayer.Location + ".");
            
            if (GlobalData.MapDictionary[Singleton<GameWorld>.Instance.MainPlayer.Location].IsNullOrEmpty() || SettingsModel.Instance.SoundtrackPlaylist.Value == ESoundtrackPlaylist.DefaultPlaylistOnly)
            {
                _ambientTrackListToPlay.AddRange(_defaultTrackList);
            }
            else if (SettingsModel.Instance.SoundtrackPlaylist.Value == ESoundtrackPlaylist.CombinedPlaylists)
            {
                _ambientTrackListToPlay.AddRange(_defaultTrackList);
                _ambientTrackListToPlay.AddRange(GlobalData.MapDictionary[Singleton<GameWorld>.Instance.MainPlayer.Location]);
            }
            else if (SettingsModel.Instance.SoundtrackPlaylist.Value == ESoundtrackPlaylist.MapSpecificPlaylistOnly)
            {
                _ambientTrackListToPlay.AddRange(GlobalData.MapDictionary[Singleton<GameWorld>.Instance.MainPlayer.Location]);
            }
            while ((totalLength < targetLength) && (!_ambientTrackListToPlay.IsNullOrEmpty()))
            {
                int nextRandom = Range(0, _ambientTrackListToPlay.Count);
                string track = _ambientTrackListToPlay[nextRandom];
                string trackName = Path.GetFileName(track);
                // Use cached clip instead of reloading
                AudioClip unityAudioClip = await BobbysMusicPlayerPlugin.Instance.GetCache().GetOrCacheAudioClip(track);
                if (unityAudioClip != null)
                {
                    AmbientTrackArray.Add(unityAudioClip);
                    AmbientTrackNamesArray.Add(trackName);
                    _ambientTrackListToPlay.Remove(track);
                    
                    // Adding the length of each track to totalLength makes sure that the mod loads the minimum number of random tracks to meet the target length.
                    totalLength += unityAudioClip.length;
                    BobbysMusicPlayerPlugin.LogSource.LogInfo("[LoadAmbientSoundtrackClips] "+ trackName + $" has been loaded from cache and added to playlist. Lenght: {unityAudioClip.length}");
                }
                else
                {
                    // Remove failed track from list to avoid infinite loop
                    _ambientTrackListToPlay.Remove(track);
                }
            }
            HasFinishedLoadingAudio = true;
        }
        
        /// <summary>
        /// Async load audio file as AudioClip without cache
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static async Task<AudioClip> AsyncRequestAudioClip(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();

            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, GlobalData.AudioTypes[extension]))
            {
                var operation = uwr.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    BobbysMusicPlayerPlugin.LogSource.LogError(
                        $"Soundtrack: Failed to fetch audio clip -> '{path}', Error: {uwr.error}"
                    );
                    return null;
                }

                return DownloadHandlerAudioClip.GetContent(uwr);
            }
        }

        
        /// <summary>
        /// Sync load audio file as AudioClip without cache
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static AudioClip RequestAudioClip(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();

            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, GlobalData.AudioTypes[extension]))
            {
                var operation = uwr.SendWebRequest();

                // Can do big freeze main thread if file big
                while (!operation.isDone) {}

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    BobbysMusicPlayerPlugin.LogSource.LogError(
                        $"Soundtrack: Failed to fetch audio clip -> '{path}', Error: {uwr.error}"
                    );
                    return null;
                }

                return DownloadHandlerAudioClip.GetContent(uwr);
            }
        }


        #endregion
    }
}