using UnityEditor;

class DisableOptimizeOnImport : AssetPostprocessor
{
	public void OnPreprocessModel()
	{
		/* 
		 * The following statement disables "Optimize Mesh" for all 3D models during import.
		 * This is done because otherwise, the order of the vertex indices gets mixed up!
		 */
		(base.assetImporter as ModelImporter).optimizeMesh = false;
	}
}