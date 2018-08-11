using System;
using UnityEngine;

namespace Adrenak.UniMic {
    public class AudioBuffer : MonoBehaviour{
        public AudioClip Clip { get; private set; }
        public event Action OnReady;

        int m_FeedCount;
        int m_BaseIndex;
        int m_SegCapacity;
        int m_SegSize;
        float m_Ready;

        public static AudioBuffer Create(int frequency, int channels, int segSize, int segCapacity, float ready = .5F) {
            var cted = new GameObject().AddComponent<AudioBuffer>();
            cted.Clip = AudioClip.Create("clip", segSize * segCapacity, channels, frequency, false);

            cted.m_BaseIndex = -1;
            cted.m_SegSize = segSize;
            cted.m_SegCapacity = segCapacity;
            cted.m_Ready = ready;

            return cted;
        }

        public void Feed(float[] segment, int index) {
            if (m_BaseIndex == -1) m_BaseIndex = index;
            if (index < m_BaseIndex) return;

            m_FeedCount++;

            index = (index - m_BaseIndex) % m_SegCapacity;
            Clip.SetData(segment, index * m_SegSize);
            
            if (m_FeedCount > m_SegCapacity * m_Ready - 1 && OnReady != null) {
                OnReady();
                OnReady = null;
            }
        }
    }
}
