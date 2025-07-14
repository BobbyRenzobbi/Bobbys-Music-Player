#if DEBUG
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BobbysMusicPlayer.Utils
{
    public class OverlayDebug: MonoBehaviour
    {
        private static OverlayDebug _instance;
        public static OverlayDebug Instance => _instance ??= new OverlayDebug();
        
        private TextMeshProUGUI _overlayText;
        private GameObject _overlay;
        
        public void Enable()
        {
            _instance = this;
            
            _overlay = new GameObject("[BobbysMusicPlayer] Overlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(_overlay);
            var canvas = _overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = _overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            var textObj = new GameObject("[BobbysMusicPlayer] OverlayText", typeof(RectTransform));
            textObj.transform.SetParent(_overlay.transform, false);

            _overlayText = textObj.AddComponent<TextMeshProUGUI>();
            _overlayText.text = "Overlay BobbysMusicPlayer initialized";
            _overlayText.fontSize = Plugin.FontSizeDebug.Value;
            _overlayText.color = Color.white;
            _overlayText.alignment = TextAlignmentOptions.TopLeft;
            _overlayText.enableWordWrapping = false;

            var rectTransform = _overlayText.rectTransform;
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.sizeDelta = new Vector2(800, 200);
            
            SetOverlayPosition(new Vector2(Plugin.PositionXDebug.Value, Plugin.PositionYDebug.Value));
            UpdateOverlay();
        }
        
        public void UpdateOverlay()
        {
            if (_overlayText == null) return;
            
            _overlayText.text = $"InRaid -> {Plugin.InRaid}\n" +
                                $"\n" +
                                $"Combat Timer -> {Plugin.combatTimer}" + 
                                $"\n" +
                                $"[CombatLerp Volume Data]\n" +
                                $"CombatAudioSource Volume -> {Mathf.Lerp(0f, Plugin.combatMusicVolume, Plugin.lerp)}\n" +
                                $"SoundtrackAudioSource Volume -> {Mathf.Lerp(Plugin.soundtrackVolume, Plugin.AmbientCombatMultiplier.Value*Plugin.soundtrackVolume, Plugin.lerp)}\n" +
                                $"SpawnAudioSource Volume -> {Mathf.Lerp(Plugin.spawnMusicVolume, Plugin.AmbientCombatMultiplier.Value*Plugin.spawnMusicVolume, Plugin.lerp)}\n" +
                                $"\n" +
                                $"[VolumeSetter Volume Data]\n" +
                                $"CombatAudioSource Volume -> {Plugin.combatMusicVolume}\n" +
                                $"SoundtrackAudioSource Volume -> {Plugin.soundtrackVolume}\n" +
                                $"SpawnAudioSource Volume -> {Plugin.spawnMusicVolume}\n" +
                                $"\n" +
                                $"[AudioSource is playing?]\n" +
                                $"CombatAudioSource isPlay? -> {Audio.combatAudioSource?.isPlaying}\n" +
                                $"SoundtrackAudioSource isPlay? -> {Audio.soundtrackAudioSource?.isPlaying}\n" +
                                $"SpawnAudioSource isPlay? -> {Audio.spawnAudioSource?.isPlaying}\n";
                                
        }

        public void SetOverlayPosition(Vector2 anchoredPosition)
        {
            if (_overlayText != null)
                _overlayText.rectTransform.anchoredPosition = anchoredPosition;
        }
        
        public void SetFontSize(int size)
        {
            if (_overlayText != null)
                _overlayText.fontSize = size;
        }

        public void Disable()
        {
            Destroy(_overlay);
            Destroy(this);
        }
    }
}
#endif