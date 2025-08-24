using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using BobbysMusicPlayer.Data;

namespace BobbysMusicPlayer.Utils
{
    /// <summary>
    /// Centralized audio caching system to avoid reloading the same audio files
    /// </summary>
    public class AudioCache
    {
        private readonly Dictionary<string, AudioClipData> _audioClipCache = new();
        private readonly Dictionary<string, List<AudioClipData>> _playlistCache = new();
        private bool _isInitialized = false;
        private CancellationTokenSource _initializationCancellationTokenSource;
        
        /// <summary>
        /// Initialize the audio cache with all available music files
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;
            
            _isInitialized = true;
            _initializationCancellationTokenSource = new CancellationTokenSource();
            
            BobbysMusicPlayerPlugin.LogSource.LogInfo("[AUDIO CACHE] Initializing audio cache...");
            
            try
            {
                var allMusicFiles = new List<string>();
                
                // Collect all music file paths
                CollectMusicFiles(allMusicFiles);
                
                // Remove duplicates and cache unique files
                var uniqueFiles = allMusicFiles.Distinct().ToList();
                BobbysMusicPlayerPlugin.LogSource.LogInfo($"[AUDIO CACHE] Found {uniqueFiles.Count} unique music files to cache");
                
                // Cache files with limited concurrency to avoid hanging
                await CacheFilesWithLimitedConcurrency(uniqueFiles, _initializationCancellationTokenSource.Token);
                
                BobbysMusicPlayerPlugin.LogSource.LogInfo($"[AUDIO CACHE] Audio cache initialized with {_audioClipCache.Count} clips");
            }
            catch (OperationCanceledException)
            {
                BobbysMusicPlayerPlugin.LogSource.LogInfo("[AUDIO CACHE] Initialization was cancelled");
                _isInitialized = false;
            }
            catch (Exception e)
            {
                BobbysMusicPlayerPlugin.LogSource.LogError($"[AUDIO CACHE] Failed to initialize cache: {e}");
                _isInitialized = false; // Reset flag on error
            }
            finally
            {
                _initializationCancellationTokenSource?.Dispose();
                _initializationCancellationTokenSource = null;
            }
        }
        
        /// <summary>
        /// Cache files with limited concurrency to prevent hanging
        /// </summary>
        private async Task CacheFilesWithLimitedConcurrency(List<string> filePaths, CancellationToken cancellationToken)
        {
            const int maxConcurrentTasks = 3;
            var semaphore = new SemaphoreSlim(maxConcurrentTasks);
            var tasks = new List<Task>();
            var completedCount = 0;
            var totalCount = filePaths.Count;
            
            BobbysMusicPlayerPlugin.LogSource.LogInfo($"[AUDIO CACHE] Starting to cache {totalCount} files with max {maxConcurrentTasks} concurrent downloads");
            
            foreach (var filePath in filePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await semaphore.WaitAsync(cancellationToken);
                
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await CacheAudioFileAsync(filePath);
                        completedCount++;
                        
                        // Log progress every 10 files or when complete
                        if (completedCount % 10 == 0 || completedCount == totalCount)
                        {
                            var progress = (float)completedCount / totalCount * 100f;
                            BobbysMusicPlayerPlugin.LogSource.LogInfo($"[AUDIO CACHE] Progress: {completedCount}/{totalCount} ({progress:F1}%)");
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken);
                
                tasks.Add(task);
            }
            
            // Wait for all tasks to complete
            await Task.WhenAll(tasks);
            BobbysMusicPlayerPlugin.LogSource.LogInfo($"[AUDIO CACHE] Completed caching {completedCount}/{totalCount} files");
        }
        
