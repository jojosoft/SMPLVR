/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ComparisonRender : MonoBehaviour {

	private RenderTexture renderTex;
	private Camera renderCam;

	void Awake()
	{
		this.renderCam = this.GetComponent<Camera>();
	}

	/// <summary>
	/// Renders the current frame after setting the size of the camera according to the given world space size.
	/// This is possible ComparisonRenderer will always make the camera next to it ortographic.
	/// </summary>
	/// <param name="filePath">File path.</param>
	/// <param name="widthMeters">The width the ortographic camera should have in world space.</param>
	/// <param name="heightMeters">The height the ortographic camera should have in world space.</param>
	/// <param name="pixelsMinimum">The amount of pixels for the shorter dimension.</param>
	public void RenderCurrentFrame(string filePath, float widthMeters, float heightMeters, float depthMeters, int pixelsMinimum, bool showGrid = false, float cameraHeight = 0f)
	{
		// Adjust the camera's height and width and create an appropriate RenderTexture:
		renderCam.orthographic = true;
		renderCam.orthographicSize = heightMeters / 2f;
		renderCam.aspect = widthMeters / heightMeters;
		if (widthMeters > heightMeters)
		{
			this.renderTex = new RenderTexture(Mathf.RoundToInt(renderCam.aspect * pixelsMinimum), pixelsMinimum, 24);
		}
		else
		{
			this.renderTex = new RenderTexture(pixelsMinimum, Mathf.RoundToInt(heightMeters / widthMeters * pixelsMinimum), 24);
		}
		// If the background grid is requested and available, activate it:
		GameObject backgroundGrid = null;
		if (showGrid && this.transform.childCount > 0)
		{
			backgroundGrid = this.transform.GetChild(0).gameObject;
			backgroundGrid.SetActive(true);
		}
		LogMessage("Render " + renderTex.width + "x" + renderTex.height + " image of " + new Vector3(widthMeters, heightMeters, depthMeters) + " space.");
		renderCam.targetTexture = this.renderTex;
		// Move the camera to a point with 1 m backup where the complete depth range can be seen in any case:
		renderCam.transform.position = new Vector3(0, cameraHeight, 0) - renderCam.transform.forward * (depthMeters / 2f + 1);
		renderCam.farClipPlane = depthMeters + 2 + (backgroundGrid != null ? backgroundGrid.transform.localPosition.z : 0);
		// Render the camera's current view to the specified file:
		RenderTexture.active = renderTex;
		Texture2D currentRendering = new Texture2D(renderTex.width, renderTex.height, TextureFormat.RGBA32, false);
		this.GetComponent<Camera>().Render();
		currentRendering.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
		File.WriteAllBytes(filePath, currentRendering.EncodeToPNG());
		RenderTexture.active = null;
		if (backgroundGrid != null)
		{
			backgroundGrid.SetActive(true);
		}
	}

	/// <summary>
	/// Logs a message to the console and adds a leading class identifier.
	/// </summary>
	/// <param name="message">The message you want to log without leading class identifier.</param>
	/// <param name="warning">Should the message be logged as warning?</param>
	private void LogMessage(string message, bool warning = false)
	{
		// Add the leading class identifier for this class:
		message = "[" + this.GetType().Name + "] " + message;
		if (warning)
		{
			Debug.LogWarning(message);
		}
		else
		{
			Debug.Log(message);
		}
	}
}