using UnityEngine;
using Adrenak.UniMic;

public class AudioVisualizer : MonoBehaviour {
    [SerializeField]
    Transform[] vizBars;

    [SerializeField]
    Transform smoothVolBar;

    [SerializeField]
    Transform absVolBar;

    [SerializeField]
    [Range(1, 1000)]
    public float scale = 100;

    [SerializeField]
    float scaleRate = 1;

    Mic m_Mic;

    void Start() {
        m_Mic = Mic.Create();
        m_Mic.StartRecording();
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.R))
            m_Mic.RestartRecording();

        if (!m_Mic.IsRunning) return;
        // Update volume bars
        smoothVolBar.localScale = new Vector3(
            smoothVolBar.localScale.x,
            Mathf.Lerp(smoothVolBar.localScale.y, m_Mic.GetRMS() * scale, scaleRate),
            smoothVolBar.localScale.z
        );

        absVolBar.localScale = new Vector3(
            absVolBar.localScale.x,
            Mathf.Lerp(absVolBar.localScale.y, m_Mic.GetRMS() * scale, scaleRate),
            absVolBar.localScale.z
        );

        // Update spectrum bars
        var spectrum = m_Mic.GetSpectrumData(FFTWindow.Rectangular, 512);
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
