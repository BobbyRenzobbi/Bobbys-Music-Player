using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using BepInEx.Configuration;
using EFT;
using Comfort.Common;
using BobbysMusicPlayer.Patches;

namespace BobbysMusicPlayer
{
    [RequireComponent(typeof(AudioSource))]
    public class Audio : MonoBehaviour
    {
        public static AudioSource soundtrackAudioSource;
        public static AudioSource spawnAudioSource;
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
        public static ConfigEntry<float> SoundtrackVolume { get; set; }
        public static ConfigEntry<float> SpawnMusicVolume { get; set; }
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
        private static CustomMusicPatch customMusicPatch = new CustomMusicPatch();
        private static List<AudioClip> trackArray = new List<AudioClip>();
        private static List<AudioClip> storedTrackArray = new List<AudioClip>();
        internal static ManualLogSource LogSource;
        private static List<string> trackList = new List<string>();
        private static List<string> trackListToPlay = new List<string>();
        private static List<string> spawnTrackList = new List<string>();
        private static AudioClip spawnTrackClip = null;
        private static bool HasStartedLoadingAudio = false;
        private static bool HasFinishedLoadingAudio = false;
        private static float targetLength = 3000f;
        private static float totalLength = 0f;
        private static List<string> trackNamesArray = new List<string>();
        private static List<string> storedTrackNamesArray = new List<string>();
        private static bool spawnTrackHasPlayed = false;

        private async void LoadAudioClips()
        {
            HasFinishedLoadingAudio = false;
            storedTrackArray.Clear();
            trackArray.Clear();
            storedTrackNamesArray.Clear();
            trackNamesArray.Clear();
            trackListToPlay.Clear();
            trackListToPlay.AddRange(trackList);
            do
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
            } while ((totalLength < targetLength) && (!trackListToPlay.IsNullOrEmpty()));
            storedTrackArray.AddRange(trackArray);
            storedTrackNamesArray.AddRange(trackNamesArray);
            LogSource.LogInfo("trackArray stored in storeTrackArray");
            HasFinishedLoadingAudio = true;
            totalLength = 0;
        }

        private void Awake()
        {
            CustomMusicPatch.menuTrackList.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\BobbysMusicPlayer\\CustomMenuMusic\\sounds"));
            if (CustomMusicPatch.menuTrackList.IsNullOrEmpty() && Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\CustomMenuMusic\\sounds"))
            {
                CustomMusicPatch.menuTrackList.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\CustomMenuMusic\\sounds"));
            }
            trackList.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\BobbysMusicPlayer\\Soundtrack\\sounds"));
            if (trackList.IsNullOrEmpty() && Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Soundtrack\\sounds"))
            {
                trackList.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Soundtrack\\sounds"));
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
            string settings = "Soundtrack Settings";
            SoundtrackVolume = Config.Bind<float>(settings, "In-raid music volume", 0.025f, new ConfigDescription("Volume of the music played in raid", new AcceptableValueRange<float>(0f, 1f)));
            SpawnMusicVolume = Config.Bind<float>(settings, "Spawn music volume", 0.06f, new ConfigDescription("Volume of the music played on spawn", new AcceptableValueRange<float>(0f, 1f)));
            LogSource = Logger;
            LogSource.LogInfo("plugin loaded!");
            new CustomMusicPatch().Enable();
            new RaidEndMusicPatch().Enable();
            new UISoundsPatch().Enable();
            customMusicPatch.LoadAudioClips();
        }

        private void Update()
        {
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
                if (!trackList.IsNullOrEmpty())
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
            if (!Audio.soundtrackAudioSource.isPlaying && !Audio.spawnAudioSource.isPlaying && !trackArray.IsNullOrEmpty() && HasFinishedLoadingAudio)
            {
                LogSource.LogInfo("trackArray has " + trackArray.Count + " elements");
                Audio.soundtrackAudioSource.clip = trackArray[0];
                Audio.soundtrackAudioSource.Play();
                LogSource.LogInfo("Playing " + trackNamesArray[0]);
                trackArray.RemoveAt(0);
                trackNamesArray.RemoveAt(0);
                if (trackArray.IsNullOrEmpty())
                {
                    trackArray.AddRange(storedTrackArray);
                    trackNamesArray.AddRange(storedTrackNamesArray);
                    LogSource.LogInfo("Refilled trackArray");
                }
            }
        }
    }
}