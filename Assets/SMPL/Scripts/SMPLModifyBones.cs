//
// Joachim Tesch, Max Planck Institute for Biological Cybernetics
//
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LightweightMatrixCSharp;

public class SMPLModifyBones : MonoBehaviour {

	public TextAsset jointsRegressorJSON;

	private int _numberOfJoints = 24;
	private int _numberOfBetas = 10;

	private Matrix[] _template_J;
	private Matrix[] _regressor;

	private Vector3[] _joints;


	public int standardDeviationsFBX = 5;

	private SkinnedMeshRenderer targetRenderer;

	private Transform[] _bones = null;
	private Transform[] _bonesBackup = null;

    private string _boneNamePrefix;

    private Dictionary<string, int> _boneNameToJointIndex;

    private bool _initialized;
    private bool _bonesAreModified;

    private Transform _pelvis;
    private Vector3[] _bonePositions;

    private Mesh _bakedMesh = null;

    void Awake()
    {
		_initialized = false;
		_joints = new Vector3[_numberOfJoints];

		_template_J = new Matrix[3];
		_regressor = new Matrix[3];

		for (int i=0; i<=2; i++)
		{
			_template_J[i] = new Matrix(_numberOfJoints, 1);
			_regressor[i] = new Matrix(_numberOfJoints, _numberOfBetas);
		}


		_bonesAreModified = false;

        _boneNamePrefix = "";

        _boneNameToJointIndex = new Dictionary<string, int>();

        _boneNameToJointIndex.Add("Pelvis", 0);
        _boneNameToJointIndex.Add("L_Hip", 1);
        _boneNameToJointIndex.Add("R_Hip", 2);
        _boneNameToJointIndex.Add("Spine1", 3);
        _boneNameToJointIndex.Add("L_Knee", 4);
        _boneNameToJointIndex.Add("R_Knee", 5);
        _boneNameToJointIndex.Add("Spine2", 6);
        _boneNameToJointIndex.Add("L_Ankle", 7);
        _boneNameToJointIndex.Add("R_Ankle", 8);
        _boneNameToJointIndex.Add("Spine3", 9);
        _boneNameToJointIndex.Add("L_Foot", 10);
        _boneNameToJointIndex.Add("R_Foot", 11);
        _boneNameToJointIndex.Add("Neck", 12);
        _boneNameToJointIndex.Add("L_Collar", 13);
        _boneNameToJointIndex.Add("R_Collar", 14);
        _boneNameToJointIndex.Add("Head", 15);
        _boneNameToJointIndex.Add("L_Shoulder", 16);
        _boneNameToJointIndex.Add("R_Shoulder", 17);
        _boneNameToJointIndex.Add("L_Elbow", 18);
        _boneNameToJointIndex.Add("R_Elbow", 19);
        _boneNameToJointIndex.Add("L_Wrist", 20);
        _boneNameToJointIndex.Add("R_Wrist", 21);
        _boneNameToJointIndex.Add("L_Hand", 22);
        _boneNameToJointIndex.Add("R_Hand", 23);

        _bakedMesh = new Mesh();
    }

	void Start()
	{
		if (jointsRegressorJSON == null)
		{
			Debug.LogError("ERROR: JSON joint regressor matrix not defined");
			return;
		}

		if (! InitRegressorMatrix(ref _template_J, ref _regressor, ref jointsRegressorJSON))
		{
			Debug.LogError("ERROR: Cannot create joint regressor matrix");
			return;
		}
		// Debug.Log("Regression matrix initialized");


		if (GetComponents<SkinnedMeshRenderer>().Length < 1)
		{
			Debug.LogError("No SkinnedMeshRenderer found next to this component");
			return;
		}

		targetRenderer = GetComponent<SkinnedMeshRenderer>();

		_bones = targetRenderer.bones;

        _bonePositions = new Vector3[_bones.Length];

        _bonesBackup = new Transform[_bones.Length];
        _cloneBones(_bones, _bonesBackup);

        // Determine bone name prefix
        foreach (Transform bone in _bones)
        {
            if (bone.name.EndsWith("root"))
            {
                int index = bone.name.IndexOf("root");
                _boneNamePrefix = bone.name.Substring(0, index);
                break;
            }
        }

        // Determine pelvis node
        foreach (Transform bone in _bones)
        {
            if (bone.name.EndsWith("Pelvis"))
            {
                _pelvis = bone;
                break;
            }
        }

        //Debug.Log("INFO: Bone name prefix: '" + _boneNamePrefix + "'");

        _initialized = true;
    }


