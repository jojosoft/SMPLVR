/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace SMPLVR
{
	/// <summary>
	/// A collection of useful computing and conversion functions for the SMPLVR project.
	/// </summary>
	public static class Toolkit
	{
		/// <summary>
		/// Gets the world space vertices the first MeshRenderer or SkinnedMeshRenderer attached to the given game object or its children.
		/// With this signature, the function is independent from the type of renderer used.
		/// Please make sure the input game object has only one renderer component in its hierarchy!
		/// </summary>
		/// <returns>An array of Vector3 points in 3D space containing the converted world space vertices.</returns>
		/// <param name="input">The game object you want to extract the vertices from. (Its children will also be considered!)</param>
		public static Vector3[] GetWorldSpaceVertices(GameObject input)
		{
			return GetMeshFromObject(input).vertices.ToList().ConvertAll<Vector3>(vertex => input.GetComponentInChildren<Renderer>().transform.TransformPoint(vertex)).ToArray();
		}

		/// <summary>
		/// Gets the mesh from the first MeshRenderer or SkinnedMeshRenderer attached to the given game object or its children.
		/// With this signature, the function is independent from the type of renderer used!
		/// </summary>
		/// <returns>The mesh from the first suitable component attached to the given object or its children.</returns>
		/// <param name="input">The game object you want the mesh from. (Its children will also be considered!)</param>
		public static Mesh GetMeshFromObject(GameObject input)
		{
			if (input.GetComponentsInChildren<MeshRenderer>(true).Length < 1 && input.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length < 1)
			{
				throw new UnityException("Couldn't calculate world-space vertices for game object \"" + input.name + "\", as there are no 3D renderers attached to it.");
			}
			Mesh output;
			if (input.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0)
			{
				output = new Mesh();
				input.GetComponentInChildren<SkinnedMeshRenderer>(true).BakeMesh(output);
			}
			else
			{
				output = input.GetComponentInChildren<MeshFilter>(true).mesh;
			}
			return output;
		}
	}
}