/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace SMPLVR
{
	public static class InputFileParser
	{
		/// <summary>
		/// Parses vertex numbers and names from a given file. Each row has to have the format "VertexName = VertexNumber".
		/// Surround calls to this function with a try-catch statement, since the input file might be not in the right format.
		/// </summary>
		/// <returns>A dictionary mapping the given vertex names to the corresponding vertex numbers.</returns>
		/// <param name="filePath">The path to your input file to which the application has at least read-access.</param>
		/// <exception cref="IndexOutOfRangeException">At least one row of your file has no '=' to seperate key and value.</exception>
		/// <exception cref="FormatException">At least one of the vertex numbers in your file cannot be parsed to an integer.</exception>
		public static Dictionary<string, int> ParseVertexNumbers(string filePath)
		{
			return File.ReadAllLines(filePath).Where(line => !line.StartsWith("#")).ToDictionary<string, string, int>(
				line => line.Split('=')[0].Trim(),
				line => int.Parse(line.Split('=')[1].Trim())
			);
		}

		/// <summary>
		/// Parses distance measurements from the given file. Each row has to have the format "StartVertexName, EndVertexName, MeasurementName".
		/// Surround calls to this function with a try-catch statement, since the input file might be not in the right format.
		/// </summary>
		/// <returns>A list containing all the vertex pairs as a string array that were defined inside the given file.</returns>
		/// <param name="filePath">The path to your input file to which the application has at least read-access.</param>
		public static List<string[]> ParseDistanceMeasurements(string filePath)
		{
			return File.ReadAllLines(filePath).Where(line => !line.StartsWith("#")).ToList().ConvertAll(
				line => line.Split(',').ToList().ConvertAll(component => component.Trim()).Take(3).ToArray()
			);
		}
	}
}