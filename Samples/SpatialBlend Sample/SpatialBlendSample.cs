using UnityEngine;
using UnityEngine.UI;

namespace Adrenak.UniMic.Samples {
    public class SpatialBlendSample : MonoBehaviour {
        [SerializeField] MicAudioSource micAudioSource;
        [SerializeField] Slider slider;

        void Start() {
            Mic.Init();

            if (Mic.AvailableDevices.Count == 0) return;

            Mic.AvailableDevices[0].StartRecording();
            micAudioSource.Device = Mic.AvailableDevices[0];

            slider.onValueChanged.AddListener(x => {
                // .StreamedAudioSource.UnityAudioSource gets you access to the actual
                // AudioSource that's playing the mic audio.
                // You can do anything on the AudioSource, except call Play, Pause, Stop or UnPause
                // methods as StreamedAudioSource uses them. Calling those methods will interfere
                // with its functioning.
                micAudioSource.StreamedAudioSource.UnityAudioSource.spatialBlend = x;
            });
        }
    }
}
