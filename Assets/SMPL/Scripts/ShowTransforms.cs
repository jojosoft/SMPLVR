//
// Joachim Tesch, Max Planck Institute for Biological Cybernetics
//
using UnityEngine;
using System.Collections;

public class ShowTransforms : MonoBehaviour {

    public Transform  root;
    public GameObject prefab;

	// Use this for initialization
	void Start () {
        Transform[] children = root.GetComponentsInChildren<Transform>();	
        int count = 0;
        foreach (Transform child in children)
        {
//            Debug.Log(child.name);
            GameObject go = Instantiate(prefab, child.position, Quaternion.identity, root) as GameObject;
            go.name = go.name + count;
            go.transform.parent = child;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            count++;
        }

	}
	
}
