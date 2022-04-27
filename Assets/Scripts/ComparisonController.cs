/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SMPLVR;

public class ComparisonController : MonoBehaviour {

	public GameObject maleSMPL;
	public GameObject femaleSMPL;
	public ComparisonRender frontCamera;
	public ComparisonRender leftCamera;
	public ComparisonRender backCamera;
	public GameObject labelTemplate;
	public int standardDeviationsFBX = 5;
	[Tooltip("The specified vertex will be used to determine the facing direction of the created mesh. Choose a vertex that is at the front of the body and lies on its vertical middle line - for example the nose.")]
	public int frontProbeVertexNumber = 332;
	public Material defaultMaterial;
	public bool applyDefaultMaterial = true;
	public string participantsPath = "Input/Participants/";
	public string verticesFile = "Input/MeasurementVertices.txt";
	public string distancesFile = "Input/Distances.txt";
	public bool onlyTextualAnalysis = false;
	public bool waitForKey = false;
	public float horizontalSpacing = 0.2f;
	public float verticalSpacing = 0.2f;
	public float depthSpacing = 1f;
	[Tooltip("The resolution in pixels for the shorter dimension of the image.")]
	public int normalRenderResolution = 2000;
	public bool showGrid = false;
	public bool keepBodiesInScene = false;

