/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class AssignAssetBundles
{
	/// <summary>
	/// First, this function searches for directories with names that begin with an unsigned integer greater than zero followed by a minus sign.
	/// After having found at least one directory, it searches for files which are present with the same name in all of the directories.
	/// Each collection of files with the same name will then be added to their own AssetBundle, which will get this name, as well.
	/// Make sure that every directory only contains a file name once, not several times with different extension!
	/// </summary>
	[MenuItem("Assets/Assign AssetBundles")]
	static void AssignFilesToAssetBundles()
	{
		string[] searchDirectories = Directory.GetDirectories("Assets").Where(filePath => Regex.IsMatch(filePath, "[0-9]+-.*")).ToArray();
		// Check whether the file structure was properly prepared for this process:
		if (searchDirectories.Length < 1)
		{
			EditorUtility.DisplayDialog("No folders found!", "There are no subfolders in the Assets folder matching the name specification. Make sure that the name of each of the folders starts with an unsigned integer greater than zero followed by a minus sign.", "OK");
			return;
		}
		if (searchDirectories.Any(path => Directory.GetFiles(path).Any(file => Directory.GetFiles(path, Path.GetFileNameWithoutExtension(file) + ".*").Where(filepath => !Path.GetExtension(filepath).Equals(".meta")).Count() > 1)))
		{
			EditorUtility.DisplayDialog("Ambiguous files!", "Please make sure that the specified directories only contain every filename once. The assignment cannot process multiple files with the same name but different extensions! The reason for this is that the process should work independently from file names.\n\nCheck the following directories:\n\t" + string.Join("\n\t", searchDirectories), "OK");
			return;
		}
		// Look up which filenames are present in each of the directories:
		List<string> fileNames = new List<string>();
		foreach (string file in Directory.GetFiles(searchDirectories[0]).Where(path => !Path.GetExtension(path).Equals(".meta")))
		{
			// For each file from the first directory, check if a file with the same name exists in each of the other directories:
			if (searchDirectories.Skip(1).All(path => Directory.GetFiles(path, Path.GetFileNameWithoutExtension(file) + ".*").Length > 0))
			{
				fileNames.Add(Path.GetFileNameWithoutExtension(file));
			}
		}
		// Clear all AssetBundle assignments to prevent old files to be processed:
		foreach (string bundleName in AssetDatabase.GetAllAssetBundleNames())
		{
			AssetDatabase.RemoveAssetBundleName(bundleName, true);
		}
		// Add each of the files for one file name to the corresponding AssetBundle:
		fileNames.ForEach(fileName => searchDirectories.ToList().ForEach(path => {
			AssetImporter.GetAtPath(Directory.GetFiles(path, fileName + ".*").Where(filePath => !Path.GetExtension(filePath).Equals(".meta")).First()).assetBundleName = fileName;
		}));
		// Lastly, clear up the AssetBundle names, since the last operation might have left some of the old names unassigned.
		AssetDatabase.RemoveUnusedAssetBundleNames();
		// Notify the user that this operation succeeded:
		EditorUtility.DisplayDialog("Success!", "The AssetBundles were successfully assigned.\n\nFolders searched:\n\t" + string.Join("\n\t", searchDirectories) + "\n\nComplete AssetBundles found:\n\t" + string.Join("\n\t", fileNames.ToArray()), "OK");
	}
}