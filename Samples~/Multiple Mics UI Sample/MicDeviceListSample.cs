using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Adrenak.UniMic.Samples {
    public class MicDeviceListSample : MonoBehaviour {
        [SerializeField] MicDeviceCell cellTemplate;
        [SerializeField] Transform container;

        List<MicDeviceCell> cells = new List<MicDeviceCell>();

        IEnumerator Start() {
            Mic.Init();

            yield return new WaitUntil(() => Mic.AvailableDevices.Count > 0);

            float RMS(float[] lastPCM) {
                float sum = 0.0f;
                foreach (var sample in lastPCM) {
                    sum += sample * sample;
                }
                return Mathf.Sqrt(sum / lastPCM.Length);
            }

            foreach (var device in Mic.AvailableDevices) {
                var newCell = Instantiate(cellTemplate, container);

                // Attach a mic audio source to the new cell, set the device
                // and start playback
                var source = newCell.gameObject.AddComponent<MicAudioSource>();
                source.SetDevice(device, true);

                // Set initial state of the cell
                newCell.SetDeviceName(device.Name);
                newCell.SetIsRecording(device.IsRecording);

                // Calculate the RMS and update the visual visual every time we 
                // receive an audio frame
                device.OnFrameCollected += (x, y) => {
                    newCell.SetRMS(RMS(y));
                };

                // Listen to toggle clicks and respond
                newCell.isRecordingToggle.onValueChanged.AddListener(x => {
                    if (x)
                        device.StartRecording();
                    else {
                        device.StopRecording();
                        newCell.SetRMS(0);
                    }
                });

                cells.Add(newCell);
            }
        }
    }
}
