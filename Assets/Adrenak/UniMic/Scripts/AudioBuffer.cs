using System;
using UnityEngine;

namespace Adrenak.UniMic {
    public class AudioBuffer {
        public AudioClip Clip { get; private set; }
        public event Action OnReady;

        int m_FeedCount;
        int m_BaseIndex;
        int m_SegCapacity;
        int m_SegSize;
        float m_Ready;

        public AudioBuffer(int frequency, int channels, int segSize, int segCapacity, float ready = .5F) {
            Clip = AudioClip.Create("clip", segSize * segCapacity, channels, frequency, false);
            m_BaseIndex = -1;
            m_SegSize = segSize;
            m_SegCapacity = segCapacity;
            m_Ready = ready;
        }

        public void Feed(float[] segment, int index) {
            if (m_BaseIndex == 1) m_BaseIndex = index;
            m_FeedCount++;

            index = (index - m_BaseIndex) % m_SegCapacity;
            Clip.SetData(segment, index * m_SegSize);

            if (m_FeedCount > m_SegCapacity * m_Ready && OnReady != null) {
                OnReady();
                OnReady = null;
            }
        }
    }
}
