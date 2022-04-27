/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using UnityEngine;
using System;
using System.Collections;

public static class ExperimentToolkit {

	/// <summary>
	/// Use as a coroutine!
	/// It waits for a specific amount of milliseconds.
	/// </summary>
	/// <param name="milliseconds">The amount of milliseconds you want to wait for.</param>
	public static IEnumerator waitForTime(int milliseconds)
	{
		yield return new WaitForSeconds(milliseconds / 1000);
	}

	/// <summary>
	/// Use as a coroutine!
	/// It waits for a specific key and optionally offers to execute an action while waiting.
	/// </summary>
	/// <param name="key">The key to wait for.</param>
	/// <param name="action">An optional action to execute while waiting.</param>
	public static IEnumerator waitForKey(KeyCode key, Action action = null)
	{
		// Waiting for a key is just waiting for a special kind of condition, so use the condition method:
		return ExperimentToolkit.waitForCondition(delegate() { return Input.GetKeyDown(key); }, action, true);
	}

	/// <summary>
	/// Use as a coroutine!
	/// It waits for one key out of several keys being pressed and optionally offers to execute an action while waiting.
	/// For this, only one of the given keys has to be pressed.
	/// </summary>
	/// <param name="action">An action to execute while waiting. Use null if you don't need any action.</param>
	/// <param name="keys">A collection of keys from which the user has to press one.</param>
	public static IEnumerator waitForKeys(Action action, params KeyCode[] keys)
	{
		// Waiting for a key is just waiting for a special kind of condition, so use the condition method:
		return ExperimentToolkit.waitForCondition(delegate() { return Array.Exists(keys, delegate(KeyCode code) { return Input.GetKeyDown(code); }); }, action, true);
	}

	/// <summary>
	/// Use as a coroutine!
	/// It waits for a collection of keys being pressed at the same time and optionally offers to execute an action while waiting.
	/// For this, the user need to press all the given keys together at least for one frame. This is useful if you're waiting for a key combination.
	/// </summary>
	/// <param name="action">An action to execute while waiting. Use null if you don't need any action.</param>
	/// <param name="keys">A collection of keys from which the user has to press all at the same time.</param>
	public static IEnumerator waitForAllKeys(Action action, params KeyCode[] keys)
	{
		// Waiting for keys is just waiting for a special kind of condition, so use the condition method:
		return ExperimentToolkit.waitForCondition(delegate() { return Array.TrueForAll(keys, delegate(KeyCode code) { return Input.GetKey(code); }); }, action, true);
	}

	/// <summary>
	/// Use as a coroutine!
	/// It waits until the given function returns true and optionally offers to execute an action while waiting.
	/// </summary>
	/// <param name="condition">A function returning whether the condition is true.</param>
	/// <param name="action">An optional action to execute while waiting.</param>
	/// <param name="action">An optional parameter for not already starting to test the condition this frame. This is useful to prevent a subsequent condition being triggered by old variables from the previous condition that are only cleaned up in the next frame.</param>
	public static IEnumerator waitForCondition(Func<bool> condition, Action action = null, bool skipCurrentFrame = false)
	{
		if (action == null)
		{
			// If there is no action, create a delegate with no commands.
			action = delegate() { };
		}
		if (skipCurrentFrame)
		{
			// Explicitely perform the action if one is defined, because the first frame will be skipped.
			action();
			// Continue with the main part of this function after waiting for the current frame to finish...
			yield return null;
		}
		while (!condition())
		{
			// The caller of this function wanted to execute an action while waiting for the given condition.
			action();
			// Wait for the next frame to start...
			yield return null;
		}
		// Even if the condition was fulfilled this frame, execute the action one last time.
		action();
	}
}