/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using ViveScale;

public class ExperimentMakeAvatarController : MonoBehaviour, Scalable {

	public int blendshapeCount = 10;
	public int cycles = 3;
	public int blocks = 2;
	public GameObject maleSMPL;
	public GameObject femaleSMPL;
	public ViveScaleController scaler;
	public bool relativeScaling = true;
	public Transform scaleDisplay;

	private string participantNumber;
	private string participantGender;
	private ExperimentDataRecorder recorder;
	private int cycleNumber = 1;
	private int blockNumber = 1;
	private int currentBlendshapeIndex;
	private bool scalingStart = false;
	private float lastStartPercentageOffset;

	void Start()
	{
		// Read the input data about the participant:
		try
		{
			string[] lines = File.ReadAllLines("tmpParticipantInfoFile.txt");
			participantNumber = lines[0].Split(':')[1];
			participantGender = lines[1].Split(':')[1];
		}
		catch
		{
			throw new UnityException("There were problems with getting the participant information. Execute the scene InputMakeAvatar before this one!");
		}
		if (!Debug.isDebugBuild)
		{
			File.Delete("tmpParticipantInfoFile.txt");
		}
		// Set the corresponding model visible:
		if (participantGender.StartsWith("m"))
		{
			femaleSMPL.SetActive(false);
		}
		else
		{
			maleSMPL.SetActive(false);
		}
		// Prepare the output data recorder:
		recorder = new ExperimentDataRecorder("SMPL-Johannes-1", "ParticipantNumber", "Gender", "Block", "CycleNumber", "BlendshapeNumber", "StandardDeviations");
		recorder.separateWithTabs = true;
		// Subscribe the controller to the ViveScale instance:
		scaler.Subscribe(this);
		// Start the experiment!
		StartCoroutine(ExperimentWorkflow());
	}

	private IEnumerator ExperimentWorkflow()
	{
		yield return StartCoroutine(ExperimentToolkit.waitForKey(KeyCode.Space));

		for (int block = 0; block < blocks; block++)
		{
			// Always start blocks with a break, but not the first block.
			if (block > 0)
			{
				UpdateTextDisplay("Time for a break!");
				yield return StartCoroutine(ExperimentToolkit.waitForKey(KeyCode.Space));
				UpdateTextDisplay("");
			}
			for (int cycle = 0; cycle < cycles; cycle++)
			{
				// Since the order of the blendshapes should be randomized, create an index array:
				int[] indices = GetNextIndices();
				foreach (int i in indices)
				{
					yield return StartCoroutine(ScaleBlendshape(i));
					float standardDeviations = -5 * CurrentModelRenderer.GetBlendShapeWeight(currentBlendshapeIndex * 2 + 1) / 100 + 5 * CurrentModelRenderer.GetBlendShapeWeight(currentBlendshapeIndex * 2) / 100;
					recorder.WriteNewDataIntoRecord(participantNumber, participantGender, blockNumber.ToString(), cycleNumber.ToString(), (currentBlendshapeIndex + 1).ToString(), standardDeviations.ToString());
				}
				cycleNumber++;
			}
			blockNumber++;
			ResetBlendshapes();
			cycleNumber = 1;
		}

		UpdateTextDisplay("Thanks for participating!");
	}

