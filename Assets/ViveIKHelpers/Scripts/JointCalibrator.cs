/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

public class JointCalibrator : MonoBehaviour {

	/// <summary>
	/// A threshold that defines how far a grid point must travel in meters within one frame before it is considered to be travelled a distance distance during that frame.
	/// </summary>
	public float thresholdDistanceTravel = 0.002f;
	/// <summary>
	/// A threshold that defines after which amount of travelled distance in meters (for the most travelled grid point during the time covered by the buffer) the calibrator should start to capture data.
	/// </summary>
	public float thresholdStartCapture = 0.3f;
	/// <summary>
	/// A control variable that allows you to only use every Nth frame during the capture process.
	/// </summary>
	public int useEveryNthFrame = 2;
	/// <summary>
	/// A control variable that defines the minimum time that should pass by before a new frame is captured.
	/// </summary>
	public int minimumFrameInterspaceMs = 20;

	public Vector3 testingRangeCm3 = new Vector3(20, 20, 20);
	public int probeCountForEachAxis = 20;
	public Vector3 gridOffset = default(Vector3);
	public int bufferFramesCount = 60;
	public bool makeGridVisible = false;
	public bool createDebugObjects = false;
	public bool logReportEachCaptureFrame = false;

	public IEnumerator Calibrate()
	{
		LogMessage("Starting calibration with " + Math.Pow(probeCountForEachAxis, 3) + " probes, a test range of " + testingRangeCm3.ToString("G3") + " and a buffer for " + bufferFramesCount + " capture frames.");
		// In case we need to iterate through all grid points, this function encapsulates the index logic:
		Action<Action<int, int, int>> gridPointIterate = delegate(Action<int, int, int> action) {
			for (int x = 0; x < probeCountForEachAxis; x++)
			{
				for (int y = 0; y < probeCountForEachAxis; y++)
				{
					for (int z = 0; z < probeCountForEachAxis; z++)
					{
						action(x, y, z);
					}
				}
			}
		};
		// Generate a new cube mesh in case we need it for making the grid visible...
		GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		Mesh cubeMesh = cube.GetComponent<MeshFilter>().mesh;
		GameObject.Destroy(cube);
		// Create a 3D grid with the specified number of probes:
		Transform[,,] probeGrid = new Transform[probeCountForEachAxis, probeCountForEachAxis, probeCountForEachAxis];
		gridPointIterate((x, y, z) => {
			// For each position in the 3D grid, create a new game object and adjust its transform.
			Vector3 currentPosition = new Vector3(
				(-(testingRangeCm3.x / 2f) + x * testingRangeCm3.x / probeCountForEachAxis) / 100f,
				(-(testingRangeCm3.y / 2f) + y * testingRangeCm3.y / probeCountForEachAxis) / 100f,
				(-(testingRangeCm3.z / 2f) + z * testingRangeCm3.z / probeCountForEachAxis) / 100f
			) + gridOffset;
			GameObject probe = new GameObject(x + "|" + y + "|" + z);
			if (makeGridVisible)
			{
				probe.AddComponent<MeshRenderer>();
				probe.AddComponent<MeshFilter>().mesh = cubeMesh;
				probe.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
				probe.transform.localEulerAngles = new Vector3(45, 45, 45);
			}
			probe.transform.parent = this.transform.parent;
			probe.transform.localPosition = currentPosition;
			probeGrid[x, y, z] = probe.transform;
		});
		// Create structures that can be used to remember the last positions of each grid point and their total travelled distance within the buffered time:
		Queue<Vector3[,,]> bufferedPositions = new Queue<Vector3[,,]>(bufferFramesCount);
		float[,,] totalDistanceTravelled = new float[probeCountForEachAxis, probeCountForEachAxis, probeCountForEachAxis];
		Queue<int[]> leastTravelledPointIndices = new Queue<int[]>(bufferFramesCount);
		// Start capturing movements updating the structure just created for each frame!
		bool capturing = false;
		int capturedFrames = 0;
		DateTime lastCaptureFrame = DateTime.MinValue;
		while (capturedFrames < bufferFramesCount)
		{
			// Make a list snapshot of the queue, so we can access each frame by index if needed.
			List<Vector3[,,]> bufferedPositionsSnapshot = bufferedPositions.ToList();
			// If the list has reached its limits, drop the first item before adding the next one...
			if (bufferedPositions.Count >= bufferFramesCount)
			{
				Vector3[,,] deletedBufferObject = bufferedPositions.Dequeue();
				// After removing the oldest frame, we also need to subtract the frame's travelled distance from the total travelled distance.
				gridPointIterate((x, y, z) => {
					float deletedTravelledDistance = Vector3.Distance(bufferedPositionsSnapshot[1][x, y, z], deletedBufferObject[x, y, z]);
					totalDistanceTravelled[x, y, z] -= deletedTravelledDistance < thresholdDistanceTravel ? 0f : deletedTravelledDistance;
				});
			}
			// Remember the current position of each grid point and add the new travelled distance to the total distance if it is greater than the specified threshold.
			Vector3[,,] createdBufferObject = new Vector3[probeCountForEachAxis, probeCountForEachAxis, probeCountForEachAxis];
			gridPointIterate((x, y, z) => {
				createdBufferObject[x, y, z] = probeGrid[x, y, z].position;
				float frameTravelledDistance = bufferedPositionsSnapshot.Count < 1 ? 0f : Vector3.Distance(bufferedPositionsSnapshot[bufferedPositionsSnapshot.Count - 1][x, y, z], createdBufferObject[x, y, z]);
				totalDistanceTravelled[x, y, z] += frameTravelledDistance < thresholdDistanceTravel ? 0f : frameTravelledDistance;
			});
			bufferedPositions.Enqueue(createdBufferObject);
			// After the update, find the two grid points that currently travelled the least and the most!
			float minimumTravelledDistance = float.MaxValue;
			float maximumTravelledDistance = 0f;
			int[] lastMinimumComponents = new int[3] { 0, 0, 0 };
			int[] lastMaximumComponents = new int[3] { 0, 0, 0 };
			gridPointIterate((x, y, z) => {
				if (totalDistanceTravelled[x, y, z] < minimumTravelledDistance)
				{
					minimumTravelledDistance = totalDistanceTravelled[x, y, z];
					lastMinimumComponents = new int[] { x, y, z };
				}
				if (totalDistanceTravelled[x, y, z] > maximumTravelledDistance)
				{
					maximumTravelledDistance = totalDistanceTravelled[x, y, z];
					lastMaximumComponents = new int[] { x, y, z };
				}
			});
			if (logReportEachCaptureFrame)
			{
				LogMessage("The least distance at this frame was travelled by " + probeGrid[lastMinimumComponents[0], lastMinimumComponents[1], lastMinimumComponents[2]].gameObject.name + " with " + minimumTravelledDistance.ToString("F3") + " m.", capturing);
				LogMessage("The most distance at this frame was travelled by " + probeGrid[lastMaximumComponents[0], lastMaximumComponents[1], lastMaximumComponents[2]].gameObject.name + " with " + maximumTravelledDistance.ToString("F3") + " m.", capturing);
			}
			// To save time for later calculations, store the indices of the least travelled point for this frame:
			if (leastTravelledPointIndices.Count >= bufferFramesCount)
			{
				leastTravelledPointIndices.Dequeue();
			}
			leastTravelledPointIndices.Enqueue(lastMinimumComponents);
			// Update the logic variables for the capture:
			if (!capturing && maximumTravelledDistance >= thresholdStartCapture)
			{
				// During the last 60 frames, the maximum travelled distance was greater than the threshold. Assume that the user started moving and start the capture!
				capturing = true;
				if (!logReportEachCaptureFrame)
				{
					LogMessage("Starting capture!");
				}
			}
			if (capturing)
			{
				capturedFrames++;
			}
			// We are finished, wait for the next capture frame by skipping non-capture frames and awaiting the minimum capture frame interspace.
			lastCaptureFrame = DateTime.Now;
			for (int skip = 1; skip <= useEveryNthFrame; skip++)
			{
				yield return null;
			}
			while ((DateTime.Now - lastCaptureFrame).TotalMilliseconds < minimumFrameInterspaceMs)
			{
				yield return null;
			}
		}
		// After the calibration, analyze the current state of the buffer and calculate the local center point:
		Func<int[], Vector3> getLocalGridPointPosition = delegate(int[] indices) {
			return probeGrid[indices[0], indices[1], indices[2]].localPosition;
		};
		Vector3 sum = getLocalGridPointPosition(leastTravelledPointIndices.Peek());
		foreach (int[] indices in leastTravelledPointIndices.Skip(1))
		{
			sum += getLocalGridPointPosition(indices);
			if (createDebugObjects)
			{
				GameObject duplicate = GameObject.Instantiate(probeGrid[indices[0], indices[1], indices[2]].gameObject, this.transform.parent);
				duplicate.name = "LeastTravelled";
				duplicate.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
			}
		}
		Vector3 localCenterPointOffset = sum / bufferFramesCount;
		this.transform.localPosition = localCenterPointOffset;
		Debug.Log("Found local center point at " + localCenterPointOffset.ToString("G3") + "!");
		if (createDebugObjects)
		{
			GameObject center = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			center.name = "CalculatedCenter";
			center.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
			center.transform.parent = this.transform.parent;
			center.transform.localPosition = localCenterPointOffset;
		}
		// Lastly, delete all the grid points created for this calibration.
		foreach (Transform probe in probeGrid)
		{
			GameObject.Destroy(probe.gameObject);
		}
	}

	/// <summary>
	/// Logs a message to the console and adds a leading class identifier.
	/// </summary>
	/// <param name="message">The message you want to log without leading class identifier.</param>
	/// <param name="warning">Should the message be logged as warning?</param>
	private void LogMessage(string message, bool warning = false)
	{
		// Add the leading class identifier for this class:
		message = "[" + this.GetType().Name + "] " + message;
		if (warning)
		{
			Debug.LogWarning(message);
		}
		else
		{
			Debug.Log(message);
		}
	}
}