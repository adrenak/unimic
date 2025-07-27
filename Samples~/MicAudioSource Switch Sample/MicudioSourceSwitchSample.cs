using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

namespace Adrenak.UniMic.Samples {
    public class MicudioSourceSwitchSample : MonoBehaviour {
        [SerializeField] MicAudioSource micAudioSource;
        [SerializeField] Dropdown options;

        void Start() {
            Mic.Init();

            if (Mic.AvailableDevices.Count == 0) return;

            // Populate the options
            options.options = Mic.AvailableDevices.Select(x => new Dropdown.OptionData {
                text = $"{x.Name} [{x.MaxFrequency}, {x.MinFrequency}]"
            }).ToList();
            options.value = 0;

            // By default use the first device
            micAudioSource.Device = Mic.AvailableDevices[0];
            micAudioSource.Device.StartRecording();

            // Listen to user dropdown selection to switch device
            options.onValueChanged.AddListener(x => {
                micAudioSource.Device.StopRecording();
                micAudioSource.Device = Mic.AvailableDevices[x];
                micAudioSource.Device.StartRecording();
            });
        }
    }
}