	private IEnumerator ScaleBlendshape(int blendshapeIndex)
	{
		currentBlendshapeIndex = blendshapeIndex;
		UpdateTextDisplay(currentBlendshapeIndex.ToString());
		UpdateScaleDisplay(BlendWeightToPercent(CurrentModelRenderer.GetBlendShapeWeight(currentBlendshapeIndex * 2), CurrentModelRenderer.GetBlendShapeWeight(currentBlendshapeIndex * 2 + 1)));
		scaler.gameObject.SetActive(true);
		if (scaler.controllerManager != null && scaler.controllerManager.left != null && scaler.controllerManager.right != null)
		{
			SteamVR_Controller.Device rightController = SteamVR_Controller.Input((int)scaler.controllerManager.right.GetComponent<SteamVR_TrackedObject>().index);
			SteamVR_Controller.Device leftController = SteamVR_Controller.Input((int)scaler.controllerManager.left.GetComponent<SteamVR_TrackedObject>().index);
			DateTime lastRightClick = DateTime.MinValue;
			DateTime lastLeftClick = DateTime.MaxValue;

			yield return StartCoroutine(ExperimentToolkit.waitForCondition(
				delegate() {
					return (lastLeftClick - lastRightClick).TotalSeconds < 1;
				},
				delegate() {
					if (rightController.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad))
					{
						lastRightClick = DateTime.Now;
					}
					if (leftController.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad))
					{
						lastLeftClick = DateTime.Now;
					}
				},
				true
			));
		}
		else
		{
			Debug.Log("Controller manager not available to experiment controller, using space key...");
			yield return StartCoroutine(ExperimentToolkit.waitForKey(KeyCode.Space));
		}
		scaler.gameObject.SetActive(false);
		lastStartPercentageOffset = -1f;
	}

	public void StartScaling()
	{
		// Indicate that a new initial value should be stored.
		scalingStart = true;
	}

	public void ReceiveNewScale(float newScale)
	{
		if (scalingStart)
		{
			lastStartPercentageOffset = newScale - BlendWeightToPercent(CurrentModelRenderer.GetBlendShapeWeight(currentBlendshapeIndex * 2), CurrentModelRenderer.GetBlendShapeWeight(currentBlendshapeIndex * 2 + 1));
			scalingStart = false;
		}
		int indexOffset = 0;
		float newBlendWeight = PercentToBlendWeight(relativeScaling ? newScale - lastStartPercentageOffset : newScale, out indexOffset);
		CurrentModelRenderer.SetBlendShapeWeight(currentBlendshapeIndex * 2 + indexOffset, newBlendWeight);
		// Make sure the other blendshape gets reset correctly, since only one blendshape should have a value greater 0 at a time:
		CurrentModelRenderer.SetBlendShapeWeight(currentBlendshapeIndex * 2 + (indexOffset + 1) % 2, 0);
		// The blendshapes may have moved the avatar, so get the feet on the ground again:
		Mesh currentMesh = new Mesh();
		CurrentModelRenderer.BakeMesh(currentMesh);
		currentMesh.RecalculateBounds();
		Vector3 rootPosition = CurrentModelRenderer.gameObject.transform.parent.position;
		rootPosition.y = -currentMesh.bounds.min.y;
		CurrentModelRenderer.gameObject.transform.parent.position = rootPosition;
		// If requested, scale the display unit:
		UpdateScaleDisplay(newScale);
	}

	private void UpdateScaleDisplay(float newScale)
	{
		if (scaleDisplay != null)
		{
			scaleDisplay.localScale = new Vector3(newScale < 50 ? (50 - newScale) * -0.02f : (newScale - 50) * 0.02f, 1, 1);
		}
	}

	private void UpdateTextDisplay(string text)
	{
		Text numberDisplay = scaleDisplay.parent.parent.GetComponentInChildren<Text>();
		if (numberDisplay != null)
		{
			numberDisplay.text = text;
		}
	}

	/// <summary>
	/// Returns an array containing all used blendshape indices in a randomized order.
	/// </summary>
	/// <returns>A randomized int array with all blendshape indices.</returns>
	private int[] GetNextIndices()
	{
		int[] indices = new int[blendshapeCount];
		for (int i = 0; i < blendshapeCount; i++)
		{
			indices[i] = i;
		}
		for (int i = indices.Length - 1; i > 0; i--)
		{
			int j = UnityEngine.Random.Range(0, i + 1);
			int tmp = indices[i];
			indices[i] = indices[j];
			indices[j] = tmp;
		}
		return indices;
	}

	/// <summary>
	/// Resets the blendshapes for both models.
	/// </summary>
	private void ResetBlendshapes()
	{
		new GameObject[] { maleSMPL, femaleSMPL }.ToList().ForEach(
			model => {
				for (int i = 0; i < blendshapeCount * 2; i++)
				{
					model.GetComponentInChildren<SkinnedMeshRenderer>().SetBlendShapeWeight(i, 0);
				}
			}
		);
	}

	/// <summary>
	/// Maps a percentage onto the two blendshape channels for negative and positive deviations.
	/// Values below 50 % get mapped onto the negative channel, while the others get mapped onto the positive.
	/// </summary>
	/// <returns>The resulting blend weight for the channel specified by blendshapeIndexOffset.</returns>
	/// <param name="percent">The percentage you want to convert.</param>
	/// <param name="blendshapeIndexOffset">0 for the positive channel, 1 for the negative channel.</param>
	private float PercentToBlendWeight(float percent, out int blendshapeIndexOffset)
	{
		// Limit the percent value to [0 100].
		percent = percent < 0 ? 0 : (percent > 100 ? 100 : percent);
		blendshapeIndexOffset = percent < 50 ? 1 : 0;
		return percent < 50 ? (50 - percent) * 2 : (percent - 50) * 2;
	}

	/// <summary>
	/// Maps a pair of blendshape values onto an overall percentage.
	/// </summary>
	/// <returns>The overall percentage represented by the two blend weights.</returns>
	/// <param name="blendWeightPositive">The blend weight of the positive channel.</param>
	/// <param name="blendWeightNegative">The blend weight of the negative channel.</param>
	private float BlendWeightToPercent(float blendWeightPositive, float blendWeightNegative)
	{
		return blendWeightPositive == 0 ? -(blendWeightNegative / 2.0f - 50) : blendWeightPositive / 2.0f + 50;
	}

	private SkinnedMeshRenderer CurrentModelRenderer
	{
		get
		{
			return participantGender.StartsWith("m") ? maleSMPL.GetComponentInChildren<SkinnedMeshRenderer>() : femaleSMPL.GetComponentInChildren<SkinnedMeshRenderer>();
		}
	}
}