        /// <summary>
        /// Collect all music file paths from various directories
        /// </summary>
        private void CollectMusicFiles(List<string> allMusicFiles)
        {
            // Menu music
            if (Directory.Exists(PathData.CustomMenuMusicSounds))
                allMusicFiles.AddRange(Directory.GetFiles(PathData.CustomMenuMusicSounds));
                
            // Default soundtrack
            if (Directory.Exists(PathData.SoundtrackDefault))
                allMusicFiles.AddRange(Directory.GetFiles(PathData.SoundtrackDefault));
                
            // Combat music
            if (Directory.Exists(PathData.SoundtrackCombat))
                allMusicFiles.AddRange(Directory.GetFiles(PathData.SoundtrackCombat));
                
            // Spawn music
            if (Directory.Exists(PathData.SoundtrackSpawn))
                allMusicFiles.AddRange(Directory.GetFiles(PathData.SoundtrackSpawn));
                
            // Death music
            if (Directory.Exists(PathData.SoundtrackDeath))
                allMusicFiles.AddRange(Directory.GetFiles(PathData.SoundtrackDeath));
            
            // Extract music
            if (Directory.Exists(PathData.SoundtrackExtract))
                allMusicFiles.AddRange(Directory.GetFiles(PathData.SoundtrackExtract));
                
            // Map-specific tracks
            foreach (var mapTracks in GlobalData.MapDictionary.Values)
            {
                if (!mapTracks.IsNullOrEmpty())
                    allMusicFiles.AddRange(mapTracks);
            }
            
            // UI sounds
            foreach (var uiSoundDir in GlobalData.UISoundsDir)
            {
                var uiSoundPath = PathData.SoundtrackUI + uiSoundDir;
                if (Directory.Exists(uiSoundPath))
                    allMusicFiles.AddRange(Directory.GetFiles(uiSoundPath));
            }
        }
        
        /// <summary>
        /// Cache a single audio file
        /// </summary>
        private async Task CacheAudioFileAsync(string filePath)
        {
            if (_audioClipCache.ContainsKey(filePath)) return;
            
            try
            {
                // Add timeout to prevent hanging on individual files
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30 second timeout
                
                var audioClip = await AsyncRequestAudioClip(filePath, cts.Token);

                if (audioClip != null)
                {
                    _audioClipCache[filePath] = new AudioClipData(audioClip);
                    BobbysMusicPlayerPlugin.LogSource.LogInfo($"[AUDIO CACHE] Cached: {Path.GetFileName(filePath)}. Length: {audioClip.length}");
                }
            }
            catch (OperationCanceledException)
            {
                BobbysMusicPlayerPlugin.LogSource.LogWarning($"[AUDIO CACHE] Timeout while caching {Path.GetFileName(filePath)}");
            }
            catch (Exception e)
            {
                BobbysMusicPlayerPlugin.LogSource.LogWarning($"[AUDIO CACHE] Failed to cache {Path.GetFileName(filePath)}: {e.Message}");
            }
        }
        
        /// <summary>
        /// Get AudioClip from cache or load it if not cached
        /// </summary>
        public async Task<AudioClip> GetOrCacheAudioClip(string filePath)
        {
            if (_audioClipCache.ContainsKey(filePath))
            {
                BobbysMusicPlayerPlugin.LogSource.LogInfo($"[AUDIO CACHE] GET {filePath}");
                return _audioClipCache[filePath].Get();
            }
            
            // If not in cache, load and cache it
            var audioClip = await AsyncRequestAudioClip(filePath);

            if (audioClip != null)
            {
                _audioClipCache[filePath] = new AudioClipData(audioClip);
            }
            
            BobbysMusicPlayerPlugin.LogSource.LogInfo($"[AUDIO CACHE] REQUEST {filePath}");
            return audioClip;
        }
        
        /// <summary>
        /// Get AudioClipData from cache or load it if not cached
        /// </summary>
        public async Task<AudioClipData> GetOrCacheAudioClipData(string filePath)
        {
            if (_audioClipCache.ContainsKey(filePath))
            {
                BobbysMusicPlayerPlugin.LogSource.LogInfo($"[AUDIO CACHE] GET {filePath}");
                return _audioClipCache[filePath];
            }
            
            // If not in cache, load and cache it
            var audioClip = await AsyncRequestAudioClip(filePath);

            if (audioClip != null)
            {
                _audioClipCache[filePath] = new AudioClipData(audioClip);
            }
            
            BobbysMusicPlayerPlugin.LogSource.LogInfo($"[AUDIO CACHE] REQUEST {filePath}");
            return _audioClipCache[filePath];
        }
        
