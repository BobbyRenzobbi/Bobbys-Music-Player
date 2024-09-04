using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BobbysMusicPlayer.Patches;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using HeadsetClass = GClass2654;

namespace BobbysMusicPlayer
{
    public class MenuMusicJukebox : MonoBehaviour
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
            if (!soundtrackCalled || Audio.soundtrackAudioSource.isPlaying || paused || Audio.spawnAudioSource.isPlaying || Plugin.ambientTrackArray.IsNullOrEmpty() || !Plugin.HasFinishedLoadingAudio)
            {
                return;
            }
            if (Plugin.ambientTrackArray.Count == 1)
            {
                trackCounter = 0;
            }
            Audio.soundtrackAudioSource.clip = Plugin.ambientTrackArray[trackCounter];
            Audio.soundtrackAudioSource.Play();
            Plugin.LogSource.LogInfo("Playing " + Plugin.ambientTrackNamesArray[trackCounter]);
            trackCounter++;
            soundtrackCoroutine = StaticManager.Instance.WaitSeconds(Audio.soundtrackAudioSource.clip.length, new Action(PlaySoundtrack));
            if (trackCounter >= Plugin.ambientTrackArray.Count)
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
                    trackCounter = Plugin.ambientTrackArray.Count - 1;
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
                    trackCounter = Plugin.ambientTrackArray.Count + (trackCounter);
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
    [BepInPlugin("BobbyRenzobbi.MusicPlayer", "BobbysMusicPlayer", "1.2.3")]
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
        public static ConfigEntry<float> CombatGrenadeEntryTime { get; set; }
        public static ConfigEntry<float> CombatFireEntryTime { get; set; }
        public static ConfigEntry<float> CombatHitEntryTime { get; set; }
        public static ConfigEntry<float> CombatMusicVolume { get; set; }
        public static ConfigEntry<float> CombatInFader { get; set; }
        public static ConfigEntry<float> CombatOutFader { get; set; }
        public static ConfigEntry<float> ShotNearCutoff { get; set; }
        public static ConfigEntry<float> GrenadeNearCutoff { get; set; }
        public static ConfigEntry<float> IndoorMultiplier { get; set; }
        public static ConfigEntry<float> HeadsetMultiplier { get; set; }
        internal static System.Random rand = new System.Random();
        internal async Task<AudioClip> AsyncRequestAudioClip(string path)
        {
            string extension = Path.GetExtension(path);
            Dictionary<string, AudioType> audioType = new Dictionary<string, AudioType>
            {
                [".wav"] = AudioType.WAV,
                [".ogg"] = AudioType.OGGVORBIS,
                [".mp2"] = AudioType.MPEG,
                [".mp3"] = AudioType.MPEG,
                [".aiff"] = AudioType.AIFF,
                [".s3m"] = AudioType.S3M,
                [".it"] = AudioType.IT,
                [".mod"] = AudioType.MOD,
                [".xm"] = AudioType.XM,
                [".xma"] = AudioType.XMA,
                [".vag"] = AudioType.VAG
            };
            UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, audioType[extension.ToLower()]);
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
                [".mp2"] = AudioType.MPEG,
                [".mp3"] = AudioType.MPEG,
                [".aiff"] = AudioType.AIFF,
                [".s3m"] = AudioType.S3M,
                [".it"] = AudioType.IT,
                [".mod"] = AudioType.MOD,
                [".xm"] = AudioType.XM,
                [".xma"] = AudioType.XMA,
                [".vag"] = AudioType.VAG
            };
            UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, audioType[extension.ToLower()]);
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
        internal static List<AudioClip> ambientTrackArray = new List<AudioClip>();
        internal static ManualLogSource LogSource;
        private static List<string> defaultTrackList = new List<string>();
        private static Dictionary<string, string[]> mapDictionary = new Dictionary<string, string[]>
        {
            ["RezervBase"] = Directory.GetFiles(mapSpecificDir + "\\reserve"),
            ["bigmap"] = Directory.GetFiles(mapSpecificDir + "\\customs"),
            ["factory4_night"] = Directory.GetFiles(mapSpecificDir + "\\factory"),
            ["factory4_day"] = Directory.GetFiles(mapSpecificDir + "\\factory"),
            ["Interchange"] = Directory.GetFiles(mapSpecificDir + "\\interchange"),
            ["laboratory"] = Directory.GetFiles(mapSpecificDir + "\\labs"),
            ["Shoreline"] = Directory.GetFiles(mapSpecificDir + "\\shoreline"),
            ["Sandbox"] = Directory.GetFiles(mapSpecificDir + "\\ground_zero"),
            ["Sandbox_high"] = Directory.GetFiles(mapSpecificDir + "\\ground_zero"),
            ["Woods"] = Directory.GetFiles(mapSpecificDir + "\\woods"),
            ["Lighthouse"] = Directory.GetFiles(mapSpecificDir + "\\lighthouse"),
            ["TarkovStreets"] = Directory.GetFiles(mapSpecificDir + "\\streets")

        };
        private static Dictionary<EnvironmentType, float> environmentDict = new Dictionary<EnvironmentType, float>();
        private static float lerp = 0f;
        internal static float combatTimer = 0f;
        private static float soundtrackVolume = 0f;
        private static float spawnMusicVolume = 0f;
        private static float combatMusicVolume = 0f;
        internal static float headsetMultiplier = 1f;
        private static List<string> combatMusicTrackList = new List<string>();
        private static List<AudioClip> combatMusicClipList = new List<AudioClip>();
        private static List<string> ambientTrackListToPlay = new List<string>();
        private static List<string> spawnTrackList = new List<string>();
        private static List<AudioClip> spawnTrackClipList = new List<AudioClip>();
        private static bool HasStartedLoadingAudio = false;
        internal static bool HasFinishedLoadingAudio = false;
        internal static List<string> ambientTrackNamesArray = new List<string>();
        private static bool spawnTrackHasPlayed = false;

        private async void LoadAmbientSoundtrackClips()
        {
            float totalLength = 0f;
            HasFinishedLoadingAudio = false;
            ambientTrackArray.Clear();
            ambientTrackNamesArray.Clear();
            ambientTrackListToPlay.Clear();
            float targetLength = 60f * SoundtrackLength.Value;
            LogSource.LogInfo("Map is " + Singleton<GameWorld>.Instance.MainPlayer.Location + ".");
            if (mapDictionary[Singleton<GameWorld>.Instance.MainPlayer.Location].IsNullOrEmpty() || SoundtrackPlaylist.Value == ESoundtrackPlaylist.DefaultPlaylistOnly)
            {
                ambientTrackListToPlay.AddRange(defaultTrackList);
            }
            else if (SoundtrackPlaylist.Value == ESoundtrackPlaylist.CombinedPlaylists)
            {
                ambientTrackListToPlay.AddRange(defaultTrackList);
                ambientTrackListToPlay.AddRange(mapDictionary[Singleton<GameWorld>.Instance.MainPlayer.Location]);
            }
            else if (SoundtrackPlaylist.Value == ESoundtrackPlaylist.MapSpecificPlaylistOnly)
            {
                ambientTrackListToPlay.AddRange(mapDictionary[Singleton<GameWorld>.Instance.MainPlayer.Location]);
            }
            while ((totalLength < targetLength) && (!ambientTrackListToPlay.IsNullOrEmpty()))
            {
                int nextRandom = rand.Next(ambientTrackListToPlay.Count);
                string track = ambientTrackListToPlay[nextRandom];
                string trackPath = Path.GetFileName(track);
                AudioClip unityAudioClip = await AsyncRequestAudioClip(track);
                ambientTrackArray.Add(unityAudioClip);
                ambientTrackNamesArray.Add(trackPath);
                ambientTrackListToPlay.Remove(track);
                totalLength += ambientTrackArray.Last().length;
                LogSource.LogInfo(trackPath + " has been loaded and added to playlist");
            }
            HasFinishedLoadingAudio = true;
            totalLength = 0f;
        }
        private void CombatLerp()
        {
            Audio.AdjustVolume(Audio.combatAudioSource, Mathf.Lerp(0f, combatMusicVolume, lerp));
            Audio.AdjustVolume(Audio.soundtrackAudioSource, Mathf.Lerp(soundtrackVolume, AmbientCombatMultiplier.Value*soundtrackVolume, lerp));
            Audio.AdjustVolume(Audio.spawnAudioSource, Mathf.Lerp(spawnMusicVolume, AmbientCombatMultiplier.Value*spawnMusicVolume, lerp));
        }
        private void CombatMusic()
        {
            if (Audio.combatAudioSource != null && !combatMusicTrackList.IsNullOrEmpty())
            {
                if (combatTimer > 0)
                {
                    if (!Audio.combatAudioSource.isPlaying && Audio.combatAudioSource.loop == false)
                    {
                        Audio.combatAudioSource.loop = true;
                        Audio.combatAudioSource.Play();
                    }
                    if (lerp <= 1)
                    {
                        CombatLerp();
                        lerp += Time.deltaTime / CombatInFader.Value;
                    }
                    combatTimer -= Time.deltaTime;
                }
                else if (combatTimer <= 0)
                {
                    if (Audio.combatAudioSource.isPlaying)
                    {
                        combatTimer = 0f;
                        CombatLerp();
                        lerp -= Time.deltaTime / CombatOutFader.Value;
                        if (lerp <= 0)
                        {
                            Audio.combatAudioSource.loop = false;
                            Audio.combatAudioSource.Stop();
                            Audio.combatAudioSource.clip = combatMusicClipList[rand.Next(combatMusicClipList.Count)];
                        }
                    }
                }
            }
        }

        private void VolumeSetter()
        {
            // Next two lines are taken from Fontaine's Realism Mod. Credit to him
            LootItemClass headwear = Singleton<GameWorld>.Instance.MainPlayer.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as LootItemClass;
            HeadsetClass headset = Singleton<GameWorld>.Instance.MainPlayer.Equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem as HeadsetClass ?? ((headwear != null) ? headwear.GetAllItemsFromCollection().OfType<HeadsetClass>().FirstOrDefault<HeadsetClass>() : null);
            if (headset != null)
            {
                headsetMultiplier = HeadsetMultiplier.Value;
            }
            else
            {
                headsetMultiplier = 1f;
            }
            soundtrackVolume = SoundtrackVolume.Value * environmentDict[Singleton<GameWorld>.Instance.MainPlayer.Environment] * headsetMultiplier;
            spawnMusicVolume = SpawnMusicVolume.Value * environmentDict[Singleton<GameWorld>.Instance.MainPlayer.Environment] * headsetMultiplier;
            combatMusicVolume = CombatMusicVolume.Value * environmentDict[Singleton<GameWorld>.Instance.MainPlayer.Environment] * headsetMultiplier;
            if (!Audio.combatAudioSource.isPlaying)
            {
                    Audio.AdjustVolume(Audio.soundtrackAudioSource, soundtrackVolume);
                    Audio.AdjustVolume(Audio.spawnAudioSource, spawnMusicVolume);
            }
            if (lerp >= 1)
            {
                Audio.AdjustVolume(Audio.combatAudioSource, combatMusicVolume);
            }   
        }

        private void PrepareRaidAudioClips()
        {
            if (!HasStartedLoadingAudio)
            {
                HasStartedLoadingAudio = true;
                if (!defaultTrackList.IsNullOrEmpty())
                {
                    LoadAmbientSoundtrackClips();
                }
                if (!spawnTrackList.IsNullOrEmpty())
                {
                    spawnTrackClipList.Clear();
                    foreach (var track in spawnTrackList)
                    {
                        spawnTrackClipList.Add(RequestAudioClip(track));
                        LogSource.LogInfo("RequestAudioClip called for spawnTrackClip");
                    }
                    spawnTrackHasPlayed = false;
                }
                if (!combatMusicTrackList.IsNullOrEmpty())
                {
                    lerp = 0;
                    Audio.combatAudioSource.Stop();
                    Audio.combatAudioSource.loop = false;
                    combatMusicClipList.Clear();
                    foreach (var track in combatMusicTrackList)
                    {
                        combatMusicClipList.Add(RequestAudioClip(track));
                    }
                    Audio.combatAudioSource.clip = combatMusicClipList[rand.Next(combatMusicClipList.Count)];
                }
            }
        }

        private void PlaySpawnMusic()
        {
            if (!spawnTrackHasPlayed && !spawnTrackClipList.IsNullOrEmpty())
            {
                Audio.spawnAudioSource.clip = spawnTrackClipList[rand.Next(spawnTrackClipList.Count)];
                LogSource.LogInfo("spawnAudioSource.clip assigned to spawnTrackClip");
                Audio.spawnAudioSource.Play();
                LogSource.LogInfo("spawnAudioSource playing");
                spawnTrackHasPlayed = true;
            }
        }

        private void Awake()
        {
            LogSource = Logger;
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
            combatMusicTrackList.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\BobbysMusicPlayer\\Soundtrack\\combat_music"));
            spawnTrackList.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\BobbysMusicPlayer\\Soundtrack\\spawn_music"));
            RaidEndMusicPatch.deathMusicList.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\BobbysMusicPlayer\\DeathMusic"));
            RaidEndMusicPatch.extractMusicList.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\BobbysMusicPlayer\\ExtractMusic"));
            int counter = 0;
            foreach (var dir in UISoundsPatch.uiSoundsDir)
            {
                UISoundsPatch.uiSounds[counter] = new List<string>();
                UISoundsPatch.uiSounds[counter].AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\BobbysMusicPlayer\\UISounds\\" + dir));
                counter++;
            }

            string generalSettings = "1. General Settings";
            string ambientSoundtrackSettings = "3. Ambient Soundtrack Settings";
            string customMenuMusicSettings = "2. Custom Menu Music Settings";
            string dynamicSoundtrackSettings = "4. Dynamic Soundtrack Settings";
            SoundtrackVolume = Config.Bind<float>(ambientSoundtrackSettings, "Ambient Soundtrack volume", 0.05f, new ConfigDescription("Volume of the Ambient Soundtrack", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 3 }));
            SpawnMusicVolume = Config.Bind<float>(ambientSoundtrackSettings, "Spawn music volume", 0.05f, new ConfigDescription("Volume of the music played on spawn", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 2 }));
            SoundtrackPlaylist = Config.Bind<ESoundtrackPlaylist>(ambientSoundtrackSettings, "Ambient Soundtrack playlist selection", ESoundtrackPlaylist.CombinedPlaylists, new ConfigDescription("- Map Specific Playlist Only: Playlist will only use music from the map's soundtrack folder. If it is empty, the default soundtrack folder will be used instead.\n- Combined Playlists: Playlist will combine music from the map's soundtrack folder and the default soundtrack folder.\n- Default Playlist Only: Playlist will only use music from the default soundtrack folder.", null, new ConfigurationManagerAttributes { Order = 1 }));
            SoundtrackLength = Config.Bind<int>(ambientSoundtrackSettings, "Ambient Soundtrack playlist length (Minutes)", 50, new ConfigDescription("The length of the Ambient playlist created for each raid.\nYou should keep this around 50 unless you have modified raid times.", new AcceptableValueRange<int>(0, 600), new ConfigurationManagerAttributes { Order = 0, IsAdvanced = true }));
            CustomMenuMusicLength = Config.Bind<int>(customMenuMusicSettings, "Menu Music playlist length (Minutes)", 60, new ConfigDescription("The length of the playlist created for the main menu.\nNote: This setting's changes will take place either on game restart, or after a raid.", new AcceptableValueRange<int>(0, 600), new ConfigurationManagerAttributes { Order = 0, IsAdvanced = true }));
            RestartTrack = Config.Bind(generalSettings, "Restart track button", new KeyboardShortcut(KeyCode.Keypad2));
            SkipTrack = Config.Bind(generalSettings, "Skip track button", new KeyboardShortcut(KeyCode.Keypad6));
            PreviousTrack = Config.Bind(generalSettings, "Previous track button", new KeyboardShortcut(KeyCode.Keypad4));
            PauseTrack = Config.Bind(generalSettings, "Pause track button", new KeyboardShortcut(KeyCode.Keypad5));
            CombatAttackedEntryTime = Config.Bind<float>(dynamicSoundtrackSettings, "Combat duration when shot at (Seconds)", 12f, new ConfigDescription("The duration of the combat state when the player is shot at\nMake sure this is set less than \"Combat duration when hit (Seconds)\"", new AcceptableValueRange<float>(0f, 600f)));
            CombatDangerEntryTime = Config.Bind<float>(dynamicSoundtrackSettings, "Combat duration when a shot is fired closeby (Seconds)", 12f, new ConfigDescription("The duration of the combat state when a gun is fired within the distance set by \"Shot distance combat trigger (meters)\"", new AcceptableValueRange<float>(0f, 600f)));
            CombatGrenadeEntryTime = Config.Bind<float>(dynamicSoundtrackSettings, "Combat duration when a grenade explodes closeby (Seconds)", 12f, new ConfigDescription("The duration of the combat state when a gun is fired within the distance set by \"Shot distance combat trigger (meters)\"", new AcceptableValueRange<float>(0f, 600f)));
            CombatHitEntryTime = Config.Bind<float>(dynamicSoundtrackSettings, "Combat duration when hit (Seconds)", 20f, new ConfigDescription("The duration of the combat state when the player is hit\nMake sure this is greater than \"Combat duration when shot at (Seconds)\"", new AcceptableValueRange<float>(0f, 600f)));
            CombatFireEntryTime = Config.Bind<float>(dynamicSoundtrackSettings, "Combat duration when firing (Seconds)", 8f, new ConfigDescription("The duration of the combat state when the player fires their gun\nIf using a door breaching mod, consider reducing this value potentially down to 0", new AcceptableValueRange<float>(0f, 600f)));
            CombatMusicVolume = Config.Bind<float>(dynamicSoundtrackSettings, "Combat music volume", 0.05f, new ConfigDescription("Volume of the music played in combat", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 8 }));
            CombatInFader = Config.Bind<float>(dynamicSoundtrackSettings, "Combat entry fader", 4f, new ConfigDescription("The transition time from normal soundtrack to combat music", new AcceptableValueRange<float>(0.1f, 30f), new ConfigurationManagerAttributes {IsAdvanced = true}));
            CombatOutFader = Config.Bind<float>(dynamicSoundtrackSettings, "Combat exit fader", 8f, new ConfigDescription("The transition time from combat music to normal soundtrack\nNote: This transition begins after the combat state ends", new AcceptableValueRange<float>(0.1f, 120f), new ConfigurationManagerAttributes {IsAdvanced = true}));
            ShotNearCutoff = Config.Bind<float>(dynamicSoundtrackSettings, "Shot distance combat trigger (meters)", 15f, new ConfigDescription("If an enemy fires within this distance, it will trigger a combat state", new AcceptableValueRange<float>(0f, 150f)));
            GrenadeNearCutoff = Config.Bind<float>(dynamicSoundtrackSettings, "Explosion distance combat trigger (meters)", 20f, new ConfigDescription("If a grenade explodes within this distance, it will trigger a combat state", new AcceptableValueRange<float>(0f, 150f)));
            AmbientCombatMultiplier = Config.Bind<float>(dynamicSoundtrackSettings, "Ambient Soundtrack volume multiplier during combat", 0f, new ConfigDescription("During combat, the Ambient Soundtrack's volume will be multiplied by this value\nSetting this to 0 means your Ambient Soundtrack will be muted in combat.\nSetting this to 1 means that your Ambient Soundtrack volume is independent of your combat state.\nSpawn music volume is also affected", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { Order = 0 }));
            IndoorMultiplier = Config.Bind<float>(generalSettings, "In-Raid Soundtrack volume - Indoor multiplier", 0.75f, new ConfigDescription("When indoors, all in-raid music volume will be multiplied by this value.\nI recommend setting this somewhere between 0 and 1, since the game is much noisier outdoors than indoors", new AcceptableValueRange<float>(0f, 2f)));
            HeadsetMultiplier = Config.Bind<float>(generalSettings, "In-Raid Soundtrack volume - Active headset", 0.75f, new ConfigDescription("When wearing an active headset, all in-raid music volume will be multiplied by this value.\nI recommend setting this somewhere between 0 and 1, since the game is much noisier without an active headset", new AcceptableValueRange<float>(0f, 2f)));
            environmentDict[EnvironmentType.Outdoor] = 1f;

            LogSource.LogInfo("plugin loaded!");
            new MenuMusicPatch().Enable();
            new RaidEndMusicPatch().Enable();
            new UISoundsPatch().Enable();
            new ShotAtPatch().Enable();
            new PlayerFiringPatch().Enable();
            new DamageTakenPatch().Enable();
            new ShotFiredNearPatch().Enable();
            new GrenadePatch().Enable();
            new MenuMusicMethod5Patch().Enable();
            new StopMenuMusicPatch().Enable();
            MenuMusicPatch.LoadAudioClips();
            UISoundsPatch.LoadUIClips();
        }

        private void Update()
        {
            environmentDict[EnvironmentType.Indoor] = IndoorMultiplier.Value;
            MenuMusicJukebox.MenuMusicControls();
            SoundtrackJukebox.SoundtrackControls();
            CombatMusic();
            if (Singleton<GameWorld>.Instance == null && !MenuMusicPatch.HasReloadedAudio)
            {
                MenuMusicPatch.LoadAudioClips();
                UISoundsPatch.LoadUIClips();
            }
            if (Singleton<GameWorld>.Instance == null || (Singleton<GameWorld>.Instance?.MainPlayer is HideoutPlayer))
            {
                SoundtrackJukebox.soundtrackCalled = false;
                HasStartedLoadingAudio = false;
                spawnTrackHasPlayed = false;
                return;
            }
            MenuMusicPatch.HasReloadedAudio = false;
            if (Audio.soundtrackAudioSource == null || Audio.spawnAudioSource == null || Audio.combatAudioSource == null)
            {
                Audio.soundtrackAudioSource = gameObject.AddComponent<AudioSource>();
                Audio.spawnAudioSource = gameObject.AddComponent<AudioSource>();
                Audio.combatAudioSource = gameObject.AddComponent<AudioSource>();
                LogSource.LogInfo("AudioSources added to game");
            }
            if (Singleton<GameWorld>.Instance.MainPlayer == null)
            {
                return;
            }
            PrepareRaidAudioClips();
            if (Singleton<AbstractGame>.Instance.Status != GameStatus.Started)
            {
                return;
            }
            VolumeSetter();
            PlaySpawnMusic();
            SoundtrackJukebox.soundtrackCalled = true;
            SoundtrackJukebox.PlaySoundtrack();
        }
    }
}