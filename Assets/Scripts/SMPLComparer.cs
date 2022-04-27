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
	/// Compares two meshes with the SMPL topology in world space.
	/// In addition to the bounding boxes, it calculates the given vertex-to-vertex distances.
	/// </summary>
	public class SMPLComparer
	{
		/// <summary>
		/// A handy struct that contains a comparison result.
		/// Warning: This is mutable! Always create a new one and don't change properties directly.
		/// </summary>
		public struct MeasurementResult
		{
			public string measurePointNameA;
			public string measurePointNameB;
			public string measurementName;
			public float distance1;
			public float distance2;
			public float discrepancy;
		}

		private Vector3[] smplVertices1;
		private Vector3[] smplVertices2;
		private string nameMesh1;
		private string nameMesh2;
		private MeasurementResult[] currentResult;

		/// <summary>
		/// Initializes a new instance of the <see cref="SMPLVR.SMPLComparer"/> class.
		/// The comparer will convert all vertices into world space, which gives you control over how the bounding box is calculated.
		/// </summary>
		/// <param name="object1">The first game object you want to compare.</param>
		/// <param name="object2">The second game object you want to compare.</param>
		/// <param name="nameMesh1">The name of the first mesh. By default, the game object's name is used.</param>
		/// <param name="nameMesh2">The name of the second mesh. By default, the game object's name is used.</param>
		public SMPLComparer(GameObject object1, GameObject object2, string nameMesh1 = null, string nameMesh2 = null)
		{
			this.smplVertices1 = Toolkit.GetWorldSpaceVertices(object1);
			this.smplVertices2 = Toolkit.GetWorldSpaceVertices(object2);
			this.nameMesh1 = nameMesh1 ?? object1.name;
			this.nameMesh2 = nameMesh2 ?? object2.name;
		}

		/// <summary>
		/// Compares the two meshes referenced in the constructor in consideration of the given measurement vertices and distances to measure.
		/// Each measurement point name used in distancesToMeasure must have been defined in measurementVertices!
		/// The discrepancy is an absolute value and therefore does not describe a direction.
		/// In addition to the given measurements, this function will also always calculate the bounding box size, even without parameters.
		/// </summary>
		/// <param name="measurementVertices">A dictionary mapping measurement point names onto vertex numbers.</param>
		/// <param name="distancesToMeasure">A list of pairs of measurement point names.</param>
		public MeasurementResult[] Compare(Dictionary<string, int> measurementVertices = default(Dictionary<string, int>), List<string[]> distancesToMeasure = default(List<string[]>))
		{
			// Write the new vertex-to-vertex comparison to the current result object:
			currentResult = new MeasurementResult[distancesToMeasure.Count + 3];
			for (int i = 0; i < distancesToMeasure.Count; i++)
			{
				float distance1 = Vector3.Distance(
					smplVertices1[measurementVertices[distancesToMeasure[i][0]] - 1],
					smplVertices1[measurementVertices[distancesToMeasure[i][1]] - 1]
				);
				float distance2 = Vector3.Distance(
					smplVertices2[measurementVertices[distancesToMeasure[i][0]] - 1],
					smplVertices2[measurementVertices[distancesToMeasure[i][1]] - 1]
				);
				currentResult[i] = new MeasurementResult() {
					measurePointNameA = distancesToMeasure[i][0],
					measurePointNameB = distancesToMeasure[i][1],
					measurementName = distancesToMeasure[i][2],
					distance1 = distance1,
					distance2 = distance2,
					discrepancy = Mathf.Abs(distance1 - distance2)
				};
			}
			// Calculate the bounding box size separately:
			float boundingBoxSizeX1 = smplVertices1.Max(vertex => vertex.x) - smplVertices1.Min(vertex => vertex.x);
			float boundingBoxSizeX2 = smplVertices2.Max(vertex => vertex.x) - smplVertices2.Min(vertex => vertex.x);
			currentResult[distancesToMeasure.Count] = new MeasurementResult() {
				measurePointNameA = "BoundingBoxLeft",
				measurePointNameB = "BoundingBoxRight",
				measurementName = "BoundingBoxHeight",
				distance1 = boundingBoxSizeX1,
				distance2 = boundingBoxSizeX2,
				discrepancy = Mathf.Abs(boundingBoxSizeX1 - boundingBoxSizeX2)
			};
			float boundingBoxSizeY1 = smplVertices1.Max(vertex => vertex.y) - smplVertices1.Min(vertex => vertex.y);
			float boundingBoxSizeY2 = smplVertices2.Max(vertex => vertex.y) - smplVertices2.Min(vertex => vertex.y);
			currentResult[distancesToMeasure.Count + 1] = new MeasurementResult() {
				measurePointNameA = "BoundingBoxTop",
				measurePointNameB = "BoundingBoxBottom",
				measurementName = "BoundingBoxWidth",
				distance1 = boundingBoxSizeY1,
				distance2 = boundingBoxSizeY2,
				discrepancy = Mathf.Abs(boundingBoxSizeY1 - boundingBoxSizeY2)
			};
			float boundingBoxSizeZ1 = smplVertices1.Max(vertex => vertex.z) - smplVertices1.Min(vertex => vertex.z);
			float boundingBoxSizeZ2 = smplVertices2.Max(vertex => vertex.z) - smplVertices2.Min(vertex => vertex.z);
			currentResult[distancesToMeasure.Count + 2] = new MeasurementResult() {
				measurePointNameA = "BoundingBoxFront",
				measurePointNameB = "BoundingBoxBack",
				measurementName = "BoundingBoxDepth",
				distance1 = boundingBoxSizeZ1,
				distance2 = boundingBoxSizeZ2,
				discrepancy = Mathf.Abs(boundingBoxSizeZ1 - boundingBoxSizeZ2)
			};
			return currentResult;
		}

		/// <summary>
		/// Gets the CSV representation of the last comparison run.
		/// Except from the leading static columns, there will be five others containing the comparison results in centimeters:
		/// Mesh name 1, mesh name 2, measure point name A, measure point name B, measurement name, measured distance for mesh 1, measured distance for mesh 2 and their absolute discrepancy.
		/// </summary>
		/// <returns>A CSV representation of the last result. No leading or trailing whitespaces.</returns>
		/// <param name="leadingStaticColumns">Static colums that have the same value for each row.</param>
		public string GetCSVComparison(params string[] leadingStaticColumns)
		{
			if (currentResult == null)
			{
				throw new UnityException("You can't get the comparison in CSV format unless you run it beforehand!");
			}
			string separator = ";";
			return string.Join("\r\n", currentResult.ToList().ConvertAll(measurementResult => {
				return (leadingStaticColumns.Length > 0 ? string.Join(separator, leadingStaticColumns) + separator : "")
					+ this.nameMesh1 + separator
					+ this.nameMesh2 + separator
					+ measurementResult.measurePointNameA + separator
					+ measurementResult.measurePointNameB + separator
					+ measurementResult.measurementName + separator
					+ (measurementResult.distance1 * 100).ToString("F2") + separator
					+ (measurementResult.distance2 * 100).ToString("F2") + separator
					+ (measurementResult.discrepancy * 100).ToString("F2");
			}).ToArray());
		}

		/// <summary>
		/// Gets the TXT representation of the last comparison run.
		/// Each row will contain all five values of the comparison results in centimeters:
		/// Mesh name 1, mesh name 2, measure point name A, measure point name B, measured distance for mesh 1, measured distance for mesh 2 and their absolute discrepancy.
		/// </summary>
		/// <returns>A TXT representation of the last result. No leading or trailing whitespaces.</returns>
		/// <param name="charCountLongestMeshName">The amount of characters used by the longest mesh name. This is used to make the output nice and readable!</param>
		public string GetTXTComparison(int charCountLongestMeshName = 11)
		{
			if (currentResult == null)
			{
				throw new UnityException("You can't get the comparison in TXT format unless you run it beforehand!");
			}
			int maxCharsDistanceCombination = currentResult.Max(measurementResult => measurementResult.measurePointNameA.Length + measurementResult.measurePointNameB.Length + measurementResult.measurementName.Length);
			return string.Join("\r\n", currentResult.ToList().ConvertAll(measurementResult => {
				return "Distance between " + (measurementResult.measurePointNameA + " and " + measurementResult.measurePointNameB + " (" + measurementResult.measurementName + ")").PadLeft(maxCharsDistanceCombination + 8, ' ') + ":\t"
					+ ((measurementResult.distance1 * 100).ToString("F2") + " cm (" + nameMesh1 + "),").PadRight(6 + 5 + charCountLongestMeshName + 2 + 3, ' ')
					+ ((measurementResult.distance2 * 100).ToString("F2") + " cm (" + nameMesh2 + ").").PadRight(6 + 5 + charCountLongestMeshName + 2 + 3, ' ')
					+ "Discrepancy: " + (measurementResult.discrepancy * 100).ToString("F2") + " cm.";
			}).ToArray());
		}
	}
}