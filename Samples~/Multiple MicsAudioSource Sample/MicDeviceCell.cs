using UnityEngine;
using UnityEngine.UI;

namespace Adrenak.UniMic.Samples {
    // Component that reprents a UI cell for a single mic device
    public class MicDeviceCell : MonoBehaviour {
        public Toggle isRecordingToggle;
        public Text deviceNameText;
        public Image rmsIndicator;

        // Set and show the device name
        public void SetDeviceName(string name) {
            deviceNameText.text = name;
        }

        // Set and show if the device is currencly recording
        public void SetIsRecording(bool state) {
            isRecordingToggle.SetIsOnWithoutNotify(state);
        }

        // Set the RMS (like loudness) and show it using a filled image
        public void SetRMS(float value) {
            value = Mathf.Clamp01(value);
            rmsIndicator.fillAmount = value;
            rmsIndicator.color = Color.Lerp(Color.green, Color.red, value);
        }
    }
}
