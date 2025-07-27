using System.Collections;

using UnityEngine;

namespace Adrenak.UniMic.Samples {
    public class MultipleMicAudioSourceSample : MonoBehaviour {
        [SerializeField] MicDeviceCell cellTemplate;
        [SerializeField] Transform container;

        IEnumerator Start() {
            Mic.Init();

            // Wait until we have detected atleast one recording device
            yield return new WaitUntil(() => Mic.AvailableDevices.Count > 0);

            // Iterate through all the available devices
            foreach (var device in Mic.AvailableDevices) {
                // Instantiate a new cell for the device under the container
                var newCell = Instantiate(cellTemplate, container);

                // set initial state of the cell
                newCell.SetDeviceName(device.Name);
                newCell.SetIsRecording(device.IsRecording);

                // Listen to user toggle clicks and respond by starting or stopping the device
                newCell.isRecordingToggle.onValueChanged.AddListener(x => {
                    if (x)
                        device.StartRecording();
                    else
                        device.StopRecording();
                });

                // Subscribe to the device to know every time a frame (set of samples)
                // are collected. Use the samples to calculate the RMS and show if on the cell
                device.OnFrameCollected += (frequency, channels, samples) => {
                    newCell.SetRMS(RMS(samples));
                };

                // Update the UI every time the recording starts
                device.OnStartRecording += () => {
                    newCell.SetIsRecording(true);
                };

                // Update the UI every time the recording stops
                device.OnStopRecording += () => {
                    newCell.SetIsRecording(false);
                    newCell.SetRMS(0);
                };

                // Start the recording since now we've subscribed to the device events above
                device.StartRecording();

                // Attach a mic audio source to the new cell and set its device for playback
                var micAudioSource = newCell.gameObject.AddComponent<MicAudioSource>();
                micAudioSource.Device = device;
            }
        }

        // Returns the root mean squared value of pcm samples
        // This value can be thought of as a loudness of the samples
        // We use this to show the loudness on the cell UI
        float RMS(float[] samples) {
            float sum = 0.0f;
            foreach (var sample in samples) {
                sum += sample * sample;
            }
            return Mathf.Sqrt(sum / samples.Length);
        }
    }
}
