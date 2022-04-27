/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SMPLVR;

public class ShowVertices : MonoBehaviour {

	public string verticesFile = "Input/MeasurementVertices.txt";
	public float sphereScale = 0.01f;
	public Material sphereMaterial;

	private List<GameObject> spheres;

	void Start()
	{
		Refresh();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			Refresh();
		}
	}

	/// <summary>
	/// Generate the spheres again. Old objects are removed automatically.
	/// During this process, the input file is read every time!
	/// </summary>
	public void Refresh()
	{
		try
		{
			GenerateSpheres(InputFileParser.ParseVertexNumbers(verticesFile));
		}
		catch (System.Exception e)
		{
			throw new UnityException("There was a problem generating spheres for the given vertices:\n" + e.Message);
		}
	}

	/// <summary>
	/// Generates spheres at the positions of the given vertices and destroys old ones if neccessary.
	/// For this, the mesh of a SkinnedMeshRenderer next to this component is used.
	/// </summary>
	/// <param name="verticesWithNames">A dictionary mapping the given vertex names to the corresponding vertex numbers.</param>
	/// <exception cref="IndexOutOfRangeException">At least one vertex number from your input cannot be found in the mesh used.</exception>
	private void GenerateSpheres(Dictionary<string, int> verticesWithNames)
	{
		// First, remove old spheres:
		if (spheres != null)
		{
			foreach (GameObject sphere in spheres)
			{
				Destroy(sphere);
			}
		}
		// Then, generate the new ones according to the given input:
		spheres = new List<GameObject>(verticesWithNames.Count);
		Vector3[] worldVertices = Toolkit.GetWorldSpaceVertices(this.gameObject);
		foreach (KeyValuePair<string, int> vertexWithName in verticesWithNames)
		{
			GameObject newSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			newSphere.transform.position = worldVertices[vertexWithName.Value - 1];
			newSphere.transform.localScale = new Vector3(sphereScale, sphereScale, sphereScale);
			newSphere.GetComponent<MeshRenderer>().material = sphereMaterial;
			// TODO: Maybe create a small text label with the vertex name?
			spheres.Add(newSphere);
		}
	}
}