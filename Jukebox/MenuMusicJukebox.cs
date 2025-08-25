using System;
using BobbysMusicPlayer.Models;
using BobbysMusicPlayer.Patches;
using BobbysMusicPlayer.Utils;
using Comfort.Common;
using EFT;
using EFT.UI;
using UnityEngine;

namespace BobbysMusicPlayer.Jukebox
{
    public class MenuMusicJukebox
    {
        public Coroutine Coroutine;
        
        private AudioManager audio;
        private SoundtrackJukebox soundtrackJukebox;
        
        private static bool paused;
        private static float pausedTime;

        public void Init(AudioManager audio, SoundtrackJukebox soundtrackJukebox)
        {
            this.audio = audio;
            this.soundtrackJukebox = soundtrackJukebox;
        }
        
        /// <summary>
        /// This method is responsible for handling the "Jukebox" controls of the Main Menu
        /// </summary>
        public void CheckMenuMusicControls()
        {
            if (soundtrackJukebox.SoundtrackCalled || audio.MenuMusicAudioSource == null || audio == null) return;
            
            if (Input.GetKeyDown(SettingsModel.Instance.PauseTrack.Value.MainKey) && audio.MenuMusicAudioSource.isPlaying)
            {
                PauseTrack();
            }
            else if (Input.GetKeyDown(SettingsModel.Instance.PauseTrack.Value.MainKey) && paused)
            {
                ResumeTrack(); 
            }
            
            if (Input.GetKeyDown(SettingsModel.Instance.RestartTrack.Value.MainKey))
            {
                RestartTrack();
            }
            
            if (Input.GetKeyDown(SettingsModel.Instance.PreviousTrack.Value.MainKey))
            {
                PreviousTrack();
            }
            
            if (Input.GetKeyDown(SettingsModel.Instance.SkipTrack.Value.MainKey))
            {
                SkipTrack();
            }
        }

        #region Controls

        private void PauseTrack()
        {
            audio.MenuMusicAudioSource.Pause();
            StaticManager.Instance.StopCoroutine(Coroutine);
            pausedTime = audio.MenuMusicAudioSource.clip.length - audio.MenuMusicAudioSource.time;
            paused = true;
        }

        private void ResumeTrack()
        {
            audio.MenuMusicAudioSource.UnPause();
            Coroutine = StaticManager.Instance.WaitSeconds(pausedTime, new Action(Singleton<GUISounds>.Instance.method_3));
            paused = false;
        }

        private void RestartTrack()
        {
            audio.MenuMusicAudioSource.Stop();
            if (MenuMusicPatch.trackCounter != 0)
            {
                MenuMusicPatch.trackCounter--;
            }
            else
            {
                MenuMusicPatch.trackCounter = MenuMusicPatch.trackArray.Count - 1;
            }
            StaticManager.Instance.StopCoroutine(Coroutine);
            paused = false;
            Singleton<GUISounds>.Instance.method_3();
        }

        private void PreviousTrack()
        {
            audio.MenuMusicAudioSource.Stop();
            MenuMusicPatch.trackCounter -= 2;
            if (MenuMusicPatch.trackCounter < 0)
            {
                MenuMusicPatch.trackCounter = MenuMusicPatch.trackArray.Count + (MenuMusicPatch.trackCounter);
            }
            StaticManager.Instance.StopCoroutine(Coroutine);
            paused = false;
            Singleton<GUISounds>.Instance.method_3();
        }

        private void SkipTrack()
        {
            audio.MenuMusicAudioSource.Stop();
            StaticManager.Instance.StopCoroutine(Coroutine);
            paused = false;
            Singleton<GUISounds>.Instance.method_3();
        }

        #endregion
    }
}