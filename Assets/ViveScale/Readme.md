# ViveScale

This module offers an intuitive input method for scaling arbitrary values using the two HTC Vive controllers.
It was developed by Johannes Schirm (johannes.schirm@tuebingen.mpg.de) in the context of his placement, February 2016 to July 2016.

## Usage

First, you need your own class that implements the interface ViveScale.Scalable.
This class should then be subscribed to the scale controller using ViveScaleController.Subscribe() and passing an instance of that class.
Now you just need to handle the interface functions ViveScale.Scalable.StartScaling() and ViveScale.Scalable.ReceiveNewScale() according to your scenario!