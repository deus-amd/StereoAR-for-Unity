using UnityEngine;
using System;
using System.Collections;
using jp.nyatla.nyartoolkit.cs.markersystem;
using jp.nyatla.nyartoolkit.cs.core;
using NyARUnityUtils;
using System.IO;

public class ARCameraWithPSEyeBehaviour : MonoBehaviour
{
	// NyARToolKit
	private NyARUnityMarkerSystem markerSystem_;
	private NyARUnityPSEye psEye_;
	private int markerId_;
	public string MarkerName = "MarkerHiro"; 
	public GameObject MarkerObject  = null;
	private bool isDetect_  = false;
	
	// PS Eye
	public int PsEyeId   = 0;
	public int FrameRate = 60;
	
	// Camera Object
	public GameObject Background  = null;
	public Camera ARCamera  = null;
	
	// Background
	public int Layer       = 30;
	public int HiddenLayer = 31;
	
	// Somooth animation parameter
	private static float INELASTIC = 0.5f;
	public GameObject FilterPositionObject  = null;
	private Transform transform_  = null;
	
	void Awake()
	{
		// Check PS Eye number
		try {
			int psEyeCount = PSEyeTexture.CLEyeGetCameraCount();
			switch (psEyeCount) {
				case 0:
					Debug.LogError("No PS Eye found");
					return;
				case 1:
					Debug.LogError("Only one PS Eye found");
					return;
			}
		} catch (Exception e) {
			Debug.LogError("Oops..., Failed at loading CLEyeMulticam.dll. Please relaunch Unity!");
			Debug.LogError(e.ToString());
			return;
		}
		
		// Make PS Eye texture
		var PsEyeTexture = new PSEyeTexture(PsEyeId, FrameRate, true);
		psEye_ = new NyARUnityPSEye(PsEyeTexture);
		
		// Marker system
		var config     = new NyARMarkerSystemConfig(PsEyeTexture.Width, PsEyeTexture.Height);
		markerSystem_  = new NyARUnityMarkerSystem(config);
		
		// Load marker from texture
		var markerTexture = (Texture2D)(Resources.Load(MarkerName, typeof(Texture2D)));
		markerId_ = markerSystem_.addARMarker(markerTexture, 16, 25, 80);
		
		// Marker layer
		MarkerObject.layer = Layer;
		for (int i = 0; i < MarkerObject.transform.GetChildCount(); ++i) {
			MarkerObject.transform.GetChild(i).gameObject.layer = Layer;
		}

		// Set camera background 
		// - 
		Background.renderer.material.mainTexture = PsEyeTexture.Texture;
		Background.layer = Layer;
		ARCamera.cullingMask &= ~(1 << HiddenLayer);
		markerSystem_.setARBackgroundTransform(Background.transform);
		markerSystem_.setARCameraProjection(ARCamera);
		
		// Set transforms for animation
		transform_  = FilterPositionObject.transform;
	}
	
	void Start()
	{
		if (psEye_ == null) return; 
		
		psEye_.start();
	}
	
	void Update()
	{
		if (psEye_ == null) return; 
		
		psEye_.update();
		markerSystem_.update(psEye_);
		
		if (markerSystem_.isExistMarker(markerId_)) {
			onFindMaker();
		} else {
			if (isDetect_) {
				onLostMarker();
			}
		}
	}
	
	void onFindMaker()
	{
		markerSystem_.setMarkerTransform(markerId_, transform_);
		transform_.Rotate(new Vector3(0, 180, 180));
		if (isDetect_) {
			MarkerObject.transform.localPosition = 
				Vector3.Slerp(MarkerObject.transform.localPosition, transform_.localPosition, INELASTIC);
			MarkerObject.transform.localRotation = 
				Quaternion.Slerp(MarkerObject.transform.localRotation, transform_.localRotation, INELASTIC);
			MarkerObject.transform.localScale = 
				Vector3.Slerp(MarkerObject.transform.localScale, transform_.localScale, INELASTIC);
		} else {
			MarkerObject.transform.localPosition = transform_.localPosition;
			MarkerObject.transform.localRotation = transform_.localRotation;
			MarkerObject.transform.localScale    = transform_.localScale;
		}
		isDetect_ = true;
	}
	
	void onLostMarker()
	{
		MarkerObject.transform.localPosition = new Vector3(0, 0, -100);
		isDetect_ = false;
	}
}
