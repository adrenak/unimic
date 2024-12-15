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
                    device.OnStartRecording -= OnStartRecording;
                    device.OnFrameCollected -= OnFrameCollected;
                    device.OnStopRecording -= OnStopRecording;
                    Debug.Log("Device removed from MicAudioSource", gameObject);
                }
                if(value != null) {
                    device = value;
                    device.OnStartRecording += OnStartRecording;
                    device.OnFrameCollected += OnFrameCollected;
                    device.OnStopRecording += OnStopRecording;
                    if (device.IsRecording)
                        StreamedAudioSource.Play();
                    else
                        StreamedAudioSource.Stop();
                    Debug.Log("MicAudioSource shifted to " + device.Name, gameObject);
                }
                else
                    StreamedAudioSource.Stop();
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

        void OnStartRecording() {
            StreamedAudioSource.Play();
        }

        void OnFrameCollected(int frequency, int channels, float[] samples) {
            StreamedAudioSource.Feed(frequency, channels, samples);
        }

        void OnStopRecording() {
            StreamedAudioSource.Stop();
        }
    }
}