	IEnumerator Start()
	{
		// Get all participants that need to be processed.
		string[] participantPaths = Directory.GetFiles(participantsPath);
		if (participantsPath.Length < 1)
		{
			LogMessage("No participants were found for processing! Search directory: " + participantsPath, true);
			yield break;
		}
		// Create the output folder if it not already exists:
		Directory.CreateDirectory("Output");
		// Read in the measurement vertex definitions and distances to measure:
		Dictionary<string, int> measurementVertices = InputFileParser.ParseVertexNumbers(verticesFile);
		List<string[]> distancesToMeasure = InputFileParser.ParseDistanceMeasurements(distancesFile);
		// Inform the user that the comparison is going to start now:
		LogMessage("Successfully loaded input files, starting comparison now.");
		// For each AssetBundle in the directory, try to process one participant:
		foreach (string participantPath in participantPaths)
		{
			// If requested, first wait for the user before doing anything.
			yield return WaitForUser();
			// Create an empty game object that we can use for keeping all body variations at the same place:
			GameObject participantRoot = new GameObject(Path.GetFileNameWithoutExtension(participantPath));
			List<GameObject> participantVariations = new List<GameObject>();
			// Prepare the participant's gender and number for later use:
			string pGender = participantRoot.name.Substring(0, 1).ToUpper();
			string pNumber = participantRoot.name.Substring(1);
			// Load the participant's asset bundle and create all of the objects:
			AssetBundle participantBundle = AssetBundle.LoadFromFile(participantPath);
			participantBundle.GetAllAssetNames().ToList().ForEach(assetPath => {
				GameObject body = LoadAssetIntoScene(participantBundle, assetPath);
				if (body != null)
				{
					body.transform.parent = participantRoot.transform;
				}
				body.SetActive(true);
				participantVariations.Add(body);
			});
			// Add a label to each of the game objects:
			participantVariations.ForEach(variation => {
				GameObject variationLabel = GameObject.Instantiate(labelTemplate);
				variationLabel.name = "LabelRoot";
				variationLabel.GetComponentsInChildren<Text>(true).ToList().ForEach(textField => textField.text = variation.name + "\n" + pGender + pNumber);
				variationLabel.transform.SetParent(variation.transform, false);
				Vector3 boundingBoxExtents = variation.GetComponentInChildren<Renderer>(true).bounds.extents;
				variationLabel.transform.localPosition = new Vector3(boundingBoxExtents.x, 0, boundingBoxExtents.z);
				variationLabel.SetActive(true);
			});
			// After all object have been loaded, create a new comparer for every combination of them and run it:
			List<SMPLComparer> comparers = new List<SMPLComparer>();
			participantVariations.ForEach(variation1 => participantVariations.ForEach(variation2 => {
				SMPLComparer comparison = new SMPLComparer(variation1, variation2);
				comparison.Compare(measurementVertices, distancesToMeasure);
				comparers.Add(comparison);
			}));
			// Save the TXT [0] and CSV [1] versions all of the comparisons:
			int longestMeshNameCharCount = participantVariations.Max(variation => variation.name.Length);
			File.WriteAllText("Output/" + participantRoot.name + ".txt",
				"Comparison for " + (pGender.Equals("M") ? "male" : "female") + " participant " + pNumber + ":\r\n\r\n\r\n" +
				string.Join("\r\n\r\n", comparers.ConvertAll(comparison => comparison.GetTXTComparison(longestMeshNameCharCount)).ToArray())
			);
			File.WriteAllText("Output/" + participantRoot.name + ".csv",
				"Gender;Number;Mesh1;Mesh2;MeasureA;MeasureB;MeasureName;Distance1;Distance2;Discrepancy\r\n" +
				string.Join("\r\n", comparers.ConvertAll(comparison => comparison.GetCSVComparison(pGender, pNumber)).ToArray())
			);
			// Now that the text files have been written, generate all renderings!
			LogMessage("Generated textual analysis for participant " + pNumber + ".");
			// Skip a frame to update the renderer bounding boxes and show the current participant to the user.
			yield return null;
			// Only render out images if it was requested!
			if (!onlyTextualAnalysis)
			{
				// Arrange the meshes both vertically and horizontally and render the according images.
				Bounds horizontalBounds = ArrangeMeshObjects(participantVariations, true, horizontalSpacing);
				frontCamera.RenderCurrentFrame("Output/" + participantRoot.name + "-Horizontal-Front.png", horizontalBounds.size.x, horizontalBounds.size.y, horizontalBounds.size.z, normalRenderResolution / 2, showGrid, horizontalBounds.size.y / 2f);
				yield return WaitForUser();
				leftCamera.RenderCurrentFrame("Output/" + participantRoot.name + "-Horizontal-Left.png", horizontalBounds.size.z + depthSpacing, horizontalBounds.size.y, horizontalBounds.size.x, normalRenderResolution, showGrid, horizontalBounds.size.y / 2f);
				yield return WaitForUser();
				backCamera.RenderCurrentFrame("Output/" + participantRoot.name + "-Horizontal-Back.png", horizontalBounds.size.x, horizontalBounds.size.y, horizontalBounds.size.z, normalRenderResolution / 2, showGrid, horizontalBounds.size.y / 2f);
				yield return WaitForUser();
				Bounds verticalBounds = ArrangeMeshObjects(participantVariations, false, verticalSpacing);
				frontCamera.RenderCurrentFrame("Output/" + participantRoot.name + "-Vertical-Front.png", verticalBounds.size.x, verticalBounds.size.y, verticalBounds.size.z, normalRenderResolution / 2, showGrid);
				yield return WaitForUser();
				leftCamera.RenderCurrentFrame("Output/" + participantRoot.name + "-Vertical-Left.png", verticalBounds.size.z + depthSpacing, verticalBounds.size.y, verticalBounds.size.x, normalRenderResolution / 4, showGrid);
				yield return WaitForUser();
				backCamera.RenderCurrentFrame("Output/" + participantRoot.name + "-Vertical-Back.png", verticalBounds.size.x, verticalBounds.size.y, verticalBounds.size.z, normalRenderResolution / 2, showGrid);
			}
			// Lastly, destroy the participant's variants or set them invisible if they should be kept in the scene.
			if (keepBodiesInScene)
			{
				participantRoot.SetActive(false);
			}
			else
			{
				Destroy(participantRoot);
			}
		}
		// Notify the user that the the comparison is now finished!
		LogMessage("The comparison was successfully finished!");
	}

	private IEnumerator WaitForUser()
	{
		if (waitForKey)
		{
			yield return StartCoroutine(ExperimentToolkit.waitForKey(KeyCode.Space));
		}
	}

