/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using UnityEngine;
using System.Collections;
using System.IO;

public class ExperimentDataRecorder {

	public bool separateWithTabs = false;
	public int minimumSpacesAtTheEnd = 5;

	private string filePath;
	private string[] columns;
	private int spaceForEachEntry = 8;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExperimentDataRecorder"/> class.
	/// </summary>
	/// <param name="experimentName">The experiment's name is needed to name the output file correctly.</param>
	/// <param name="cols">For each column you want to have in the output file, pass a string with its name as a parameter.</param>
	public ExperimentDataRecorder(string experimentName, params string[] cols)
	{
		// Set the variables for this experiment record:
		filePath = "./data" + experimentName + "(" + generateTimestamp() + ").txt";
		columns = cols;
		// Detect the minimum amount of characters needed for each data field:
		foreach (string s in cols)
		{
			if (s.Length > spaceForEachEntry)
			{
				spaceForEachEntry = s.Length;
			}
		}
		spaceForEachEntry += minimumSpacesAtTheEnd;
		// Write the header line of the new experiment record:
		try
		{
			File.AppendAllText(filePath, buildNewDataLine(cols));
		}
		catch
		{
			throw new UnityException("The header couldn't be written into the data record file!");
		}
	}

	/// <summary>
	/// Writes new data into the current experiment record file.
	/// </summary>
	/// <param name="data">A data array to be recorded.</param>
	public void WriteNewDataIntoRecord(params string[] data)
	{
		if (data.Length != columns.Length)
		{
			throw new UnityException("You have to specify as many data parameters as you specified columns for this record!\r\nYou started the record with " + columns.Length + " columns and now passed " + data.Length + " data fields.");
		}
		try
		{
			File.AppendAllText(filePath, buildNewDataLine(data));
		}
		catch
		{
			throw new UnityException("The header couldn't be written into the data record file!");
		}
	}

	/// <summary>
	/// Builds a new data line in a specific format.
	/// This can directly be appended to the output file!
	/// </summary>
	/// <returns>A new data line in a specific format.</returns>
	/// <param name="data">The data to summarise as one line.</param>
	private string buildNewDataLine(string[] data)
	{
		if (separateWithTabs)
		{
			return string.Join("\t", data) + "\r\n";
		}
		// Otherwise, each data field is filled in with spaces:
		string newLine = "";
		foreach (string s in data)
		{
			newLine += s.PadRight(spaceForEachEntry, ' ');
		}
		return newLine + "\r\n";
	}

	/// <summary>
	/// Generates a timestamp which is compatible with windows file names.
	/// </summary>
	/// <returns>The timestamp as a string.</returns>
	private string generateTimestamp()
	{
		System.DateTime d = System.DateTime.Now;
		return d.Day.ToString("D2") + "." + d.Month.ToString("D2") + "." + d.Year.ToString() + ", " + d.Hour.ToString("D2") + "-" + d.Minute.ToString("D2") + "-" + d.Second.ToString("D2");
	}
}