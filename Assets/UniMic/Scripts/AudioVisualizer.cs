using UnityEngine;

namespace UniMic {
    public class AudioVisualizer : MonoBehaviour {
        [SerializeField]
        Transform[] vizBars;

        [SerializeField]
        Transform volBar;

        [SerializeField]
        [Range(1, 1000)]
        public float scale = 100;

        [SerializeField]
        float scaleRate = 1;

        MicrophoneManager m_MicrophoneManager;

        void Start() {
            m_MicrophoneManager = MicrophoneManager.Create(16000, 1, 20);
            m_MicrophoneManager.StartRecording("TEST");
        }

        void Update() {
            // Update volume bar
            volBar.localScale = new Vector3(
                volBar.localScale.x,
                Mathf.Lerp(volBar.localScale.y, m_MicrophoneManager.GetRMS() * scale, scaleRate),
                volBar.localScale.z
            );

            // Update spectrum bars
            var spectrum = m_MicrophoneManager.GetSpectrumData(FFTWindow.Rectangular, 512);
            // TODO: This is rubbish logic but it looks genuine so ok. In reality, spectrum chunks should be added to get an actual 8-ISO standard spectrum or something
            //  There is a good material on this available on youtube here : https://www.youtube.com/watch?v=4Av788P9stk
            //  Will update the code with that later.
            for (int i = 0; i < vizBars.Length; i++) {
                var desiredHeight = spectrum[i * (512 / vizBars.Length)] * scale;
                vizBars[i].localScale = new Vector3(
                    vizBars[i].localScale.x,
                    Mathf.Lerp(vizBars[i].localScale.y, desiredHeight, scaleRate),    // Make sure the spectrum doesn't go out
                    vizBars[i].localScale.z
                );
            }
        }
    }
}
