/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A little helper script for deactivating shadows for a game object and all its children.
/// </summary>
public class HideShadows : MonoBehaviour {

	public int maximumTrials = 60;

	private int trials = 0;
	private bool success = false;

	void Update()
	{
		if (!success && trials < 60)
		{
			if (this.transform.parent.GetComponentsInChildren<Renderer>(true).ToList().Count > 0)
			{
				this.transform.parent.GetComponentsInChildren<Renderer>(true).ToList().ForEach(renderer => renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off);
				success = true;
			}
			else
			{
				trials++;
			}
		}
	}
}