	private bool InitRegressorMatrix(ref Matrix[] jointTemplate, ref Matrix[] regressor, ref TextAsset ta)
	{
		string jsonText = ta.text;
		SimpleJSON.JSONNode node = SimpleJSON.JSON.Parse(jsonText);

		// Init matrices
		for (int i=0; i < _numberOfJoints; i++)
		{
			// Init joint template matrix
			double x = node["template_J"][i][0].AsDouble;
			double y = node["template_J"][i][1].AsDouble;
			double z = node["template_J"][i][2].AsDouble;

			(jointTemplate[0])[i, 0] = x;
			(jointTemplate[1])[i, 0] = y;
			(jointTemplate[2])[i, 0] = z;

			// Init beta regressor matrix    
			for (int j=0; j< _numberOfBetas; j++)
			{
				(regressor[0])[i, j] = node["betasJ_regr"][i][0][j].AsDouble;
				(regressor[1])[i, j] = node["betasJ_regr"][i][1][j].AsDouble;
				(regressor[2])[i, j] = node["betasJ_regr"][i][2][j].AsDouble;
			}
		}

		return true;
	}

	private bool CalculateJoints(float[] betas)
	{
		if (! _initialized)
			return false;

		// Check dimensions of beta values
		int numCurrentBetas = betas.Length;
		if (numCurrentBetas != _numberOfBetas)
		{
			Debug.LogError("ERROR: Invalid beta input value count in baked mesh: need " + _numberOfBetas + " but have " + numCurrentBetas);
			return false;
		}

		// Create beta value matrix
		Matrix betaMatrix = new Matrix(_numberOfBetas, 1);
		for (int row = 0; row < _numberOfBetas; row++)
		{
			betaMatrix[row, 0] = betas[row];
			// Debug.Log("beta " + row + ": " + betas[row]);
		}           

		// Apply joint regressor to beta matrix to calculate new joint positions
		Matrix newJointsX = _regressor[0] * betaMatrix + _template_J[0];
		Matrix newJointsY = _regressor[1] * betaMatrix + _template_J[1];
		Matrix newJointsZ = _regressor[2] * betaMatrix + _template_J[2];

		// Update joints vector
		for (int row = 0; row < _numberOfJoints; row++)
		{
			// Convert Maya regressor to Unity coordinate system by negating X value
			_joints[row] = new Vector3(-(float)newJointsX[row, 0], (float)newJointsY[row, 0], (float)newJointsZ[row, 0]);
		}

		return true;
	}



	public void SetShapeWeights(float[] betas)
	{
		if (! _initialized)
		{
			throw new UnityException("The SMPLMofifyBones component was not initialized yet.");
		}

		for (int i = 0; i < betas.Length; i++)
		{
			if (betas[i] >= 0.0)
			{
				targetRenderer.SetBlendShapeWeight(i * 2 + 0, Mathf.Abs(betas[i]) * 100.0f / standardDeviationsFBX);
			}
			else
			{
				targetRenderer.SetBlendShapeWeight(i * 2 + 1, Mathf.Abs(betas[i]) * 100.0f / standardDeviationsFBX);
			}
		}
	}

	public bool UpdateBonePositions(float[] betas, bool feetOnGround = true)
	{
		if (! _initialized)
			return false;

		CalculateJoints(betas);

		float heightOffset = 0.0f;

		int pelvisIndex = -1;
		for (int i=0; i<_bones.Length; i++)
		{
			int index;
			string boneName = _bones[i].name;

			// Remove f_avg/m_avg prefix
			boneName = boneName.Replace(_boneNamePrefix, "");

			if (boneName == "root")
				continue;

			if (boneName == "Pelvis")
				pelvisIndex = i;


			Transform avatarTransform = targetRenderer.transform.parent;
			if (_boneNameToJointIndex.TryGetValue(boneName, out index))
			{
				// Incoming new positions from joint calculation are centered at origin in world space
				// Transform to avatar position+orientation for correct world space position
				_bones[i].position = avatarTransform.TransformPoint(_joints[index]);
				_bonePositions[i] = _bones[i].position;
			}
			else
			{
				Debug.LogError("ERROR: No joint index for given bone name: " + boneName);
			}
		}

		_setBindPose(_bones);

		if (feetOnGround)
		{
			Vector3 min = new Vector3();
			Vector3 max = new Vector3();
			_localBounds(ref min, ref max);
			heightOffset = -min.y;

			_bones[pelvisIndex].Translate(0.0f, heightOffset, 0.0f);

			// Update bone positions to reflect new pelvis position
			for (int i=0; i<_bones.Length; i++)
			{
				_bonePositions[i] = _bones[i].position;
			}
		}

		return true;

	}

