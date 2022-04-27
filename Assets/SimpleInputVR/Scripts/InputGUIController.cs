/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;

using SimpleInputVR;

/// <summary>
/// This class controls the input of relevant values at the beginning of an experiment.
/// Since a normal GUI isn't implementable in VR, this module uses 3D text.
/// It also checks for you if the values are of the specified type!
/// All data will be written into the specified input file.
/// </summary>
public class InputGUIController : MonoBehaviour {
	
	public Text inputNameField;
	public Text inputValueField;
	public InputSpecification[] inputsToAskFor;
	public string inputFilePath;
	public string experimentSceneName;
	public string separator = ",";
	
	private int currentInputIndex = 0;
	private string currentInputValue = "";
	private string fileContents = "";
	private bool triedSceneLoad = false;
	private float timeSceneLoaded;
	
	void Start()
	{
		// Load in the first input value description if one exists:
		inputNameField.text = inputsToAskFor.Length > 0 ? inputsToAskFor[0].inputDescription + ":" : "";
	}
	
	void Update()
	{
		string newInput = Input.inputString.Trim('\r', '\n', '\b');
		if (currentInputIndex < inputsToAskFor.Length)
		{
			// There are some expected input values remaining, ask for the next one:
			if (Input.GetKeyDown(KeyCode.Return) && currentInputValue.Length > 0)
			{
				if (inputsToAskFor[currentInputIndex].type == InputType.FloatingPointNumber)
				{
					// The parser of the float type just throws out commas! Prevent from getting wrong values.
					currentInputValue = currentInputValue.Replace(",", ".");
				}
				if (inputsToAskFor[currentInputIndex].isValidValue(currentInputValue))
				{
					// The user has finished typing the current value in correctly.
					RegisterNewValue(inputsToAskFor[currentInputIndex].inputDescription, currentInputValue);
					currentInputValue = "";
					currentInputIndex++;
					if (currentInputIndex < inputsToAskFor.Length)
					{
						// There is another input value.
						inputNameField.text = inputsToAskFor[currentInputIndex].inputDescription + ":";
					}
				}
				else
				{
					// The current value isn't correct! Don't accept it.
					currentInputValue = "";
				}
				UpdateInputDisplay();
			}
			else if (Input.GetKeyDown(KeyCode.Backspace) && currentInputValue.Length > 0)
			{
				// Delete the last character if there is one:
				currentInputValue = currentInputValue.Substring(0, currentInputValue.Length - 1);
				UpdateInputDisplay();
			}
			else if (newInput.Length > 0)
			{
				// Append the new character to the value string:
				currentInputValue += newInput;
				UpdateInputDisplay();
			}
		}
		else if (!triedSceneLoad)
		{
			// All input values have been obtained, write the input file and start the experiment scene!
			if (File.Exists(inputFilePath))
			{
				// The input file already exists! Rename the old one by using the lowest number.
				int fileNumber = 1;
				while (File.Exists(inputFilePath.Insert(inputFilePath.LastIndexOf("."), fileNumber.ToString())))
				{
					fileNumber++;
				}
				File.Move(inputFilePath, inputFilePath.Insert(inputFilePath.LastIndexOf("."), fileNumber.ToString()));
			}
			// Write the new input file:
			string directoryPath = Path.GetDirectoryName(inputFilePath);
			if (directoryPath != "" && !Directory.Exists(directoryPath))
			{
				Directory.CreateDirectory(directoryPath);
			}
			File.AppendAllText(inputFilePath, fileContents);
			// First, show message in case the scene loading fails.
			triedSceneLoad = true;
			inputNameField.gameObject.SetActive(false);
			inputNameField.text = "There's no scene called \"" + experimentSceneName + "\"!";
			inputValueField.gameObject.SetActive(false);
			inputValueField.text = "Please provide a valid scene name!";
			timeSceneLoaded = Time.realtimeSinceStartup;
			// Try to load the scene. If it fails, the message will stay visible.
			UnityEngine.SceneManagement.SceneManager.LoadScene(experimentSceneName);
		}
		else
		{
			if (Time.realtimeSinceStartup - timeSceneLoaded > 2)
			{
				// Two seconds passed by and the experiment probably didn't load...
				inputNameField.gameObject.SetActive(true);
				inputValueField.gameObject.SetActive(true);
			}
		}
		if (Input.GetKeyDown(KeyCode.Delete) && Input.GetKey(KeyCode.LeftControl))
		{
			// A backdoor for testing the application when it was already built...
			UnityEngine.SceneManagement.SceneManager.LoadScene(experimentSceneName);
		}
	}
	
	/// <summary>
	/// Appends a new value to the file string.
	/// </summary>
	/// <param name="description">The description of this input value.</param>
	/// <param name="value">The new value, converted to a string.</param>
	private void RegisterNewValue(string description, string value)
	{
		fileContents += description + separator + value + "\r\n";
	}
	
	/// <summary>
	/// Shows the current input value to the user.
	/// </summary>
	private void UpdateInputDisplay()
	{
		// Show the current text in 3D space.
		inputValueField.text = currentInputValue;
	}
}