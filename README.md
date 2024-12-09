## UniMic
A wrapper for Unity's Microphone class. Proving easy APIs for mic input and management.

## API
Scripting API page and more documentation coming soon.

### Usage
`Mic.Init()` to initialize UniMic

`Mic.AvailableDevices` to get a list of available `Mic.Device` objects

The following API is available in `Mic.Device`:
* `DEFAULT_FRAME_DURATION_MS` the default length of the frame in milliseconds.
* `OnStartRecording` event fired when the device starts recording
* `OnFrameCollected` event fired when the device has gathered PCM data for one frame of user defined duration. Parameters:
    - `int, float[]` the channel count and PCM float array of the frame
* `OnStopRecording` event fired when the device stops recording
* `Name` gets the name of the device
* `MaxFrequency` is the highest sampling frequency supported by the device
* `MinFrequency` is the lowest sampling frequency supported by the device
* `VolumeMultiplier` increases/decreases volume by multiplying PCM samples
* `SupportsAnyFrequency` is true if the recording device supports any sampling frequeny
* `SamplingFrequency` is the user defined frequency at which is device will record
* `FrameDurationMS` is the audio duration of a single PCM frame that this device will report in the `OnFrameCollected` event
* `FrameLength` is the length of the float PCM array this device will report in the `OnFrameCollected` event
* `StartRecoring()` starts the device recording at the highest supported sampling frequency and the default frame duration defined by `DEFAULT_FRAME_DURATION_MS`
* `StartRecording(int frameDurationMS)` starts the device recording at the highest supported sampling frequency and the user defined frame duration
* `StartRecording(int samplingFrequency, int frameDurationMS)` starts the device recording at the user defined sampling frequency and frame duration
* `StopRecording()` stops the device recording
* `IsRecording` returns if the device is currently recording audio

`MicAudioSource` component is available for playing back microphone feed. This class is also a good reference for writing your own code for incoming audio data. This class includes:
* `autoStart` configurable in the editor. Whether the component should automatically start playing back audio from the first device detected
* `Device` is a reference to the device this object it currently playing
* `bufferDurationMS` is the length in milliseconds of the internal audio buffer that the object uses. The value defined here is also the time it would take for playback to start and the latency between audio capture and playback
* `SetDeviceByName(string deviceName, bool autoStart = false)` changes the device this instance is playing. If `autoStart` is true, it automatically starts playing the audio being captured by the new device.
* `SetDevice(Mic.Device device, bool autoStart = false)` similar to `SetDeviceByName` except it takes a `Mic.Device` object as the first parameter. This is recommended over `SetDeviceByName`
* `StartRecording` starts/resumes the recording. A device MUST be registered using `SetDevice` or `SetDeviceByName` method before calling this.
* `StopRecording` stops the recording.

## Samples
View `Basic MicAudioSource Sample` for single mic sample  
View `Multiple Mic UI Sample` for a more complicated sample that shows all the available mics on a UI and allows you to mute/unmute them.

## Note
Some Xiaomi phones may prevent side loaded APKs from functioning.  
There is no AEC (Acoustic Echo Cancelletion). If you're trying the samples be sure to use headphones to avoid creating a feedback loop.

## Contact
[@github](https://www.github.com/adrenak)  
[@website](http://www.vatsalambastha.com)  
@discord: `adrenak#1934`