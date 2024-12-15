using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Adrenak.UniMic {
    /// <summary>
    /// Provides access to all the recording devices available 
    /// </summary>
    public class Mic : MonoBehaviour {
        /// <summary>
        /// Provides information and APIs for a single recording device.
        /// </summary>
        public class Device {
            /// <summary>
            /// The default duration of the frames in milliseconds
            /// </summary>
            public const int DEFAULT_FRAME_DURATION_MS = 20;

            /// <summary>
            /// Invoked when the instance starts Recording.
            /// </summary>
            public event Action OnStartRecording;

            /// <summary>
            /// Invoked everytime an audio sample is collected.
            /// Params: (sampling frequency, channel count, PCM samples)
            /// You use the channel count provided to be able to react
            /// to it changing
            /// </summary>
            public event Action<int, int, float[]> OnFrameCollected;

            /// <summary>
            /// Invoked when the instance stop Recording.
            /// </summary>
            public event Action OnStopRecording;

            /// <summary>
            /// The name of the recording device
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// The maximum sampling frequency this device supports
            /// </summary>
            public int MaxFrequency { get; private set; }

            /// <summary>
            /// The minimum sampling frequency this device supports
            /// </summary>
            public int MinFrequency { get; private set; }

            /// <summary>
            /// If this device is capable of supporting any sampling frequency
            /// </summary>
            public bool SupportsAnyFrequency =>
                MaxFrequency == 0 && MinFrequency == 0;

            float volumeMultiplier = 1;
            /// <summary>
            /// Multiplies the incoming PCM samples by the given value
            /// to increase/decrease the volume. Default: 1
            /// </summary>
            public float VolumeMultiplier {
                get => volumeMultiplier;
                set => volumeMultiplier = value;
            }

            int samplingFrequency;
            /// <summary>
            /// The sampling frequency this device is recording at
            /// </summary>
            public int SamplingFrequency {
                get => samplingFrequency;
                private set {
                    if (!SupportsAnyFrequency && value > MaxFrequency || value < MinFrequency)
                        throw new Exception("Sampling frequency cannot be set out of min and max range");
                    samplingFrequency = value;
                }
            }

            int frameDurationMS;
            /// <summary>
            /// The duration of the audio frame (in milliseconds) that would be reported by the device.
            /// Note that, for example, setting this value to 50 does not mean you would predictably 
            /// receive 20 frames representing 50ms of audio at fixed and regular intervals. 
            /// Often times, sent multiple may be sent multiple times in a single game frame or with 
            /// varying intervals between the frames. 
            /// For playback, consider creating a buffer. See <see cref="MicAudioSource"/> for references.
            /// </summary>
            public int FrameDurationMS {
                get => frameDurationMS;
                private set {
                    if (value <= 0)
                        throw new Exception("FrameDurationMS cannot be zero or negative");
                    frameDurationMS = value;
                }
            }

            /// <summary>
            /// The length of a single PCM frame array that will be sent
            /// via <see cref="OnFrameCollected"/>
            /// </summary>
            public int FrameLength =>
                SamplingFrequency / 1000 * FrameDurationMS * ChannelCount;

            /// <summary>
            /// The number of channels the audio is captured into.
            /// Note that this value is made available ONLY after recording 
            /// starts and resets to 0 when it stops.
            /// Also note that depending on the device, channel count can be
            /// changed while the recording is ongoing use <see cref="OnFrameCollected"/>
            /// to react to such changes.
            /// </summary>
            public int ChannelCount => GetChannelCount(this);

            internal Device(string name, int maxFrequency, int minFrequency) {
                Name = name;
                MaxFrequency = maxFrequency;
                MinFrequency = minFrequency;
                samplingFrequency = maxFrequency;
            }

            /// <summary>
            /// Start recording audio using this device at the maximum supported
            /// sampling frequency and given frame duration
            /// </summary>
            /// <param name="frameDurationMS">The audio length of one frame (in MS)</param>
            public void StartRecording(int frameDurationMS = DEFAULT_FRAME_DURATION_MS) {
                StartRecording(MaxFrequency, frameDurationMS);
            }

            /// <summary>
            /// Start recording audio using this device at the provided sampling frequency
            /// and frame duration
            /// </summary>
            /// <param name="samplingFrequency"></param>
            /// <param name="frameDurationMS"></param>
            public void StartRecording(int samplingFrequency, int frameDurationMS = DEFAULT_FRAME_DURATION_MS) {
                // Return if we're already recording at the same value
                if (SamplingFrequency == samplingFrequency
                && FrameDurationMS == frameDurationMS
                && IsRecording)
                    return;

                Mic.StopRecording(this);
                SamplingFrequency = samplingFrequency;
                FrameDurationMS = frameDurationMS;

                Mic.StartRecording(this);
                if (IsRecording)
                    OnStartRecording?.Invoke();
            }

            /// <summary>
            /// Stop recording audio
            /// </summary>
            public void StopRecording() {
                // Return if we're not recording
                if (!IsRecording) return;

                Mic.StopRecording(this);
                if (!IsRecording)
                    OnStopRecording?.Invoke();
            }

            /// <summary>
            /// Whether this device is currently recording audio
            /// </summary>
            public bool IsRecording =>
                Mic.IsRecording(this);

            internal void BroadcastFrame(int channelCount, float[] pcm) {
                if (VolumeMultiplier != 1) {
                    for (int i = 0; i < pcm.Length; i++)
                        pcm[i] *= VolumeMultiplier;
                }
                OnFrameCollected?.Invoke(SamplingFrequency, channelCount, pcm);
            }
        }

        readonly static Dictionary<string, Device> deviceMap = new Dictionary<string, Device>();
        /// <summary>
        /// Gets the available recording devices
        /// </summary>
        public static List<Device> AvailableDevices {
            get {
                // Add to the map if we've detected a new device
                var deviceNames = Microphone.devices;
                foreach (var deviceName in deviceNames) {
                    if (!deviceMap.ContainsKey(deviceName)) {
                        Microphone.GetDeviceCaps(deviceName, out int max, out int min);
                        var device = new Device(deviceName, max, min);
                        deviceMap.Add(deviceName, device);
                    }
                }

                // Remove from the map any device that may have been disconnected
                var removedDeviceNames = deviceMap.Where(x => !deviceNames.Contains(x.Key));
                foreach (var removed in removedDeviceNames)
                    deviceMap.Remove(removed.Key);

                // return the values of the map as a list
                return deviceMap.Values.ToList();
            }
        }

        // Prevent 'new' keyword construction
        [Obsolete("Mic is a MonoBehaviour class. Use Mic.Instance to get the instance", true)]
        public Mic() { }

        static Mic instance;
        /// <summary>
        /// Initialize the Mic class for use.
        /// </summary>
        public static void Init() {
            if (instance != null) return;

            var go = new GameObject("UniMic.Mic");
            go.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(go);
            instance = go.AddComponent<Mic>();
        }

        static void StartRecording(Device device) {
            StopRecording(device);

            var newClip = Microphone.Start(device.Name, true, 1, device.SamplingFrequency);
            if (newClip != null) {
                clips.Add(device, newClip);
                prevPositions.Add(device, 0);
                pcms.Add(device, new Queue<float>());
            }
        }

        static bool IsRecording(Device device) {
            return Microphone.IsRecording(device.Name);
        }

        static int GetChannelCount(Device device) {
            if (!clips.ContainsKey(device))
                return 0;
            return clips[device].channels;
        }

        static void StopRecording(Device device) {
            if (device == null)
                return;

            Microphone.End(device.Name);
            ClearDeviceData(device);
        }

        static void ClearDeviceData(Device device) {
            if (clips.ContainsKey(device)) {
                Destroy(clips[device]);
                clips.Remove(device);
            }

            if (prevPositions.ContainsKey(device))
                prevPositions.Remove(device);

            if (pcms.ContainsKey(device)) {
                pcms[device] = null;
                pcms.Remove(device);
            }
        }

        // PCM DATA RETRIEVAL LOOP
        static Dictionary<Device, AudioClip> clips = new Dictionary<Device, AudioClip>();
        static Dictionary<Device, int> prevPositions = new Dictionary<Device, int>();
        static Dictionary<Device, Queue<float>> pcms = new Dictionary<Device, Queue<float>>();

        // Variables declared once and re-assigned for every device in the Update loop
        int pos;
        bool didLoop;
        Device device;
        AudioClip clip;
        float[] frame;
        int prevPos;
        Queue<float> pcm;
        int frameLen;

        void Update() {
            foreach (var pair in clips) {
                device = pair.Key;
                clip = pair.Value;
                prevPos = prevPositions[device];
                pcm = pcms[device];

                // Skip the device if it is not recording
                // or if the clip is null (this can happen when quitting the game)
                if (!device.IsRecording || clip == null)
                    continue;

                // If the mic position hasn't moved, skip
                pos = Microphone.GetPosition(device.Name);
                if (pos == prevPos)
                    continue;

                // Check if the mic has looped over the clip. If it hasn't, 
                // we just need the data between the current and last mic positions.
                // If it has, we need to read the data from last position to the end
                // of the clip, then the data from the start of the clip to current position
                didLoop = pos < prevPos;
                if (!didLoop) {
                    var sample = new float[pos - prevPos];
                    clip.GetData(sample, prevPos);
                    foreach (var t in sample)
                        pcm.Enqueue(t);
                }
                else {
                    int lastLoopSampleLen = clip.samples - prevPos - 1;
                    int currLoopSampleLen = pos + 1;
                    var lastLoopSamples = new float[lastLoopSampleLen];
                    var currLoopSamples = new float[currLoopSampleLen];
                    clip.GetData(lastLoopSamples, prevPos - 1);
                    clip.GetData(currLoopSamples, 0);

                    foreach (var sample in lastLoopSamples)
                        pcm.Enqueue(sample);

                    foreach (var sample in currLoopSamples)
                        pcm.Enqueue(sample);
                }

                // Send as many complete frames from the pcm history as possible
                frameLen = device.SamplingFrequency / 1000 * device.FrameDurationMS * device.ChannelCount;
                frame = new float[frameLen];
                while (pcm.Count >= frame.Length) {
                    for (int i = 0; i < frame.Length; i++)
                        frame[i] = pcm.Dequeue();
                    device.BroadcastFrame(clip.channels, frame);
                }

                // Update the last position of this device
                prevPositions[device] = pos;
            }
        }
    }
}