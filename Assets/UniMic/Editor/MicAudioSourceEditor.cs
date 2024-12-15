#if UNITY_EDITOR

using UnityEditor;

using UnityEngine;

namespace Adrenak.UniMic {
    [CustomEditor(typeof(MicAudioSource))]
    public class MicAudioSourceEditor : Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            var me = (MicAudioSource)target;

            GUI.enabled = false;
            if (me.Device != null) {

                EditorGUILayout.LabelField("Device Info");
                Microphone.GetDeviceCaps(me.Device.Name, out int min, out int max);
                EditorGUILayout.IntField("Max Frequency", max);
                EditorGUILayout.IntField("Min Frequency", min);
                EditorGUILayout.Toggle("Is Recording", me.Device.IsRecording);
                EditorGUILayout.IntField("Sampling Frequency", me.Device.SamplingFrequency);
                EditorGUILayout.IntField("Frame Duration (ms)", me.Device.FrameDurationMS);
            }
            else
                EditorGUILayout.LabelField("Device Info will be shown in playmode when a device is assigned using .Device");
            GUI.enabled = true;
        }
    }
}
#endif