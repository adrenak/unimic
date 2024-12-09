using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace Adrenak.UniMic.Samples {
    public class MicDeviceCell : MonoBehaviour {
        public Toggle isRecordingToggle;
        public Text deviceNameText;
        public Image rmsIndicator;

        public void SetDeviceName(string name) {
            deviceNameText.text = name;
        }

        public void SetIsRecording(bool state) {
            isRecordingToggle.SetIsOnWithoutNotify(state);
        }

        public void SetRMS(float value) {
            value = Mathf.Clamp01(value);
            rmsIndicator.fillAmount = value;
            rmsIndicator.color = Color.Lerp(Color.green, Color.red, value);
        }
    }
}
