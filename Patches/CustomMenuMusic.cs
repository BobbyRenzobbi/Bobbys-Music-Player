using SPT.Reflection.Patching;
using EFT.UI;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using HarmonyLib;
using Comfort.Common;
using System.Linq;
using SoundtrackMod;
using System.Threading.Tasks;

namespace BobbyRenzobbi.CustomMenuMusic
{
    public class CustomMusicPatch : ModulePatch
    {
        private static List<AudioClip> audioClips = new List<AudioClip>();
        private static System.Random rand = new System.Random();
        internal static List<string> menuTrackList = new List<string>();
        private static List<AudioClip> trackArray = new List<AudioClip>();
        private static List<AudioClip> storedTrackArray = new List<AudioClip>();
        private static List<string> trackListToPlay = new List<string>();
        private static float targetLength = 3600f;
        private static float totalLength = 0f;
        private static List<string> trackNamesArray = new List<string>();
        private static List<string> storedTrackNamesArray = new List<string>();
        internal static bool HasReloadedAudio = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GUISounds), nameof(GUISounds.method_3));
        }

        internal async void LoadAudioClips()
        {
            HasReloadedAudio = true;
            trackArray.Clear();
            trackListToPlay.Clear();
            trackListToPlay.AddRange(menuTrackList);
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
                Plugin.LogSource.LogInfo(trackPath + " has been loaded and added to playlist");
            } while ((totalLength < targetLength) && (!trackListToPlay.IsNullOrEmpty()));
            storedTrackArray.AddRange(trackArray);
            storedTrackNamesArray.AddRange(trackNamesArray);
            Plugin.LogSource.LogInfo("trackArray stored in storeTrackArray");
            totalLength = 0;
        }

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

        [PatchPrefix]
        static bool Prefix()
        {
            if (menuTrackList.IsNullOrEmpty())
            {
                return true;
            }
            CustomMusicPatch patch = new CustomMusicPatch();
            audioClips.Clear();
            audioClips.Add(trackArray[0]);
            audioClips.Add(trackArray[0]);
            trackArray.RemoveAt(0);
            //Credit to SamSWAT for discovering that the game loads infinitely if the audioClip_0 array has only one element
            if (trackArray.IsNullOrEmpty())
            {
                trackArray.AddRange(storedTrackArray);
            }
            Traverse.Create(Singleton<GUISounds>.Instance).Field("audioClip_0").SetValue(audioClips.ToArray());
            Plugin.LogSource.LogInfo("Playing " + trackNamesArray[0]);
            trackNamesArray.RemoveAt(0);
            if (trackNamesArray.IsNullOrEmpty())
            {
                trackNamesArray.AddRange(storedTrackNamesArray);
            }
            return true;
        }
    }
}