using UnityEngine;

namespace UniMic {
    public class AudioVisualizer : MonoBehaviour {
        [SerializeField]
        MicrophoneManager m_MicrophoneManager;

        [SerializeField]
        Transform[] vizBars;

        [Range(1, 1000)]
        public float scale = 100;

        public float scaleRate = 1;

        void Start() {
            m_MicrophoneManager = MicrophoneManager.Create(44100, 1, 20);
            m_MicrophoneManager.StartRecording("TEST");
        }

        void Update() {
            var spectrum = m_MicrophoneManager.GetSpectrumData(FFTWindow.Rectangular, 512);
            // TODO: This is rubbish logic but it looks genuine so ok. In reality, spectrum chunks should be added to get an actual 8-ISO standard spectrum or something
            //  There is a good material on this available on youtube here : https://www.youtube.com/watch?v=4Av788P9stk
            //  Will update the code with that later.
            for (int i = 0; i < vizBars.Length; i++) {
                var desiredHeight = spectrum[i] * scale;
                vizBars[i].localScale = new Vector3(
                    vizBars[i].localScale.x,
                    Mathf.Clamp(Mathf.Lerp(vizBars[i].localScale.y, desiredHeight, scaleRate), 0, 10),    // Make sure the spectrum doesn't go out
                    vizBars[i].localScale.z
                );
            }
        }
    }
}
