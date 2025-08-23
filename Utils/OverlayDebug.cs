#if DEBUG
using BobbysMusicPlayer.Models;
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
            _overlayText.fontSize = SettingsModel.Instance.FontSizeDebug.Value;
            _overlayText.color = Color.white;
            _overlayText.alignment = TextAlignmentOptions.TopLeft;
            _overlayText.enableWordWrapping = false;

            var rectTransform = _overlayText.rectTransform;
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.sizeDelta = new Vector2(800, 200);
            
            SetOverlayPosition(new Vector2(SettingsModel.Instance.PositionXDebug.Value, SettingsModel.Instance.PositionYDebug.Value));
            UpdateOverlay();
        }
        
        public void UpdateOverlay()
        {
            if (!_overlayText) return;

            var audio = BobbysMusicPlayerPlugin.Instance.GetAudio();
            
            if (audio == null) return;
            
            _overlayText.text = $"Combat Timer -> {audio.CombatTimer}" + 
                                $"\n" +
                                $"Headset Multiplier -> {audio.HeadsetMultiplier}" + 
                                $"\n" +
                                $"Current Env.Multiplier -> {audio.CurrentEnvironmentMultiplier}" + 
                                $"\n" +
                                $"[CombatLerp Volume Data]\n" +
                                $"   CombatAudioSource Volume -> {Mathf.Lerp(0f, audio.CombatMusicVolume, audio.Lerp)}\n" +
                                $"   SoundtrackAudioSource Volume -> {Mathf.Lerp(audio.SoundtrackVolume, SettingsModel.Instance.AmbientCombatMultiplier.Value * audio.SoundtrackVolume, audio.Lerp)}\n" +
                                $"   SpawnAudioSource Volume -> {Mathf.Lerp(audio.SpawnMusicVolume, SettingsModel.Instance.AmbientCombatMultiplier.Value * audio.SpawnMusicVolume, audio.Lerp)}\n" +
                                $"\n" +
                                $"[VolumeSetter Volume Data]\n" +
                                $"   CombatAudioSource Volume -> {audio.CombatMusicVolume}\n" +
                                $"   SoundtrackAudioSource Volume -> {audio.SoundtrackVolume}\n" +
                                $"   SpawnAudioSource Volume -> {audio.SpawnMusicVolume}\n" +
                                $"\n" +
                                $"[AudioSource is playing?]\n" +
                                $"   CombatAudioSource isPlay? -> {audio.CombatAudioSource?.isPlaying}\n" +
                                $"   SoundtrackAudioSource isPlay? -> {audio.SoundtrackAudioSource?.isPlaying}\n" +
                                $"   SpawnAudioSource isPlay? -> {audio.SpawnAudioSource?.isPlaying}\n";
        }

        public void SetOverlayPosition(Vector2 anchoredPosition)
        {
            if (_overlayText)
                _overlayText.rectTransform.anchoredPosition = anchoredPosition;
        }
        
        public void SetFontSize(int size)
        {
            if (_overlayText)
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
