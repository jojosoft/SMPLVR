/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This module reassigns the device index for five tracked objects.
/// For this, the device position is used and the user is considered to be in T-pose.
/// </summary>
public class TPoseCalibrator : MonoBehaviour {

	public int retryDelayMs = 1000;
	public SteamVR_TrackedObject handRight;
	public SteamVR_TrackedObject handLeft;
	public SteamVR_TrackedObject pelvis;
	public SteamVR_TrackedObject footRight;
	public SteamVR_TrackedObject footLeft;

	private List<SteamVR_Controller.Device> devices;

	public IEnumerator Prepare(Action<string> trackerStatus = null)
	{
		LogMessage("Preparing calibration.");
		// Try to analyze the current position of the trackers and reassign their indices accordingly!
		do
		{
			// Only continue directly if this is the first try.
			if (devices != null)
			{
				// Before trying it the next time, wait for the specified time to pass by:
				yield return new WaitForSeconds(retryDelayMs / 1000f);
			}
			// Get all tracked objects that are currently available:
			devices = GetGenericTrackersAvailable();
			LogMessage("Devices available: " + ListDevices(devices) + ".");
			// If an Action was specified, call it with the current tracker status:
			if (trackerStatus != null)
			{
				trackerStatus(ListDevices(devices, "\n"));
			}
		}
		while (devices.Count < 5);
	}

	public void Calibrate()
	{
		if (devices != null && devices.Count >= 5)
		{
			// There are at least five trackers, so sort them top to bottom.
			LogMessage("Capturing pose!");
			devices.Sort((dev1, dev2) => -dev1.transform.pos.y.CompareTo(dev2.transform.pos.y));
			// Assign the new order to all tracked objects and swap left and right trackers for hand and feet if needed:
			bool swapHandLR = devices[0].transform.pos.x < devices[1].transform.pos.x;
			bool swapFeetLR = devices[3].transform.pos.x < devices[4].transform.pos.x;
			handRight.SetDeviceIndex((int)devices[swapHandLR ? 1 : 0].index);
			handLeft.SetDeviceIndex((int)devices[swapHandLR ? 0 : 1].index);
			pelvis.SetDeviceIndex((int)devices[2].index);
			footRight.SetDeviceIndex((int)devices[swapFeetLR ? 4 : 3].index);
			footLeft.SetDeviceIndex((int)devices[swapFeetLR ? 3 : 4].index);
			LogMessage("Successfully assigned trackes.");
		}
		else
		{
			LogMessage("The TPoseCalibrator needs at least five active trackers!", true);
		}
	}

	/// <summary>
	/// Gets all generic tracker devices available.
	/// Available means: The device is connected, is valid and has tracking.
	/// </summary>
	/// <returns>All generic trackers currently available.</returns>
	private List<SteamVR_Controller.Device> GetGenericTrackersAvailable()
	{
		return DeviceIndices.Where(index => {
			SteamVR_Controller.Device device = SteamVR_Controller.Input(index);
			return device.connected && device.valid && device.hasTracking && Valve.VR.OpenVR.System.GetTrackedDeviceClass((uint)index) == Valve.VR.ETrackedDeviceClass.GenericTracker;
		}).ToList().ConvertAll(index => SteamVR_Controller.Input(index));
	}

	/// <summary>
	/// Creates a string with the device classes for the given devices.
	/// They can be seperated differently by using the seperator parameter.
	/// </summary>
	/// <returns>The string with the device classes.</returns>
	/// <param name="devices">An array of devices that you want to convert to a string.</param>
	/// <param name="separator">The seperator that should be used to seperate the different device classes.</param>
	private string ListDevices(List<SteamVR_Controller.Device> devices, string separator = ", ")
	{
		return devices.Count == 0 ? "None" : devices.Aggregate<SteamVR_Controller.Device, string>("", (str, dev) => str + (str.Length == 0 ? "" : separator) + Valve.VR.OpenVR.System.GetTrackedDeviceClass((uint)dev.index) + " [" + dev.index + "]");
	}

	private int[] DeviceIndices
	{
		get
		{
			return new int[] {
				(int)SteamVR_TrackedObject.EIndex.Device1,
				(int)SteamVR_TrackedObject.EIndex.Device2,
				(int)SteamVR_TrackedObject.EIndex.Device3,
				(int)SteamVR_TrackedObject.EIndex.Device4,
				(int)SteamVR_TrackedObject.EIndex.Device5,
				(int)SteamVR_TrackedObject.EIndex.Device6,
				(int)SteamVR_TrackedObject.EIndex.Device7,
				(int)SteamVR_TrackedObject.EIndex.Device8,
				(int)SteamVR_TrackedObject.EIndex.Device9,
				(int)SteamVR_TrackedObject.EIndex.Device10,
				(int)SteamVR_TrackedObject.EIndex.Device11,
				(int)SteamVR_TrackedObject.EIndex.Device12,
				(int)SteamVR_TrackedObject.EIndex.Device13,
				(int)SteamVR_TrackedObject.EIndex.Device14,
				(int)SteamVR_TrackedObject.EIndex.Device15
			};
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