using UnityEngine;
using System.Diagnostics;

namespace Adrenak.UniMic {
    [RequireComponent(typeof(AudioSource))]
    public class StreamedAudioSource : MonoBehaviour {
        [Tooltip("Target playback latency in seconds.")]
        [SerializeField] float targetLatency = 0.25f;

        /// <summary>
        /// Target delay between receiving and playing audio
        /// </summary>
        public float TargetLatency {
            get => targetLatency;
            set => targetLatency = value;
        }

        [Tooltip("Maximum age of buffered audio before it's considered stale.")]
        [Range(0.1f, 0.75f)]
        [SerializeField] float frameLifetime = 0.5f;

        /// <summary>
        /// Maximum time to keep audio in buffer before discarding it
        /// </summary>
        public float FrameLifetime {
            get => frameLifetime;
            set => frameLifetime = value;
        }

        [Tooltip("The multiplier for the buffer length")]
        [SerializeField] int bufferFactor = 4;

        /// <summary>
        /// The multiplier for the buffer length. 
        /// </summary>
        public int BufferFactor {
            get => bufferFactor;
            set => bufferFactor = value;
        }

        [Tooltip("Proportional gain for pitch correction (per second of latency error).")]
        [Range(0f, 10f)]
        [SerializeField] float pitchProportionalGain = 1f;

        /// <summary>
        /// Controls how aggressively pitch is adjusted to reach target latency
        /// </summary>
        public float PitchProportionalGame {
            get => pitchProportionalGain;
            set => pitchProportionalGain = value;
        }

        [Tooltip("Maximum pitch adjustment (as a percentage).")]
        [Range(0f, 0.5f)]
        public float pitchMaxCorrection = 0.15f;

        /// <summary>
        /// Caps pitch adjustment so audio doesn’t sound unnatural
        /// </summary>
        public float PitchMaxCorrection {
            get => pitchMaxCorrection;
            set => pitchMaxCorrection = value;
        }

        /// <summary>
        /// The length of the internal buffer in milliseconds
        /// </summary>
        public int BufferDurationMS =>
            clip != null ? clip.samples * 1000 / clip.channels / clip.frequency : 0;

        /// <summary>
        /// Current clip’s sample rate
        /// </summary>
        public int SamplingFrequency => clip != null ? clip.frequency : 0;

        /// <summary>
        /// Current clip’s channel count
        /// </summary>
        public int ChannelCount => clip != null ? clip.channels : 0;

        /// <summary>
        /// Whether playback is currently running
        /// </summary>
        public bool IsPlaying => UnityAudioSource.isPlaying;

        /// <summary>
        /// Whether audio is currently being fed and buffered for playback eventually.
        /// </summary>
        public bool IsBuffering { get; private set; }

        /// <summary>
        /// Accessor for AudioSource with lazy initialization and setup
        /// </summary>
        public AudioSource UnityAudioSource {
            get {
                if (source == null) {
                    source = GetComponent<AudioSource>();
                    source.loop = true;
                    source.playOnAwake = false;
                    source.dopplerLevel = 0;
                }
                return source;
            }
        }

        private AudioSource source;
        private AudioClip clip;

        // Buffering and frame tracking variables
        private int estimatedClipSamples;
        private int samplesPerFrame;
        private float secondsPerFrame;

        // Write pointer and playback tracking
        private int setDataPos;
        private long absSetDataPos;
        private int playbackLoops;
        private int lastPlaybackPos;
        private int absPlaybackPos;

        // Timer for frame lifetime checks
        private readonly Stopwatch frameStopwatch = new Stopwatch();
        private float TimeSinceLastFrame => (float)frameStopwatch.Elapsed.TotalSeconds;
        private static readonly object audioWriteLock = new object();

        [System.Obsolete("new not allowed. Use StreamedAudioSource.New", true)]
        public StreamedAudioSource() { }

        public static StreamedAudioSource New(string name = null) {
            var go = new GameObject(name ?? "StreamedAudioSource");
            go.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(go);
            var instance = go.AddComponent<StreamedAudioSource>();
            return instance;
        }

