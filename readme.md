# Unity App for Soundscape Design and Evaluation – Quickstart

Welcome to the [Innovedum Project “Unity App for Soundscape Design and Evaluation”](https://innovedumprojekte.ethz.ch/4110/en)! This Unity package enables auralizing 3D landscapes so that students of architecture and spatial planning disciplines and other users who are not familiar with environmental acoustics understand principles of soundscape and perception processes and can better imagine the soundscape. It uses Max 9 as an external sound engine.

## Installation & Setup 

1. Download the Unity package from the [releases page](https://github.com/danieborethz/DBAUG-DARCH-Soundscapes/releases)
2. Start up Max 9 and load the Patch. You can read more about how the patch works [here](https://github.com/danieborethz/DBAUG-DARCH-Soundscapes/blob/main/docs/Handout-SoundscapeGenerator-v4_compl_250306.pdf)
3. Create new unity project (universal 3D or 3D work with the provided assets) or open existing Unity Scene
4. Add the downloaded Unity package <img alt="Instructions on installing the unity package" src="/docs/images/Package_installation.jpg" />
5. Open up the Soundscape tool and load the audio library
    > [!IMPORTANT]
    > Only the folders that contain at least one supported sound file (mp3, wav, aiff) will be identified as a category and added. If the folder doesn't have a supported audio in it it will NOT be displayed.

    <img alt="Instructions on updating the audio library" src="/docs/images/Update_Audio_Library.jpg" />
6. In your scene, replace the camera with the Player prefab
7. Add the settings prefab to the scene as well
8. You're all set up! Check the [Components Guide section](#components-guide) for a detailed explanation of each component or open up the [Sample Scene](#sample-scene-walkthrough)
 
## Components Guide 
### Sound Source Audio Component
<img alt="Sound Source Audio Component" src="/docs/images/Sound_Source_Audio_Component.png" />

- **Category**: The category of the sound sources. Usually a folder in the provided audio library (See step 5 of Installation and setup)
- **Audio**: The audio files the Soundscape tool found in the respective category
- **Source Type**: Source channel on where the audio should play on the sound engine
- **Source**: Index of the source channel

<hr>

### Sound Source Generator
With the sound source generator you can generate two types: 
- Wind: Simulates rustling of leaves or needles of one or multiple trees
- Water: Simulates different water bodies such as rivers and fountains

### Sound Source Generator - Wind
<img alt="Sound Source Audio Component" src="/docs/images/Sound_Source_Generator_Wind.png" />
With wind you can either assign it to a single tree or a group objects of multiple trees (only the parent needs the component). If you assign it to a parent of a group then a forest size is calculated using the elements in the parent creating a wider sound instead of a single one. In the prefabs folder you find a forest prefab showcasing how it should be set up.

- **Foliage Type**: If the wind should either simulate a rustling of leaves or needles
- **Leaves/Tree Size**: Size of the leaves resp. tree
- **Channel**: On which stereo channel the generator should play. The generators always run on stereo

### Sound Source Generator - Water
<img alt="Sound Source Audio Component" src="/docs/images/Sound_Source_Generator_Water.png" />

- **Water Type**: If it either is a flow type water like river or a fountain type
- **Size**: Size of the water body
- **Channel**: On which stereo channel the generator should play. The generators always run on stereo
- **Splashing Time**: Only on water type splashing fountain. The duration is splashes water
- **Splashing Break**: Only on water type splashing fountain. The break between splashings.

<hr>

### Reverb Zone
<img alt="Sound Source Audio Component" src="/docs/images/Reverb_Zone_Handles.png" />
Local Reverb zones that alter/blend the reverb of the scene. They can be defined in the scene through handles (Colored cubes)
<img alt="Sound Source Audio Component" src="/docs/images/Reverb_Zone.png" />

- **Room Size**: Room Size of the local reverb
- **Decay Time**: Decay Time of the local reverb
- **Wet Dry Mix**: Wet/Dry Mix of the local reverb
- **Eq**: Eq of the local reverb
- **Radii**: The dimensions of the reverb zone. They can either be manipulated via the parameters or the handles in the scene itself.
- **Fade Radius**: The radius in which the fading to the new local reverb starts

<hr>

### Scene Manager
<img alt="Sound Source Audio Component" src="/docs/images/Scene_Manager.png" />
The Scene manager is inside the settings prefab which should always be added to the scene since it handles global settings and the communication between Unity and Max.

- **Wind Intensity**: The general intensity of the wind in the scene. Affects the Sound Source Generators of type wind
- **Wind Material**: Material which will be affected by the wind intensity. If you created a custom material for the trees you can set it here. The parameter the Scene Manager looks for is "_MotionSpeed"
- **Distance Scale**: Distance Scale of the whole scene
- **Occlusion Diameter Threshold**: The minimum diameter/area an object needs to have to generate occlusion
- **Global Room Size**: Global Room size for the scene reverb
- **Global Decay Time**: Global Decay Time for the scene reverb
- **Global Wet Dry Mix**: Global Wet/Dry Mix for the scene reverb
- **Global Eq**: Global Eq for the scene reverb
- **Enable Ambisonic Audio**: If the scene should have an ambisonic audio for the ambient of the environment
- **Ambisonic File**: All files for ambisonic found in the provided audio path. The Soundscape Tool looks through the provided audio library for a folder named "Ambisonics".

<hr>

## Sample Scene Walkthrough 
<img alt="Screenshot of the sample scene" src="/docs/images/Sample_Scene.png" />
The sample scene showcases the two main components in action: Sound Source Generator and Sound Source Audio Component and can be used as a starting point since it already includes the necessary components for the communication between Unity and Max.
### Forest
Showcases on how multiple trees together can be used to create a forest sound generator. Only the parent object needs to have a sound source and it will automatically calculate the size of the forest based on how big the area is that the child trees cover.
### Water
Showcases how the sound generator can be used for rivers (set to flow type)
### Bird & Path Bird
Showcases how the sound source audio can be used for moving objects as well. In this case Bird has the sound source audio component and visual representation and is linked to the path object that moves the bird object. The sound location gets adjusted and updated automatically.


 

   
