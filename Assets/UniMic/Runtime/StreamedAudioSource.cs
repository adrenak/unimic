using UnityEngine;
using System.Diagnostics;

namespace Adrenak.UniMic {
    [RequireComponent(typeof(AudioSource))]
    public class StreamedAudioSource : MonoBehaviour {
        [Tooltip("Desired steady-state playback latency (seconds).")]
        [SerializeField] float targetLatency = 0.25f;
        /// <summary>
        /// Desired steady-state playback latency (seconds).
        /// </summary>
        public float TargetLatency {
            get => targetLatency;
            set => targetLatency = value;
        }

        [Tooltip("If no new frame arrives for longer than this, stop playback (seconds).")]
        [Range(0.1f, 0.75f)]
        [SerializeField] float frameLifetime = 0.5f;
        /// <summary>
        /// If no new frame arrives for longer than this, stop playback (seconds).
        /// </summary>
        public float FrameLifetime {
            get => frameLifetime;
            set => frameLifetime = value;
        }

        [Tooltip("How large the internal ring buffer is, relative to (targetLatency + frameLifetime).")]
        [SerializeField] int bufferFactor = 4;
        /// <summary>
        /// How large the internal ring buffer is, relative to (targetLatency + frameLifetime).
        /// </summary>
        public int BufferFactor {
            get => bufferFactor;
            set => bufferFactor = value;
        }

        [Header("Pitch controller")]
        [Tooltip("P gain: pitch response per second of latency error.")]
        [Range(0f, 5f)]
        [SerializeField] float pitchProportionalGain = 1f;
        /// <summary>
        /// P gain: pitch response per second of latency error.
        /// </summary>
        public float PitchProportionalGain {
            get => pitchProportionalGain;
            set => pitchProportionalGain = value;
        }

        [Tooltip("Maximum absolute pitch deviation.")]
        [Range(0f, 0.5f)]
        [SerializeField] float pitchMaxCorrection = 0.2f;
        /// <summary>
        /// Maximum absolute pitch deviation.
        /// </summary>
        public float PitchMaxCorrection {
            get => pitchMaxCorrection;
            set => pitchMaxCorrection = value;
        }

        [Tooltip("Scale for downward (pitch < 1) correction.")]
        [Range(0f, 1.0f)]
        [SerializeField] float downwardPitchCorrectionScale = 0.25f;
        /// <summary>
        /// Scale for downward (pitch < 1) correction.
        /// </summary>
        public float DownwardPitchCorrectionScale {
            get => downwardPitchCorrectionScale;
            set => downwardPitchCorrectionScale = value;
        }

        [Tooltip("No pitch adjustment if |error| <= deadzone (seconds).")]
        [Range(0f, 0.05f)]
        [SerializeField] float pitchDeadzoneSec = 0.025f;
        /// <summary>
        /// No pitch adjustment if |error| <= deadzone (seconds).
        /// </summary>
        public float PitchDeadZoneSec {
            get => pitchDeadzoneSec;
            set => pitchDeadzoneSec = value;
        }

        [Tooltip("How fast pitch drifts back to 1.0 when within the deadzone.")]
        [Range(0f, 2f)]
        [SerializeField] float pitchReturnSpeed = 0.5f;
        /// <summary>
        /// How fast pitch drifts back to 1.0 when within the deadzone.
        /// </summary>
        public float PitchReturnSpeed {
            get => pitchReturnSpeed;
            set => pitchReturnSpeed = value;
        }

        [Header("Startup")]
        [Tooltip("Extra safety buffer on first start (seconds). Prevents razor-edge starts.")]
        [Range(0f, 0.1f)]
        [SerializeField] float startSafetyMarginSec = 0.02f;
        /// <summary>
        /// Extra safety buffer on first start (seconds). Prevents razor-edge starts.
        /// </summary>
        public float StartSafetyMarginSec {
            get => startSafetyMarginSec;
            set => startSafetyMarginSec = value;
        }

