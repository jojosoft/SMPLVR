# SimpleInputVR

This module provides you with a simple setup that allows you to enter data values for predefined keys in VR only by using the keyboard and use them later in your main scene.
It was developed by Johannes Schirm (johannes.schirm@tuebingen.mpg.de) in the context of his placement, February 2016 to July 2016.

## Usage

Copy the InputDemo scene from the module's scenes folder to your scene folder.
Configure the InputGUIController component at the MPI object to fit your scenario and enter the main scene name that should be loaded once all values have been entered.
In your main scene, just read the text file that was created with the name you specified earlier, parse the values and delete it again if Debug.isDebugBuild is false to keep your files clean.