/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointCalibratorDemoController : MonoBehaviour {

	public JointCalibrator demoCalibrator;

	IEnumerator Start()
	{
		while (!Input.GetKeyDown(KeyCode.Space))
		{
			yield return null;
		}
		// Some parameters of the demo can be activated by using some shortcuts:
		if (Input.GetKey(KeyCode.LeftControl))
		{
			// With left control, hide the puk's model.
			GameObject.Find("Controller (right)").SetActive(false);
		}
		if (Input.GetKey(KeyCode.LeftShift))
		{
			// With left shift, hide the orientation markers.
			GameObject.Find("Arm-Marker").SetActive(false);
			GameObject.Find("Hand-Marker").SetActive(false);
		}
		StartCoroutine(demoCalibrator.Calibrate());
	}
}