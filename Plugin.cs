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
using BobbyRenzobbi.CustomMenuMusic;
using HarmonyLib;
using System.Text.RegularExpressions;

namespace SoundtrackMod
{
    [RequireComponent(typeof(AudioSource))]
    public class Audio : MonoBehaviour
    {
        public static AudioSource myaudioSource;
        public static void SetClip(AudioClip clip)
        {
            myaudioSource.clip = clip;
        }
        public static void AdjustVolume(float volume)
        {
            myaudioSource.volume = volume;
        }
    }
    [BepInPlugin("BobbyRenzobbi.MusicPlayer", "BobbysMusicPlayer", "0.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> MusicVolume { get; set; }
        private static System.Random rand = new System.Random();
        private async Task<AudioClip> RequestAudioClip(string path)
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
                Logger.LogError("Soundtrack Mod: Failed To Fetch Audio Clip");
                return null;
            }
            else
            {
                AudioClip audioclip = DownloadHandlerAudioClip.GetContent(uwr);
                return audioclip;
            }
        }
        private static CustomMusicPatch patch = new CustomMusicPatch();
        private static List<AudioClip> trackArray = new List<AudioClip>();
        private static List<AudioClip> storedTrackArray = new List<AudioClip>();
        internal static ManualLogSource LogSource;
        private static List<string> trackList = new List<string>();
        private static List<string> trackListToPlay = new List<string>();
        private static bool HasStartedLoadingAudio = false;
        private static bool HasFinishedLoadingAudio = false;
        private static float targetLength = 3000f;
        private static float totalLength = 0f;
        private static List<string> trackNamesArray = new List<string>();
        private static List<string> storedTrackNamesArray = new List<string>();

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
                AudioClip unityAudioClip = await RequestAudioClip(track);
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
            CustomMusicPatch.menuTrackList.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\CustomMenuMusic\\sounds"));
            trackList.AddRange(Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Soundtrack\\sounds"));
            string settings = "Soundtrack Settings";
            MusicVolume = Config.Bind<float>(settings, "In-raid music volume", 0.025f, new ConfigDescription("Volume of the music heard in raid", new AcceptableValueRange<float>(0f, 1f)));
            LogSource = Logger;
            LogSource.LogInfo("plugin loaded!");
            new CustomMusicPatch().Enable();
            patch.LoadAudioClips();
        }

        private void LateUpdate()
        {
            if (Audio.myaudioSource != null)
            {
                try
                {
                    Audio.AdjustVolume(MusicVolume.Value);
                }
                catch (Exception exception)
                {
                    LogSource.LogError(exception);
                }
            }
            if (Singleton<GameWorld>.Instance == null && !CustomMusicPatch.HasReloadedAudio)
            {
                patch.LoadAudioClips();
            }
            if (Singleton<GameWorld>.Instance == null || (Singleton<GameWorld>.Instance?.MainPlayer is HideoutPlayer))
            {
                HasStartedLoadingAudio = false;
                return;
            }
            CustomMusicPatch.HasReloadedAudio = false;
            if (trackList.IsNullOrEmpty())
            {
                return;
            }
            if (Audio.myaudioSource == null)
            {
                try
                {
                    Audio.myaudioSource = gameObject.GetOrAddComponent<AudioSource>();
                }
                catch (Exception ex)
                {
                    LogSource.LogInfo(ex.Message);
                }
            }
            if (Singleton<GameWorld>.Instance.MainPlayer == null)
            {
                return;
            }
            if (!HasStartedLoadingAudio)
            {
                HasStartedLoadingAudio = true;
                LoadAudioClips();
            }
            if (!Audio.myaudioSource.isPlaying && !trackArray.IsNullOrEmpty() && HasFinishedLoadingAudio && Singleton<AbstractGame>.Instance.Status == GameStatus.Started)
            {
                LogSource.LogInfo("trackArray has " + trackArray.Count + " elements");
                Audio.myaudioSource.clip = trackArray[0];
                Audio.myaudioSource.Play();
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