        /// <summary>
        /// The <see cref="AudioSource"/> that plays the streaming audio
        /// </summary>
        public AudioSource UnityAudioSource {
            get {
                if (source == null) {
                    source = GetComponent<AudioSource>();
                    source.loop = true;
                    source.playOnAwake = false;
                    source.dopplerLevel = 0f;
                }
                return source;
            }
        }
        
        /// <summary>
        /// The length of the internal buffer in milliseconds
        /// </summary>
        public int BufferDurationMS => clip != null ? clip.samples * 1000 / clip.frequency : 0;

        /// <summary>
        /// Current clip's sample rate
        /// </summary>
        public int SamplingFrequency => clip != null ? clip.frequency : 0;

        /// <summary>
        /// Current clip's channel count
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

        #region INTERNAL STATE
        private AudioSource source;
        private AudioClip clip;

        // Current format
        private int curFrequency = 0;
        private int curChannels = 0;

        // Ring buffer geometry (PER-CHANNEL samples)
        private int samplesPerChannel = 0;
        private float clipLengthSec = 0f;

        // Write pointers/counters
        private int writePosPerChannel = 0;
        private long absWritePerChannel = 0;

        // Frame geometry (computed at each Feed)
        private int perChannelSamplesInFrame = 0;
        private float secondsPerFrame = 0f;

        // Frame staleness tracking
        private readonly Stopwatch frameStopwatch = new Stopwatch();
        private float TimeSinceLastFrame => (float)frameStopwatch.Elapsed.TotalSeconds;

        private static readonly object audioWriteLock = new object();
        #endregion

        public static StreamedAudioSource New(string name = null) {
            var go = new GameObject(name ?? "StreamedAudioSource");
            go.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(go);
            return go.AddComponent<StreamedAudioSource>();
        }

        /// <summary>
        /// Feed one frame of interleaved audio (float PCM).
        /// - frequency: Hz of this frame
        /// - channels: channel count of this frame
        /// - samples: interleaved samples (length = perChSamplesInFrame * channels)
        /// </summary>
        public void Feed(int frequency, int channels, float[] samples) {
            if (!gameObject.activeInHierarchy) return;
            if (!UnityAudioSource.enabled) return;
            if (samples == null || samples.Length == 0) return;

            // Frame geometry (channel-aware)
            int totalInFrame = samples.Length;
            perChannelSamplesInFrame = totalInFrame / channels;
            secondsPerFrame = (float)perChannelSamplesInFrame / frequency;

            // Desired clip size (per-channel samples)
            int desiredClipSamplesPerCh = Mathf.CeilToInt(
                (targetLatency + frameLifetime) * bufferFactor * frequency
            );

            bool formatChanged = clip == null
                || frequency != curFrequency
                || channels != curChannels
                || desiredClipSamplesPerCh != samplesPerChannel;

            if (formatChanged) {
                StopPlayback();
                ReinitClip(desiredClipSamplesPerCh, channels, frequency);
            }

            // Write the frame into the ring (SetData offset is per-channel samples)
            lock (audioWriteLock) {
                clip.SetData(samples, writePosPerChannel);
            }

            // Advance write pointers
            absWritePerChannel += perChannelSamplesInFrame;
            writePosPerChannel = (writePosPerChannel + perChannelSamplesInFrame) % samplesPerChannel;
            frameStopwatch.Restart();

            // Startup: begin only when enough audio is prebuffered
            if (!IsPlaying) {
                IsBuffering = true;

                // Total written duration (sec) from zero; fine for first start
                float writtenSec = (float)absWritePerChannel / curFrequency;
                if (writtenSec >= targetLatency + startSafetyMarginSec) {
                    // CHANGE: Start EXACTLY at target latency behind the write head
                    float writeTimeSec = WrappedWriteTimeSec();
                    float desiredReadSec = writeTimeSec - targetLatency;
                    if (desiredReadSec < 0f) desiredReadSec += clipLengthSec;

                    UnityAudioSource.time = desiredReadSec;
                    UnityAudioSource.pitch = 1f;
                    UnityAudioSource.Play();

                    IsBuffering = false;
                }
            }
        }

