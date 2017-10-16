using UnityEngine;

public class AudioVisualizer : MonoBehaviour {
    [SerializeField]
    MicrophoneManager micManager;

    [SerializeField]
    Transform[] visualizerBars;

    [Range(1, 1000)]
    public float visualizerScale = 100;

    public float barInterpolationRate = 1;

    void Update() {
        float[] spectrum = micManager.GetSpectrumData(1024) ;

        // TODO: This is rubbish logic but it looks genuine so ok. In reality, spectrum chunks should be added to get an actual 8-ISO standard spectrum or something
        //  There is a good material on this available on youtube here : https://www.youtube.com/watch?v=4Av788P9stk
        //  Will update the code with that later.
        for (int i = 0; i < visualizerBars.Length; i++) {
            var desiredHeight = spectrum[i] * visualizerScale;
            visualizerBars[i].localScale = new Vector3(
                visualizerBars[i].localScale.x,
                Mathf.Clamp(Mathf.Lerp(visualizerBars[i].localScale.y, desiredHeight, barInterpolationRate), 0, 20),    // Make sure the spectrum doesn't go out
                visualizerBars[i].localScale.z
            );
        }
    }
}
