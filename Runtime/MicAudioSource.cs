using UnityEngine;

namespace Adrenak.UniMic {
    /// <summary>
    /// A simple AudioSource based component that just plays what 
    /// the <see cref="Mic"/> instance is receiving.
    /// Provides optional feature to start the recording by itself (as a testing tool)
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MicAudioSource : MonoBehaviour {
        public bool startRecordingAutomatically = true;
        [Header("If startRecordingAutomatically is true:")]
        public int recordingFrequency = 48000;
        public int sampleDurationMS = 100;
        AudioClip clip;

        void Start() {
            var audioSource = gameObject.GetComponent<AudioSource>();
            audioSource.loop = false;

            var mic = Mic.Instance;

            if(startRecordingAutomatically)
                mic.StartRecording(recordingFrequency, sampleDurationMS);

            mic.OnTimestampedSampleReady += (timestamp, segment) => {
                if (clip != null)
                    Destroy(clip);
                clip = AudioClip.Create("clip", segment.Length, mic.AudioClip.channels, mic.AudioClip.frequency, false);
                clip.SetData(segment, 0);
                audioSource.clip = clip;
                audioSource.Play();
            };
        }
    }

}
