using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class MicrophoneManager : MonoBehaviour {
    float threshold = 0;
    float frequency = 0.0f;

    public int recordingSampleRate = 44100;
    public string microphoneDeviceName;
    public FFTWindow fftWindow;

    List<string> namesOfMicsAvailable = new List<string>();
    int spectrumSampleRate = 8192;
    AudioSource audioSource;

    void Awake() {
        audioSource = GetComponent<AudioSource>();

        // get all available microphones
        foreach (string device in Microphone.devices) {
            if (microphoneDeviceName == null) {
                //set default mic to first mic found.
                microphoneDeviceName = device;
            }
            namesOfMicsAvailable.Add(device);
        }
        microphoneDeviceName = namesOfMicsAvailable[0]; // default to first

        UpdateMicrophone();
    }

    void UpdateMicrophone() {
        audioSource.Stop();
        audioSource.clip = Microphone.Start(microphoneDeviceName, true, 10, recordingSampleRate);
        audioSource.loop = true;

        if (Microphone.IsRecording(microphoneDeviceName)) {
            // Wait until the recording starts
            while (!(Microphone.GetPosition(microphoneDeviceName) > 0)) { }

            Debug.Log("Recording started with " + microphoneDeviceName);
            audioSource.Play();
        }
        else
            Debug.Log(microphoneDeviceName + " doesn't work!");
    }


    /// <summary>
    /// Sets the given index from <see cref="GetDeviceNames()"/> as the active Mic
    /// </summary>
    /// <param name="micIndex">The index of the microphone to be used as returned by <see cref="GetDeviceNames()"/></param>
    public void ChangeMicDevice(int micIndex) {  // Use GetDeviceNames to get all the mics available and ten send the appropriate index
        microphoneDeviceName = namesOfMicsAvailable[micIndex];
        UpdateMicrophone();
    }

    /// <summary>
    /// Returns all the microphone names as a list
    /// </summary>
    /// <returns>All available mic device names as a list of strings</returns>
    public List<string> GetDeviceNames() {
        return namesOfMicsAvailable;
    }

    /// <summary>
    /// Sets the threshold for the spectrum, used for calculating fundamental frequency
    /// </summary>
    /// <param name="value">The new threshold to be set</param>
    public void SetThreshold(float value) {
        threshold = value;
    }

    /// <summary>
    /// Gets the average volume from the current output data
    /// </summary>
    /// <returns>Average volume in a float</returns>
    public float GetAveragedVolume() {
        float[] data = new float[256];
        float volumeSum = 0;
        audioSource.GetOutputData(data, 0);
        foreach (float s in data) {
            volumeSum += Mathf.Abs(s);
        }
        return volumeSum / 256;
    }

    /// <summary>
    /// Gets the current audio spectrum
    /// </summary>
    /// <param name="samplingRate">Number of samples required</param>
    /// <returns>Samples in a float array</returns>
    public float[] GetSpectrumData(int samplingRate) {
        var spectrumData = new float[samplingRate];
        audioSource.GetSpectrumData(spectrumData, 0, fftWindow);
        return spectrumData;
    }

    /// <summary>
    /// Gets the lowest frequency in the spectrum with the threshold in mid
    /// </summary>
    /// <returns>Fundamental frequency as a float</returns>
    public float GetFundamentalFrequency() {
        float fundamentalFrequency = 0.0f;
        var spectrumData = new float[spectrumSampleRate];
        audioSource.GetSpectrumData(spectrumData, 0, fftWindow);
        float s = 0.0f;
        int i = 0;
        for (int j = 1; j < spectrumSampleRate; j++) {
            if (spectrumData[j] > threshold) // volume must meet minimum threshold
            {
                if (s < spectrumData[j]) {
                    s = spectrumData[j];
                    i = j;
                }
            }
        }
        fundamentalFrequency = i * recordingSampleRate / spectrumSampleRate;
        frequency = fundamentalFrequency;
        return fundamentalFrequency;
    }
}