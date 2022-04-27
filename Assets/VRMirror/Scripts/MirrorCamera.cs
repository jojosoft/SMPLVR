using UnityEngine;
using System.Collections;

public class MirrorCamera : MonoBehaviour {
	
	public Transform CameraRig;
	public Camera VrEye;
	public bool copyClearSettings = true;
	public int TextureSize = 2048;

	Camera cameraForPortal;
	RenderTexture leftEyeRenderTexture;
	RenderTexture rightEyeRenderTexture;
	Vector3 mirrorMatrixScale = new Vector3 (-1f, 1f, 1f);
	Vector3 reflectionRotation = new Vector3 (0f, 180f, 0f);
	Vector3 eyeOffset;

	private Transform MirrorCameraParent;
	private Transform ReflectionTransform;
	private bool RenderAsMirror = true;

	protected void Awake() {

		MirrorCameraParent = gameObject.transform.parent;
		ReflectionTransform = MirrorCameraParent.parent;

		cameraForPortal = GetComponent<Camera>();
		cameraForPortal.enabled = false;

		//TODO: Check performance impact of using non-pow2 textures
		//      old code: leftEyeRenderTexture = new RenderTexture (2160, 1200, 24);
		leftEyeRenderTexture = new RenderTexture (TextureSize, TextureSize, 24);
		rightEyeRenderTexture = new RenderTexture (TextureSize, TextureSize, 24);

		if (copyClearSettings)
		{
			cameraForPortal.clearFlags = VrEye.clearFlags;
			cameraForPortal.backgroundColor = VrEye.backgroundColor;
		}

	}

	protected Matrix4x4 HMDMatrix4x4ToMatrix4x4(Valve.VR.HmdMatrix44_t input) {
		var m = Matrix4x4.identity;

		m[0, 0] = input.m0;
		m[0, 1] = input.m1;
		m[0, 2] = input.m2;
		m[0, 3] = input.m3;

		m[1, 0] = input.m4;
		m[1, 1] = input.m5;
		m[1, 2] = input.m6;
		m[1, 3] = input.m7;

		m[2, 0] = input.m8;
		m[2, 1] = input.m9;
		m[2, 2] = input.m10;
		m[2, 3] = input.m11;

		m[3, 0] = input.m12;
		m[3, 1] = input.m13;
		m[3, 2] = input.m14;
		m[3, 3] = input.m15;

		return m;
	}

	public void RenderIntoMaterial(Material material) {
		if (Camera.current == VrEye) {
			if (RenderAsMirror) {
				ReflectionTransform.localPosition = Vector3.zero;
				ReflectionTransform.localRotation = Quaternion.identity;
				MirrorCameraParent.position = CameraRig.position;
				MirrorCameraParent.rotation = CameraRig.rotation;
				ReflectionTransform.localEulerAngles = reflectionRotation;

				Vector3 centerAnchorPosition = MirrorCameraParent.localPosition;
				centerAnchorPosition.x *= -1;
				Vector3 centerAnchorRotation = -MirrorCameraParent.localEulerAngles;
				centerAnchorRotation.x *= -1;
				MirrorCameraParent.localPosition = centerAnchorPosition;
				MirrorCameraParent.localEulerAngles = centerAnchorRotation;
			}

			transform.localRotation = VrEye.transform.localRotation;

			if (RenderAsMirror) {
				GL.invertCulling = true;
			}
			// left eye
			if (RenderAsMirror) {
				eyeOffset = SteamVR.instance.eyes [0].pos;
				transform.localPosition = VrEye.transform.localPosition + VrEye.transform.TransformVector (eyeOffset);
				cameraForPortal.projectionMatrix = HMDMatrix4x4ToMatrix4x4 (SteamVR.instance.hmd.GetProjectionMatrix (Valve.VR.EVREye.Eye_Left, VrEye.nearClipPlane, VrEye.farClipPlane)) * Matrix4x4.Scale (mirrorMatrixScale);
			} else {
				eyeOffset = SteamVR.instance.eyes [0].pos;
				transform.localPosition = VrEye.transform.localPosition + VrEye.transform.TransformVector (eyeOffset);
				cameraForPortal.projectionMatrix = HMDMatrix4x4ToMatrix4x4 (SteamVR.instance.hmd.GetProjectionMatrix (Valve.VR.EVREye.Eye_Left, VrEye.nearClipPlane, VrEye.farClipPlane));
			}

			cameraForPortal.targetTexture = leftEyeRenderTexture;
			cameraForPortal.Render();
			material.SetTexture("_LeftEyeTexture", leftEyeRenderTexture);

			// right eye
			if (RenderAsMirror) {
				eyeOffset = SteamVR.instance.eyes [1].pos;
				transform.localPosition = VrEye.transform.localPosition + VrEye.transform.TransformVector (eyeOffset);
				cameraForPortal.projectionMatrix = HMDMatrix4x4ToMatrix4x4 (SteamVR.instance.hmd.GetProjectionMatrix (Valve.VR.EVREye.Eye_Right, VrEye.nearClipPlane, VrEye.farClipPlane)) * Matrix4x4.Scale (mirrorMatrixScale);
			} else {
				eyeOffset = SteamVR.instance.eyes [1].pos;
				transform.localPosition = VrEye.transform.localPosition + VrEye.transform.TransformVector (eyeOffset);
				cameraForPortal.projectionMatrix = HMDMatrix4x4ToMatrix4x4 (SteamVR.instance.hmd.GetProjectionMatrix (Valve.VR.EVREye.Eye_Right, VrEye.nearClipPlane, VrEye.farClipPlane));
			}

			cameraForPortal.targetTexture = rightEyeRenderTexture;
			cameraForPortal.Render();
			material.SetTexture("_RightEyeTexture", rightEyeRenderTexture);

			if (RenderAsMirror) {
				GL.invertCulling = false;
			}

		}
	}

}
