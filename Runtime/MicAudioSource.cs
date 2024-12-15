using System.Linq;

using UnityEngine;

namespace Adrenak.UniMic {
    /// <summary>
    /// A simple AudioSource based component that just plays what 
    /// the default <see cref="Mic.Device"/> instance is receiving.
    /// Provides optional feature to start the recording by itself.
    /// NOTE: This component is pretty basic. Also there isn't much
    /// use case for playing back what the user is saying. Use this
    /// class as a reference for your own UniMic usage.
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
