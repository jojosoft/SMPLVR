/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ViveScale;

public class ViveScaleDemoController : MonoBehaviour, Scalable {

	public ViveScaleController scaler;

	void Awake()
	{
		scaler.gameObject.SetActive(false);
	}

	IEnumerator Start()
	{
		scaler.Subscribe(this);
		yield return new WaitForSeconds(2);
		scaler.gameObject.SetActive(true);
	}

	public void StartScaling()
	{ }

	public void ReceiveNewScale(float newScale)
	{
		/*
		 * This demonstration shows an easy absolute scaling mechanism.
		 * Additionally using Scalable.StartScaling() allows you to convert this into relative scaling.
		 */
		this.transform.localScale = new Vector3(1, newScale / 100, 1);
	}
}