        private void Update() {
            if (!IsPlaying || clip == null) return;

            // Stale frame protection
            if (TimeSinceLastFrame > frameLifetime) {
                StopPlayback();
                return;
            }

            // Compute current latency (seconds) with simple wrap math
            float latencySec = CurrentLatencySec();

            // If dangerously low latency, treat as underrun and stop (will restart on next feeds)
            if (latencySec < 0.5f * secondsPerFrame) {
                StopPlayback();
                return;
            }

            // Proportional-only controller
            // error > 0 -> we are SHORT of target -> need to SLOW playback -> pitch < 1
            float errorSec = targetLatency - latencySec;

            if (Mathf.Abs(errorSec) <= pitchDeadzoneSec) {
                // Within the deadzone: gently relax pitch back to 1.0
                UnityAudioSource.pitch = Mathf.MoveTowards(
                    UnityAudioSource.pitch, 1f, pitchReturnSpeed * Time.deltaTime
                );
            }
            else {
                // sign: short => negative; long => positive
                float raw = -errorSec * pitchProportionalGain;

                // Asymmetric clamp: prefer speeding up (pitch>1) over slowing down (pitch<1)
                float minResp = -pitchMaxCorrection * downwardPitchCorrectionScale;
                float maxResp = pitchMaxCorrection;
                float resp = Mathf.Clamp(raw, minResp, maxResp);
                UnityAudioSource.pitch = 1f + resp;
            }
        }

        #region HELPERS
        private float WrappedWriteTimeSec() {
            // Position of write head (per-channel) mapped to seconds in [0, clipLengthSec)
            int writePosWrapped = (int)(absWritePerChannel % samplesPerChannel);
            float writeTimeSec = (float)writePosWrapped / curFrequency;
            return writeTimeSec;
        }

        private float CurrentLatencySec() {
            // Latency = time distance from read head to write head (wrapped)
            float writeTime = WrappedWriteTimeSec();
            float readTime = UnityAudioSource.time; // seconds in [0, clipLengthSec)

            float latency = writeTime - readTime;
            if (latency < 0f) latency += clipLengthSec; // wrap into [0, clipLengthSec)
            return latency;
        }

        private void StopPlayback() {
            IsBuffering = false;
            writePosPerChannel = 0;
            absWritePerChannel = 0;
            UnityAudioSource.pitch = 1f;
            UnityAudioSource.Stop();
            frameStopwatch.Reset();
        }

        private void ReinitClip(int sampleLenPerCh, int channels, int frequency) {
            DestroyClip();
            CreateClip(sampleLenPerCh, channels, frequency);
        }

        private void DestroyClip() {
            if (clip != null) Destroy(clip);
            clip = null;
        }

        private void CreateClip(int sampleLenPerCh, int channels, int frequency) {
            clip = AudioClip.Create("StreamedClip", sampleLenPerCh, channels, frequency, false);

            // Init clip with silence
            var zeros = new float[sampleLenPerCh * channels];
            clip.SetData(zeros, 0);

            UnityAudioSource.clip = clip;

            // Update cached geometry
            samplesPerChannel = sampleLenPerCh;                  // per-channel samples
            clipLengthSec = (float)samplesPerChannel / frequency;

            curFrequency = frequency;
            curChannels = channels;

            // Reset write pointers
            writePosPerChannel = 0;
            absWritePerChannel = 0;
        }
        #endregion

        #region OBSOLETE

        [System.Obsolete("new not allowed. Use StreamedAudioSource.New", true)]
        public StreamedAudioSource() { }

        [System.Obsolete("Use PitchProportionalGain instead. This property was a typo!")]
        public float PitchProportionalGame {
            get => PitchProportionalGain;
            set => PitchProportionalGame = value;
        }

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
