using UnityEngine;
using Adrenak.UniMic;
using System.Collections;

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
    AudioBuffer m_AudioBuffer;
    AudioSource m_Source;

    void Start() {
        m_Source = gameObject.AddComponent<AudioSource>();

        m_Mic = Mic.Create();
        m_Mic.StartStreaming(16000, 20, 0);

        m_AudioBuffer = AudioBuffer.Create(16000, 1, 320, 100, .8F);

        int count = 0;
        m_Mic.OnSegmentReady.AddListener(segment => {
            // Do a moving average to remove noise
            for (int i = 2; i < segment.Length - 2; i++)
                segment[i] = (segment[i - 2] + segment[i - 1] + segment[i] + segment[i + 1] + segment[i + 2]) / 5;
            m_AudioBuffer.Feed(segment, count);
            count++;
        });

        m_Source.loop = true;
        m_Source.clip = m_AudioBuffer.Clip;
        m_AudioBuffer.OnReady += delegate () {
            m_Source.Play();
        };    
    }
    
    void Update() {
        if (Input.GetKeyDown(KeyCode.R))
            m_Mic.StartStreaming();

        if (!m_Mic.IsRunning) return;
        
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
