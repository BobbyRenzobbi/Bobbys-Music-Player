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
                        BobbysMusicPlayerPlugin.LogSource.LogInfo("Combat music started");
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
            if (check)
            {
                if (SpawnTrackHasPlayed || _spawnTrackClipList.IsNullOrEmpty())
                {
                    return;
                }
            }
            else
            {
                if(_spawnTrackClipList.IsNullOrEmpty())
                {
                    BobbysMusicPlayerPlugin.LogSource.LogInfo("Empty spawn track list");
                    return;
                }
            }
            
            SpawnAudioSource.clip = _spawnTrackClipList[Range(0, _spawnTrackClipList.Count)];
            BobbysMusicPlayerPlugin.LogSource.LogInfo("spawnAudioSource.clip assigned to spawnTrackClip");
            SpawnAudioSource.Play();
            BobbysMusicPlayerPlugin.LogSource.LogInfo("spawnAudioSource playing");
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
                        _spawnTrackClipList.Clear();
                        foreach (var track in _spawnTrackList)
                        {
                            _spawnTrackClipList.Add(await AsyncRequestAudioClip(track));
                            BobbysMusicPlayerPlugin.LogSource.LogInfo("RequestAudioClip called for spawnTrackClip");
                        }
                        SpawnTrackHasPlayed = false;
                    }
                
                    if (!_combatMusicTrackList.IsNullOrEmpty())
                    {
                        BobbysMusicPlayerPlugin.LogSource.LogInfo("Load music to combat");
                    
                        // The next 4 lines prevent any issues that could be caused by exiting a raid before the combat timer ends
                        CombatTimer = 0f;
                        Lerp = 0;
                        CombatAudioSource.Stop();
                        CombatAudioSource.loop = false;
                    
                        _combatMusicClipList.Clear();
                        foreach (var track in _combatMusicTrackList)
                        {
                            _combatMusicClipList.Add(await AsyncRequestAudioClip(track));
                        }
                    
                        CombatAudioSource.clip = _combatMusicClipList[Range(0, _combatMusicClipList.Count)];
                        BobbysMusicPlayerPlugin.LogSource.LogInfo($"Music in combat loaded! {CombatAudioSource.clip.length}");
                    }
                }
            }
            catch (Exception e)
            {
                BobbysMusicPlayerPlugin.LogSource.LogError($"[PrepareRaidAudioClips] Throw error {e}");
            }
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
                AudioClip unityAudioClip = await AsyncRequestAudioClip(track);
                AmbientTrackArray.Add(unityAudioClip);
                AmbientTrackNamesArray.Add(trackName);
                _ambientTrackListToPlay.Remove(track);
                
                // Adding the length of each track to totalLength makes sure that the mod loads the minimum number of random tracks to meet the target length.
                totalLength += AmbientTrackArray.Last().length;
                BobbysMusicPlayerPlugin.LogSource.LogInfo(trackName + " has been loaded and added to playlist");
            }
            HasFinishedLoadingAudio = true;
        }
        
        /// <summary>
        /// Async load audio file as AudioClip
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
        /// Sync load audio file as AudioClip
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