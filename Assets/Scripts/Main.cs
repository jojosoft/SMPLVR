/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;

using RootMotion.FinalIK;

/// <summary>
/// The main controller for the SMPLVR setup.
/// Provide a callback function and you will be notified once your full self-avatar ready to go!
/// The complete calibration and the avatar personalization will be done here.
/// </summary>
public class Main : MonoBehaviour {

	public GameObject maleSMPL;
	public GameObject femaleSMPL;
	public GameObject mirror;
	public Text textDisplay;
	public JointCalibrator armRightCalibrator;
	public JointCalibrator armLeftCalibrator;
	public JointCalibrator footRightCalibrator;
	public JointCalibrator footLeftCalibrator;
	public int poseCaptureDelayMs = 5000;
	public int offsetCaptureDelayMs = 5000;
	public IKSolverVR solver = new IKSolverVR();

	private List<Action> callbacksWhenFinished = new List<Action>();
	private TPoseCalibrator trackerCalibrator;

	void Start()
	{
		// Retrieve other components needed for the setup:
		trackerCalibrator = this.GetComponent<TPoseCalibrator>();
		// Start the main workflow for setting up the self-avatar:
		StartCoroutine(MainSMPLVRWorkflow());
	}

	private IEnumerator MainSMPLVRWorkflow()
	{
		yield return ExperimentToolkit.waitForKey(KeyCode.Space);
		// Before doing anything, check whether the beta input file exists:
		if (Directory.GetFiles(".", "input?.txt").Length < 1)
		{
			yield return DisplayText("No input file with betas found.");
			throw new UnityException("No input file with betas found. It must be either ./inputF.txt or ./inputM.txt, next to the application.");
		}
		// 1. Prepare the TPoseCalibrator and calibrate the trackers
		yield return trackerCalibrator.Prepare(delegate(string status) {
			StartCoroutine(DisplayText("Trackers found:\n\n" + status));
		});
		yield return ShowCountDown(poseCaptureDelayMs, delegate(int remainingMs) {
			return "1. Tracker calibration phase!\n\nYou have " + (remainingMs / 1000f).ToString("F1") + " seconds left to get into TPose.";
		});
		trackerCalibrator.Calibrate();
		// 2. Run the IK target calibration for each of the four main trackers:
		yield return ShowCountDown(offsetCaptureDelayMs, delegate(int remainingMs) {
			return "2. Calibration of the right arm.\n\nStart in " + (remainingMs / 1000f).ToString("F1") + " seconds.";
		});
		yield return armRightCalibrator.Calibrate();
		yield return ShowCountDown(offsetCaptureDelayMs, delegate(int remainingMs) {
			return "3. Calibration of the left arm.\n\nStart in " + (remainingMs / 1000f).ToString("F1") + " seconds.";
		});
		yield return armLeftCalibrator.Calibrate();
		yield return ShowCountDown(offsetCaptureDelayMs, delegate(int remainingMs) {
			return "4. Calibration of the right foot.\n\nStart in " + (remainingMs / 1000f).ToString("F1") + " seconds.";
		});
		yield return footRightCalibrator.Calibrate();

		yield return ShowCountDown(offsetCaptureDelayMs, delegate(int remainingMs) {
			return "5. Calibration of the left foot.\n\nStart in " + (remainingMs / 1000f).ToString("F1") + " seconds.";
		});
		yield return footLeftCalibrator.Calibrate();
		// 3. Apply the given beta values to the model and recalculate the rig:
		string inputFile = Directory.GetFiles(".", "input?.txt")[0];
		GameObject smplBody = inputFile.Contains("inputM.txt") ? maleSMPL : femaleSMPL;
		SMPLModifyBones bonesModifier = smplBody.GetComponentInChildren<SMPLModifyBones>(true);
		float[] betas = File.ReadAllLines(inputFile).ToList().ConvertAll(line => float.Parse(line.Trim())).ToArray();
		bonesModifier.SetShapeWeights(betas);
		bonesModifier.UpdateBonePositions(betas);
		// 4. Move the foot targets because FinalIK will use the toe joint instead of the ankle joint for foot IK:
		GameObject.Find("FootRightTarget").transform.position += smplBody.GetComponent<VRIK>().references.rightToes.transform.position - smplBody.GetComponent<VRIK>().references.rightFoot.transform.position;
		GameObject.Find("FootLeftTarget").transform.position += smplBody.GetComponent<VRIK>().references.leftToes.transform.position - smplBody.GetComponent<VRIK>().references.leftFoot.transform.position;
		// 5. Deactivate the other SMPL model and apply the IK solver with the corresponding arm bend goals to the model we want to use:
		if (smplBody == maleSMPL)
		{
			femaleSMPL.SetActive(false);
			maleSMPL.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToList().ForEach(renderer => renderer.enabled = true);
		}
		else
		{
			maleSMPL.SetActive(true);
			femaleSMPL.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToList().ForEach(renderer => renderer.enabled = true);
		}
		this.solver.rightArm.bendGoal = smplBody.GetComponentsInChildren<Transform>(true).ToList().Where(transform => transform.gameObject.name.Equals("ArmRightBendGoal")).First();
		this.solver.leftArm.bendGoal = smplBody.GetComponentsInChildren<Transform>(true).ToList().Where(transform => transform.gameObject.name.Equals("ArmLeftBendGoal")).First();
		smplBody.GetComponentInChildren<VRIK>(true).solver = this.solver;
		smplBody.GetComponentInChildren<VRIK>(true).enabled = true;
		// Done! Empty the text field and call the callback functions.
		yield return DisplayText("Finished!\n\nPress enter to toggle the mirror.");
		Finished();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Return))
		{
			mirror.SetActive(!mirror.activeSelf);
		}
	}

	/// <summary>
	/// Displays the text to the user.
	/// </summary>
	/// <param name="text">The text you want to display.</param>
	/// <param name="waitForKey">If set to <c>true</c>, this function will wait for key before it returns.</param>
	/// <param name="key">If you want to wait for a specific key, provide it here.</param>
	private IEnumerator DisplayText(string text, bool waitForKey = false, KeyCode key = KeyCode.Space)
	{
		textDisplay.text = text;
		if (waitForKey)
		{
			yield return ExperimentToolkit.waitForKey(key);
		}
	}

	private IEnumerator ShowCountDown(int timeMs, Func<int, string> textBuilder)
	{
		DateTime startTime = DateTime.Now;
		yield return ExperimentToolkit.waitForCondition(delegate() {
			return (DateTime.Now - startTime).TotalMilliseconds >= timeMs;
		}, delegate() {
			StartCoroutine(DisplayText(textBuilder(timeMs - (int)(DateTime.Now - startTime).TotalMilliseconds)));
		});
	}

	/// <summary>
	/// Adds the callback to the internal list.
	/// It will be called as soon as SMPLVR was fully set up.
	/// </summary>
	/// <param name="callback">Your callback function.</param>
	public void AddFinishedCallback(Action callback)
	{
		callbacksWhenFinished.Add(callback);
	}

	/// <summary>
	/// Is called when the SMPLVR scene with a full self-avatar was set up.
	/// All callback functions will be called.
	/// </summary>
	private void Finished()
	{
		// Trigger all the callbacks:
		callbacksWhenFinished.ForEach(callback => callback());
	}
}