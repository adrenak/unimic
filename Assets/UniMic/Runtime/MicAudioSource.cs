using System;
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
        public bool autoStart = false;
        public Mic.Device Device { get; private set; }
        public int bufferDurationMS = 200;

        /// <summary>
        /// If we're expecting 480 values per sample, we want a 
        /// clip that's much longer than that so we don't end up
        /// overwriting previous frames before they're played.
        /// This is because if a Device has a frame duration of 10ms
        /// while running on 48000Hz, the device need not send one frame
        /// every 10ms. Several frames may quickly arrive in a 
        /// single render frame one after the other. On the other
        /// hand, sometimes there may be gaps between their arrival.
        /// 
        /// We make the clip something like a buffer that 
        /// adds new audio frames before the previous one is done
        /// playing. To do this, we use a multiplier. The value of the
        /// multiplier is the closest frame duration multiple
        /// that gets it just above <see cref="bufferDurationMS"/>
        /// 
        /// If the frame duration is greater than the buffer duration,
        /// we use a multiple of 2.
        /// 
        /// The result is a smooth audio playback.
        /// 
        /// This adds a delay in starting audio playback and latency.
        /// The greater the buffer duration, the longer the delay and
        /// latency would be. But the audio would also be smoother.
        /// A value of 200 is generally sufficient.
        /// </summary>
        int ClipLengthMultiplier {
            get {
                if (Device == null)
                    return 1;
                if (Device.FrameDurationMS >= bufferDurationMS)
                    return 2;
                return bufferDurationMS / Device.FrameDurationMS + 1;
            }
        }

        AudioClip clip;
        AudioSource audioSource;
        long receivedFrameCount = 0;

        void Awake() {
            Mic.Init();

            audioSource = gameObject.GetComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false;
        }

        void Start() {
            if (Mic.AvailableDevices.Count == 0) {
                Debug.Log("There are no recording devices available");
                return;
            }
            SetDevice(Mic.AvailableDevices[0], autoStart);
        }

        void OnStartRecording() {
            receivedFrameCount = 0;
            clip = AudioClip.Create(
                "clip",
                Device.FrameLength * ClipLengthMultiplier,
                Device.ChannelCount,
                Device.SamplingFrequency,
                false
            );
            var empty = new float[clip.samples];
            for (int i = 0; i < empty.Length; i++)
                empty[i] = 0;
            clip.SetData(empty, 0);
            audioSource.clip = clip;
        }

        void OnFrameCollected(int channels, float[] samples) {
            if (clip.SetData(samples, (int)((receivedFrameCount % ClipLengthMultiplier) * samples.Length)))
                receivedFrameCount++;
            else 
                return;

            if (receivedFrameCount > 0) {
                if (!audioSource.isPlaying) {
                    audioSource.timeSamples = 0;
                    audioSource.Play();
                }
            }
        }

        void OnStopRecording() {
            receivedFrameCount = 0;
            audioSource.Stop();
            clip = null;
        }

        public void SetDeviceByName(string deviceName, bool autoStart = false) {
            var newDevice = Mic.AvailableDevices.Where(x => x.Name == deviceName).First();
            SetDevice(newDevice, autoStart);
        }

        public void SetDevice(Mic.Device device, bool autoStart = false) {
            StopRecording();
            SetDevice(device, autoStart);
        }

        public void StartRecording() {
            if (Device == null)
                return;
            Device.OnStartRecording += OnStartRecording;
            Device.OnFrameCollected += OnFrameCollected;
            Device.OnStopRecording += OnStopRecording;
            Device.StartRecording();
        }

        public void StopRecording() {
            if (Device == null)
                return;
            Device.StopRecording();
            Device.OnStartRecording -= OnStartRecording;
            Device.OnFrameCollected -= OnFrameCollected;
            Device.OnStopRecording -= OnStopRecording;
        }
    }

}
