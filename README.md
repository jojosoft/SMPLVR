# SMPLVR

During my [bachelor's thesis](https://www.johannes-schirm.de/research/thesis/bachelor-self-avatars-in-virtual-reality/) about personalized avatars in virtual reality with SMPL, I developed a validation tool and a template scene for a self-avatar scenario, which can both be found inside this project made with Unity 5.5.0f3.

## Usage

### Self-avatar template

The scene named "SMPLVR" is the main scene in this project and can be used as a template for future scenarios featuring a self-avatar.
Before having the self-avatar ready, the following steps must be accomplished for every use:

1. Press space to start the setup routine.
1. With the participant in T-pose, the application can reassign the trackers correctly.
1. The offsets from the trackers to the participant's joints are now being estimated.
	For this, the participant should rotate their hands and feet around the joint without moving it too much.
	This has to be done for both hands and feet.
1. The beta values from the file "inputM.txt" or "inputF.txt" next to the application are used to personalize the SMPL avatar.

You can add callback functions to the Main component to be informed when the calibration phase for SMPLVR was finished.

### Experiment

The scene named "ExperimentMakeAvatar" contains the corresponding experiment workflow.
Please be aware that this scene requires the scene "InputMakeAvatar" to be executed beforehand, since it relies on the input values provided by it.

During the experiment, the participant can adjust all ten beta values in a random order with a specific amount of cycles (values can be adjusted) and blocks (values get reset).
The output is saved into a text file next to the main executable.

### Comparison

The scene named "MeasurementsComparison" can read in AssetBundles (one for each person) with a variable amount of body variations - either as a mesh or a text file with SMPL beta values - and perform a detailed comparison between them.
For this, it uses two additional input files: One that defines points of measure through vertex indices and one that combines these points to vertex-to-vertex measurements between them.

Each comparison will output both CSV and human-readable TXT files along with front and left renderings of all bodies from one person both vertically and horizontally aligned.
You can also activate an additional background grid, set the minimum resolution of the images, adjust the spacing between the bodies or skip the image rendering to only generate the textual comparison.
Depending on the SMPL model used, you need to set the amount of standard deviations, so the beta values can be converted correctly.
You can configure all this through the ComparisonController component, which is attached to the first object in the scene named "MPI".

## Legal information

SteamVR was used to connect to the HTC Vive headset, but since the plugin is freely available on the Unity Asset Store and changes frequently, its files are excluded from this project.

The SMPL model is free to use for research purposes and is available [here](http://smpl.is.tue.mpg.de/), the link was last visited in May 2017.
Please note that the SMPL model is used for research purposes in this project, according to [the official license](http://smpl.is.tue.mpg.de/license)!

The HTC Vive HMD model used in this project was created by Eternal Realm and is licensed under CC Attribution.
The model can be downloaded [here](https://sketchfab.com/models/4cee0970fe60444ead77d41fbb052a33), the link was last visited in May 2017.

The following third party modules are being used in this project:
+ [FinalIK](http://www.root-motion.com/final-ik.html) (/Assets/Plugins/RootMotion)  
	**IMPORTANT: FinalIK was deleted from this public release of the project because its commercial license prohibits redistribution of the source code!
	You need to import the FinalIK Unity package before opening the scene named "SMPLVR", otherwise the references to the Scripts may be confused.**
+ [VRMirror](https://github.com/AADProductions/VR-Mirrors) (/Assets/Mirror)
+ [FastMatrix](http://blog.ivank.net/lightweight-matrix-class-in-c-strassen-algorithm-lu-decomposition.html) (/Assets/SMPL/ThirdParty/Matrix)
+ [SimpleJSON](https://github.com/Bunny83/SimpleJSON) (/Assets/SMPL/ThirdParty/JSON)

The other scripts in /Assets/SMPL were written by [Joachim Tesch](https://gitlab.tue.mpg.de/jtesch).

Everything else was created by Johannes Schirm.
