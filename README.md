# OneWorldSDK Unity Demo

The One World SDK Unity Demo showcases how to view a worldwide base terrain at multiple levels of detail with support for WGS84 coordinates.
The user may fly anywhere around the world while a terrain streaming system automatically loads imagery and elevation data from disk or through web services.

With the One World SDK, the Earth can be seen from a low altitude to a high altitude.
![One World SDK - Earth](/docs/images/OneWorldSDK_Earth.PNG)

While connected to Bing Maps with an API key, elevation data is streamed on-the-fly for the current location.
![One World SDK - Mountains](/docs/images/OneWorldSDK_Mountains.PNG)

## Goals
We believe in leveraging the massive Unity developer base to build a realistic, whole-earth, and interactive world. **One World**. Free and Open-Source.

The world should be available on the devices you use from desktop to mobile devices. The Unity real-time 3D engine also has first-class support for Virtual Reality, Augmented Reality, and Mixed-Reality headsets to create immersive and highly compelling experiences.

Help us on this journey. If you have ideas to contribute to this project, **join us**. 

## Developer Setup

C# middleware libraries can be found in the [src/Libraries/](src/Libraries/) directory.
The [src/OneWorldSDK_UnityDemo/](src/OneWorldSDK_UnityDemo/) directory is a Unity Project with additional demo integration scripts in [src/OneWorldSDK_UnityDemo/Assets/Scripts/](src/OneWorldSDK_UnityDemo/Assets/Scripts/).

1. Open [src/Libraries/OneWorldSDK.sln](src/Libraries/OneWorldSDK.sln) using Visual Studio 2019.
2. Build the solution.
3. Then run copy_DLLs.bat.
4. Open the [src\OneWorldSDK_UnityDemo](src/OneWorldSDK_UnityDemo/) project in Unity. (The default Unity version is 2019.4.14f1)
5. In Unity, navigate to Assets->Scenes and double-click on the OneWorldDemo.unity scene.
6. Press the Play button.

## Camera Control
* C - Increase altitude
* X - Decrease altitude
* R - Pitch up
* F - Pitch down
* Q - Roll counter-clockwise
* E - Roll clockwise
* A - Left
* D - Right
* W - Forward
* S - Backward
* Space - reset view to be looking down at the globe

## Configuration

The [One World SDK Unity Demo configuration file](data/OneWorldSDK_Viewer.config.json) serves as a reference, and we recommend creating a local copy `data/OneWorldSDK_Viewer.local.config.json` for your local configuration settings to avoid accidentally committing local settings or your private API key.

### Elevation

By default, no elevation will be present. Elevation can be easily supported using a [Bing Maps](https://www.microsoft.com/en-us/maps/create-a-bing-maps-key) API key:

``` json
  "elevationProviderType": "bing",
  "elevationProviderSettings": {
    "bing": {
      "apiKey": "<YOUR_API_KEY_HERE>"
    }
  }
```

## Setup Video
Please see this video showing the steps to setup the One World SDK for Unity.  
[![One World SDK Setup Tutorial](/docs/images/OneWorldSDKSetup.PNG)](https://www.youtube.com/watch?v=fdBDXimGr_o)

## Demo Videos
Several videos created using the One World SDK for Unity can be found on our [YouTube](https://www.youtube.com/c/SimBlocksio) channel.
[![One World SDK for Unity on YouTube](/docs/images/OneWorldSDK_YouTube.PNG)](https://www.youtube.com/playlist?list=PLj4ixw_Qvkbc7ujRJX40OT1G7cLq2Wc31)
  
Please **subscribe** to our channel and give us a **like** if you would like to see more of this content.

## Professional Services
Do you need to augment your software development team, or are you looking for an experienced team to develop a solution that leverages the One World SDK for Unity?
For custom development projects, please contact sales@simblocks.io  

## License
The source code in this repository is licensed under the MIT License. See the [LICENSE](LICENSE) text file for full terms.

