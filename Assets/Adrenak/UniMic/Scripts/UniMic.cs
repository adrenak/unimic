using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace Adrenak {
    [RequireComponent(typeof(AudioSource))]
    public class UniMic : MonoBehaviour {
        // ================================================
        // FIELDS
        // ================================================
        #region FIELDS
        /// <summary>
        /// Whether the microphone is running
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Last populated audio buffer
        /// </summary>
        public float[] Buffer { get; private set; }

        /// <summary>
        /// Buffer duration/length in milliseconds
        /// </summary>
        public int BufferLen { get; private set; }

        /// <summary>
        /// The AudioClip currently being streamed in the Mic
        /// </summary>
        public AudioClip AudioClip { get; private set; }

        /// <summary>
        /// List of all the available Mic devices
        /// </summary>
        public List<string> Devices { get; private set; }

        /// <summary>
        /// Index of the current Mic device in m_Devices
        /// </summary>
        public int CurrentDeviceIndex { get; private set; }

        /// <summary>
        /// Gets the name of the Mic device currently in use
        /// </summary>
        public string CurrentDeviceName {
            get { return Devices[CurrentDeviceIndex]; }
        }

        int m_SampleRate;
        AudioSource m_AudioSource;      // Plays the audio clip at 0 volume to get spectrum data
        #endregion

        // ================================================
        // EVENTS
        // ================================================
        #region EVENTS
        public class FloatArrayEvent : UnityEvent<float[]> { }

        /// <summary>
        /// Invoked when the instance starts Recording.
        /// </summary>
        public UnityEvent OnStartRecording;

        /// <summary>
        /// Invoked everytime an audio frame is collected. Includes the frame.
        /// </summary>
        public FloatArrayEvent OnBufferReady = new FloatArrayEvent();

        /// <summary>
        /// Invoked when the instance stop Recording.
        /// </summary>
        public UnityEvent OnStopRecording;
        #endregion 

        // ================================================
        // METHODS
        // ================================================
        #region METHODS
        /// <summary>
        /// Creates an instance and initialises with the given parameters
        /// </summary>
        /// <param name="sampleRate">The sample rate of the audio input. </param>
        /// <param name="bufferDuration">The buffer length in seconds</param>
        /// <param name="bufferLen">The frame length in milliseconds</param>
        /// <returns>A new instance</returns>
        public static UniMic Create(int sampleRate = 16000, int bufferLen = 10) {
            GameObject cted = new GameObject("UniMic");
            DontDestroyOnLoad(cted);
            UniMic instance = cted.AddComponent<UniMic>();

            instance.m_SampleRate = sampleRate;
            instance.BufferLen = bufferLen;

            instance.m_AudioSource = cted.GetComponent<AudioSource>();

            instance.Devices = new List<string>();
            foreach (var device in Microphone.devices)
                instance.Devices.Add(device);
            instance.CurrentDeviceIndex = 0;

            return instance;
        }

        /// <summary>
        /// Changes to a Mic device for Recording
        /// </summary>
        /// <param name="index">The index of the Mic device. Refer to <see cref="Devices"/></param>
        public void ChangeDevice(int index) {
            Microphone.End(CurrentDeviceName);
            CurrentDeviceIndex = index;
            Microphone.Start(CurrentDeviceName, true, 1, m_SampleRate);
        }

        /// <summary>
        /// Starts to stream the input of the current Mic device
        /// </summary>
        public void StartRecording() {
            if (Microphone.IsRecording(CurrentDeviceName)) return;

            StopRecording();
            IsRunning = true;

            AudioClip = Microphone.Start(CurrentDeviceName, true, 1, m_SampleRate);
            Buffer = new float[m_SampleRate / 1000 * BufferLen * AudioClip.channels];

            m_AudioSource.clip = AudioClip;
            m_AudioSource.loop = true;
            m_AudioSource.volume = 0;
            m_AudioSource.Play();

            StartCoroutine(ReadRawAudio());

            if(OnStartRecording != null)
                OnStartRecording.Invoke();
        }

        /// <summary>
        /// Ends the Mic stream.
        /// </summary>
        public void StopRecording() {
            if (!Microphone.IsRecording(CurrentDeviceName)) return;

            IsRunning = false;

            Microphone.End(CurrentDeviceName);
            Destroy(AudioClip);
            AudioClip = null;
            m_AudioSource.Stop();

            StopCoroutine(ReadRawAudio());

            if(OnStopRecording != null)
                OnStopRecording.Invoke();
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
        /// Returns a Root Mean Squared value of the audio data. Can be used to approximate volume.
        /// </summary>
        /// <returns>A float from 0 to 1</returns>
        public float GetRMS() {
            float sum = 0;
            for (int i = 0; i < Buffer.Length; i++)
                sum += Buffer[i] * Buffer[i];

            var rms = Mathf.Sqrt(sum / Buffer.Length);
            return rms;
        }

        IEnumerator ReadRawAudio() {
            int loops = 0;
            int readAbsPos = 0;
            int prevPos = 0;
            float[] tempAudioFrame = new float[Buffer.Length];

            while (AudioClip != null && Microphone.IsRecording(CurrentDeviceName)) {
                bool isNewDataAvailable = true;

                while (isNewDataAvailable) {
                    int currPos = Microphone.GetPosition(CurrentDeviceName);
                    if (currPos < prevPos)
                        loops++;
                    prevPos = currPos;

                    var currAbsPos = loops * AudioClip.samples + currPos;
                    var nextReadAbsPos = readAbsPos + tempAudioFrame.Length;

                    if (nextReadAbsPos < currAbsPos) {
                        AudioClip.GetData(tempAudioFrame, readAbsPos % AudioClip.samples);

                        Buffer = tempAudioFrame;

                        if(OnBufferReady != null)
                            OnBufferReady.Invoke(Buffer);

                        readAbsPos = nextReadAbsPos;
                        isNewDataAvailable = true;
                    }
                    else
                        isNewDataAvailable = false;
                }
                yield return null;
            }
        }
        #endregion 
    }
}