	/// <summary>
	/// Loads an asset into the scene while treating different types of assets accordingly.
	/// 1. GameObject:	It is assumed that a game object has a MeshRenderer attached to it.
	/// 2. TextAsset:	It is assumed that a text asset contains beta values that should be applied to the FBX file of the correct gender.
	/// All returned objects will have already been correctly centered in world space. For this, the nearest effective transform to the renderer will be used.
	/// Furthermore, the object will in every case be activated and the renderer will be enabled.
	/// </summary>
	/// <returns>The resulting GameObject, ready for comparison.</returns>
	/// <param name="assetBundle">The AssetBundle you want to load the asset from.</param>
	/// <param name="assetPath">The path to the asset in the AssetBundle.</param>
	private GameObject LoadAssetIntoScene(AssetBundle assetBundle, string assetPath)
	{
		Object asset = assetBundle.LoadAsset(assetPath);
		// Discard the asset path and a possible prefix to get the asset's raw name:
		string objectName = Path.GetDirectoryName(assetPath).Split('/').Last().Split('-').Last();
		// To make it look nicer, make the first letter of the name upper case:
		char[] name = objectName.ToCharArray();
		name[0] = char.ToUpper(name[0]);
		objectName = new string(name);
		// Now, distinguish different types of assets and load them accordingly!
		if (asset is GameObject)
		{
			// The asset is already a GameObject, check whether a renderer is attatched!
			GameObject instantiatedAsset = Instantiate(asset as GameObject);
			if (instantiatedAsset.GetComponentsInChildren<MeshRenderer>(true).Length > 0)
			{
				instantiatedAsset.SetActive(true);
				instantiatedAsset.name = objectName;
				MeshRenderer rendererModel = instantiatedAsset.GetComponentInChildren<MeshRenderer>();
				rendererModel.enabled = true;
				// Set the renderer's material to the default material if requested:
				if (applyDefaultMaterial)
				{
					rendererModel.material = defaultMaterial;
				}
				// Reset the model's position, so calculating the bounding box offset makes sense:
				instantiatedAsset.transform.position = new Vector3(0, 0, 0);
				rendererModel.gameObject.transform.position  = new Vector3(0, 0, 0);
				// Center the model on the x-z-plane and align its feet to the ground:
				Vector3 currentModelPosition = rendererModel.gameObject.transform.position;
				currentModelPosition.y = -rendererModel.bounds.min.y;
				currentModelPosition.x = -rendererModel.bounds.center.x;
				currentModelPosition.z = -rendererModel.bounds.center.z;
				rendererModel.gameObject.transform.position = currentModelPosition;
				// If needed, rotate the body so its front probe vertex is facing towards the positive z-axis!
				Vector3 currentProbeVertexPosition = Toolkit.GetWorldSpaceVertices(rendererModel.gameObject)[frontProbeVertexNumber - 1];
				if (Mathf.Abs(currentProbeVertexPosition.x) > Mathf.Abs(currentProbeVertexPosition.z) && currentProbeVertexPosition.x > 0)
				{
					rendererModel.gameObject.transform.RotateAround(default(Vector3), new Vector3(0, 1, 0), 270);
				}
				else if (Mathf.Abs(currentProbeVertexPosition.z) > Mathf.Abs(currentProbeVertexPosition.x) && currentProbeVertexPosition.z < 0)
				{
					rendererModel.gameObject.transform.RotateAround(default(Vector3), new Vector3(0, 1, 0), 180);
				}
				else if (Mathf.Abs(currentProbeVertexPosition.x) > Mathf.Abs(currentProbeVertexPosition.z) && currentProbeVertexPosition.x < 0)
				{
					rendererModel.gameObject.transform.RotateAround(default(Vector3), new Vector3(0, 1, 0), 90);
				}
				return instantiatedAsset;
			}
			else
			{
				LogMessage("Model " + objectName + " for participant " + instantiatedAsset.name + " didn't have a MeshRenderer attatched!", true);
				return null;
			}
		}
		else if (asset is TextAsset)
		{
			// The asset just contains beta values, clone the corresponding SMPL model of the correct gender and apply the values to it.
			TextAsset castedAsset = asset as TextAsset;
			List<float> betas = castedAsset.text.Split(new string[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries).ToList().ConvertAll<float>(input => float.Parse(input));
			GameObject smplInstance = GameObject.Instantiate(asset.name.ToLower().StartsWith("f") ? femaleSMPL : maleSMPL);
			smplInstance.SetActive(true);
			smplInstance.name = objectName;
			SkinnedMeshRenderer rendererSMPL = smplInstance.GetComponentInChildren<SkinnedMeshRenderer>(true);
			rendererSMPL.enabled = true;
			for (int i = 0; i < betas.Count; i++)
			{
				rendererSMPL.SetBlendShapeWeight(i * 2 + (betas[i] < 0 ? 1 : 0), Mathf.Abs(betas[i]) * 100 / standardDeviationsFBX);
			}
			// Set the renderer's material to the default material if requested:
			if (applyDefaultMaterial)
			{
				rendererSMPL.material = defaultMaterial;
			}
			// Reset the model's position, so calculating the bounding box offset makes sense:
			rendererSMPL.transform.parent.position = new Vector3(0, 0, 0);
			// Center the FBX model on the x-z-plane and align its feet to the ground:
			Mesh bakeSMPL = new Mesh();
			rendererSMPL.BakeMesh(bakeSMPL);
			bakeSMPL.RecalculateBounds();
			Vector3 currentModelPosition = rendererSMPL.transform.parent.position;
			currentModelPosition.y = -bakeSMPL.bounds.min.y;
			currentModelPosition.x = -bakeSMPL.bounds.center.x;
			currentModelPosition.z = -bakeSMPL.bounds.center.z;
			rendererSMPL.transform.parent.position = currentModelPosition;
			return smplInstance;
		}
		LogMessage("Model " + objectName + " with file name " + Path.GetFileNameWithoutExtension(assetPath) + " wasn't recognized as a body model asset!", true);
		return null;
	}

	/// <summary>
	/// Arranges the mesh objects in the given direction.
	/// Horizontal: Diagonal distribution on the x-z-plane.
	/// Vertical: Diagonal distribution on the y-z-plane.
	/// This function assumes for some measurements that all objects have already been centered with their bottom on (0, 0, 0).
	/// </summary>
	/// <returns>The total space used by the objects after the arrangement.</returns>
	/// <param name="objects">A list of all objects to consider.</param>
	/// <param name="horizontally">If set to <c>true</c>, the objects are arranged horizontally, otherwise vertically.</param>
	/// <param name="spacing">The minimum space in meters to keep between the objects.</param>
	/// <param name="spacingDepth">The minimum space in meters to keep between the objects when arranging them in depth. Only needed for horizontal arrangement.</param>
	/// <param name="centerPoint">The center point to arrange the objects around.</param>
	private Bounds ArrangeMeshObjects(List<GameObject> objects, bool horizontally, float spacing = 0.2f, float spacingDepth = 1f, Vector3 centerPoint = default(Vector3))
	{
		// Collect the bounds from all of the objects, since this is this function's basis.
		List<Bounds> objectBounds = objects.ConvertAll(meshObject => meshObject.GetComponentInChildren<Renderer>(true).bounds);
		float spacingSum = (objects.Count - 1) * spacing;
		if (horizontally)
		{
			float widthSum = objectBounds.Sum(bound => bound.size.x);
			float depthSum = objectBounds.Sum(bound => bound.size.z);
			float spacingDepthSum = (objects.Count - 1) * spacingDepth;
			for (int i = 0; i < objects.Count; i++)
			{
				objects[i].transform.localPosition = centerPoint
					+ new Vector3((widthSum + spacingSum) / 2f, 0, (depthSum + spacingDepthSum) / 2f)
					- new Vector3(
						objectBounds.Take(i).Sum(bound => bound.size.x) + i * spacing + objectBounds[i].size.x / 2f,
						0,
						objectBounds.Take(i).Sum(bound => bound.size.z) + i * spacingDepth + objectBounds[i].size.z / 2f
					);
			}
			float maxHeight = objectBounds.Max(bound => bound.size.y);
			return new Bounds(centerPoint + new Vector3(0, maxHeight / 2f, 0), new Vector3(spacingSum + widthSum, maxHeight, spacingDepthSum + depthSum));
		}
		else
		{
			float heightSum = objectBounds.Sum(bound => bound.size.y);
			for (int i = 0; i < objects.Count; i++)
			{
				objects[i].transform.localPosition = centerPoint
					+ new Vector3(0, (heightSum + spacingSum) / 2f, 0)
					- new Vector3(
						0,
						objectBounds.Take(i).Sum(bound => bound.size.y) + i * spacing + objectBounds[i].size.y / 2f + (objectBounds[i].center.y - objects[i].transform.position.y),
						0
					);
			}
			return new Bounds(centerPoint, new Vector3(objectBounds.Max(bound => bound.size.x), spacingSum + heightSum, objectBounds.Max(bound => bound.size.z)));
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