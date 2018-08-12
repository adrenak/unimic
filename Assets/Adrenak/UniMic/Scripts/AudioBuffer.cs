using System;
using UnityEngine;

namespace Adrenak.UniMic {
    /// <summary>
    /// Used to put together seguesnces of out of order audio segments for seamless playback
    /// </summary>
    public class AudioBuffer {
        public AudioClip Clip { get; private set; }
        public event Action OnReady;

        int m_BaseIndex;
        int m_SegCapacity;
        int m_SegSize;
        float m_Ready;

        /// <summary>
        /// Create an instance
        /// </summary>
        /// <param name="frequency">The frequency of the audio segment</param>
        /// <param name="channels">Number of channels in the segments</param>
        /// <param name="segSize">Segment length. Should remain constant throughout the stream</param>
        /// <param name="segCapacity">The total number of segments stored as buffer. Longer buffers are required for high latency fluctuations. </param>
        /// <param name="ready">Completion of buffer upon which it is deemed ready for playback. Recommended .75 (75%)</param>
        public AudioBuffer (int frequency, int channels, int segSize, int segCapacity, float ready = .75F) {
            Clip = AudioClip.Create("clip", segSize * segCapacity, channels, frequency, false);

            m_BaseIndex = -1;
            m_SegSize = segSize;
            m_SegCapacity = segCapacity;
        }

        /// <summary>
        /// Feed an audio segment
        /// </summary>
        /// <param name="segment">Float array representation of the segment</param>
        /// <param name="index">the sequence number of the segment. Order should be ensured.</param>
        public void Feed(float[] segment, int index) {
            if (m_BaseIndex == -1) m_BaseIndex = index;
            if (index < m_BaseIndex) return;
            if (segment.Length != m_SegSize) return;

            index = (index - m_BaseIndex) % m_SegCapacity;
            Clip.SetData(segment, index * m_SegSize);
            if (index > m_SegCapacity * m_Ready - 1 && OnReady != null) {
                OnReady();
                OnReady = null;
            }
        }
    }
}
