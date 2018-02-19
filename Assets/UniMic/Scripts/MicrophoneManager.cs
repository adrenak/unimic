using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UniMic {
    [RequireComponent(typeof(AudioSource))]
    public class MicrophoneManager : MonoBehaviour {
        bool m_IsRecording;
        string m_Key;

        // Unity Microphone params
        int m_SampleRate = 16000;
        int m_BufferDurationSec = 1;

        // Internal concat params
        float[] m_AudioFrameBuffer;
        int m_FrameDurationMs = 20;

        // Unity Components
        AudioClip m_AudioClip;
        AudioSource m_AudioSource;

        // Devices
        int m_CurrMicDeviceIndex;
        List<string> m_MicDevices = new List<string>();

        // Events
        public delegate void StringEvent(string param);
        public delegate void FloatArrayEvent(float[] param);

        public event StringEvent onRecordingStart;
        public event FloatArrayEvent onAudioFrameCollected;
        public event StringEvent onRecordingStop;

        public static MicrophoneManager Create(int sampleRate, int bufferDurationSec, int frameDurationMs) {
            GameObject cted = new GameObject("MicrophoneManager");
            DontDestroyOnLoad(cted);
            MicrophoneManager instance = cted.AddComponent<MicrophoneManager>();

            instance.m_SampleRate = sampleRate;
            instance.m_BufferDurationSec = bufferDurationSec;
            instance.m_FrameDurationMs = frameDurationMs;

            return instance;
        }

        void Awake() {
            m_AudioSource = GetComponent<AudioSource>();
            m_AudioFrameBuffer = new float[m_SampleRate / 1000 * m_FrameDurationMs];

            foreach (var device in Microphone.devices)
                m_MicDevices.Add(device);

            m_CurrMicDeviceIndex = 0;
        }

        public void StartRecording (string key) {
            Microphone.End(GetCurrMicDevice()); 
            if (!Microphone.IsRecording(GetCurrMicDevice())) {
                m_Key = key;
                m_IsRecording = true;

                m_AudioClip = Microphone.Start(GetCurrMicDevice(), true, m_BufferDurationSec, m_SampleRate);
                m_AudioSource.clip = m_AudioClip;
                m_AudioSource.loop = true;
                m_AudioSource.volume = 1;
                m_AudioSource.Play();

                StartCoroutine(ReadRawAudio());

                if(onRecordingStart != null)
                    onRecordingStart(m_Key);
            }
        }

        public float[] GetSpectrumData(FFTWindow fftWindow, int sampleCount) {
            var spectrumData = new float[sampleCount];
            m_AudioSource.GetSpectrumData(spectrumData, 0, fftWindow);
            return spectrumData;
        }

        public void StopRecording() {
            if (Microphone.IsRecording(GetCurrMicDevice())) {
                m_IsRecording = false;
                Microphone.End(GetCurrMicDevice());

                StopCoroutine(ReadRawAudio());

                Destroy(m_AudioClip);
                m_AudioClip = null;

                if(onRecordingStop != null)
                    onRecordingStop(m_Key);
            }
        }

        IEnumerator ReadRawAudio() {
            int loops = 0;
            int readAbsPos = 0;
            int prevPos = 0;

            while (m_AudioClip != null && Microphone.IsRecording(GetCurrMicDevice())) {
                bool isNewDataAvailable = true;

                while (isNewDataAvailable) {
                    int currPos = Microphone.GetPosition(GetCurrMicDevice());
                    if (currPos < prevPos)
                        loops++;
                    prevPos = currPos;

                    var currAbsPos = loops * m_AudioClip.samples + currPos;
                    var nextReadAbsPos = readAbsPos + m_AudioFrameBuffer.Length;

                    if (nextReadAbsPos < currAbsPos) {
                        m_AudioClip.GetData(m_AudioFrameBuffer, readAbsPos % m_AudioClip.samples);

                        if(onAudioFrameCollected != null)
                            onAudioFrameCollected(m_AudioFrameBuffer);

                        readAbsPos = nextReadAbsPos;
                        isNewDataAvailable = true;
                    }
                    else
                        isNewDataAvailable = false;
                }
                yield return null;
            }
        }
    
        string GetCurrMicDevice() {
            return m_MicDevices[m_CurrMicDeviceIndex];
        }
    }
}