        public AudioClip GetCacheAudioClip(string filePath)
        {
            if (_audioClipCache.ContainsKey(filePath))
            {
                BobbysMusicPlayerPlugin.LogSource.LogInfo($"[AUDIO CACHE] GET {filePath}");
                return _audioClipCache[filePath].Get();
            }

            return null;
        }
        
        /// <summary>
        /// Get a cached playlist by key (e.g., "spawn", "combat", "menu")
        /// </summary>
        public List<AudioClipData> GetCachedPlaylist(string key)
        {
            return _playlistCache.ContainsKey(key) ? _playlistCache[key] : new List<AudioClipData>();
        }
        
        /// <summary>
        /// Cache a playlist by key
        /// </summary>
        public void CachePlaylist(string key, List<AudioClipData> clips)
        {
            _playlistCache[key] = clips;
        }
        
        /// <summary>
        /// Check if a playlist is cached
        /// </summary>
        public bool IsPlaylistCached(string key)
        {
            return _playlistCache.ContainsKey(key) && _playlistCache[key].Count > 0;
        }

        public Dictionary<string, AudioClipData> GetAllCache()
        {
            return _audioClipCache;
        }
        
        /// <summary>
        /// Clear specific playlist from cache
        /// </summary>
        public void ClearPlaylist(string key)
        {
            if (_playlistCache.ContainsKey(key))
            {
                _playlistCache.Remove(key);
            }
        }
        
        /// <summary>
        /// Clear entire cache when memory usage is high or when explicitly requested
        /// </summary>
        public void ClearCache()
        {
            if (_audioClipCache.Count > 0)
            {
                BobbysMusicPlayerPlugin.LogSource.LogInfo($"[AUDIO CACHE] Clearing audio cache ({_audioClipCache.Count} clips)");
                _audioClipCache.Clear();
                _playlistCache.Clear();
                _isInitialized = false;
            }
        }
        
        /// <summary>
        /// Force reload of all audio clips (useful when music files are changed)
        /// </summary>
        public async Task ForceReloadAsync()
        {
            BobbysMusicPlayerPlugin.LogSource.LogInfo("[AUDIO CACHE] Force reload requested - clearing cache and reinitializing");
            ClearCache();
            await InitializeAsync();
        }
        
        /// <summary>
        /// Stop the current initialization process
        /// </summary>
        public void StopInitialization()
        {
            if (_initializationCancellationTokenSource != null && !_initializationCancellationTokenSource.IsCancellationRequested)
            {
                BobbysMusicPlayerPlugin.LogSource.LogInfo("[AUDIO CACHE] Stopping initialization...");
                _initializationCancellationTokenSource.Cancel();
            }
        }
        
        /// <summary>
        /// Async load audio file as AudioClip
        /// </summary>
        private static async Task<AudioClip> AsyncRequestAudioClip(string path, CancellationToken cancellationToken = default)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();

            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, GlobalData.AudioTypes[extension]))
            {
                ((DownloadHandlerAudioClip)uwr.downloadHandler).streamAudio = false;
                var operation = uwr.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    BobbysMusicPlayerPlugin.LogSource.LogError(
                        $"Soundtrack: Failed to fetch audio clip -> '{path}', Error: {uwr.error}"
                    );
                    return null;
                }
                
                var original = DownloadHandlerAudioClip.GetContent(uwr);
                
                var bytes = AudioClipToBytes(original);
                var stableClip = BytesToAudioClip(bytes, original.name, original.channels, original.frequency);
                
                return stableClip;
            }
        }
        
        public static byte[] AudioClipToBytes(AudioClip clip)
        {
            if (clip == null)
                return null;
            
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
            
            byte[] bytes = new byte[samples.Length * sizeof(float)];
            Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);

            return bytes;
        }
        
        public static AudioClip BytesToAudioClip(byte[] bytes, string name, int channels, int frequency)
        {
            if (bytes == null || bytes.Length == 0)
                return null;
            
            float[] samples = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, samples, 0, bytes.Length);
            
            int sampleCount = samples.Length / channels;
            AudioClip clip = AudioClip.Create(name, sampleCount, channels, frequency, false);
            clip.SetData(samples, 0);

            return clip;
        }
    }
}