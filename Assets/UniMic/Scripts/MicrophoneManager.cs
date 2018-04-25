using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UniMic {
    [RequireComponent(typeof(AudioSource))]
    public class MicrophoneManager : MonoBehaviour {
        // ================================================
        // FIELDS
        // ================================================
        // If the recording is ongoing
        bool m_IsRecording;

        // The key used to refer to this instance
        string m_Key;

        // Sample rate of the recorded audio. Default to 16k
        int m_SampleRate = 16000;

        // Smoothness of the RMS value
        float m_RMSSmoothness;

        // The smoothened RMS value
        float m_SmoothenedRMS;

        // The length of the AudioClip in Mic buffer in seconds
        int m_BufferLengthSec = 1;

        // Stores the audio data for a frame
        float[] m_AudioFrame;

        // The duration of a single audio data frame
        int m_FrameLengthMs = 20;

        // Reference to the clip being streamed from the Microphone
        AudioClip m_AudioClip;

        // AudioSource component that plays the audio clip at 0 volume. Required to get spectrum data
        AudioSource m_AudioSource;

        // List of all the available Mic devices
        List<string> m_Devices = new List<string>();

        // Index of the current Mic device in m_Devices
        int m_CurrentDeviceIndex;

        // ================================================
        // EVENTS
        // ================================================
        public delegate void StringEvent(string param);
        public delegate void FloatArrayEvent(float[] param);

        /// <summary>
        /// Invoked when the instance starts Recording. Includes the key.
        /// </summary>
        public event StringEvent OnStartRecording;

        /// <summary>
        /// Invoked everytime an audio frame is collected. Includes the frame.
        /// </summary>
        public event FloatArrayEvent OnAudioFrameCollected;

        /// <summary>
        /// Invoked when the instance stop Recording. Includes the key.
        /// </summary>
        public event StringEvent OnStopRecording;

        // ================================================
        // PROPERTIES
        // ================================================
        /// <summary>
        /// If the instance is Recording to Mic input
        /// </summary>
        public bool IsRecording {
            get { return m_IsRecording; }
        }

        /// <summary>
        /// The key used to identify the instance
        /// </summary>
        public string Key {
            get { return m_Key; }
        }

        /// <summary>
        /// The available Mic devices
        /// </summary>
        public List<string> Devices {
            get { return m_Devices; }
        }

        /// <summary>
        /// The Mic name currently in use
        /// </summary>
        public string CurrentDeviceName {
            get { return m_Devices[m_CurrentDeviceIndex]; }
        }

        /// <summary>
        /// The index of the Mic device in use
        /// </summary>
        public int CurrentDeviceIndex {
            get { return m_CurrentDeviceIndex; }
        }

        /// <summary>
        /// Gets the <see cref="AudioClip"/> reference where the Mic input is stored.
        /// Access this to attach it to an <see cref="AudioSource"/> to play the input.
        /// </summary>
        public AudioClip Audio {
            get { return m_AudioClip; }
        }

        // ================================================
        // METHODS
        // ================================================
        /// <summary>
        /// Creates an instance and initialises with the given parameters
        /// </summary>
        /// <param name="sampleRate">The sample rate of the audio input. </param>
        /// <param name="bufferLengthSec">The buffer length in seconds</param>
        /// <param name="frameLengthMs">The frame length in milliseconds</param>
        /// <returns>A new instance</returns>
        public static MicrophoneManager Create(int sampleRate, int bufferLengthSec, int frameLengthMs) {
            GameObject cted = new GameObject("MicrophoneManager");
            DontDestroyOnLoad(cted);
            MicrophoneManager instance = cted.AddComponent<MicrophoneManager>();

            instance.m_SampleRate = sampleRate;
            instance.m_BufferLengthSec = bufferLengthSec;
            instance.m_FrameLengthMs = frameLengthMs;

            instance.m_AudioSource = cted.GetComponent<AudioSource>();
            instance.m_AudioFrame = new float[instance.m_SampleRate / 1000 * instance.m_FrameLengthMs];

            foreach (var device in Microphone.devices)
                instance.m_Devices.Add(device);
            instance.m_CurrentDeviceIndex = 0;

            return instance;
        }

        /// <summary>
        /// Changes to a Mic device for Recording
        /// </summary>
        /// <param name="index">The index of the Mic device. Refer to <see cref="Devices"/></param>
        public void ChangeDevice(int index) {
            Microphone.End(CurrentDeviceName);
            m_CurrentDeviceIndex = index;
            Microphone.Start(CurrentDeviceName, true, m_BufferLengthSec, m_SampleRate);
        }

        /// <summary>
        /// Starts to stream the input of the current Mic device
        /// </summary>
        /// <param name="key">A user defined key to identify the recording</param>
        public void StartRecording(string key) {
            StopRecording();
            if (!Microphone.IsRecording(CurrentDeviceName)) {
                m_Key = key;
                m_IsRecording = true;

                m_AudioClip = Microphone.Start(CurrentDeviceName, true, m_BufferLengthSec, m_SampleRate);
                m_AudioSource.clip = m_AudioClip;
                m_AudioSource.loop = true;
                m_AudioSource.volume = 1;
                m_AudioSource.Play();

                StartCoroutine(ReadRawAudio());

                if (OnStartRecording != null)
                    OnStartRecording(m_Key);
            }
        }

        /// <summary>
        /// Ends the Mic stream.
        /// </summary>
        public void StopRecording() {
            if (Microphone.IsRecording(CurrentDeviceName)) {
                m_IsRecording = false;
                Microphone.End(CurrentDeviceName);

                StopCoroutine(ReadRawAudio());

                Destroy(m_AudioClip);
                m_AudioClip = null;

                if (OnStopRecording != null)
                    OnStopRecording(m_Key);
            }
        }

        /// <summary>
        /// Gets the current audio spectrum
        /// </summary>
        /// <param name="fftWindow">The <see cref="FFTWindow"/> type used to create the spectrum.</param>
        /// <param name="sampleCount">The number of samples required in the output. Use POT numbers</param>
        /// <returns></returns>
        public float[] GetSpectrumData(FFTWindow fftWindow, int sampleCount) {
            var spectrumData = new float[sampleCount];
            m_AudioSource.GetSpectrumData(spectrumData, 0, fftWindow);
            return spectrumData;
        }

        /// <summary>
        /// Returns a Root Mean Squared value of the audio frame. Can be used to approximate volume.
        /// </summary>
        /// <returns>A float from 0 to 1</returns>
        public float GetAbsoluteRMS() {
            float sum = 0;
            for (int i = 0; i < m_AudioFrame.Length; i++)
                sum += m_AudioFrame[i] * m_AudioFrame[i];

            var rms = Mathf.Sqrt(sum / m_AudioFrame.Length);
            return rms;
        }

        /// <summary>
        /// Returns a smoothened RMS value. It is more stable than <see cref="GetAbsoluteRMS"/> and lower in magnitude.
        /// </summary>
        /// <returns>Smoothened RMS</returns>
        public float GetSmoothRMS() {
            if (m_RMSSmoothness == 0) return GetAbsoluteRMS();
            return m_SmoothenedRMS = Mathf.Lerp(m_SmoothenedRMS, GetAbsoluteRMS(), m_RMSSmoothness);
        }

        /// <summary>
        /// Sets the linear interolation rate for smoothing the RMS 
        /// </summary>
        /// <param name="smoothness"></param>
        public void SetRMSSmoothness(float smoothness) {
            m_RMSSmoothness = smoothness;
        }

        IEnumerator ReadRawAudio() {
            int loops = 0;
            int readAbsPos = 0;
            int prevPos = 0;
            float[] tempAudioFrame = new float[m_AudioFrame.Length];

            while (m_AudioClip != null && Microphone.IsRecording(CurrentDeviceName)) {
                bool isNewDataAvailable = true;

                while (isNewDataAvailable) {
                    int currPos = Microphone.GetPosition(CurrentDeviceName);
                    if (currPos < prevPos)
                        loops++;
                    prevPos = currPos;

                    var currAbsPos = loops * m_AudioClip.samples + currPos;
                    var nextReadAbsPos = readAbsPos + tempAudioFrame.Length;

                    if (nextReadAbsPos < currAbsPos) {
                        m_AudioClip.GetData(tempAudioFrame, readAbsPos % m_AudioClip.samples);

                        m_AudioFrame = tempAudioFrame;
                        if (OnAudioFrameCollected != null)
                            OnAudioFrameCollected(m_AudioFrame);

                        readAbsPos = nextReadAbsPos;
                        isNewDataAvailable = true;
                    }
                    else
                        isNewDataAvailable = false;
                }
                yield return null;
            }
        }
    }
}
