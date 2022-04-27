/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TPoseCalibratorDemoController : MonoBehaviour {

	public TPoseCalibrator tposeCalibrator;
	public float captureDelaySeconds = 5;

	IEnumerator Start()
	{
		yield return StartCoroutine(tposeCalibrator.Prepare());
		Debug.Log("[TPoseCalibratorDemoController] Finished preparation! Starting calibration in " + captureDelaySeconds + " seconds!");
		yield return new WaitForSeconds(captureDelaySeconds);
		tposeCalibrator.Calibrate();
		Debug.Log("[TPoseCalibratorDemoController] Calibration finished!");
	}
}