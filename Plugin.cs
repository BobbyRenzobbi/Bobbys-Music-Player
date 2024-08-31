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
        public static void MenuMusicControls()
        {
            if (!SoundtrackJukebox.soundtrackCalled && Audio.menuMusicAudioSource != null)
            {
                if (Input.GetKeyDown(Plugin.PauseTrack.Value.MainKey) && Audio.menuMusicAudioSource.isPlaying)
                {
                    Audio.menuMusicAudioSource.Pause();
                    StaticManager.Instance.StopCoroutine(menuMusicCoroutine);
                    pausedTime = Audio.menuMusicAudioSource.clip.length - Audio.menuMusicAudioSource.time;
                    paused = true;
                }
                else if (Input.GetKeyDown(Plugin.PauseTrack.Value.MainKey) && paused)
                {
                    Audio.menuMusicAudioSource.UnPause();
                    menuMusicCoroutine = StaticManager.Instance.WaitSeconds(pausedTime, new Action(Singleton<GUISounds>.Instance.method_3));
                    paused = false;
                }
                if (Input.GetKeyDown(Plugin.RestartTrack.Value.MainKey))
                {
                    Audio.menuMusicAudioSource.Stop();
                    if (MenuMusicPatch.trackCounter != 0)
                    {
                        MenuMusicPatch.trackCounter--;
                    }
                    else
                    {
                        MenuMusicPatch.trackCounter = MenuMusicPatch.trackArray.Count - 1;
                    }
                    StaticManager.Instance.StopCoroutine(menuMusicCoroutine);
                    paused = false;
                    Singleton<GUISounds>.Instance.method_3();
                }
                if (Input.GetKeyDown(Plugin.PreviousTrack.Value.MainKey))
                {
                    Audio.menuMusicAudioSource.Stop();
                    MenuMusicPatch.trackCounter -= 2;
                    if (MenuMusicPatch.trackCounter < 0)
                    {
                        MenuMusicPatch.trackCounter = MenuMusicPatch.trackArray.Count + (MenuMusicPatch.trackCounter);
                    }
                    StaticManager.Instance.StopCoroutine(menuMusicCoroutine);
                    paused = false;
                    Singleton<GUISounds>.Instance.method_3();
                }
                if (Input.GetKeyDown(Plugin.SkipTrack.Value.MainKey))
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
        internal static bool inRaid = false; 
        private static int trackCounter = 0;
        internal static Coroutine soundtrackCoroutine;
        private static bool paused = false;
        private static float pausedTime = 0f;
        public static void PlaySoundtrack()
        {
            if (!soundtrackCalled || Audio.soundtrackAudioSource.isPlaying || paused || Audio.spawnAudioSource.isPlaying || Plugin.trackArray.IsNullOrEmpty() || !Plugin.HasFinishedLoadingAudio)
            {
                return;
            }
            if (Plugin.trackArray.Count == 1)
            {
                trackCounter = 0;
            }
            Audio.soundtrackAudioSource.clip = Plugin.trackArray[trackCounter];
            Audio.soundtrackAudioSource.Play();
            Plugin.LogSource.LogInfo("Playing " + Plugin.trackNamesArray[trackCounter]);
            trackCounter++;
            soundtrackCoroutine = StaticManager.Instance.WaitSeconds(Audio.soundtrackAudioSource.clip.length, new Action(PlaySoundtrack));
            if (trackCounter >= Plugin.trackArray.Count)
            {
                trackCounter = 0;
            }
        }
        public static void SoundtrackControls()
        {
            if (!soundtrackCalled || Audio.spawnAudioSource.isPlaying)
            {
                return;
            }
            if (Input.GetKeyDown(Plugin.PauseTrack.Value.MainKey) && Audio.soundtrackAudioSource.isPlaying)
            {
                Audio.soundtrackAudioSource.Pause();
                StaticManager.Instance.StopCoroutine(soundtrackCoroutine);
                pausedTime = Audio.soundtrackAudioSource.clip.length - Audio.soundtrackAudioSource.time;
                paused = true;
            }
            else if (Input.GetKeyDown(Plugin.PauseTrack.Value.MainKey) && paused)
            {
                Audio.soundtrackAudioSource.UnPause();
                soundtrackCoroutine = StaticManager.Instance.WaitSeconds(pausedTime, new Action(PlaySoundtrack));
                paused = false;
            }
            if (Input.GetKeyDown(Plugin.RestartTrack.Value.MainKey))
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
                PlaySoundtrack();
            }
            if (Input.GetKeyDown(Plugin.PreviousTrack.Value.MainKey))
            {
                Audio.soundtrackAudioSource.Stop();
                trackCounter -= 2;
                if (trackCounter < 0)
                {
                    trackCounter = Plugin.trackArray.Count + (trackCounter);
                }
                StaticManager.Instance.StopCoroutine(soundtrackCoroutine);
                paused = false;
                PlaySoundtrack();
            }
            if (Input.GetKeyDown(Plugin.SkipTrack.Value.MainKey))
            {
                Audio.soundtrackAudioSource.Stop();
                StaticManager.Instance.StopCoroutine(soundtrackCoroutine);
                paused = false;
                PlaySoundtrack();
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
    [BepInPlugin("BobbyRenzobbi.MusicPlayer", "BobbysMusicPlayer", "1.2.0")]
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
        public static ConfigEntry<KeyboardShortcut> RestartTrack { get; set; }
        public static ConfigEntry<KeyboardShortcut> SkipTrack { get; set; }
        public static ConfigEntry<KeyboardShortcut> PreviousTrack { get; set; }
        public static ConfigEntry<KeyboardShortcut> PauseTrack { get; set; }
        public static ConfigEntry<float> AmbientCombatMultiplier { get; set; }
        public static ConfigEntry<float> CombatAttackedEntryTime { get; set; }
        public static ConfigEntry<float> CombatDangerEntryTime { get; set; }
        public static ConfigEntry<float> CombatFireEntryTime { get; set; }
        public static ConfigEntry<float> CombatHitEntryTime { get; set; }
        public static ConfigEntry<float> CombatMusicVolume { get; set; }
        public static ConfigEntry<float> CombatInFader { get; set; }
        public static ConfigEntry<float> CombatOutFader { get; set; }
        public static ConfigEntry<float> ShotNearCutoff { get; set; }
        public static ConfigEntry<float> IndoorMultiplier { get; set; }
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
        private static MenuMusicPatch menuMusicPatch = new MenuMusicPatch();
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
        private static Dictionary<int, float> combatDict = new Dictionary<int, float>();
        private static Dictionary<EnvironmentType, float> environmentDict = new Dictionary<EnvironmentType, float>();
        internal static float enterCombatLerp = 0f;
        internal static float exitCombatLerp = 0f;
        internal static float lerp = 0f;
        internal static float combatTimer = 0f;
        private static float soundtrackVolume = 0f;
        private static List<string> combatMusicTrackList = new List<string>();
        private static AudioClip combatMusicClip;
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
            if (mapDictionary[Singleton<GameWorld>.Instance.MainPlayer.Location.ToLower()].IsNullOrEmpty() || SoundtrackPlaylist.Value == ESoundtrackPlaylist.DefaultPlaylistOnly)
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
        private void CombatLerp()
        {
            Audio.AdjustVolume(Audio.combatAudioSource, Mathf.Lerp(0f, CombatMusicVolume.Value, lerp));
            Audio.AdjustVolume(Audio.soundtrackAudioSource, Mathf.Lerp(soundtrackVolume, AmbientCombatMultiplier.Value*soundtrackVolume, lerp));
        }
        private void CombatMusic()
        {
            if (combatTimer > 0 && Audio.combatAudioSource != null && !combatMusicTrackList.IsNullOrEmpty())
            {
                if (!Audio.combatAudioSource.isPlaying)
                {
                    Audio.combatAudioSource.clip = combatMusicClip;
                    Audio.combatAudioSource.Play();
                }
                if (lerp <= 1)
                {
                    CombatLerp();
                    lerp += Time.deltaTime / combatDict[0];
                }
                combatTimer -= Time.deltaTime;
            }
            else if (combatTimer <= 0 && Audio.combatAudioSource != null && Audio.combatAudioSource.isPlaying && !combatMusicTrackList.IsNullOrEmpty())
            {
                combatTimer = 0f;
                CombatLerp();
                lerp -= Time.deltaTime / combatDict[1];
                if (lerp <= 0)
                {
                    Audio.combatAudioSource.Stop();
                }
            }
            else
            {
                VolumeSetter();
            }
        }

        private void VolumeSetter()
        {
            if (Audio.soundtrackAudioSource != null && (!Audio.combatAudioSource.isPlaying))
            {
                Audio.AdjustVolume(Audio.soundtrackAudioSource, soundtrackVolume);
            }
            if (Audio.spawnAudioSource != null)
            {
                Audio.AdjustVolume(Audio.spawnAudioSource, SpawnMusicVolume.Value);
            }
        }

        private void PrepareRaidAudioClips()
        {
            if (!HasStartedLoadingAudio)
            {
                HasStartedLoadingAudio = true;
                if (!defaultTrackList.IsNullOrEmpty())
                {
                    LoadAudioClips();
                }
                if (!spawnTrackList[0].IsNullOrEmpty())
                {
                    spawnTrackClip = RequestAudioClip(spawnTrackList[0]);
                    LogSource.LogInfo("RequestAudioClip called for spawnTrackClip");
                    spawnTrackHasPlayed = false;
                }
                if (!combatMusicTrackList[0].IsNullOrEmpty())
                {
                    combatMusicClip = RequestAudioClip(combatMusicTrackList[0]);
                }
            }
        }

        private void PlaySpawnMusic()
        {
            if (!spawnTrackHasPlayed && spawnTrackClip != null)
            {
                Audio.spawnAudioSource.clip = spawnTrackClip;
                LogSource.LogInfo("spawnAudioSource.clip assigned to spawnTrackClip");
                Audio.spawnAudioSource.Play();
                LogSource.LogInfo("spawnAudioSource playing");
                spawnTrackHasPlayed = true;
            }
        }

        private void Awake()
        {
            MenuMusicPatch.menuTrackList.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\BobbysMusicPlayer\\CustomMenuMusic\\sounds"));
            if (MenuMusicPatch.menuTrackList.IsNullOrEmpty() && Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\CustomMenuMusic\\sounds"))
            {
                MenuMusicPatch.menuTrackList.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\CustomMenuMusic\\sounds"));
            }
            defaultTrackList.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\BobbysMusicPlayer\\Soundtrack\\default_soundtrack"));
            if (defaultTrackList.IsNullOrEmpty() && Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Soundtrack\\sounds"))
            {
                defaultTrackList.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Soundtrack\\sounds"));
            }
            combatMusicTrackList.Add(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\BobbysMusicPlayer\\Soundtrack\\combat_music").FirstOrDefault());
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
            string ambientSoundtrackSettings = "3. Ambient Soundtrack Settings";
            string customMenuMusicSettings = "2. Custom Menu Music Settings";
            string dynamicSoundtrackSettings = "4. Dynamic Soundtrack Settings";
            SoundtrackVolume = Config.Bind<float>(ambientSoundtrackSettings, "Ambient Soundtrack volume", 0.05f, new ConfigDescription("Volume of the Ambient Soundtrack", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 3 }));
            SpawnMusicVolume = Config.Bind<float>(ambientSoundtrackSettings, "Spawn music volume", 0.06f, new ConfigDescription("Volume of the music played on spawn", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 2 }));
            SoundtrackPlaylist = Config.Bind<ESoundtrackPlaylist>(ambientSoundtrackSettings, "Ambient Soundtrack playlist selection", ESoundtrackPlaylist.CombinedPlaylists, new ConfigDescription("- Map Specific Playlist Only: Playlist will only use music from the map's soundtrack folder. If it is empty, the default soundtrack folder will be used instead.\n- Combined Playlists: Playlist will combine music from the map's soundtrack folder and the default soundtrack folder.\n- Default Playlist Only: Playlist will only use music from the default soundtrack folder.", null, new ConfigurationManagerAttributes { Order = 1 }));
            SoundtrackLength = Config.Bind<int>(ambientSoundtrackSettings, "Ambient Soundtrack playlist length (Minutes)", 50, new ConfigDescription("The length of the Ambient playlist created for each raid.\nYou should keep this around 50 unless you have modified raid times.", new AcceptableValueRange<int>(0, 600), new ConfigurationManagerAttributes { Order = 0, IsAdvanced = true }));
            CustomMenuMusicLength = Config.Bind<int>(customMenuMusicSettings, "Menu Music playlist length (Minutes)", 60, new ConfigDescription("The length of the playlist created for the main menu.\nNote: This setting's changes will take place either on game restart, or after a raid.", new AcceptableValueRange<int>(0, 600), new ConfigurationManagerAttributes { Order = 0, IsAdvanced = true }));
            RestartTrack = Config.Bind(generalSettings, "Restart track button", new KeyboardShortcut(KeyCode.Keypad2));
            SkipTrack = Config.Bind(generalSettings, "Skip track button", new KeyboardShortcut(KeyCode.Keypad6));
            PreviousTrack = Config.Bind(generalSettings, "Previous track button", new KeyboardShortcut(KeyCode.Keypad4));
            PauseTrack = Config.Bind(generalSettings, "Pause track button", new KeyboardShortcut(KeyCode.Keypad5));
            CombatAttackedEntryTime = Config.Bind<float>(dynamicSoundtrackSettings, "Combat duration when shot at (Seconds)", 12f, new ConfigDescription("The duration of the combat state when the player is shot at\nMake sure this is set less than \"Combat duration when hit (Seconds)\"", new AcceptableValueRange<float>(0f, 600f)));
            CombatDangerEntryTime = Config.Bind<float>(dynamicSoundtrackSettings, "Combat duration when a shot is fired closeby (Seconds)", 12f, new ConfigDescription("The duration of the combat state when a gun is fired within the distance set by \"Shot distance combat trigger (meters)\"", new AcceptableValueRange<float>(0f, 600f)));
            CombatHitEntryTime = Config.Bind<float>(dynamicSoundtrackSettings, "Combat duration when hit (Seconds)", 20f, new ConfigDescription("The duration of the combat state when the player is hit\nMake sure this is greater than \"Combat duration when shot at (Seconds)\"", new AcceptableValueRange<float>(0f, 600f)));
            CombatFireEntryTime = Config.Bind<float>(dynamicSoundtrackSettings, "Combat duration when firing (Seconds)", 8f, new ConfigDescription("The duration of the combat state when the player fires their gun\nIf using a door breaching mod, consider reducing this value potentially down to 0", new AcceptableValueRange<float>(0f, 600f)));
            CombatMusicVolume = Config.Bind<float>(dynamicSoundtrackSettings, "Combat music volume", 0.06f, new ConfigDescription("Volume of the music played in combat", new AcceptableValueRange<float>(0f, 1f)));
            CombatInFader = Config.Bind<float>(dynamicSoundtrackSettings, "Combat entry fader", 4f, new ConfigDescription("The transition time from normal soundtrack to combat music", new AcceptableValueRange<float>(0f, 30f), new ConfigurationManagerAttributes {IsAdvanced = true}));
            CombatOutFader = Config.Bind<float>(dynamicSoundtrackSettings, "Combat exit fader", 8f, new ConfigDescription("The transition time from combat music to normal soundtrack\nNote: This transition begins after the combat state ends", new AcceptableValueRange<float>(0f, 120f), new ConfigurationManagerAttributes {IsAdvanced = true}));
            ShotNearCutoff = Config.Bind<float>(dynamicSoundtrackSettings, "Shot distance combat trigger (meters)", 15f, new ConfigDescription("If an enemy fires within this distance, it will trigger a combat state", new AcceptableValueRange<float>(0f, 150f)));
            AmbientCombatMultiplier = Config.Bind<float>(dynamicSoundtrackSettings, "Ambient Soundtrack volume combat multiplier", 0f, new ConfigDescription("Multiplier of the volume of the Ambient Soundtrack during combat\nSetting this to 1 means that your Ambient Soundtrack volume is independent of your combat state", new AcceptableValueRange<float>(0f, 2f)));
            IndoorMultiplier = Config.Bind<float>(ambientSoundtrackSettings, "Ambient Soundtrack volume - Indoor multiplier", 0.75f, new ConfigDescription("When indoors, the Ambient Soundtrack volume will be multiplied by this value.\nI recommend setting this somewhere between 0 and 1, since the game is much noisier outdoors than indoors", new AcceptableValueRange<float>(0f, 2f)));
            environmentDict[EnvironmentType.Outdoor] = 1f;

            LogSource = Logger;
            LogSource.LogInfo("plugin loaded!");
            new MenuMusicPatch().Enable();
            new RaidEndMusicPatch().Enable();
            new UISoundsPatch().Enable();
            new ShotAtPatch().Enable();
            new PlayerFiringPatch().Enable();
            new DamageTakenPatch().Enable();
            new ShotFiredNearPatch().Enable();
            menuMusicPatch.LoadAudioClips();
        }

        private void Update()
        {
            combatDict[0] = CombatInFader.Value;
            combatDict[1] = CombatOutFader.Value;
            environmentDict[EnvironmentType.Indoor] = IndoorMultiplier.Value;
            CustomMusicJukebox.MenuMusicControls();
            SoundtrackJukebox.SoundtrackControls();
            CombatMusic();
            if (Singleton<GameWorld>.Instance == null && !MenuMusicPatch.HasReloadedAudio)
            {
                menuMusicPatch.LoadAudioClips();
            }
            if (Singleton<GameWorld>.Instance == null || (Singleton<GameWorld>.Instance?.MainPlayer is HideoutPlayer))
            {
                SoundtrackJukebox.soundtrackCalled = false;
                HasStartedLoadingAudio = false;
                return;
            }
            MenuMusicPatch.HasReloadedAudio = false;
            if (Audio.soundtrackAudioSource == null || Audio.spawnAudioSource == null)
            {
                Audio.soundtrackAudioSource = gameObject.AddComponent<AudioSource>();
                Audio.spawnAudioSource = gameObject.AddComponent<AudioSource>();
                Audio.combatAudioSource = gameObject.AddComponent<AudioSource>();
            }
            if (Singleton<GameWorld>.Instance.MainPlayer == null)
            {
                return;
            }
            soundtrackVolume = SoundtrackVolume.Value * environmentDict[Singleton<GameWorld>.Instance.MainPlayer.Environment];
            PrepareRaidAudioClips();
            if (Singleton<AbstractGame>.Instance.Status != GameStatus.Started)
            {
                return;
            }
            if (CustomMusicJukebox.menuMusicCoroutine != null)
            {
                StaticManager.Instance.StopCoroutine(CustomMusicJukebox.menuMusicCoroutine);
            }
            Audio.menuMusicAudioSource.Stop();
            PlaySpawnMusic();
            SoundtrackJukebox.soundtrackCalled = true;
            SoundtrackJukebox.PlaySoundtrack();
        }
    }
}