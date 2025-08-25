using System;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using BobbysMusicPlayer.Data;
using BobbysMusicPlayer.Patches;
using BobbysMusicPlayer.Jukebox;
using BobbysMusicPlayer.Models;
using BobbysMusicPlayer.Utils;

namespace BobbysMusicPlayer
{
    [BepInPlugin("BobbyRenzobbi.MusicPlayer", "BobbysMusicPlayer", "1.2.4")]
    public class BobbysMusicPlayerPlugin : BaseUnityPlugin
    {
        public static BobbysMusicPlayerPlugin Instance { get; private set; }
        
        private SettingsModel _settings;
        private AudioManager _audio;
        private AudioCache _cache;
        private MenuMusicJukebox _menuMusicJukebox;
        private SoundtrackJukebox _soundtrackJukebox;
        
        internal static ManualLogSource LogSource;
        
        public static bool InRaid { get; set; }
        
        private void Awake()
        {
            LogSource = Logger;
            LogSource.LogInfo("Plugin loading...");
            try
            {
                Instance = this;
                
                // Init config
                _settings = SettingsModel.Create(Config);

                GlobalData.EnvironmentDict[EnvironmentType.Indoor] = _settings.IndoorMultiplier.Value;
                
                // Initialization audio side
                _audio = new AudioManager();
                _audio.Init(gameObject);
                
                // Initialize audio cache in background to avoid blocking
                _cache = new AudioCache();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _cache.InitializeAsync();
                        MenuMusicPatch.LoadAudioClips();
                        UISoundsPatch.LoadUIClips();
                        LogSource.LogInfo("[AUDIO CACHE] Background initialization completed");
                    }
                    catch (Exception e)
                    {
                        LogSource.LogError($"[AUDIO CACHE] Background initialization failed: {e.Message}");
                    }
                });

                // Init audio controls
                _soundtrackJukebox = new SoundtrackJukebox();
                _soundtrackJukebox.Init(_audio);

                _menuMusicJukebox = new MenuMusicJukebox();
                _menuMusicJukebox.Init(_audio, _soundtrackJukebox);

                new MenuMusicPatch().Enable();
                new RaidEndMusicPatch().Enable();
                new UISoundsPatch().Enable();
                new ShotAtPatch().Enable();
                new PlayerFiringPatch().Enable();
                new DamageTakenPatch().Enable();
                new ShotFiredNearPatch().Enable();
                new GrenadePatch().Enable();
                new MenuMusicMethod8Patch().Enable();
                new StopMenuMusicPatch().Enable();
                new OnGameWorldStartPatch().Enable();
                new OnGameWorldDisposePatch().Enable();

                LogSource.LogInfo("Plugin loaded!");
            }
            catch (Exception e)
            {
                LogSource.LogError($"Error: {e.Message}");
            }
        }

        private void Update()
        {
#if DEBUG
            // Debug keybind for testing spawn music
            if (_settings.KeyBind.Value.IsDown())
            {
                foreach (var clip in _cache.GetAllCache())
                {
                    LogSource.LogInfo($"Name: {clip.Key} | Lenght: {clip.Value.clipBytes.Length}");
                }
                // _audio.PlaySpawnMusic(false);
            }
#endif

            _menuMusicJukebox.CheckMenuMusicControls();
            
            if (!InRaid)
            {
                if (!MenuMusicPatch.HasReloadedAudio)
                {
                    MenuMusicPatch.LoadAudioClips();
                    UISoundsPatch.LoadUIClips();
                }
                _soundtrackJukebox.SoundtrackCalled = false;
                _audio.HasStartedLoadingAudio = false;
                _audio.SpawnTrackHasPlayed = false;
                return;
            }

            _audio.PrepareRaidAudioClips();
#if DEBUG
            OverlayDebug.Instance.UpdateOverlay();
#endif

            // Play spawn music only once when raid starts
            if (!_audio.SpawnTrackHasPlayed)
            {
                _audio.PlaySpawnMusic();
            }

            _audio.VolumeSetter();
            _audio.CombatMusic();

            _soundtrackJukebox.CheckSoundtrackControls();

            // Only start soundtrack after spawn music has finished playing
            if (_audio.SpawnTrackHasPlayed && !_audio.SpawnAudioSource.isPlaying)
            {
                _soundtrackJukebox.SoundtrackCalled = true;
                _soundtrackJukebox.PlaySoundtrack();
            }
        }

        public AudioManager GetAudio() => _audio;
        public MenuMusicJukebox GetMenuMusicJukeBox() => _menuMusicJukebox;
        public AudioCache GetCache() => _cache;
    }
}