	public Transform getPelvis()
    {
        return _pelvis;
    }

    public Vector3[] getBonePositions()
    {
        return _bonePositions;
    }

    private void _cloneBones(Transform[] bonesOriginal, Transform[] bonesModified)
	{
		// Clone transforms (name, position, rotation)
		for (int i=0; i<bonesModified.Length; i++)
		{
			bonesModified[i] = new GameObject().transform;
			bonesModified[i].name = bonesOriginal[i].name + "_clone";
			bonesModified[i].position = bonesOriginal[i].position;
			bonesModified[i].rotation = bonesOriginal[i].rotation;
		}

		// Clone hierarchy
		for (int i=0; i<bonesModified.Length; i++)
		{
			string parentName = bonesOriginal[i].parent.name;

			// Find transform with same name in copy
			GameObject go = GameObject.Find(parentName + "_clone");
			if (go == null)
			{
				// Cannot find parent so must be armature
				bonesModified[i].parent = bonesOriginal[i].parent;
			}
			else
			{
				bonesModified[i].parent = go.transform;
			}

		}

		return;

	}	

	private void _restoreBones()
	{
		// Restore transforms (name, position, rotation)
		for (int i=0; i<_bones.Length; i++)
		{
			_bones[i].position = _bonesBackup[i].position;
			_bones[i].rotation = _bonesBackup[i].rotation;
		}
	}	

	private void _setBindPose(Transform[] bones)
	{
		Matrix4x4[] bindPoses = targetRenderer.sharedMesh.bindposes;
		// Debug.Log("Bind poses: " + bindPoses.Length);

        Transform avatarRootTransform = targetRenderer.transform.parent;

		for (int i=0; i<bones.Length; i++)
		{
	        // The bind pose is bone's inverse transformation matrix.
	        // Make this matrix relative to the avatar root so that we can move the root game object around freely.            
            bindPoses[i] = bones[i].worldToLocalMatrix * avatarRootTransform.localToWorldMatrix;
		}

		targetRenderer.bones = bones;
		Mesh sharedMesh = targetRenderer.sharedMesh;
		sharedMesh.bindposes = bindPoses;
		targetRenderer.sharedMesh = sharedMesh;

        _bonesAreModified = true;
	}

    private void _localBounds(ref Vector3 min, ref Vector3 max)
    {
        targetRenderer.BakeMesh(_bakedMesh);
        Vector3[] vertices = _bakedMesh.vertices;
        int numVertices = vertices.Length;

        float xMin = Mathf.Infinity;
        float xMax = Mathf.NegativeInfinity;
        float yMin = Mathf.Infinity;
        float yMax = Mathf.NegativeInfinity;
        float zMin = Mathf.Infinity;
        float zMax = Mathf.NegativeInfinity;

        for (int i=0; i<numVertices; i++)
        {
            Vector3 v = vertices[i];

            if (v.x < xMin)
            {
                xMin = v.x;
            }
            else if (v.x > xMax)
            {
                xMax = v.x;
            }

            if (v.y < yMin)
            {
                yMin = v.y;
            }
            else if (v.y > yMax)
            {
                yMax = v.y;
            }

            if (v.z < zMin)
            {
                zMin = v.z;
            }
            else if (v.z > zMax)
            {
                zMax = v.z;
            }
        }

        min.x = xMin;
        min.y = yMin;
        min.z = zMin;
        max.x = xMax;
        max.y = yMax;
        max.z = zMax;
		Debug.Log("MinMax: x[" + xMin + "," + xMax + "], y["  + yMin + "," + yMax + "], z["  + zMin + "," + zMax + "]");
    }

    // Note: Cannot use OnDestroy() because in OnDestroy the bone Transform objects are already destroyed
    //       See also https://docs.unity3d.com/Manual/ExecutionOrder.html
	void OnApplicationQuit()
	{
		// Debug.Log("OnApplicationQuit: Restoring original bind pose");

        if (! _initialized)
            return;

        if (! _bonesAreModified)
            return;

		if ((_bones != null) && (_bonesBackup != null))
		{
			_restoreBones();
			_setBindPose(_bones);
		}
	}
}