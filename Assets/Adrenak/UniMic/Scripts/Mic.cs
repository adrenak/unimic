using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace Adrenak.UniMic {
	[RequireComponent(typeof(AudioSource))]
	public class Mic : MonoBehaviour {
		// ================================================
		// FIELDS
		// ================================================
		#region MEMBERS
		/// <summary>
		/// Whether the microphone is running
		/// </summary>
		public bool IsRunning { get; private set; }

		/// <summary>
		/// The frequency at which the mic is operating
		/// </summary>
		public int Frequency { get; private set; }

		/// <summary>
		/// Last populated audio segment
		/// </summary>
		public float[] Segment { get; private set; }

		/// <summary>
		/// Segment duration/length in milliseconds
		/// </summary>
		public int SegmentLen { get; private set; }

		/// <summary>
		/// The length of the segment float array
		/// </summary>
		public int SegmentSize {
			get { return Frequency * SegmentLen / 1000; }
		}

		/// <summary>
		/// The AudioClip currently being streamed in the Mic
		/// </summary>
		public AudioClip AudioClip { get; private set; }

		/// <summary>
		/// List of all the available Mic devices
		/// </summary>
		public List<string> Devices { get; private set; }

		/// <summary>
		/// Index of the current Mic device in m_Devices
		/// </summary>
		public int CurrentDeviceIndex { get; private set; }

		/// <summary>
		/// Gets the name of the Mic device currently in use
		/// </summary>
		public string CurrentDeviceName {
			get { return Devices[CurrentDeviceIndex]; }
		}

		AudioSource m_AudioSource;      // Plays the audio clip at 0 volume to get spectrum data
		int m_SegmentCount = 0;
		#endregion

		// ================================================
		// EVENTS
		// ================================================
		#region EVENTS
		public class SegmentReadyEvent : UnityEvent<int, float[]> { }

		/// <summary>
		/// Invoked when the instance starts Recording.
		/// </summary>
		public UnityEvent OnStartRecording;

		/// <summary>
		/// Invoked everytime an audio frame is collected. Includes the frame.
		/// </summary>
		public SegmentReadyEvent OnSegmentReady = new SegmentReadyEvent();

		/// <summary>
		/// Invoked when the instance stop Recording.
		/// </summary>
		public UnityEvent OnStopRecording;
		#endregion

		// ================================================
		// METHODS
		// ================================================
		#region METHODS

		static Mic m_Instance;
		public static Mic Instance {
			get {
				if (m_Instance == null)
					m_Instance = GameObject.FindObjectOfType<Mic>();
				if (m_Instance == null) {
					m_Instance = new GameObject("UniMic Microphone").AddComponent<Mic>();
					DontDestroyOnLoad(m_Instance.gameObject);
				}
				return m_Instance;
			}
		}

		void Awake() {
			m_AudioSource = GetComponent<AudioSource>();

			Devices = new List<string>();
			foreach (var device in Microphone.devices)
				Devices.Add(device);
			CurrentDeviceIndex = 0;
		}

		/// <summary>
		/// Changes to a Mic device for Recording
		/// </summary>
		/// <param name="index">The index of the Mic device. Refer to <see cref="Devices"/></param>
		public void ChangeDevice(int index) {
			Microphone.End(CurrentDeviceName);
			CurrentDeviceIndex = index;
			Microphone.Start(CurrentDeviceName, true, 1, Frequency);
		}

		/// <summary>
		/// Starts to stream the input of the current Mic device
		/// </summary>
		public void StartStreaming(int frequency = 16000, int segmentLen = 10) {
			StopStreaming();
			IsRunning = true;

			Frequency = frequency;
			SegmentLen = segmentLen;

			AudioClip = Microphone.Start(CurrentDeviceName, true, 1, Frequency);
			Segment = new float[Frequency / 1000 * SegmentLen * AudioClip.channels];

			m_AudioSource.clip = AudioClip;
			m_AudioSource.loop = true;
			m_AudioSource.volume = .001f;
			m_AudioSource.maxDistance = m_AudioSource.minDistance = 0;
			m_AudioSource.spatialBlend = 0;
			m_AudioSource.Play();

			StartCoroutine(ReadRawAudio());

			if (OnStartRecording != null)
				OnStartRecording.Invoke();
		}

		/// <summary>
		/// Ends the Mic stream.
		/// </summary>
		public void StopStreaming() {
			if (!Microphone.IsRecording(CurrentDeviceName)) return;

			IsRunning = false;

			Microphone.End(CurrentDeviceName);
			Destroy(AudioClip);
			AudioClip = null;
			m_AudioSource.Stop();

			StopCoroutine(ReadRawAudio());

			if (OnStopRecording != null)
				OnStopRecording.Invoke();
		}

		/// <summary>
		/// Gets the current audio spectrum
		/// </summary>
		/// <param name="fftWindow">The <see cref="FFTWindow"/> type used to create the spectrum.</param>
		/// <param name="sampleCount">The number of samples required in the output. Use POT numbers</param>
		/// <returns></returns>
		public float[] GetSpectrumData(FFTWindow fftWindow, int sampleCount) {
			var spectrumData = new float[sampleCount];
			try {
				m_AudioSource.GetSpectrumData(spectrumData, 0, fftWindow);
			}
			catch (NullReferenceException e) {
				spectrumData = null;
			}
			return spectrumData;
		}

		public float[] GetOutputData(int sampleCount) {
			var data = new float[sampleCount];
			m_AudioSource.GetOutputData(data, 0);
			return data;
		}

		public float GetVolume(int sampleCount = 1024) {
			var data = GetOutputData(sampleCount);
			float sum = 0;
			// Multiply by 1000 to compensate for the 0.01 self-volume value
			for (int i = 0; i < data.Length; i++)
				sum += Mathf.Abs(data[i] * 1000 * 100000);
			return sum / sampleCount;
		}

		IEnumerator ReadRawAudio() {
			int loops = 0;
			int readAbsPos = 0;
			int prevPos = 0;
			float[] tempAudioFrame = new float[Segment.Length];

			while (AudioClip != null && Microphone.IsRecording(CurrentDeviceName)) {
				bool isNewDataAvailable = true;

				while (isNewDataAvailable) {
					int currPos = Microphone.GetPosition(CurrentDeviceName);
					if (currPos < prevPos)
						loops++;
					prevPos = currPos;

					var currAbsPos = loops * AudioClip.samples + currPos;
					var nextReadAbsPos = readAbsPos + tempAudioFrame.Length;

					if (nextReadAbsPos < currAbsPos) {
						AudioClip.GetData(tempAudioFrame, readAbsPos % AudioClip.samples);

						Segment = tempAudioFrame;
						m_SegmentCount++;
						if (OnSegmentReady != null)
							OnSegmentReady.Invoke(m_SegmentCount, Segment);

						readAbsPos = nextReadAbsPos;
						isNewDataAvailable = true;
					}
					else
						isNewDataAvailable = false;
				}
				yield return null;
			}
		}
		#endregion
	}
}