        /// <summary>
        /// Feeds audio into the buffer. Reinitializes format if it changes.
        /// Starts playback when target latency is reached.
        /// </summary>
        public void Feed(int frequency, int channels, float[] samples) {
            if (!gameObject.activeInHierarchy) return;
            if (!UnityAudioSource.enabled) return;

            estimatedClipSamples = Mathf.CeilToInt((targetLatency + frameLifetime) * bufferFactor * frequency);
            samplesPerFrame = samples.Length;
            secondsPerFrame = (float)samplesPerFrame / frequency;

            // Reinitialize the clip if format or size has changed
            if (frequency != SamplingFrequency || channels != ChannelCount || clip == null || clip.samples != estimatedClipSamples) {
                StopPlayback();
                ReinitClip(estimatedClipSamples, channels, frequency);
            }

            // Write samples into ring buffer
            lock (audioWriteLock) {
                clip.SetData(samples, setDataPos);
            }

            absSetDataPos += samples.Length;
            setDataPos = (int)(absSetDataPos % clip.samples);
            frameStopwatch.Restart();

            // Compute buffered duration in seconds
            float bufferedTimeInSeconds = (float)absSetDataPos / (frequency * channels);
            if (!IsPlaying)
                IsBuffering = true;

            // Start playback once sufficient buffer is accumulated
            if (bufferedTimeInSeconds >= targetLatency && !IsPlaying) {
                UnityAudioSource.time = GetWrappedTime((int)(absSetDataPos / samplesPerFrame) - 1);
                IsBuffering = false;
                UnityAudioSource.Play();
            }
        }

        /// <summary>
        /// Monitors playback position, latency, and handles underruns or staleness.
        /// </summary>
        private void Update() {
            if (!IsPlaying) return;

            // Track playback loop and update absolute position
            int currentSamplePos = UnityAudioSource.timeSamples;
            if (currentSamplePos < lastPlaybackPos) playbackLoops++;
            lastPlaybackPos = currentSamplePos;
            absPlaybackPos = playbackLoops * clip.samples + currentSamplePos;

            // Stop playback if it catches up to write position
            if (absPlaybackPos > absSetDataPos) {
                StopPlayback();
                return;
            }

            // Apply pitch correction to reach target latency
            float latency = GetLatency();
            float error = targetLatency - latency;
            float response = Mathf.Clamp(-error * pitchProportionalGain, -pitchMaxCorrection, pitchMaxCorrection);
            UnityAudioSource.pitch = 1f + response;

            // Stop playback if frame data becomes stale
            if (TimeSinceLastFrame > frameLifetime) {
                StopPlayback();
            }
        }

        /// <summary>
        /// Computes playback latency with circular buffer wraparound handling.
        /// </summary>
        private float GetLatency() {
            float writeTime = GetWrappedTime((int)(absSetDataPos / samplesPerFrame));
            float readTime = UnityAudioSource.time;
            float clipLength = clip.length;

            float latency = writeTime - readTime;
            if (latency < 0) latency += clipLength;
            if (clipLength - frameLifetime < latency) latency -= clipLength;

            return latency + TimeSinceLastFrame;
        }

        /// <summary>
        /// Converts frame index to playback time, wrapped around clip length.
        /// </summary>
        private float GetWrappedTime(int frameIndex) {
            return frameIndex * secondsPerFrame % clip.length;
        }

        /// <summary>
        /// Resets all state and stops playback.
        /// </summary>
        private void StopPlayback() {
            IsBuffering = false;
            setDataPos = 0;
            absSetDataPos = 0;
            playbackLoops = 0;
            lastPlaybackPos = 0;
            absPlaybackPos = 0;
            UnityAudioSource.Stop();
        }

        /// <summary>
        /// Recreates the audio clip with new format settings.
        /// </summary>
        private void ReinitClip(int sampleLen, int channels, int frequency) {
            DestroyClip();
            CreateClip(sampleLen, channels, frequency);
        }

        /// <summary>
        /// Destroys the current audio clip.
        /// </summary>
        private void DestroyClip() {
            if (clip != null)
                Destroy(clip);
            clip = null;
        }

        /// <summary>
        /// Creates a new silent audio clip and assigns it to the AudioSource.
        /// </summary>
        private void CreateClip(int sampleLen, int channels, int frequency) {
            clip = AudioClip.Create("StreamedClip", sampleLen, channels, frequency, false);
            clip.SetData(new float[sampleLen], 0);
            UnityAudioSource.clip = clip;
        }

        #region OBSOLETE

        [System.Obsolete("FrameCountForPlay is no longer supported. Use targetLatency instead to configure buffer size.")]
        public int FrameCountForPlay { get; set; }

        [System.Obsolete("Feed no longer needs autoPlayWhenReady. Auto play is always on.")]
        public void Feed(int frequency, int channels, float[] samples, bool autoPlayWhenReady = true) =>
            Feed(frequency, channels, samples);

        [System.Obsolete("Play() is no longer supported. Calling this method will do nothing. " +
            "When enough audio has been buffered the audio will always play automatically.")]
        public void Play() { }

        [System.Obsolete("Stop() is no longer supported. Calling this method will do nothing. " +
            "If you want to stop playback, stop calling the Feed method and playback will end automatically when the buffer is cleared. " +
            "For immediately stopping playback consider setting UnityAudioSource.volume to 0")]
        public void Stop() { }

        [System.Obsolete("This property has been deprecated. Use IsBuffering instead.")]
        public bool Buffering => IsBuffering;

        #endregion
    }
}
