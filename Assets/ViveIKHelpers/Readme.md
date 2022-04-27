# ViveIKHelpers

This module supports you with full body tracking using the HTC Vive and five HTC Vive pucks as trackers for hand right, hand left, pelvis, foot right and foot left.
It was developed by Johannes Schirm (johannes.schirm@tuebingen.mpg.de) in the context of the SMPLVR project for his bachelor's thesis, February 2017 to June 2017.

## TPoseCalibrator

Just attach the component to any game object in the scene and create a reference to the five tracked objects you want to use to receive tracking data.
The TPoseCalibrator expects the user standing in T-pose with his arms along the x-axis while having all five trackers attached and turned on.
For the best results, the user should face towards the positive z-axis during the calibration.
Call MonoBehaviour.StartCoroutine() with the result of the function TPoseCalibrator.Prepare() to start the preparation phase.
It will not return until there are at least five active and tracked generic devices available.
With the optional Action parameter, you can inform the user about the current number of active devices by displaying the given status string.
Once all five devices have been found and the function returned, you are ready to call TPoseCalibrator.Calibrate() to reassign the device indices according to their tracker's current position.
You can do this as many times as you need it.
Please note that the component will use the first five trackers that are available, so keep other puks turned off when you don't need them.

## JointCalibrator

This component must be attached to a child of a tracked object.
Call MonoBehaviour.StartCoroutine() with the result of the function JointCalibrator.Calibrate() to start the calibration phase.
Using the configuration via public variables, you can customize the calibration process in many ways.
Depending on the configuration of the component, it will wait until the user starts moving before running the analysis.
At the end of the process, the child object this component is attached to will be locally moved to the measured center point of the user's movement.
Of course, you can repeat the process as many times as you need it.
This component can be used universally, since it does not depend on the SteamVR setup.
It just uses the parent object's transform to grab the current tracked position.