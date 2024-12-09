using System.Linq;

using UnityEngine;

namespace Adrenak.UniMic {
    /// <summary>
    /// A simple AudioSource based component that just plays what 
    /// the <see cref="Mic"/> instance is receiving.
    /// Provides optional feature to start the recording by itself (as a testing tool)
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MicAudioSource : MonoBehaviour {
        public bool autoStart = true;
        public int clipLengthMultiplier = 10;
        public int frameDurationMS = 20;
        public Mic.Device myDevice;

        AudioClip clip;
        AudioSource audioSource;

        int count = 0;

        void Start() {
            Mic.Init();

            audioSource = gameObject.GetComponent<AudioSource>();
            audioSource.loop = true;

            if (autoStart) {
                if (Mic.AvailableDevices.Count == 0) {
                    Debug.Log("There are no recording devices available");
                    return;
                }
                Play(Mic.AvailableDevices[0]);
            }
        }

        // We create an audio clip much longer than the frame length.
        // This is because frames don't arrive at regular intervals and
        // we don't want to overwrite the clip while we're still playing
        // the last frame.
        void OnStartRecording() {
            if (clip != null)
                Destroy(clip);
            clip = AudioClip.Create(
                "clip",
                myDevice.FrameLength * clipLengthMultiplier,
                myDevice.ChannelCount,
                myDevice.SamplingFrequency,
                false
            );
            audioSource.clip = clip;
            audioSource.Play();
        }

        // Since the clip is longer than the length of samples we're expecting,
        // we need to set the data at the right place by using an offset
        void OnFrameCollected(int channels, float[] samples) {
            clip.SetData(samples, (count % clipLengthMultiplier) * samples.Length);
            count++;
        }

        public void SetDevice(string deviceName) {
            if(myDevice != null) {
                myDevice.StopRecording();
                myDevice.OnStartRecording -= OnStartRecording;
                myDevice.OnFrameCollected -= OnFrameCollected;
            }
            var newDevice = Mic.AvailableDevices.Where(x => x.Name == deviceName).First();
            Play(newDevice);
        }

        void Play(Mic.Device device) {
            myDevice = device;
            myDevice.OnStartRecording += OnStartRecording;
            myDevice.OnFrameCollected += OnFrameCollected;
            myDevice.StartRecording(frameDurationMS);
        }
    }

}
