#if UNITY_EDITOR

using UnityEditor;

using UnityEngine;

namespace Adrenak.UniMic {
    [CustomEditor(typeof(MicAudioSource))]
    public class MicAudioSourceEditor : Editor {
        private bool showInfo;

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            var me = (MicAudioSource)target;

            if (me.Device == null && Mic.AvailableDevices.Count == 0) return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Device");
            var selectedName = Mic.AvailableDevices[0].Name;
            if (me.Device != null) 
                selectedName = me.Device.Name;

            if (EditorGUILayout.DropdownButton(new GUIContent(selectedName), FocusType.Keyboard)) {
                var menu = new GenericMenu();

                foreach (var device in Mic.AvailableDevices)
                    menu.AddItem(
                        new GUIContent(device.Name),
                        me.Device != null ? me.Device.Name == device.Name : false,
                        OnDeviceSelected,
                        device.Name
                    );

                menu.ShowAsContext();
            }

            EditorGUILayout.EndHorizontal();

            if(me.Device != null) {
                if (showInfo = EditorGUILayout.BeginFoldoutHeaderGroup(showInfo, "Debug Info")) {
                    GUI.enabled = false;

                    Microphone.GetDeviceCaps(me.Device.Name, out int min, out int max);
                    EditorGUILayout.IntField("Max Frequency", max);
                    EditorGUILayout.IntField("Min Frequency", min);
                    EditorGUILayout.Toggle("Is Recording", me.Device.IsRecording);
                    EditorGUILayout.IntField("Sampling Frequency", me.Device.SamplingFrequency);
                    EditorGUILayout.IntField("Frame Duration (ms)", me.Device.FrameDurationMS);

                    GUI.enabled = true;
                }
            }
        }

        // Handler for when a menu item is selected
        private void OnDeviceSelected(object deviceName_) {
            var deviceName = (string)deviceName_;
            var me = (MicAudioSource)target;
            me.SetDeviceByName(deviceName);
        }
    }
}
#endif