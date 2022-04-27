/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using ViveScale;

public class ViveScaleController : MonoBehaviour {

	public SteamVR_ControllerManager controllerManager;
	[Tooltip("The minimum controller distance that should be considered a scaling of 0 %.")]
	public float controlOffset = 0.15f;
	[Tooltip("The totally available space for scaling. The distance for 100 % is therefore control offset plus control range.")]
	public float controlRange = 0.45f;
	public bool logScaleInDebugMode = false;

	private List<Scalable> clientsToScale = new List<Scalable>();
	private bool firstTriggerCurrentlyDown = false;
	private bool secondTriggerCurrentlyDown = false;
	private float oldControllerDistance;

	void Update()
	{
		if (controllerManager == null || FirstController == null || SecondController == null)
		{
			LogMessage("At least one of the controllers or the controller manager itself is not available, the module is now deactivating itself.", true);
			this.gameObject.SetActive(false);
			return;
		}
		// Analyze which buttons have been pressed or released and adjust the states accordingly:
		UpdateTriggerStates();
		// Calculate the new scaling value represented by the current controller distance:
		if (firstTriggerCurrentlyDown && secondTriggerCurrentlyDown)
		{
			// Both of the triggers are pressed! Scale the value.
			if (!ScaleCylinder.activeSelf)
			{
				clientsToScale.ForEach(client => client.StartScaling());
				// A new scaling gesture was started. Note down the current distance between the controllers!
				oldControllerDistance = ControllerDistance;
				// Set the scaling mode active and show the scaler object:
				ScaleCylinder.SetActive(true);
			}
			// Bring the scaler object in position:
			this.transform.position = FirstControllerPosition;
			this.transform.LookAt(SecondControllerPosition);
			float invertedScale = oldControllerDistance / ControllerDistance;
			this.transform.localScale = new Vector3(invertedScale, invertedScale, ControllerDistance);
			// Notify all subscribers that there is an updated scaling value available:
			float newWeight = (ControllerDistance - controlOffset) / (controlRange + controlOffset) * 100;
			float scaleLimitedToPositive = newWeight < 0 ? 0 : newWeight;
			clientsToScale.ForEach(client => client.ReceiveNewScale(scaleLimitedToPositive));
			// For debugging, log the new scale value.
			if (Debug.isDebugBuild && logScaleInDebugMode)
			{
				LogMessage("Applied new scale of " + scaleLimitedToPositive.ToString("F2") + " %.");
			}
		}
		else
		{
			if (ScaleCylinder.activeSelf)
			{
				ScaleCylinder.SetActive(false);
			}
		}
	}

	/// <summary>
	/// If your object implements the Scalable interface you can subscribe to this instance.
	/// You will then be able to get notified whenever something about the scaling status changes.
	/// </summary>
	/// <param name="scalableObject">The scalable instance that should from now on be notified about scaling state changes.</param>
	public void Subscribe(Scalable scalableObject)
	{
		clientsToScale.Add(scalableObject);
	}

	/// <summary>
	/// Unsubscribe your object from this VRScaler's instance.
	/// You won't get notified anymore about scaling status changes!
	/// </summary>
	/// <param name="scalableObject">The scalable instance that should not be notified about scaling state changes any more.</param>
	public void Unsubscribe(Scalable scalableObject)
	{
		clientsToScale.Remove(scalableObject);
	}

	/// <summary>
	/// This helper function updates the representation of the trigger states for this class.
	/// A function that should be called every frame to not miss any PressDown or PressUp events...
	/// </summary>
	private void UpdateTriggerStates()
	{
		bool firstTriggerDown = FirstController.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
		bool secondTriggerDown = SecondController.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
		bool firstTriggerUp = FirstController.GetPressUp(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
		bool secondTriggerUp = SecondController.GetPressUp(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);

		if (firstTriggerCurrentlyDown && firstTriggerUp || !firstTriggerCurrentlyDown && firstTriggerDown)
		{
			firstTriggerCurrentlyDown = !firstTriggerCurrentlyDown;
		}
		if (secondTriggerCurrentlyDown && secondTriggerUp || !secondTriggerCurrentlyDown && secondTriggerDown)
		{
			secondTriggerCurrentlyDown = !secondTriggerCurrentlyDown;
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

	private SteamVR_Controller.Device FirstController
	{
		get
		{
			int index = (int)controllerManager.right.GetComponent<SteamVR_TrackedObject>().index;
			try
			{
				return SteamVR_Controller.Input(index);
			}
			catch
			{
				return null;
			}
		}
	}

	private SteamVR_Controller.Device SecondController
	{
		get
		{
			int index = (int)controllerManager.left.GetComponent<SteamVR_TrackedObject>().index;
			try
			{
				return SteamVR_Controller.Input(index);
			}
			catch
			{
				return null;
			}
		}
	}

	private float ControllerDistance
	{
		get
		{
			return (FirstControllerPosition - SecondControllerPosition).magnitude;
		}
	}

	private Vector3 FirstControllerPosition
	{
		get
		{
			return controllerManager.right.transform.position;
		}
	}

	private Vector3 SecondControllerPosition
	{
		get
		{
			return controllerManager.left.transform.position;
		}
	}

	private GameObject ScaleCylinder
	{
		get
		{
			return this.transform.GetChild(0).gameObject;
		}
	}
}