using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BobbysMusicPlayer.Patches;
using Comfort.Common;
using EFT;
using EFT.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace BobbysMusicPlayer
{
    public class CustomMusicJukebox : MonoBehaviour
    {
        internal static Coroutine menuMusicCoroutine;
        private static bool paused = false;
        private static float pausedTime = 0f;
        public static void menuMusicControls()
        {
            if (!SoundtrackJukebox.soundtrackCalled)
            {
                if (Input.GetKeyDown(Plugin.pauseTrack.Value.MainKey) && Audio.menuMusicAudioSource.isPlaying)
                {
                    Audio.menuMusicAudioSource.Pause();
                    StaticManager.Instance.StopCoroutine(menuMusicCoroutine);
                    pausedTime = Audio.menuMusicAudioSource.clip.length - Audio.menuMusicAudioSource.time;
                    paused = true;
                }
                else if (Input.GetKeyDown(Plugin.pauseTrack.Value.MainKey) && paused)
                {
                    Audio.menuMusicAudioSource.UnPause();
                    menuMusicCoroutine = StaticManager.Instance.WaitSeconds(pausedTime, new Action(Singleton<GUISounds>.Instance.method_3));
                    paused = false;
                }
                if (Input.GetKeyDown(Plugin.restartTrack.Value.MainKey))
                {
                    Audio.menuMusicAudioSource.Stop();
                    if (CustomMusicPatch.trackCounter != 0)
                    {
                        CustomMusicPatch.trackCounter--;
                    }
                    else
                    {
                        CustomMusicPatch.trackCounter = CustomMusicPatch.trackArray.Count - 1;
                    }
                    StaticManager.Instance.StopCoroutine(menuMusicCoroutine);
                    paused = false;
                    Singleton<GUISounds>.Instance.method_3();
                }
                if (Input.GetKeyDown(Plugin.previousTrack.Value.MainKey))
                {
                    Audio.menuMusicAudioSource.Stop();
                    CustomMusicPatch.trackCounter -= 2;
                    if (CustomMusicPatch.trackCounter < 0)
                    {
                        CustomMusicPatch.trackCounter = CustomMusicPatch.trackArray.Count + (CustomMusicPatch.trackCounter);
                    }
                    StaticManager.Instance.StopCoroutine(menuMusicCoroutine);
                    paused = false;
                    Singleton<GUISounds>.Instance.method_3();
                }
                if (Input.GetKeyDown(Plugin.skipTrack.Value.MainKey))
                {
                    Audio.menuMusicAudioSource.Stop();
                    StaticManager.Instance.StopCoroutine(menuMusicCoroutine);
                    paused = false;
                    Singleton<GUISounds>.Instance.method_3();
                }
            }
        }
    }
    public class SoundtrackJukebox : MonoBehaviour
    {
        internal static bool soundtrackCalled = false;
        private static int trackCounter = 0;
        internal static Coroutine soundtrackCoroutine;
        private static bool paused = false;
        private static float pausedTime = 0f;
        public static void playSoundtrack()
        {
            if (Plugin.trackArray.Count == 1)
            {
                trackCounter = 0;
            }
            Audio.soundtrackAudioSource.clip = Plugin.trackArray[trackCounter];
            Audio.soundtrackAudioSource.Play();
            Plugin.LogSource.LogInfo("Playing " + Plugin.trackNamesArray[trackCounter]);
            trackCounter++;
            soundtrackCoroutine = StaticManager.Instance.WaitSeconds(Audio.soundtrackAudioSource.clip.length, new Action(playSoundtrack));
            if (trackCounter >= Plugin.trackArray.Count)
            {
                trackCounter = 0;
            }
        }
        public static void soundtrackControls()
        {
            if (soundtrackCalled)
            {
                if (Input.GetKeyDown(Plugin.pauseTrack.Value.MainKey) && Audio.soundtrackAudioSource.isPlaying)
                {
                    Audio.soundtrackAudioSource.Pause();
                    StaticManager.Instance.StopCoroutine(soundtrackCoroutine);
                    pausedTime = Audio.soundtrackAudioSource.clip.length - Audio.soundtrackAudioSource.time;
                    paused = true;
                }
                else if (Input.GetKeyDown(Plugin.pauseTrack.Value.MainKey) && paused)
                {
                    Audio.soundtrackAudioSource.UnPause();
                    soundtrackCoroutine = StaticManager.Instance.WaitSeconds(pausedTime, new Action(playSoundtrack));
                    paused = false;
                }
                if (Input.GetKeyDown(Plugin.restartTrack.Value.MainKey))
                {
                    Audio.soundtrackAudioSource.Stop();
                    if (trackCounter != 0)
                    {
                        trackCounter--;
                    }
                    else
                    {
                        trackCounter = Plugin.trackArray.Count - 1;
                    }
                    StaticManager.Instance.StopCoroutine(soundtrackCoroutine);
                    paused = false;
                    playSoundtrack();
                }
                if (Input.GetKeyDown(Plugin.previousTrack.Value.MainKey))
                {
                    Audio.soundtrackAudioSource.Stop();
                    trackCounter -= 2;
                    if (trackCounter < 0)
                    {
                        trackCounter = Plugin.trackArray.Count + (trackCounter);
                    }
                    StaticManager.Instance.StopCoroutine(soundtrackCoroutine);
                    paused = false;
                    playSoundtrack();
                }
                if (Input.GetKeyDown(Plugin.skipTrack.Value.MainKey))
                {
                    Audio.soundtrackAudioSource.Stop();
                    StaticManager.Instance.StopCoroutine(soundtrackCoroutine);
                    paused = false;
                    playSoundtrack();
                }
            }
        }
    }

    [RequireComponent(typeof(AudioSource))]
    public class Audio : MonoBehaviour
    {
        public static AudioSource soundtrackAudioSource;
        public static AudioSource spawnAudioSource;
        public static AudioSource combatAudioSource;
        public static AudioSource menuMusicAudioSource;
        public static void SetClip(AudioSource audiosource, AudioClip clip)
        {
            audiosource.clip = clip;
        }
        public static void AdjustVolume(AudioSource audiosource, float volume)
        {
            audiosource.volume = volume;
        }
    }
    [BepInPlugin("BobbyRenzobbi.MusicPlayer", "BobbysMusicPlayer", "1.1.3")]
    public class Plugin : BaseUnityPlugin
    {
        public enum ESoundtrackPlaylist
        {
            MapSpecificPlaylistOnly,
            CombinedPlaylists,
            DefaultPlaylistOnly
        }
        public static ConfigEntry<float> SoundtrackVolume { get; set; }
        public static ConfigEntry<float> SpawnMusicVolume { get; set; }
        public static ConfigEntry<int> SoundtrackLength { get; set; }
        public static ConfigEntry<int> CustomMenuMusicLength { get; set; }
        public static ConfigEntry<ESoundtrackPlaylist> SoundtrackPlaylist;
        public static ConfigEntry<KeyboardShortcut> restartTrack { get; set; }
        public static ConfigEntry<KeyboardShortcut> skipTrack { get; set; }
        public static ConfigEntry<KeyboardShortcut> previousTrack { get; set; }
        public static ConfigEntry<KeyboardShortcut> pauseTrack { get; set; }
        private static System.Random rand = new System.Random();
        internal async Task<AudioClip> AsyncRequestAudioClip(string path)
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
                await Task.Yield();
            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Logger.LogError("Soundtrack: Failed To Fetch Audio Clip");
                return null;
            }
            else
            {
                AudioClip audioclip = DownloadHandlerAudioClip.GetContent(uwr);
                return audioclip;
            }
        }
        internal AudioClip RequestAudioClip(string path)
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
                    Logger.LogError("Soundtrack: Failed To Fetch Audio Clip");
                    return null;
                }
            AudioClip audioclip = DownloadHandlerAudioClip.GetContent(uwr);
            return audioclip;
        }
        private static string mapSpecificDir = AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\BobbysMusicPlayer\\Soundtrack\\map_specific_soundtrack";
        private static CustomMusicPatch customMusicPatch = new CustomMusicPatch();
        internal static List<AudioClip> trackArray = new List<AudioClip>();
        internal static ManualLogSource LogSource;
        internal static List<string> defaultTrackList = new List<string>();
        private static Dictionary<string, string[]> mapDictionary = new Dictionary<string, string[]>
        {
            ["rezervbase"] = Directory.GetFiles(mapSpecificDir + "\\reserve"),
            ["bigmap"] = Directory.GetFiles(mapSpecificDir + "\\customs"),
            ["factory4_night"] = Directory.GetFiles(mapSpecificDir + "\\factory"),
            ["factory4_day"] = Directory.GetFiles(mapSpecificDir + "\\factory"),
            ["interchange"] = Directory.GetFiles(mapSpecificDir + "\\interchange"),
            ["laboratory"] = Directory.GetFiles(mapSpecificDir + "\\labs"),
            ["shoreline"] = Directory.GetFiles(mapSpecificDir + "\\shoreline"),
            ["sandbox"] = Directory.GetFiles(mapSpecificDir + "\\ground_zero"),
            ["sandbox_high"] = Directory.GetFiles(mapSpecificDir + "\\ground_zero"),
            ["woods"] = Directory.GetFiles(mapSpecificDir + "\\woods"),
            ["lighthouse"] = Directory.GetFiles(mapSpecificDir + "\\lighthouse"),
            ["tarkovstreets"] = Directory.GetFiles(mapSpecificDir + "\\streets")

        };
        private static List<string> trackListToPlay = new List<string>();
        private static List<string> spawnTrackList = new List<string>();
        private static AudioClip spawnTrackClip = null;
        private static bool HasStartedLoadingAudio = false;
        internal static bool HasFinishedLoadingAudio = false;
        internal static List<string> trackNamesArray = new List<string>();
        private static bool spawnTrackHasPlayed = false;

        private async void LoadAudioClips()
        {
            float totalLength = 0f;
            HasFinishedLoadingAudio = false;
            trackArray.Clear();
            trackNamesArray.Clear();
            trackListToPlay.Clear();
            float targetLength = 60f * SoundtrackLength.Value;
            if (mapDictionary[Singleton<GameWorld>.Instance.MainPlayer.Location].IsNullOrEmpty() || SoundtrackPlaylist.Value == ESoundtrackPlaylist.DefaultPlaylistOnly)
            {
                trackListToPlay.AddRange(defaultTrackList);
            }
            else if (SoundtrackPlaylist.Value == ESoundtrackPlaylist.CombinedPlaylists)
            {
                trackListToPlay.AddRange(defaultTrackList);
                trackListToPlay.AddRange(mapDictionary[Singleton<GameWorld>.Instance.MainPlayer.Location]);
            }
            else if (SoundtrackPlaylist.Value == ESoundtrackPlaylist.MapSpecificPlaylistOnly)
            {
                trackListToPlay.AddRange(mapDictionary[Singleton<GameWorld>.Instance.MainPlayer.Location]);
            }
            while ((totalLength < targetLength) && (!trackListToPlay.IsNullOrEmpty()))
            {
                int nextRandom = rand.Next(trackListToPlay.Count);
                string track = trackListToPlay[nextRandom];
                string trackPath = Path.GetFileName(track);
                AudioClip unityAudioClip = await AsyncRequestAudioClip(track);
                trackArray.Add(unityAudioClip);
                trackNamesArray.Add(trackPath);
                trackListToPlay.Remove(track);
                totalLength += trackArray.Last().length;
                LogSource.LogInfo(trackPath + " has been loaded and added to playlist");
            }
            HasFinishedLoadingAudio = true;
            totalLength = 0f;
        }

        private void Awake()
        {
            CustomMusicPatch.menuTrackList.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\BobbysMusicPlayer\\CustomMenuMusic\\sounds"));
            if (CustomMusicPatch.menuTrackList.IsNullOrEmpty() && Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\CustomMenuMusic\\sounds"))
            {
                CustomMusicPatch.menuTrackList.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\CustomMenuMusic\\sounds"));
            }
            defaultTrackList.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\BobbysMusicPlayer\\Soundtrack\\default_soundtrack"));
            if (defaultTrackList.IsNullOrEmpty() && Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Soundtrack\\sounds"))
            {
                defaultTrackList.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Soundtrack\\sounds"));
            }
            spawnTrackList.Add(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\BobbysMusicPlayer\\Soundtrack\\spawn_music").FirstOrDefault());
            RaidEndMusicPatch.deathMusicList.Add(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\BobbysMusicPlayer\\DeathMusic").FirstOrDefault());
            RaidEndMusicPatch.extractMusicList.Add(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\BobbysMusicPlayer\\ExtractMusic").FirstOrDefault());
            int counter = 0;
            foreach (var dir in UISoundsPatch.questSoundsDir)
            {
                UISoundsPatch.questSounds[counter] = (Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\BobbysMusicPlayer\\UISounds\\" + dir).FirstOrDefault());
                counter++;
            }
            string generalSettings = "1. General Settings";
            string soundtrackSettings = "3. In-Raid Soundtrack Settings";
            string customMenuMusicSettings = "2. Custom Menu Music Settings";
            SoundtrackVolume = Config.Bind<float>(soundtrackSettings, "In-raid music volume", 0.025f, new ConfigDescription("Volume of the music played in raid", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 3 }));
            SpawnMusicVolume = Config.Bind<float>(soundtrackSettings, "Spawn music volume", 0.06f, new ConfigDescription("Volume of the music played on spawn", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 2 }));
            SoundtrackPlaylist = Config.Bind<ESoundtrackPlaylist>(soundtrackSettings, "Soundtrack playlist selection", ESoundtrackPlaylist.CombinedPlaylists, new ConfigDescription("- Map Specific Playlist Only: Playlist will only use music from the map's soundtrack folder. If it is empty, the default soundtrack folder will be used instead.\n- Combined Playlists: Playlist will combine music from the map's soundtrack folder and the default soundtrack folder.\n- Default Playlist Only: Playlist will only use music from the default soundtrack folder.", null, new ConfigurationManagerAttributes { Order = 1 }));
            SoundtrackLength = Config.Bind<int>(soundtrackSettings, "Soundtrack playlist length (Minutes)", 50, new ConfigDescription("The length of the playlist created for each raid.\nYou should keep this at 50 unless you have modified raid times.", new AcceptableValueRange<int>(0, 600), new ConfigurationManagerAttributes { Order = 0, IsAdvanced = true }));
            CustomMenuMusicLength = Config.Bind<int>(customMenuMusicSettings, "Menu Music playlist length (Minutes)", 60, new ConfigDescription("The length of the playlist created for the main menu.\nNote: This setting's changes will take place either on game restart, or after a raid.", new AcceptableValueRange<int>(0, 600), new ConfigurationManagerAttributes { Order = 0, IsAdvanced = true }));
            restartTrack = Config.Bind(generalSettings, "Restart track button", new KeyboardShortcut(KeyCode.Keypad2));
            skipTrack = Config.Bind(generalSettings, "Skip track button", new KeyboardShortcut(KeyCode.Keypad6));
            previousTrack = Config.Bind(generalSettings, "Previous track button", new KeyboardShortcut(KeyCode.Keypad4));
            pauseTrack = Config.Bind(generalSettings, "Pause track button", new KeyboardShortcut(KeyCode.Keypad5));

            LogSource = Logger;
            LogSource.LogInfo("plugin loaded!");
            new CustomMusicPatch().Enable();
            new RaidEndMusicPatch().Enable();
            new UISoundsPatch().Enable();
            customMusicPatch.LoadAudioClips();
        }

        private void Update()
        {
            CustomMusicJukebox.menuMusicControls();
            SoundtrackJukebox.soundtrackControls();
            if (Audio.soundtrackAudioSource != null)
            {
                    Audio.AdjustVolume(Audio.soundtrackAudioSource, SoundtrackVolume.Value);
            }
            if (Audio.spawnAudioSource != null)
            {
                Audio.AdjustVolume(Audio.spawnAudioSource, SpawnMusicVolume.Value);
            }
            if (Singleton<GameWorld>.Instance == null && !CustomMusicPatch.HasReloadedAudio)
            {
                customMusicPatch.LoadAudioClips();
            }
            if (Singleton<GameWorld>.Instance == null || (Singleton<GameWorld>.Instance?.MainPlayer is HideoutPlayer))
            {
                SoundtrackJukebox.soundtrackCalled = false;
                HasStartedLoadingAudio = false;
                return;
            }
            CustomMusicPatch.HasReloadedAudio = false;
            if (Audio.soundtrackAudioSource == null || Audio.spawnAudioSource == null)
            {
                Audio.soundtrackAudioSource = gameObject.AddComponent<AudioSource>();
                Audio.spawnAudioSource = gameObject.AddComponent<AudioSource>();
            }
            if (Singleton<GameWorld>.Instance.MainPlayer == null)
            {
                return;
            }
            if (!HasStartedLoadingAudio)
            {
                HasStartedLoadingAudio = true;
                if (!defaultTrackList.IsNullOrEmpty())
                {
                    LoadAudioClips();
                }
                if (!spawnTrackList.IsNullOrEmpty())
                {
                    spawnTrackClip = RequestAudioClip(spawnTrackList[0]);
                    LogSource.LogInfo("RequestAudioClip called for spawnTrackClip");
                    spawnTrackHasPlayed = false;
                }
            }
            if (Singleton<AbstractGame>.Instance.Status != GameStatus.Started)
            {
                return;
            }
            if (!spawnTrackHasPlayed && spawnTrackClip != null)
            {
                Audio.spawnAudioSource.clip = spawnTrackClip;
                LogSource.LogInfo("spawnAudioSource.clip assigned to spawnTrackClip");
                Audio.spawnAudioSource.Play();
                LogSource.LogInfo("spawnAudioSource playing");
                spawnTrackHasPlayed = true;
            }
            if (!SoundtrackJukebox.soundtrackCalled && !Audio.spawnAudioSource.isPlaying && !trackArray.IsNullOrEmpty() && HasFinishedLoadingAudio)
            {
                SoundtrackJukebox.playSoundtrack();
                SoundtrackJukebox.soundtrackCalled = true;
            }
        }
    }
}