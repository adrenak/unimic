## UniMic
A wrapper for Unity's Microphone class. Proving easy APIs for mic input and management.

## API
Scripting API page, better samples and more documentation coming soon.

### Usage
`Mic.Init()` to initialize UniMic

`Mic.AvailableDevices` to get a list of available `Mic.Device` objects

The following API is available in `Mic.Device`:
* `OnStartRecording` event fired when the device starts recording
* `OnFrameCollected` event fired when the device has gathered PCM data for one frame of user defined duration. Parameters:
    - `int, float[]` the channel count and PCM float array of the frame
* `OnStopRecording` event fired when the device stops recording
* `Name` gets the name of the device
* `MaxFrequency` is the highest sampling frequency supported by the device
* `MinFrequency` is the lowest sampling frequency supported by the device
* `SupportsAnyFrequency` is true if the recording device supports any sampling frequeny
* `SamplingFrequency` is the user defined frequency at which is device will record
* `FrameDurationMS` is the audio duration of a single PCM frame that this device will report in the `OnFrameCollected` event
* `FrameLength` is the length of the float PCM array this device will report in the `OnFrameCollected` event
* `StartRecording(int frameDurationMS)` starts the device recording at the highest supported sampling frequency and the user defined frame duration
* `StartRecording(int samplingFrequency, int frameDurationMS)` starts the device recording at the user defined sampling frequency and frame duration
* `StopRecording()` stops the device recording
* `IsRecording` returns if the device is currently recording audio

`MicAudioSource` component is available for playing back microphone feed. This class is also a good reference for writing your own code for incoming audio data.

## Tips
Just open the Unity project in Unity 2017.4.40f1+ and try the sample scene.  

## Contact
[@github](https://www.github.com/adrenak)  
[@website](http://www.vatsalambastha.com)  
@discord: `adrenak#1934`