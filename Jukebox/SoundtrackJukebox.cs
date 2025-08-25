using BobbysMusicPlayer.Models;
using BobbysMusicPlayer.Utils;
using EFT;
using UnityEngine;

namespace BobbysMusicPlayer.Jukebox
{
    public class SoundtrackJukebox
    {
        public bool SoundtrackCalled = false;
        
        private Coroutine soundtrackCoroutine;
        private AudioManager audio;
        
        private bool paused;
        private int trackCounter;
        private float pausedTime;
        
        public void Init(AudioManager audio)
        {
            this.audio =  audio;
        }
        
        /// <summary>
        /// This method is responsible for handling the "Jukebox" controls of the Ambient Soundtrack
        /// </summary>
        public void CheckSoundtrackControls()
        {
            // Don't allow soundtrack controls if spawn music or combat music is playing
            if (audio.SpawnAudioSource.isPlaying || audio.CombatAudioSource.isPlaying || audio == null) return;
            
            if (Input.GetKeyDown(SettingsModel.Instance.PauseTrack.Value.MainKey) && audio.SoundtrackAudioSource.isPlaying)
            {
                PauseSoundtrack();
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
        
        public void PlaySoundtrack()
        {
            if (!SoundtrackCalled || audio.SoundtrackAudioSource.isPlaying || paused || audio.SpawnAudioSource.isPlaying || audio.CombatAudioSource.isPlaying || audio.AmbientTrackArray.IsNullOrEmpty() || !audio.HasFinishedLoadingAudio || audio == null)
            {
                return;
            }
            
            if (audio.AmbientTrackArray.Count == 1)
            {
                trackCounter = 0;
            }
            
            audio.SoundtrackAudioSource.clip = audio.AmbientTrackArray[trackCounter];
            audio.SoundtrackAudioSource.Play();
            
            BobbysMusicPlayerPlugin.LogSource.LogInfo("[SOUNDTRACK] Play " + audio.AmbientTrackNamesArray[trackCounter]);
            
            trackCounter++;
            if (audio.SoundtrackAudioSource.clip == null)
            {
                BobbysMusicPlayerPlugin.LogSource.LogInfo("[SOUNDTRACK] WTF");
            }
            soundtrackCoroutine = StaticManager.Instance.WaitSeconds(audio.SoundtrackAudioSource.clip.length, PlaySoundtrack);
            
            if (trackCounter >= audio.AmbientTrackArray.Count)
            {
                trackCounter = 0;
            }
        }
        
        #region Controls

        private void PauseSoundtrack()
        {
            audio.SoundtrackAudioSource.Pause();
            StaticManager.Instance.StopCoroutine(soundtrackCoroutine);
            pausedTime = audio.SoundtrackAudioSource.clip.length - audio.SoundtrackAudioSource.time;
            paused = true;
        }

        private void ResumeTrack()
        {
            audio.SoundtrackAudioSource.UnPause();
            soundtrackCoroutine = StaticManager.Instance.WaitSeconds(pausedTime, PlaySoundtrack);
            paused = false;
        }

        private void RestartTrack()
        {
            audio.SoundtrackAudioSource.Stop();
                
            if (trackCounter != 0)
            {
                trackCounter--;
            }
            else
            {
                trackCounter = audio.AmbientTrackArray.Count - 1;
            }
                
            StaticManager.Instance.StopCoroutine(soundtrackCoroutine);
            paused = false;
            PlaySoundtrack();
        }

        private void PreviousTrack()
        {
            audio.SoundtrackAudioSource.Stop();
            trackCounter -= 2;
                
            if (trackCounter < 0)
            {
                trackCounter = audio.AmbientTrackArray.Count + (trackCounter);
            }
                
            StaticManager.Instance.StopCoroutine(soundtrackCoroutine);
            paused = false;
            PlaySoundtrack();
        }
        
        private void SkipTrack()
        {
            audio.SoundtrackAudioSource.Stop();
            StaticManager.Instance.StopCoroutine(soundtrackCoroutine);
            paused = false;
            PlaySoundtrack();
        }
        
        #endregion
    }
}