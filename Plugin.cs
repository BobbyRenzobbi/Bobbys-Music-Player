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
        private static AudioClip unityAudioClip;
        private static System.Random rand = new System.Random();
        private async void RequestAudioClip(string path)
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
                return;
            }
            else
            {
                unityAudioClip = DownloadHandlerAudioClip.GetContent(uwr);
                return;
            }
        }

        private static Dictionary<string, AudioClip> tracks = new Dictionary<string, AudioClip>();
        internal static ManualLogSource LogSource;
        private static string track = "";
        private static string trackPath = "";
        private static List<string> trackList = new List<string>();
        private static List<string> unPlayedTrackList = new List<string>();
        private static string lastTrack = "";
        private static int rndNumber = 0;
        public static ConfigEntry<float> MusicVolume { get; set; }
        private void LoadNextTrack()
        {
            if (unPlayedTrackList.IsNullOrEmpty())
            {
                unPlayedTrackList.AddRange(trackList);
            }
            tracks.Clear();

            do
            {
                rndNumber = rand.Next(unPlayedTrackList.Count);
                track = unPlayedTrackList[rndNumber];
            } 
            while ((track == lastTrack) && trackList.Count > 1);

            unPlayedTrackList.Remove(track);
            lastTrack = track;
            trackPath = Path.GetFileName(track);
            RequestAudioClip(track);
            LogSource.LogInfo("loaded " + trackPath);
            tracks[trackPath] = unityAudioClip;
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
            if (trackList.IsNullOrEmpty())
            {
                return;
            }
            if (Singleton<GameWorld>.Instance == null || (Singleton<GameWorld>.Instance?.MainPlayer is HideoutPlayer))
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
            if (!Audio.myaudioSource.isPlaying)
            {
                LoadNextTrack();
                Audio.SetClip(tracks[trackPath]);
                Audio.myaudioSource.Play();
            }
        }
    }
}
