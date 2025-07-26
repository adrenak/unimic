using UnityEngine;

namespace Adrenak.UniMic {
    /// <summary>
    /// A wrapper over StreamedAudioSource to play what a <see cref="Mic.Device"/>
    /// is capturing. 
    /// </summary>
    [RequireComponent(typeof(StreamedAudioSource))]
    public class MicAudioSource : MonoBehaviour {
        [SerializeField] Mic.Device device;
        public Mic.Device Device {
            get => device;
            set {
                if (device != null) {
                    device.OnFrameCollected -= OnFrameCollected;
                    Debug.Log("Device removed from MicAudioSource", gameObject);
                }
                if (value != null) {
                    device = value;
                    device.OnFrameCollected += OnFrameCollected;
                    Debug.Log("MicAudioSource shifted to " + device.Name, gameObject);
                }
            }
        }

        StreamedAudioSource streamedAudioSource;
        public StreamedAudioSource StreamedAudioSource {
            get {
                if (streamedAudioSource == null)
                    streamedAudioSource = gameObject.GetComponent<StreamedAudioSource>();
                if (streamedAudioSource == null)
                    streamedAudioSource = gameObject.AddComponent<StreamedAudioSource>();
                return streamedAudioSource;
            }
        }

        void OnFrameCollected(int frequency, int channels, float[] samples) {
            StreamedAudioSource.Feed(frequency, channels, samples);
        }
    }
}
