using UnityEngine;

namespace Adrenak.UniMic.Samples {
    public class SimpleMicAudioSourceSample : MonoBehaviour {
        [SerializeField] MicAudioSource micAudioSource;

        void Start() {
            Mic.Init();

            if(Mic.AvailableDevices.Count > 0) {
                Mic.AvailableDevices[0].StartRecording();
                micAudioSource.Device = Mic.AvailableDevices[0];
            }
        }
    }
}
