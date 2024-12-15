# UniMic
A wrapper for Unity's Microphone class. 
Provides easy APIs for mic input and management.

## Documentation
[Refer to the Scripting Reference](https://www.vatsalambastha.com/unimic/api/Adrenak.UniMic.html)

## Installing
⚠️ [OpenUPM](https://openupm.com/packages/com.adrenak.unimic/?subPage=versions) doesn't have up to date releases. Install using NPM registry instead 👇

Ensure you have the NPM registry in the `packages.json` file of your Unity project with `com.adrenak.unimic` as one of the scopes
```
"scopedRegistries": [
    {
        "name": "npmjs",
        "url": "https://registry.npmjs.org",
        "scopes": [
            "com.npmjs",
            "com.adrenak.unimic"
        ]
    }
}
```

Add `"com.adrenak.unimic" : "x.y.z"` to `dependencies` list in `packages.json` file where `x.y.z` is the version name
> 💬 You can see the versions on the NPM page [here](https://www.npmjs.com/package/com.adrenak.unimic?activeTab=versions). 

## Samples
⚠️ For the samples to work in the editor, ensure that atleast one recording device is connected to your PC/Mac.  

### Simple MicAudioSource Sample
- A gameobject called `My Mic` is placed. It has the `MicAudioSource` component. 
- Another gmeobject called `Sample` has `SimpleMicAudioSourceSample`
    - On start it checks if there are any mic devices available
    - If yes, it takes the first recording device available and starts the recording using `.StartRecording()`
    - Then assigns the device to `MicAudioSource`, which then starts playing the audio
- Play the scene. You should be able to hear your voice in the editor
### Multiple Mics UI Sample
- Run the scene, you should see every mic device connected 
    - On start, all the devices will start recording in parallel and playing back
    - Toggling the checkbox on or off will start or stop the recording for a device
- Prefab `MicDeviceCell` is used to represent one device. The `MicDeviceCell.cs` script handles its UI.
- Check out the `MicDeviceListSample` script to see how the UI has been created

## Note
* Some Xiaomi phones may prevent side loaded APKs from functioning.  
* There is no AEC (Acoustic Echo Cancelletion). If you're trying the samples be sure to use headphones to avoid creating a feedback loop.

## Contact
[@github](https://www.github.com/adrenak)  
[@website](http://www.vatsalambastha.com)  
@discord: `adrenak#1934`