using RogoDigital;
using RogoDigital.Lipsync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

public class LipSyncClipSetup : ModalParent
{
#pragma warning disable 618

	#region Delegates
	public static LipSyncClipEditorMenuDelegate onDrawTopMenuBar;
	public static LipSyncClipEditorMenuItemDelegate onDrawFileMenu;
	public static LipSyncClipEditorMenuItemDelegate onDrawEditMenu;
	public static LipSyncClipEditorMenuItemDelegate onDrawAutoSyncMenu;
	public static LipSyncClipEditorMenuItemDelegate onDrawHelpMenu;
	public static List<RDEditorShortcut.Action> shortcutActions;
	#endregion

	public AudioClip clip;

	private const float version = 1.43f;
	private const string versionNumber = "Pro 1.43";

#if !(UNITY_5_3_6 || UNITY_5_4_OR_NEWER)
	private Texture2D waveform;
#endif
	private float seekPosition;

	private bool isPlaying = false;
	private bool isPaused = false;
	private bool previewing = false;
	private bool looping = false;

	private Rect oldPos;
	private int waveformHeight;
	private AudioClip oldClip;

	private TimeSpan timeSpan;
	private float oldSeekPosition;
	private float stopTimer = 0;
	private float prevTime = 0;
	private float resetTime = 0;
	private float viewportStart = 0;
	private float viewportEnd = 10;

	private int draggingScrollbar;
	private float scrollbarStartOffset;

	private List<int> selection;
	private int firstSelection;
	private float[] selectionOffsets;
	private float[] sequentialStartOffsets;
	private float[] sequentialEndOffsets;
	private Rect selectionRect = new Rect(0, 0, 0, 0);

	private int copyBufferType;
	private List<object> copyBuffer = new List<object>();

	private int currentMarker = -1;
	private int highlightedMarker = -1;
	private int currentComponent = 0;
	private int markerTab = 0;

	private bool dragging = false;

	private int filterMask = -1;
	private string[] phonemeSetNames;

	private float startOffset;
	private float endOffset;
	private float lowSnapPoint;
	private float highSnapPoint;

	private Color nextColor;
	private Color lastColor;

	private EmotionMarker nextMarker = null;
	private EmotionMarker previousMarker = null;

	private string[] markerTypes = new string[] { "Phonemes", "Emotions", "Gestures" };

	private string lastLoad = "";
	public string fileName = "Untitled";
	public bool changed = false;

	#region GUI Images
	private Texture2D playhead_top;
	private Texture2D playhead_line;
	private Texture2D playhead_bottom;
	private Texture2D track_top;
	private Texture2D playIcon;
	private Texture2D stopIcon;
	private Texture2D pauseIcon;
	private Texture2D loopIcon;
	private Texture2D settingsIcon;
	private Texture2D previewIcon;
	private Texture2D windowIcon;
	private Texture2D indicator;

	private Texture2D marker_normal;
	private Texture2D marker_hover;
	private Texture2D marker_selected;
	private Texture2D marker_sustained;
	private Texture2D marker_line;

	private Texture2D emotion_start;
	private Texture2D emotion_start_hover;
	private Texture2D emotion_start_selected;
	private Texture2D emotion_area;
	private Texture2D emotion_end;
	private Texture2D emotion_blend_in;
	private Texture2D emotion_blend_out;
	private Texture2D emotion_error;
	private Texture2D emotion_end_hover;
	private Texture2D emotion_end_selected;
	private Texture2D emotion_blend_start;
	private Texture2D emotion_blend_end;

	private Texture2D gesture_normal;
	private Texture2D gesture_hover;
	private Texture2D gesture_selected;

	private Texture2D preview_bar;
	private Texture2D preview_icon;
	#endregion

	public List<PhonemeMarker> phonemeData = new List<PhonemeMarker>();
	public List<GestureMarker> gestureData = new List<GestureMarker>();

	public List<EmotionMarker> emotionData = new List<EmotionMarker>();
	public List<EmotionMarker> unorderedEmotionData = new List<EmotionMarker>();

	private float m_fileLength = 10;
	public float fileLength
	{
		get
		{
			return m_fileLength;
		}
		set
		{
			m_fileLength = value;
			float thumbSize = viewportEnd - viewportStart;
			viewportEnd = Mathf.Clamp(viewportEnd, 0, value);
			viewportStart = Mathf.Clamp(viewportEnd - thumbSize, 0, viewportEnd - 0.5f);
		}
	}
	public string transcript = "";

	private Rect previewRect;

	#region Editor Settings Variables
	public LipSyncProject settings;
	private string[] languageModelNames;
	private bool settingsOpen = false;
	private Vector2 settingsScroll;
	private int settingsTab = 0;

	private bool visualPreview = false;
	private LipSync previewTarget = null;
	public bool previewOutOfDate = true;

	private bool useColors;
	private int defaultColor;

	private int defaultLanguageModel;
	private bool soXAvailable;
	private string soXPath;
	private bool allphone_ciEnabled;
	private bool backtraceEnabled;
	private int beamExponent;
	private int pbeamExponent;
	private float lwValue;
	private bool doCleanup;
	private float cleanupAggression;

	private bool advancedOptions = false;
	private AnimBool advancedBool;
	private AnimBool cleanupBool;

	public float defaultEmotionIntensity;
	public bool defaultContinuousVariation;
	public float defaultVariationFrequency;
	public float defaultIntensityVariation;
	public float defaultBlendableVariation;
	public float defaultBonePositionVariation;
	public float defaultBoneRotationVariation;

	public float defaultPhonemeIntensity;
	public bool defaultUseRandomness;
	public float defaultIntensityRandomness;
	public float defaultBlendableRandomness;
	public float defaultBonePositionRandomness;
	public float defaultBoneRotationRandomness;

	private bool continuousUpdate;
	private float snappingDistance;
	private bool snapping;
	private bool setViewportOnLoad;
#if !(UNITY_5_3_6 || UNITY_5_4_OR_NEWER)
	private int maxWaveformWidth;
#endif
	private bool showExtensionsOnLoad;
	private bool showTimeline;
	private float scrubLength;
	private float volume;

	private RDEditorShortcut[] keyboardShortcuts;
	private bool shortcutsChanged;
	#endregion

	#region EditorWindow Life Cycle Methods
	void OnEnable ()
	{
		//Load Resources;
		playhead_top = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Playhead_top.png");
		playhead_line = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Playhead_middle.png");
		playhead_bottom = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Playhead_bottom.png");

		marker_normal = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/marker.png");
		marker_hover = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/marker-selected.png");
		marker_selected = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/marker-highlight.png");
		marker_sustained = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/marker-sustain.png");
		marker_line = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/white.png");
		indicator = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/indicator.png");

		gesture_normal = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/gesture.png");
		gesture_hover = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/gesture-selected.png");
		gesture_selected = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/gesture-highlight.png");

		emotion_start = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-start.png");
		emotion_start_hover = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-start-highlight.png");
		emotion_start_selected = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-start-select.png");
		emotion_area = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-area.png");
		emotion_end = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-end.png");
		emotion_blend_in = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-blend-in.png");
		emotion_blend_out = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-blend-out.png");
		emotion_error = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-error.png");
		emotion_end_hover = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-end-highlight.png");
		emotion_end_selected = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-end-select.png");
		emotion_blend_start = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-blend-start.png");
		emotion_blend_end = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-blend-end.png");

		preview_bar = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/preview-bar.png");
		preview_icon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/preview-icon.png");

		if (!EditorGUIUtility.isProSkin)
		{
			track_top = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/track.png");
			playIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/play.png");
			stopIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/stop.png");
			pauseIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/pause.png");
			loopIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/loop.png");
			settingsIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/settings.png");
			previewIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/eye.png");
		}
		else
		{
			track_top = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/track.png");
			playIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/play.png");
			stopIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/stop.png");
			pauseIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/pause.png");
			loopIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/loop.png");
			settingsIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/settings.png");
			previewIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/eye.png");
		}

		//Get Settings File
		string[] guids = AssetDatabase.FindAssets("ProjectSettings t:LipSyncProject");
		string path = "";

		if (guids.Length > 0)
		{
			path = AssetDatabase.GUIDToAssetPath(guids[0]);

			if (guids.Length > 1)
				Debug.LogWarning("LipSync: Multiple LipSyncProject files found. Only one will be used.");
		}

		settings = (LipSyncProject)AssetDatabase.LoadAssetAtPath(path, typeof(LipSyncProject));

		if (settings == null)
		{
			settings = ScriptableObject.CreateInstance<LipSyncProject>();

			LipSyncProject newSettings = ScriptableObject.CreateInstance<LipSyncProject>();
			newSettings.emotions = new string[] { "default" };
			newSettings.emotionColors = new Color[] { new Color(1f, 0.7f, 0.1f) };

			EditorUtility.CopySerialized(newSettings, settings);
			AssetDatabase.CreateAsset(settings, "Assets/Rogo Digital/LipSync/ProjectSettings.asset");
			AssetDatabase.Refresh();
			DestroyImmediate(newSettings);
		}

		phonemeSetNames = new string[settings.phonemeSet.phonemes.Length];
		for (int i = 0; i < settings.phonemeSet.phonemes.Length; i++)
		{
			phonemeSetNames[i] = settings.phonemeSet.phonemes[i].name;
		}

		//Get Editor Settings
		continuousUpdate = EditorPrefs.GetBool("LipSync_ContinuousUpdate", true);

		useColors = EditorPrefs.GetBool("LipSync_UseColors", true);
		defaultColor = EditorPrefs.GetInt("LipSync_DefaultColor", 0xAAAAAA);
		setViewportOnLoad = EditorPrefs.GetBool("LipSync_SetViewportOnLoad", true);
		showTimeline = EditorPrefs.GetBool("LipSync_ShowTimeline", true);
		showExtensionsOnLoad = EditorPrefs.GetBool("LipSync_ShowExtensionsOnLoad", true);
#if !(UNITY_5_3_6 || UNITY_5_4_OR_NEWER)
		maxWaveformWidth = EditorPrefs.GetInt("LipSync_MaxWaveformWidth", 2048);
#endif
		scrubLength = EditorPrefs.GetFloat("LipSync_ScrubLength", 0.075f);
		volume = EditorPrefs.GetFloat("LipSync_Volume", 1f);

		defaultPhonemeIntensity = EditorPrefs.GetFloat("LipSync_DefaultPhonemeIntensity", 1f);
		defaultUseRandomness = EditorPrefs.GetBool("LipSync_DefaultUseRandomness", false);
		defaultIntensityRandomness = EditorPrefs.GetFloat("LipSync_DefaultIntensityRandomness", 0.1f);
		defaultBlendableRandomness = EditorPrefs.GetFloat("LipSync_DefaultBlendableRandomness", 0.3f);
		defaultBonePositionRandomness = EditorPrefs.GetFloat("LipSync_DefaultBonePositionRandomness", 0.3f);
		defaultBoneRotationRandomness = EditorPrefs.GetFloat("LipSync_DefaultBoneRotationRandomness", 0.3f);

		defaultEmotionIntensity = EditorPrefs.GetFloat("LipSync_DefaultEmotionIntensity", 1f);
		defaultContinuousVariation = EditorPrefs.GetBool("LipSync_DefaultContinuousVariation", false);
		defaultVariationFrequency = EditorPrefs.GetFloat("LipSync_DefaultVariationFrequency", 0.5f);
		defaultIntensityVariation = EditorPrefs.GetFloat("LipSync_DefaultIntensityVariation", 0.35f);
		defaultBlendableVariation = EditorPrefs.GetFloat("LipSync_DefaultBlendableVariation", 0.1f);
		defaultBonePositionVariation = EditorPrefs.GetFloat("LipSync_DefaultBonePositionVariation", 0.1f);
		defaultBoneRotationVariation = EditorPrefs.GetFloat("LipSync_DefaultBoneRotationVariation", 0.1f);

		defaultLanguageModel = EditorPrefs.GetInt("LipSync_DefaultLanguageModel", 0);
		soXAvailable = EditorPrefs.GetBool("LipSync_SoXAvailable", false);
		soXPath = EditorPrefs.GetString("LipSync_SoXPath", "");
		allphone_ciEnabled = EditorPrefs.GetBool("LipSync_Allphone_ciEnabled", true);
		backtraceEnabled = EditorPrefs.GetBool("LipSync_BacktraceEnabled", false);
		beamExponent = EditorPrefs.GetInt("LipSync_BeamExponent", -20);
		pbeamExponent = EditorPrefs.GetInt("LipSync_PbeamExponent", -20);
		lwValue = EditorPrefs.GetFloat("LipSync_LwValue", 2.5f);
		doCleanup = EditorPrefs.GetBool("LipSync_DoCleanup", false);
		cleanupAggression = EditorPrefs.GetFloat("LipSync_CleanupAggression", 0.003f);

		cleanupBool = new AnimBool(doCleanup);
		cleanupBool.valueChanged.AddListener(Repaint);
		advancedBool = new AnimBool(advancedOptions);
		advancedBool.valueChanged.AddListener(Repaint);

		if (EditorPrefs.HasKey("LipSync_Snapping"))
		{
			if (EditorPrefs.GetBool("LipSync_Snapping"))
			{
				if (EditorPrefs.HasKey("LipSync_SnappingDistance"))
				{
					snappingDistance = EditorPrefs.GetFloat("LipSync_SnappingDistance");
					snapping = true;
				}
				else
				{
					snappingDistance = 0;
					snapping = false;
				}
			}
			else
			{
				snappingDistance = 0;
				snapping = false;
			}
		}
		else
		{
			snappingDistance = 0;
			snapping = false;
		}

		// Define Rebindable Actions
		shortcutActions = new List<RDEditorShortcut.Action> {
			new RDEditorShortcut.Action("File/New File", OnNewClick),
			new RDEditorShortcut.Action("File/Open File", OnLoadClick),
			new RDEditorShortcut.Action("File/Import XML", OnXMLImport),
			new RDEditorShortcut.Action("File/Save", OnSaveClick),
			new RDEditorShortcut.Action("File/Save As", OnSaveAsClick),
			new RDEditorShortcut.Action("File/Export (Unitypackage)", OnUnityExport),
			new RDEditorShortcut.Action("File/Export (XML)", OnXMLExport),
			new RDEditorShortcut.Action("Other/Show Project Settings", ShowProjectSettings),
			new RDEditorShortcut.Action("Other/Close Window", Close),
			new RDEditorShortcut.Action("Edit/Select All", SelectAll),
			new RDEditorShortcut.Action("Edit/Select None", SelectNone),
			new RDEditorShortcut.Action("Edit/Invert Selection", InvertSelection),
			new RDEditorShortcut.Action("Edit/Show Clip Settings", ClipSettings),
			new RDEditorShortcut.Action("AutoSync/AutoSync (Default Settings)", StartAutoSyncDefault),
			new RDEditorShortcut.Action("AutoSync/AutoSync (High Quality Settings)", StartAutoSyncHQ),
			new RDEditorShortcut.Action("AutoSync/AutoSync (Custom Settings)", ()=> { OpenAutoSyncWindow(0); }),
			new RDEditorShortcut.Action("AutoSync/AutoSync Batch Process", ()=> { OpenAutoSyncWindow(1); }),
			new RDEditorShortcut.Action("Other/Open Extensions Window", RDExtensionWindow.ShowWindow),
			new RDEditorShortcut.Action("Playback/Seek Backwards 1%", ()=> { seekPosition -= 0.01f; }),
			new RDEditorShortcut.Action("Playback/Seek Forwards 1%", ()=> { seekPosition += 0.01f; }),
			new RDEditorShortcut.Action("Other/Go To Phonemes Tab", ()=> { markerTab = 0; }),
			new RDEditorShortcut.Action("Other/Go To Emotions Tab", ()=> { markerTab = 1; }),
			new RDEditorShortcut.Action("Other/Go To Gestures Tab", ()=> { markerTab = 2; }),
			new RDEditorShortcut.Action("Other/Toggle Settings Page", ()=> { settingsOpen = !settingsOpen; }),
			new RDEditorShortcut.Action("Playback/Play\\Pause", PlayPause),
			new RDEditorShortcut.Action("Playback/Stop", Stop),
			new RDEditorShortcut.Action("Playback/Loop", ()=> { looping = !looping; })
		};

		// Add Phonemes
		for (int p = 0; p < settings.phonemeSet.phonemes.Length; p++)
		{
			int temp = p;
			shortcutActions.Add(new RDEditorShortcut.Action("Insert/Add " + settings.phonemeSet.phonemes[p].name + " Phoneme", () => { PhonemePicked(new object[] { temp, seekPosition }); }));
		}

		// Later-added actions are defined here, to avoid shuffling existing shortcuts around.
		shortcutActions.Add(new RDEditorShortcut.Action("Edit/Copy", CopyMarkers));
		shortcutActions.Add(new RDEditorShortcut.Action("Edit/Paste", () => { PasteMarkers(seekPosition); }));

		// Set Shortcuts
		keyboardShortcuts = RDEditorShortcut.Deserialize("LipSyncPro", shortcutActions, keyboardShortcuts);
		if (keyboardShortcuts == null)
			SetDefaultShortcuts();

		if (languageModelNames == null)
		{
			languageModelNames = AutoSyncLanguageModel.FindModels();
		}

		SceneView.onSceneGUIDelegate += OnSceneGUI;

		selection = new List<int>();
		selectionOffsets = new float[0];
		oldPos = this.position;
		oldClip = clip;
	}

	private void SetDefaultShortcuts ()
	{
		keyboardShortcuts = new RDEditorShortcut[] {
			new RDEditorShortcut(9, KeyCode.A, EventModifiers.Control),
			new RDEditorShortcut(3, KeyCode.S, EventModifiers.Control),
			new RDEditorShortcut(18, KeyCode.Comma, EventModifiers.Shift),
			new RDEditorShortcut(19, KeyCode.Period, EventModifiers.Shift),
			new RDEditorShortcut(27, KeyCode.A, (EventModifiers.Control | EventModifiers.Shift)),
			new RDEditorShortcut(28, KeyCode.E, (EventModifiers.Control | EventModifiers.Shift)),
			new RDEditorShortcut(29, KeyCode.U, (EventModifiers.Control | EventModifiers.Shift)),
			new RDEditorShortcut(30, KeyCode.O, (EventModifiers.Control | EventModifiers.Shift)),
			new RDEditorShortcut(31, KeyCode.C, (EventModifiers.Control | EventModifiers.Shift)),
			new RDEditorShortcut(32, KeyCode.F, (EventModifiers.Control | EventModifiers.Shift)),
			new RDEditorShortcut(33, KeyCode.L, (EventModifiers.Control | EventModifiers.Shift)),
			new RDEditorShortcut(34, KeyCode.M, (EventModifiers.Control | EventModifiers.Shift)),
			new RDEditorShortcut(35, KeyCode.W, (EventModifiers.Control | EventModifiers.Shift)),
			new RDEditorShortcut(36, KeyCode.C, EventModifiers.Control),
			new RDEditorShortcut(37, KeyCode.V, EventModifiers.Control),
		};
	}

	void Update ()
	{
		float deltaTime = Time.realtimeSinceStartup - prevTime;
		prevTime = Time.realtimeSinceStartup;

		if (clip != null)
		{
			isPlaying = AudioUtility.IsClipPlaying(clip);
		}

		if (isPlaying && !isPaused)
		{
			if ((seekPosition * (fileLength * (position.width / (viewportEnd - viewportStart)))) > position.width + (viewportStart * (position.width / (viewportEnd - viewportStart))))
			{
				float viewportSeconds = viewportEnd - viewportStart;
				viewportStart = seekPosition * fileLength;
				viewportEnd = viewportStart + viewportSeconds;
			}
			else if ((seekPosition * (fileLength * (position.width / (viewportEnd - viewportStart)))) < viewportStart * (position.width / (viewportEnd - viewportStart)))
			{
				float viewportSeconds = viewportEnd - viewportStart;
				viewportStart = seekPosition * fileLength;
				viewportEnd = viewportStart + viewportSeconds;
			}
		}

		//Check for clip change
		if (oldClip != clip)
		{
			Undo.RecordObject(this, "Change AudioClip");
#if !(UNITY_5_3_6 || UNITY_5_4_OR_NEWER)
			DestroyImmediate(waveform);
#endif
			oldClip = clip;
			if (setViewportOnLoad)
			{
				if (clip)
					fileLength = clip.length;
				viewportEnd = fileLength;
				viewportStart = 0;
			}
		}

		//Check for resize;
		if (oldPos.width != this.position.width || oldPos.height != this.position.height)
		{
			oldPos = this.position;
#if !(UNITY_5_3_6 || UNITY_5_4_OR_NEWER)
			if (clip) DestroyImmediate(waveform);
#endif
		}

		//Check for Seek Position change
		if (oldSeekPosition != seekPosition)
		{
			oldSeekPosition = seekPosition;

			if (!isPlaying || isPaused)
			{
				if (!previewing && clip != null)
				{
					AudioUtility.PlayClip(clip);
				}
				previewing = true;
				stopTimer = scrubLength;
				prevTime = Time.realtimeSinceStartup;
				resetTime = seekPosition;

				FixEmotionBlends();
			}

			if (clip)
				AudioUtility.SetClipSamplePosition(clip, (int)(seekPosition * AudioUtility.GetSampleCount(clip)));
		}

		if (isPlaying && !isPaused && clip != null && focusedWindow == this || continuousUpdate && focusedWindow == this)
		{
			this.Repaint();
		}

		if (clip != null)
		{
			seekPosition = AudioUtility.GetClipPosition(clip) / fileLength;

			if (seekPosition >= 0.999f && looping)
			{
				AudioUtility.PlayClip(clip);
				AudioUtility.SetClipSamplePosition(clip, 0);
			}
		}
		else if (isPlaying && !isPaused)
		{
			seekPosition += deltaTime / fileLength;

			if (seekPosition >= 1)
			{
				if (looping)
				{
					seekPosition = 0;
					oldSeekPosition = 0;
				}
				else
				{
					isPlaying = false;
					seekPosition = 0;
				}
			}
		}

		oldSeekPosition = seekPosition;

		if (previewing)
		{

			stopTimer -= deltaTime;

			if (stopTimer <= 0)
			{
				previewing = false;
				isPaused = true;
				seekPosition = resetTime;
				oldSeekPosition = seekPosition;
				if (clip != null)
				{
					AudioUtility.PauseClip(clip);
					AudioUtility.SetClipSamplePosition(clip, (int)(seekPosition * AudioUtility.GetSampleCount(clip)));
				}
			}
		}
#if !(UNITY_5_3_6 || UNITY_5_4_OR_NEWER)
		if (waveform == null && waveformHeight > 0 && clip != null) {
			waveform = AudioUtility.GetWaveForm(clip, 0, (int)Mathf.Clamp(fileLength * (position.width / (viewportEnd - viewportStart)), 0, maxWaveformWidth), (position.height - waveformHeight) - 18);
			Repaint();
		}
#endif
		if (isPlaying && !isPaused && visualPreview || previewing && visualPreview || previewOutOfDate && visualPreview)
		{
			UpdatePreview(seekPosition);
		}
	}

	void OnDisable ()
	{
		if (previewTarget != null)
		{
			UpdatePreview(0);
			previewTarget = null;
		}
	}

	void OnDestroy ()
	{
		// Save file and offer to save changes.
		AudioUtility.StopAllClips();
		SceneView.onSceneGUIDelegate -= OnSceneGUI;

		if (changed)
		{
			string oldName = fileName;
			string oldLastLoad = lastLoad;
			float localOldSeekPosition = seekPosition;
			;
			SaveFile(settings, "Assets/LIPSYNC_AUTOSAVE.asset", false, transcript, fileLength, phonemeData.ToArray(), emotionData.ToArray(), gestureData.ToArray(), clip);
			int choice = EditorUtility.DisplayDialogComplex("Save Changes", "You have made changes to the current file, do you want to save them before closing?", "Yes", "No", "Cancel");

			if (choice == 0)
			{
				OnSaveClick();
				AssetDatabase.DeleteAsset("Assets/LIPSYNC_AUTOSAVE.asset");
			}
			else if (choice == 1)
			{
				AssetDatabase.DeleteAsset("Assets/LIPSYNC_AUTOSAVE.asset");
			}
			else
			{
				ShowWindow("Assets/LIPSYNC_AUTOSAVE.asset", true, oldName, oldLastLoad, markerTab, localOldSeekPosition);
			}
		}
	}
	#endregion

	#region GUI Rendering
	void OnSceneGUI (SceneView sceneView)
	{
		// Draw overlay when realtime preview is enabled
		if (visualPreview)
		{
			Camera cam = sceneView.camera;
			Handles.BeginGUI();

			Rect bottom = new Rect(0, cam.pixelHeight - 3, cam.pixelWidth, 3);
			GUI.DrawTexture(bottom, preview_bar);

			GUI.DrawTexture(new Rect(cam.pixelWidth - 256, cam.pixelHeight - 64, 256, 64), preview_icon);
			Handles.EndGUI();
		}
	}

	public override void OnModalGUI ()
	{
		// Create Styles
		GUIStyle centeredStyle = new GUIStyle(EditorStyles.whiteLabel);
		centeredStyle.alignment = TextAnchor.MiddleCenter;


		#region Top Menu
		//Toolbar
		Rect topToolbarRect = EditorGUILayout.BeginHorizontal();
		GUI.Box(topToolbarRect, "", EditorStyles.toolbar);
		Rect fileRect = EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("File", EditorStyles.toolbarDropDown, GUILayout.Width(60)))
		{
			GenericMenu fileMenu = new GenericMenu();

			fileMenu.AddItem(new GUIContent("New File"), false, OnNewClick);
			fileMenu.AddItem(new GUIContent("Open File"), false, OnLoadClick);
			fileMenu.AddItem(new GUIContent("Import XML"), false, OnXMLImport);
			fileMenu.AddSeparator("");
			if (phonemeData.Count > 0 || emotionData.Count > 0 || gestureData.Count > 0)
			{
				fileMenu.AddItem(new GUIContent("Save"), false, OnSaveClick);
				fileMenu.AddItem(new GUIContent("Save As"), false, OnSaveAsClick);
				fileMenu.AddItem(new GUIContent("Export"), false, OnUnityExport);
				fileMenu.AddItem(new GUIContent("Export XML"), false, OnXMLExport);
			}
			else
			{
				fileMenu.AddDisabledItem(new GUIContent("Save"));
				fileMenu.AddDisabledItem(new GUIContent("Save As"));
				fileMenu.AddDisabledItem(new GUIContent("Export"));
				fileMenu.AddDisabledItem(new GUIContent("Export XML"));
			}
			fileMenu.AddSeparator("");

			if (onDrawFileMenu != null)
			{
				onDrawFileMenu.Invoke(this, fileMenu);
				fileMenu.AddSeparator("");
			}

			fileMenu.AddItem(new GUIContent("Project Settings"), false, ShowProjectSettings);
			fileMenu.AddSeparator("");
			fileMenu.AddItem(new GUIContent("Exit"), false, Close);
			fileMenu.DropDown(fileRect);
		}
		GUILayout.EndHorizontal();
		Rect editRect = EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Edit", EditorStyles.toolbarDropDown, GUILayout.Width(60)))
		{
			GenericMenu editMenu = new GenericMenu();
			if (selection.Count > 0)
			{
				editMenu.AddItem(new GUIContent("Copy"), false, CopyMarkers);
			}
			else
			{
				editMenu.AddDisabledItem(new GUIContent("Copy"));
			}
			if (copyBuffer.Count > 0)
			{
				editMenu.AddItem(new GUIContent("Paste"), false, () => { PasteMarkers(seekPosition); });
			}
			else
			{
				editMenu.AddDisabledItem(new GUIContent("Paste"));
			}
			editMenu.AddSeparator("");
			editMenu.AddItem(new GUIContent("Select All"), false, SelectAll);
			editMenu.AddItem(new GUIContent("Select None"), false, SelectNone);
			editMenu.AddItem(new GUIContent("Invert Selection"), false, InvertSelection);
			editMenu.AddSeparator("");
			if (markerTab == 0)
			{
				if (clip != null)
				{
					editMenu.AddItem(new GUIContent("Set Intensity From Volume"), false, SetIntensitiesVolume);
				}
				else
				{
					editMenu.AddDisabledItem(new GUIContent("Set Intensity From Volume"));
				}

				editMenu.AddItem(new GUIContent("Reset Intensities"), false, ResetIntensities);
			}
			else if (markerTab == 1)
			{
				editMenu.AddItem(new GUIContent("Remove Missing Emotions"), false, RemoveMissingEmotions);
				editMenu.AddItem(new GUIContent("Reset Intensities"), false, ResetIntensities);
			}
			else if (markerTab == 2)
			{
				editMenu.AddItem(new GUIContent("Remove Missing Gestures"), false, RemoveMissingGestures);
			}
			editMenu.AddSeparator("");
			if (markerTab == 0 || markerTab == 1)
				editMenu.AddItem(new GUIContent("Default Marker Settings"), false, DefaultMarkerSettings);
			editMenu.AddItem(new GUIContent("Clip Settings"), false, ClipSettings);
			if (onDrawEditMenu != null)
			{
				editMenu.AddSeparator("");
				onDrawEditMenu.Invoke(this, editMenu);
				editMenu.AddSeparator("");
			}

			editMenu.DropDown(editRect);
		}
		GUILayout.EndHorizontal();
		Rect autoRect = EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("AutoSync", EditorStyles.toolbarDropDown, GUILayout.Width(70)))
		{
			GenericMenu autoMenu = new GenericMenu();

			autoMenu.AddDisabledItem(new GUIContent("AutoSync 2"));
			autoMenu.AddDisabledItem(new GUIContent("Powered by CMU Sphinx"));
			autoMenu.AddSeparator("");
			if (clip != null)
			{
				autoMenu.AddItem(new GUIContent("Start (Default Settings)"), false, StartAutoSyncDefault);
				autoMenu.AddItem(new GUIContent("Start (High Quality Settings)"), false, StartAutoSyncHQ);
				autoMenu.AddItem(new GUIContent("Custom Settings"), false, OpenAutoSyncWindow, 0);
			}
			else
			{
				autoMenu.AddDisabledItem(new GUIContent("Start (Default Settings)"));
				autoMenu.AddDisabledItem(new GUIContent("Start (High Quality Settings)"));
				autoMenu.AddDisabledItem(new GUIContent("Custom Settings"));
			}
			autoMenu.AddSeparator("");
			autoMenu.AddItem(new GUIContent("Batch Process"), false, OpenAutoSyncWindow, 1);
			autoMenu.DropDown(autoRect);
		}
		GUILayout.EndHorizontal();
		Rect helpRect = EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Help", EditorStyles.toolbarDropDown, GUILayout.Width(60)))
		{
			GenericMenu helpMenu = new GenericMenu();

			helpMenu.AddDisabledItem(new GUIContent("LipSync " + versionNumber));
			helpMenu.AddDisabledItem(new GUIContent("© Rogo Digital " + DateTime.Now.Year.ToString()));
			helpMenu.AddSeparator("");
			helpMenu.AddItem(new GUIContent("Get LipSync Extensions"), false, RDExtensionWindow.ShowWindowGeneric, "LipSync_Pro");
			helpMenu.AddSeparator("");
			helpMenu.AddItem(new GUIContent("Forum Thread"), false, OpenURL, "http://forum.unity3d.com/threads/beta-lipsync-a-flexible-lipsyncing-and-facial-animation-system.309324/");
			helpMenu.AddItem(new GUIContent("Email Support"), false, OpenURL, "mailto:contact@rogodigital.com");
			if (onDrawHelpMenu != null)
			{
				helpMenu.AddSeparator("");
				onDrawHelpMenu.Invoke(this, helpMenu);
				helpMenu.AddSeparator("");
			}
			helpMenu.DropDown(helpRect);
		}
		GUILayout.EndHorizontal();
		if (onDrawTopMenuBar != null)
			onDrawTopMenuBar.Invoke(this);
		GUILayout.FlexibleSpace();
		if (changed == true)
		{
			GUILayout.Box(fileName + "*", EditorStyles.label);
		}
		else
		{
			GUILayout.Box(fileName, EditorStyles.label);
		}
		GUILayout.FlexibleSpace();

		settingsOpen = GUILayout.Toggle(settingsOpen, new GUIContent(settingsIcon, "Settings"), EditorStyles.toolbarButton, GUILayout.MaxWidth(40));

		bool newPreviewState = GUILayout.Toggle(visualPreview, new GUIContent(previewIcon, "Realtime Preview"), EditorStyles.toolbarButton, GUILayout.MaxWidth(40));
		if (visualPreview != newPreviewState)
		{
			GenericMenu previewMenu = new GenericMenu();

			previewMenu.AddDisabledItem(new GUIContent("Choose a target"));

			LipSync[] targets = GameObject.FindObjectsOfType<LipSync>();

			previewMenu.AddItem(new GUIContent("No Preview"), !visualPreview, TargetChosen, null);
			foreach (LipSync t in targets)
			{
				previewMenu.AddItem(new GUIContent(t.name), previewTarget == t ? true : false, TargetChosen, t);
			}

			previewMenu.ShowAsContext();
		}

		GUILayout.Space(20);
		GUILayout.Box(versionNumber, EditorStyles.label);
		GUILayout.Box("", EditorStyles.toolbar);
		EditorGUILayout.EndHorizontal();
		#endregion

		// Keyboard Shortcuts
		if (Event.current.type == EventType.KeyUp)
		{
			Event e = Event.current;

			for (int i = 0; i < keyboardShortcuts.Length; i++)
			{
				if (e.keyCode == keyboardShortcuts[i].key && e.modifiers == keyboardShortcuts[i].modifiers)
				{
					shortcutActions[keyboardShortcuts[i].action].action.Invoke();
					e.Use();
					break;
				}
			}
		}

		#region Settings Screen
		if (settingsOpen)
		{
			//Settings Screen
			GUILayout.Space(10);
			LipSyncEditorExtensions.BeginPaddedHorizontal(20);
			settingsTab = GUILayout.Toolbar(settingsTab, new string[] { "General", "AutoSync", "Keyboard Shortcuts" }, GUILayout.MaxWidth(700));
			LipSyncEditorExtensions.EndPaddedHorizontal(20);
			settingsScroll = GUILayout.BeginScrollView(settingsScroll);
			GUILayout.Space(10);
			switch (settingsTab)
			{
				case 0:
					GUILayout.Box("General Settings", EditorStyles.boldLabel);
					bool oldUpdate = continuousUpdate;
					continuousUpdate = GUILayout.Toggle(continuousUpdate, new GUIContent("Continuous Update", "Whether to update the window every frame. This makes editing more responsive, but may be taxing on low-powered systems."));
					if (oldUpdate != continuousUpdate)
					{
						EditorPrefs.SetBool("LipSync_ContinuousUpdate", continuousUpdate);
					}

					bool oldSetViewportOnLoad = setViewportOnLoad;
					setViewportOnLoad = GUILayout.Toggle(setViewportOnLoad, new GUIContent("Set Viewport on File Load", "Whether to set the viewport to show the entire clip when a new file is loaded."));
					if (oldSetViewportOnLoad != setViewportOnLoad)
					{
						EditorPrefs.SetBool("LipSync_SetViewportOnLoad", setViewportOnLoad);
					}

					bool oldShowTimeline = showTimeline;
					showTimeline = GUILayout.Toggle(showTimeline, new GUIContent("Show Time Markers", "Whether to show time markers under the timeline."));
					if (oldShowTimeline != showTimeline)
					{
						EditorPrefs.SetBool("LipSync_ShowTimeline", showTimeline);
					}

					float oldScrubLength = scrubLength;
					scrubLength = EditorGUILayout.FloatField(new GUIContent("Scrubbing Preview Length", "The duration, in seconds, the clip will be played for when scrubbing."), scrubLength, GUILayout.MaxWidth(400));
					if (oldScrubLength != scrubLength)
					{
						EditorPrefs.SetFloat("LipSync_ScrubLength", scrubLength);
					}

					float oldVolume = volume;
					volume = EditorGUILayout.Slider(new GUIContent("Preview Volume"), volume, 0, 1, GUILayout.MaxWidth(400));
					if (oldVolume != volume)
					{
						EditorPrefs.SetFloat("LipSync_Volume", volume);
						AudioUtility.SetVolume(volume);
					}
					GUILayout.Space(10);
#if !(UNITY_5_3_6 || UNITY_5_4_OR_NEWER)
					int oldMaxWaveformWidth = maxWaveformWidth;
					maxWaveformWidth = EditorGUILayout.IntField(new GUIContent("Max Waveform Width", "The Maximum width for the waveform preview image. Warning: very high values can cause crashes when zooming in on the clip."), maxWaveformWidth, GUILayout.MaxWidth(400));
					if (oldMaxWaveformWidth != maxWaveformWidth) {
						EditorPrefs.SetInt("LipSync_MaxWaveformWidth", maxWaveformWidth);
					}
#endif
					bool oldShowExtensionsOnLoad = showExtensionsOnLoad;
					showExtensionsOnLoad = GUILayout.Toggle(showExtensionsOnLoad, new GUIContent("Show Extensions Window", "Whether to automatically dock an extensions window to this one when it is opened."));
					if (oldShowExtensionsOnLoad != showExtensionsOnLoad)
					{
						EditorPrefs.SetBool("LipSync_ShowExtensionsOnLoad", showExtensionsOnLoad);
					}
					GUILayout.Space(15);
					GUILayout.Box("Emotion Editing", EditorStyles.boldLabel);
					EditorGUILayout.BeginHorizontal();
					bool oldsnapping = snapping;
					snapping = GUILayout.Toggle(snapping, "Emotion Snapping");
					if (oldsnapping != snapping)
					{
						EditorPrefs.SetBool("LipSync_Snapping", snapping);
						snappingDistance = 0;
					}
					if (snapping)
					{
						GUILayout.Space(10);
						float oldSnappingDistance = snappingDistance;
						snappingDistance = EditorGUILayout.Slider(new GUIContent("Snapping Distance", "The strength of the emotion snapping."), (snappingDistance * 200), 0, 10) / 200;
						GUILayout.FlexibleSpace();
						if (oldSnappingDistance != snappingDistance)
						{
							EditorPrefs.SetFloat("LipSync_SnappingDistance", snappingDistance);
						}
					}
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					bool oldcolors = useColors;
					useColors = GUILayout.Toggle(useColors, "Use Emotion Colors");
					if (oldcolors != useColors)
					{
						EditorPrefs.SetBool("LipSync_UseColors", useColors);
					}
					if (!useColors)
					{
						GUILayout.Space(10);
						int oldColour = defaultColor;
						defaultColor = ColorToHex(EditorGUILayout.ColorField("Default Color", HexToColor(defaultColor)));
						GUILayout.FlexibleSpace();
						if (oldColour != defaultColor)
						{
							EditorPrefs.SetInt("LipSync_DefaultColor", defaultColor);
						}
					}
					EditorGUILayout.EndHorizontal();
					break;
				case 1:
					GUILayout.Box("Default AutoSync Settings", EditorStyles.boldLabel);
					if (languageModelNames.Length > 0)
					{
						int oldLanguageModel = defaultLanguageModel;
						defaultLanguageModel = EditorGUILayout.Popup("Default Language Model", defaultLanguageModel, languageModelNames, GUILayout.MaxWidth(400));
						if (oldLanguageModel != defaultLanguageModel)
						{
							EditorPrefs.SetInt("LipSync_DefaultLanguageModel", defaultLanguageModel);
						}
					}
					else
					{
						EditorGUILayout.HelpBox("No language models found. You can download language models from the extensions window or the LipSync website.", MessageType.Error);
					}
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("SoX Path");
					GUILayout.Space(5);
					GUI.color = soXAvailable ? Color.green : Color.red;
					GUILayout.Box(new GUIContent(indicator, soXAvailable ? "SoX is installed. Audio conversion available." : "SoX is not installed."), GUIStyle.none);
					GUI.color = Color.white;
					GUILayout.Space(10);
					soXPath = EditorGUILayout.TextField(soXPath);
					if (GUILayout.Button("Browse"))
					{
						string path = EditorUtility.OpenFilePanel("Find SoX Application", "", "");
						if (!string.IsNullOrEmpty(path))
							soXPath = path;
					}
					GUILayout.Space(5);
					if (GUILayout.Button("Verify"))
					{
						EditorPrefs.SetString("LipSync_SoXPath", soXPath);

						soXAvailable = AutoSync.CheckSoX();
						EditorPrefs.SetBool("LipSync_SoXAvailable", soXAvailable);

						if (soXAvailable)
						{
							ShowNotification(new GUIContent("Verification Successful. SoX is installed."));
						}
						else
						{
							ShowNotification(new GUIContent("Verification Failed. Incorrect File."));
						}
					}
					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();
					bool oldDoCLeanup = doCleanup;
					doCleanup = EditorGUILayout.Toggle(new GUIContent("Enable Auto Cleanup", "Will attempt to improve quality by removing markers close to other markers"), doCleanup);
					cleanupBool.target = doCleanup;
					if (oldDoCLeanup != doCleanup)
					{
						EditorPrefs.SetBool("LipSync_DoCleanup", showExtensionsOnLoad);
					}
					if (EditorGUILayout.BeginFadeGroup(cleanupBool.faded))
					{
						float oldCleanupAggression = cleanupAggression;
						cleanupAggression = EditorGUILayout.Slider(new GUIContent("Cleanup Aggression", "How aggressive auto cleanup is. VERY low values recommended."), cleanupAggression, 0, 1, GUILayout.MaxWidth(400));

						if (oldCleanupAggression != cleanupAggression)
						{
							EditorPrefs.SetFloat("LipSync_CleanupAggression", cleanupAggression);
						}
					}
					EditorGUILayout.EndFadeGroup();
					GUILayout.Space(10);
					advancedOptions = EditorGUILayout.Toggle(new GUIContent("Show Advanced Options", "Warning: These options can cause more problems than they solve if set wrong!"), advancedOptions);
					advancedBool.target = advancedOptions;
					if (EditorGUILayout.BeginFadeGroup(advancedBool.faded))
					{
						GUILayout.Label("Advanced", EditorStyles.boldLabel);
						EditorGUILayout.HelpBox("Leave these options alone unless you know what you're doing! Incorrect settings here can seriously reduce AutoSync quality.", MessageType.Warning);
						EditorGUI.BeginChangeCheck();
						allphone_ciEnabled = EditorGUILayout.Toggle("Enable Allphone_ci", allphone_ciEnabled);
						backtraceEnabled = EditorGUILayout.Toggle("Enable Backtrace", backtraceEnabled);
						beamExponent = EditorGUILayout.IntField("Beam Exponent", beamExponent, GUILayout.MaxWidth(400));
						pbeamExponent = EditorGUILayout.IntField("Pbeam Exponent", pbeamExponent, GUILayout.MaxWidth(400));
						lwValue = EditorGUILayout.FloatField("Lw Value", lwValue, GUILayout.MaxWidth(400));

						if (EditorGUI.EndChangeCheck())
						{
							EditorPrefs.SetBool("LipSync_Allphone_ciEnabled", allphone_ciEnabled);
							EditorPrefs.SetBool("LipSync_BacktraceEnabled", backtraceEnabled);
							EditorPrefs.SetInt("LipSync_BeamExponent", beamExponent);
							EditorPrefs.SetInt("LipSync_PbeamExponent", pbeamExponent);
							EditorPrefs.SetFloat("LipSync_LwValue", lwValue);
						}
					}
					EditorGUILayout.EndFadeGroup();
					break;
				case 2:
					GUILayout.Box("Keyboard Shortcut Rebinding", EditorStyles.boldLabel);
					GUILayout.Space(7);
					if (shortcutsChanged)
						EditorGUILayout.HelpBox("You have made changes to the keyboard shortcuts. Press Save Shortcuts to avoid losing them.", MessageType.Warning);
					GUILayout.Space(7);
					if (keyboardShortcuts.Length == 0)
						GUILayout.Box("No Keyboard Shortcuts!", EditorStyles.centeredGreyMiniLabel);

					for (int i = 0; i < keyboardShortcuts.Length; i++)
					{
						Rect lineRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(25));
						if (i % 2 == 0)
						{
							GUI.Box(lineRect, "", (GUIStyle)"hostview");
						}
						GUILayout.Space(10);
						GUILayout.Label("Action");
						EditorGUI.BeginChangeCheck();
						keyboardShortcuts[i].action = EditorGUILayout.Popup(keyboardShortcuts[i].action, Array.ConvertAll(shortcutActions.ToArray(), item => (string)item));
						GUILayout.FlexibleSpace();
						GUILayout.Label("Shortcut");
						//todo: Clean this up, it feels hacky.
						keyboardShortcuts[i].modifiers = (EventModifiers)((int)(EventModifiers)EditorGUILayout.EnumMaskField((EventModifiers)((int)keyboardShortcuts[i].modifiers << 1)) >> 1);
						GUILayout.Space(5);
						keyboardShortcuts[i].key = (KeyCode)EditorGUILayout.EnumPopup(keyboardShortcuts[i].key);
						if (EditorGUI.EndChangeCheck())
						{
							shortcutsChanged = true;
						}
						GUILayout.Space(5);
						if (GUILayout.Button("X"))
						{
							RDEditorShortcut[] newArray = new RDEditorShortcut[keyboardShortcuts.Length - 1];
							int b = 0;
							for (int a = 0; a < keyboardShortcuts.Length; a++)
							{
								if (a != i)
								{
									newArray[b] = keyboardShortcuts[a];
									b++;
								}
							}
							shortcutsChanged = true;
							keyboardShortcuts = newArray;
							break;
						}
						GUILayout.Space(10);
						EditorGUILayout.EndHorizontal();
					}
					GUILayout.Space(5);
					EditorGUILayout.BeginHorizontal();
					GUILayout.Space(53);
					if (GUILayout.Button("Add Shortcut", GUILayout.MaxWidth(200)))
					{
						GenericMenu menu = new GenericMenu();
						for (int i = 0; i < shortcutActions.Count; i++)
						{
							menu.AddItem(new GUIContent(shortcutActions[i].name), false, (object choice) =>
							{
								RDEditorShortcut[] newArray = new RDEditorShortcut[keyboardShortcuts.Length + 1];
								for (int a = 0; a < keyboardShortcuts.Length; a++)
								{
									newArray[a] = keyboardShortcuts[a];
								}
								newArray[newArray.Length - 1] = new RDEditorShortcut((int)choice, KeyCode.None, EventModifiers.None);
								shortcutsChanged = true;
								keyboardShortcuts = newArray;
							}, i);
						}
						menu.ShowAsContext();
					}
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Save Shortcuts"))
					{
						shortcutsChanged = false;
						RDEditorShortcut.Serialize("LipSyncPro", keyboardShortcuts);
					}

					if (GUILayout.Button("Revert to Saved"))
					{
						shortcutsChanged = false;
						SetDefaultShortcuts();
						keyboardShortcuts = RDEditorShortcut.Deserialize("LipSyncPro", shortcutActions, keyboardShortcuts);
					}
					GUILayout.Space(10);
					EditorGUILayout.EndHorizontal();
					if (shortcutsChanged)
						EditorGUILayout.HelpBox("You have made changes to the keyboard shortcuts. Press Save Shortcuts to avoid losing them.", MessageType.Warning);
					break;
			}
			GUILayout.Space(20);
			GUILayout.EndScrollView();
			return;
		}
		#endregion

		// Main Body
		GUILayout.Space(15);
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Space(20);
		EditorGUI.BeginChangeCheck();
		clip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", clip, typeof(AudioClip), false, GUILayout.MaxWidth(800));

		// Regenerate Waveform on 5.3 & <
		if (EditorGUI.EndChangeCheck())
		{
#if !(UNITY_5_3_6 || UNITY_5_4_OR_NEWER)
			DestroyImmediate(waveform);
#endif
			changed = true;
		}
		GUILayout.Space(20);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Space(15);

		// Define input variables
		float viewportSeconds = (viewportEnd - viewportStart);
		float pixelsPerSecond = position.width / viewportSeconds;
		float mouseX = Event.current.mousePosition.x;
		float mouseY = Event.current.mousePosition.y;

		// Tab Control
		int oldTab = markerTab;
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Space(20);
		markerTab = GUILayout.Toolbar(markerTab, new string[] { "Phonemes", "Emotions", "Gestures" }, GUILayout.MaxWidth(800));
		GUILayout.Space(20);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		if (oldTab != markerTab)
		{
			selection.Clear();
			firstSelection = 0;
		}
		GUILayout.Space(40);

		// Waveform Visulization
		GUILayout.Box("", (GUIStyle)"PreBackground", GUILayout.Width(position.width), GUILayout.Height((position.height - waveformHeight) - 18));
		if (Event.current.type == EventType.Repaint)
			previewRect = GUILayoutUtility.GetLastRect(); // Only set previewRect dimensions on Repaint, GetLastRect is incorrect on Layout.

		// Right Click Menu
		if (Event.current.type == EventType.ContextClick && previewRect.Contains(Event.current.mousePosition))
		{
			GenericMenu previewMenu = new GenericMenu();
			float cursorTime = (viewportStart + (Event.current.mousePosition.x / pixelsPerSecond)) / fileLength;

			if (markerTab == 0)
			{
				// Phonemes
				for (int a = 0; a < settings.phonemeSet.phonemes.Length; a++)
				{
					previewMenu.AddItem(new GUIContent("Add Marker Here/" + settings.phonemeSet.phonemes[a].name), false, PhonemePicked, new object[] { a, cursorTime });
				}
			}
			else if (markerTab == 1)
			{
				// Emotions
				for (int a = 0; a < settings.emotions.Length; a++)
				{
					string emote = settings.emotions[a];
					previewMenu.AddItem(new GUIContent("Add Marker Here/" + emote), false, EmotionPicked, new object[] { emote, cursorTime });
				}
				previewMenu.AddSeparator("Add Marker Here/");
				previewMenu.AddItem(new GUIContent("Add Marker Here/Add New Mixer"), false, AddEmotionMixer, cursorTime);
				previewMenu.AddItem(new GUIContent("Add Marker Here/Add\\Remove Emotions"), false, ShowProjectSettings);
			}
			else if (markerTab == 2)
			{
				// Gestures
				for (int a = 0; a < settings.gestures.Count; a++)
				{
					string gesture = settings.gestures[a];
					previewMenu.AddItem(new GUIContent("Add Marker Here/" + gesture), false, GesturePicked, new object[] { gesture, cursorTime });
				}
				previewMenu.AddSeparator("Add Marker Here/");
				previewMenu.AddItem(new GUIContent("Add Marker Here/Add\\Remove Gestures"), false, ShowProjectSettings);
			}

			// Paste
			if (copyBuffer.Count > 0)
			{
				previewMenu.AddItem(new GUIContent("Paste Here"), false, () => { PasteMarkers(cursorTime); });
			}
			else
			{
				previewMenu.AddDisabledItem(new GUIContent("Paste Here"));
			}

			previewMenu.ShowAsContext();
		}

		// Draw Waveform Visulization
		waveformHeight = (int)previewRect.y;
#if UNITY_5_3_6 || UNITY_5_4_OR_NEWER
		if (clip != null)
			AudioUtility.DrawWaveForm(clip, 0, previewRect, viewportStart, viewportSeconds);
#else
		if (clip != null && waveform != null) GUI.DrawTexture(new Rect(-(viewportStart * pixelsPerSecond), previewRect.y + 3, clip.length * pixelsPerSecond, (position.height - waveformHeight) - 33), waveform);
#endif
		// Draw Playhead
		if (Event.current.button != 1)
		{
			if (clip != null)
			{
				seekPosition = GUI.HorizontalSlider(new Rect(-(viewportStart * pixelsPerSecond), previewRect.y + 3, clip.length * pixelsPerSecond, (position.height - waveformHeight) - 33), seekPosition, 0, 1, GUIStyle.none, GUIStyle.none);
			}
			else
			{
				seekPosition = GUI.HorizontalSlider(new Rect(-(viewportStart * pixelsPerSecond), previewRect.y + 3, fileLength * pixelsPerSecond, (position.height - waveformHeight) - 33), seekPosition, 0, 1, GUIStyle.none, GUIStyle.none);
			}
		}

		GUI.DrawTexture(new Rect((-(viewportStart * pixelsPerSecond)) + (seekPosition * (fileLength * pixelsPerSecond)) - 3, previewRect.y, 7, previewRect.height - 20), playhead_line);
		GUI.DrawTexture(new Rect((-(viewportStart * pixelsPerSecond)) + (seekPosition * (fileLength * pixelsPerSecond)) - 7, previewRect.y, 15, 15), playhead_top);
		GUI.DrawTexture(new Rect((-(viewportStart * pixelsPerSecond)) + (seekPosition * (fileLength * pixelsPerSecond)) - 7, position.height - 48, 15, 15), playhead_bottom);

		GUI.DrawTexture(new Rect(0, previewRect.y - 35, position.width, 36), track_top);

		// Draw Time Lines
		if (showTimeline)
		{
			LipSyncEditorExtensions.DrawTimeline(previewRect.y, viewportStart, viewportEnd, position.width);
		}

		// Preview Warning
		if (visualPreview)
		{
			EditorGUI.HelpBox(new Rect(20, previewRect.y + previewRect.height - 45, position.width - 40, 25), "Preview mode active. Note: only Phonemes and Emotions will be shown in the preview.", MessageType.Info);
		}
		else if (previewTarget != null)
		{
			UpdatePreview(0);
			previewTarget = null;
		}

		// Viewport Scrolling
		if (MinMaxScrollbar(new Rect(0, (previewRect.y + previewRect.height) - 15, previewRect.width, 15), previewRect, ref viewportStart, ref viewportEnd, 0, fileLength, 0.5f))
		{
#if !(UNITY_5_3_6 || UNITY_5_4_OR_NEWER)
			if (fileLength * pixelsPerSecond <= maxWaveformWidth) {
				DestroyImmediate(waveform);
			}
#endif
		}

		// ********************
		// Render Marker Tracks
		// ********************

		GUIContent tip = null;
		MessageType tipType = MessageType.None;

		if (markerTab == 0)
		{
			// Phonemes
			#region Phoneme Interaction
			if (currentModal == null)
			{
				highlightedMarker = -1;
			}

			if (dragging == false)
			{
				for (int m = 0; m < phonemeData.Count; m++)
				{
					PhonemeMarker marker = phonemeData[m];

					if ((filterMask & (int)Mathf.Pow(2, marker.phonemeNumber)) == (int)Mathf.Pow(2, marker.phonemeNumber))
					{
						Rect markerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.time * (fileLength * pixelsPerSecond)) - 12.5f, previewRect.y - 25, 25, 26);

						// Test for marker at mouse position
						if (mouseX > markerRect.x + 5 && mouseX < markerRect.x + markerRect.width - 5 && mouseY > markerRect.y && mouseY < markerRect.y + markerRect.height - 4 && currentMarker == -1 && selectionRect.width < 2 && selectionRect.width > -2)
						{
							highlightedMarker = m;
							break;
						}
					}
				}
			}

			if (dragging == false && highlightedMarker > -1 && focusedWindow == this)
			{
				PhonemeMarker cm = phonemeData[highlightedMarker];

				// Get Input
				if (Event.current.type == EventType.MouseDrag)
				{
					// Started dragging a marker

					currentMarker = highlightedMarker;
					startOffset = cm.time - ((mouseX + (viewportStart * pixelsPerSecond)) / (fileLength * pixelsPerSecond));

					// Clear selection if this marker isn't in it, otherwise calculate offsets for whole selection
					if (selection.Count > 0)
					{
						if (!selection.Contains(highlightedMarker))
						{
							selection.Clear();
						}
						else
						{
							selectionOffsets = new float[selection.Count];
							for (int marker = 0; marker < selectionOffsets.Length; marker++)
							{
								selectionOffsets[marker] = phonemeData[currentMarker].time - phonemeData[selection[marker]].time;
							}
						}
					}

					dragging = true;
				}
				else if (Event.current.clickCount == 2 && Event.current.button == 0 && selection.Count < 2)
				{
					PhonemeMarkerSettings(cm);
				}
				else if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
				{
					// Released left mouse button

					if (Event.current.modifiers == EventModifiers.Shift)
					{
						// Holding shift
						// Clear selection and add all markers between this one and the last selected one.
						// If selection is empty, start afresh.
						if (selection.Count > 0)
						{
							List<PhonemeMarker> tempData = new List<PhonemeMarker>(phonemeData);
							tempData.Sort(LipSync.SortTime);

							int tempIndex = tempData.IndexOf(cm);
							int tempStart = tempData.IndexOf(phonemeData[firstSelection]);
							selection.Clear();

							if (tempStart > tempIndex)
							{
								for (int m = tempIndex; m <= tempStart; m++)
								{
									int realIndex = phonemeData.IndexOf(tempData[m]);

									selection.Add(realIndex);
								}
							}
							else if (tempStart < tempIndex)
							{
								for (int m = tempStart; m <= tempIndex; m++)
								{
									int realIndex = phonemeData.IndexOf(tempData[m]);

									selection.Add(realIndex);
								}
							}
						}
						else
						{
							firstSelection = highlightedMarker;
							selection.Add(highlightedMarker);
						}
					}
					else if (Event.current.modifiers == EventModifiers.Control)
					{
						// Holding control
						// Simply add this marker to the existing selection.
						// If marker is already in the selection, remove it.
						if (!selection.Contains(highlightedMarker))
						{
							selection.Add(highlightedMarker);
							if (cm.time < phonemeData[firstSelection].time || selection.Count == 1)
							{
								firstSelection = highlightedMarker;
							}
						}
						else
						{
							selection.Remove(highlightedMarker);
							if (highlightedMarker == firstSelection)
							{
								firstSelection = 0;
							}
						}
						selection.Sort(SortInt);
					}
					else
					{
						selection.Clear();
						selection.Add(highlightedMarker);
						firstSelection = highlightedMarker;
					}
				}
				else if (Event.current.type == EventType.ContextClick)
				{
					// Right clicked (two-finger tap on macOS)
					// Open marker context menu.
					GenericMenu markerMenu = new GenericMenu();
					markerMenu.AddItem(new GUIContent("Copy"), false, CopyMarkers);

					if (selection.Count > 1)
					{
						markerMenu.AddItem(new GUIContent("Delete Selection"), false, DeleteSelectedPhonemes);
						markerMenu.AddSeparator("");
						markerMenu.AddItem(new GUIContent("Selection Settings"), false, PhonemeMarkerSettingsMulti);
					}
					else
					{
						markerMenu.AddItem(new GUIContent("Delete"), false, DeletePhoneme, cm);

						for (int a = 0; a < settings.phonemeSet.phonemes.Length; a++)
						{
							markerMenu.AddItem(new GUIContent("Change/" + settings.phonemeSet.phonemes[a].name), false, ChangePhonemePicked, new List<int> { highlightedMarker, a });
						}

						if (highlightedMarker + 1 < phonemeData.Count)
						{
							if (phonemeData[highlightedMarker + 1].phonemeNumber == cm.phonemeNumber)
							{
								markerMenu.AddItem(new GUIContent("Sustain Marker"), cm.sustain, ToggleSustain, cm);
							}
							else
							{
								markerMenu.AddDisabledItem(new GUIContent("Sustain Marker"));
							}
						}
						else
						{
							markerMenu.AddDisabledItem(new GUIContent("Sustain Marker"));
						}

						markerMenu.AddSeparator("");
						markerMenu.AddItem(new GUIContent("Marker Settings"), false, PhonemeMarkerSettings, cm);
					}

					markerMenu.ShowAsContext();
				}
				else if (Event.current.type == EventType.KeyUp && selection.Count == 0)
				{
					// Check keyboard input (individual marker)
					if (Event.current.keyCode == KeyCode.Delete)
					{
						DeletePhoneme(phonemeData[highlightedMarker]);
					}
				}
			}
			else if (dragging == false && focusedWindow == this)
			{
				if (Event.current.type == EventType.MouseUp && !(Event.current.modifiers == EventModifiers.Control || Event.current.modifiers == EventModifiers.Shift))
				{
					// Release mouse button while not dragging or holding any modifiers - clear selection
					selection.Clear();
				}
				else if (Event.current.type == EventType.KeyUp && selection.Count > 0)
				{
					// Check keyboard input (on selection)
					if (Event.current.keyCode == KeyCode.Delete)
					{
						DeleteSelectedPhonemes();
					}
				}
			}
			#endregion

			#region Phoneme Drawing
			// Draw Markers
			for (int m = 0; m < phonemeData.Count; m++)
			{
				PhonemeMarker marker = phonemeData[m];

				if ((filterMask & (int)Mathf.Pow(2, marker.phonemeNumber)) == (int)Mathf.Pow(2, marker.phonemeNumber))
				{
					Rect markerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.time * (fileLength * pixelsPerSecond)) - 12.5f, previewRect.y - 25, 25, 26);

					if (marker.sustain)
					{
						GUI.DrawTexture(new Rect(markerRect.x + 12.5f, markerRect.y + 7, ((-(viewportStart * pixelsPerSecond)) + (phonemeData[m + 1].time * (fileLength * pixelsPerSecond))) - (markerRect.x + 12.5f), 1), marker_line);
					}

					GUI.color = Color.Lerp(Color.gray, Color.white, marker.intensity);
					if (currentMarker == m)
					{
						GUI.Box(markerRect, marker_selected, GUIStyle.none);
						GUI.DrawTexture(new Rect(markerRect.x + 12, previewRect.y, 1, previewRect.height - 15), marker_line);
						tip = new GUIContent(settings.phonemeSet.phonemes[marker.phonemeNumber].name + " - " + Mathf.RoundToInt(marker.intensity * 100f).ToString() + "%");
					}
					else if (highlightedMarker == m)
					{
						GUI.Box(markerRect, marker_hover, GUIStyle.none);
						GUI.DrawTexture(new Rect(markerRect.x + 12, previewRect.y, 1, previewRect.height - 15), marker_line);
						tip = new GUIContent(settings.phonemeSet.phonemes[marker.phonemeNumber].name + " - " + Mathf.RoundToInt(marker.intensity * 100f).ToString() + "%");
					}
					else if (selection.Contains(m))
					{
						GUI.Box(markerRect, marker_selected, GUIStyle.none);
						GUI.DrawTexture(new Rect(markerRect.x + 12, previewRect.y, 1, previewRect.height - 15), marker_line);
					}
					else
					{
						GUI.Box(markerRect, marker_normal, GUIStyle.none);
					}

					GUI.color = Color.white;

					if (marker.sustain)
					{
						GUI.Box(markerRect, marker_sustained, GUIStyle.none);
					}
				}
			}
			#endregion

		}
		else if (markerTab == 1)
		{
			// Emotions
			#region Emotion Interaction
			if (currentModal == null)
			{
				highlightedMarker = -1;
			}
			int highlightComponent = -1;

			if (dragging == false)
			{
				foreach (EmotionMarker marker in unorderedEmotionData)
				{
					Rect markerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.startTime * (fileLength * pixelsPerSecond)), previewRect.y - 30, (marker.endTime - marker.startTime) * (fileLength * pixelsPerSecond) - 2, 31);
					Rect startMarkerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.startTime * (fileLength * pixelsPerSecond)) - 6, previewRect.y - 30, 25, 31);
					Rect endMarkerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.endTime * (fileLength * pixelsPerSecond)) - 20, previewRect.y - 30, 25, 31);

					// Test for marker blend handles and main bars at mouse position
					if (focusedWindow == this)
					{
						// Blend In/Out Handles
						if (mouseX > startMarkerRect.x + (marker.blendInTime * (fileLength * pixelsPerSecond)) && mouseX < startMarkerRect.x + (marker.blendInTime * (fileLength * pixelsPerSecond)) + startMarkerRect.width && mouseY > startMarkerRect.y && mouseY < startMarkerRect.y + startMarkerRect.height - 4 && currentMarker == -1 && !marker.blendFromMarker && selectionRect.width < 2 && selectionRect.width > -2)
						{
							EditorGUIUtility.AddCursorRect(new Rect(startMarkerRect.x + (marker.blendInTime * (fileLength * pixelsPerSecond)), startMarkerRect.y, startMarkerRect.width, startMarkerRect.height), MouseCursor.SlideArrow);
							highlightedMarker = emotionData.IndexOf(marker);
							highlightComponent = 3;
						}
						else if (mouseX > endMarkerRect.x + (marker.blendOutTime * (fileLength * pixelsPerSecond)) && mouseX < endMarkerRect.x + (marker.blendOutTime * (fileLength * pixelsPerSecond)) + endMarkerRect.width && mouseY > endMarkerRect.y && mouseY < endMarkerRect.y + endMarkerRect.height - 4 && currentMarker == -1 && !marker.blendToMarker && selectionRect.width < 2 && selectionRect.width > -2)
						{
							EditorGUIUtility.AddCursorRect(new Rect(endMarkerRect.x + (marker.blendOutTime * (fileLength * pixelsPerSecond)), endMarkerRect.y, endMarkerRect.width, endMarkerRect.height), MouseCursor.SlideArrow);
							highlightedMarker = emotionData.IndexOf(marker);
							highlightComponent = 4;
						}
						else if (mouseX > markerRect.x && mouseX < markerRect.x + markerRect.width && mouseY > markerRect.y && mouseY < markerRect.y + markerRect.height - 4 && currentMarker == -1 && selectionRect.width < 2 && selectionRect.width > -2)
						{
							// Bars
							highlightedMarker = emotionData.IndexOf(marker);
							highlightComponent = 1;

							if (marker.invalid)
							{
								tip = new GUIContent("Markers are not allowed inside one another.");
								tipType = MessageType.Error;
							}
						}
					}
				}
			}

			// Test for marker start/end handles ar mouse position
			// Done in a separate loop to allow selecting through other bars
			if (dragging == false)
			{
				foreach (EmotionMarker marker in unorderedEmotionData)
				{
					Rect startMarkerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.startTime * (fileLength * pixelsPerSecond)) - 6, previewRect.y - 30, 25, 31);
					Rect endMarkerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.endTime * (fileLength * pixelsPerSecond)) - 20, previewRect.y - 30, 25, 31);

					if (focusedWindow == this)
					{
						// Start/End Handles
						if (mouseX > startMarkerRect.x && mouseX < startMarkerRect.x + startMarkerRect.width - 7 && mouseY > startMarkerRect.y && mouseY < startMarkerRect.y + startMarkerRect.height - 4 && currentMarker == -1 && selectionRect.width < 2 && selectionRect.width > -2)
						{
							highlightedMarker = emotionData.IndexOf(marker);
							highlightComponent = 0;
							EditorGUIUtility.AddCursorRect(startMarkerRect, MouseCursor.ResizeHorizontal);
						}
						else if (mouseX > endMarkerRect.x + 7 && mouseX < endMarkerRect.x + endMarkerRect.width && mouseY > endMarkerRect.y && mouseY < endMarkerRect.y + endMarkerRect.height - 4 && currentMarker == -1 && selectionRect.width < 2 && selectionRect.width > -2)
						{
							highlightedMarker = emotionData.IndexOf(marker);
							highlightComponent = 2;
							EditorGUIUtility.AddCursorRect(endMarkerRect, MouseCursor.ResizeHorizontal);
						}
					}
				}
			}

			// Get Input
			if (dragging == false && highlightedMarker > -1 && focusedWindow == this)
			{
				EmotionMarker em = emotionData[highlightedMarker];

				if (Event.current.type == EventType.MouseDrag)
				{
					// Started dragging a control

					dragging = true;
					currentMarker = highlightedMarker;
					currentComponent = highlightComponent;

					if (selection.Count > 0)
					{
						// Clear selection if the current marker isn't in the selection, otherwise calculate offsets
						if (!selection.Contains(highlightedMarker))
						{
							selection.Clear();
						}
						else
						{
							selectionOffsets = new float[selection.Count];
							sequentialStartOffsets = new float[selection.Count];
							sequentialEndOffsets = new float[selection.Count];

							for (int marker = 0; marker < selectionOffsets.Length; marker++)
							{
								selectionOffsets[marker] = em.startTime - emotionData[selection[marker]].startTime;
								sequentialStartOffsets[marker] = emotionData[selection[marker]].startTime - emotionData[selection[0]].startTime;
								sequentialEndOffsets[marker] = emotionData[selection[selection.Count - 1]].endTime - emotionData[selection[marker]].endTime;
							}
						}
					}

					// Calculate offset to mouse cursor (prevents handles 'jumping' to the cursor)
					if (currentComponent < 3)
					{
						// Dragging a bar or start/end handle
						startOffset = em.startTime - ((Event.current.mousePosition.x + (viewportStart * pixelsPerSecond)) / (fileLength * pixelsPerSecond));
						endOffset = em.endTime - ((Event.current.mousePosition.x + (viewportStart * pixelsPerSecond)) / (fileLength * pixelsPerSecond));

						lowSnapPoint = snappingDistance;
						highSnapPoint = 1 - snappingDistance;

						// Find snapping points (fallback to timeline start/end)
						if (selection.Count <= 1)
						{
							if (currentMarker > 0)
							{
								lowSnapPoint = emotionData[currentMarker - 1].endTime + snappingDistance;
							}

							if (currentMarker < emotionData.Count - 1)
							{
								highSnapPoint = emotionData[currentMarker + 1].startTime - snappingDistance;
							}
						}
						else
						{
							if (currentMarker > 0)
							{
								for (int m = currentMarker - 1; m >= 0; m--)
								{
									if (!selection.Contains(m))
									{
										lowSnapPoint = emotionData[m].endTime + snappingDistance;
									}
								}
							}

							if (currentMarker < emotionData.Count - 1)
							{
								for (int m = currentMarker + 1; m < emotionData.Count; m++)
								{
									if (!selection.Contains(m))
									{
										highSnapPoint = emotionData[m].startTime - snappingDistance;
									}
								}
							}
						}

					}
					else
					{
						// Dragging a blend in/out handle
						startOffset = (em.blendInTime) - ((Event.current.mousePosition.x + (viewportStart * pixelsPerSecond)) / (fileLength * pixelsPerSecond));
						endOffset = (em.blendOutTime) - ((Event.current.mousePosition.x + (viewportStart * pixelsPerSecond)) / (fileLength * pixelsPerSecond));
					}

					previousMarker = null;
					nextMarker = null;
				}
				else if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
				{
					// Released left mouse button

					if (Event.current.modifiers == EventModifiers.Shift)
					{
						// Holding shift
						// Clear selection and add all markers between this one and the last selected one.
						// If selection is empty, start afresh.
						if (selection.Count > 0)
						{
							List<EmotionMarker> tempData = new List<EmotionMarker>(emotionData);

							int tempIndex = tempData.IndexOf(em);
							int tempStart = tempData.IndexOf(emotionData[firstSelection]);
							selection.Clear();

							if (tempStart > tempIndex)
							{
								for (int m = tempIndex; m <= tempStart; m++)
								{
									int realIndex = emotionData.IndexOf(tempData[m]);

									selection.Add(realIndex);
								}
							}
							else if (tempStart < tempIndex)
							{
								for (int m = tempStart; m <= tempIndex; m++)
								{
									int realIndex = emotionData.IndexOf(tempData[m]);

									selection.Add(realIndex);
								}
							}
						}
						else
						{
							firstSelection = highlightedMarker;
							selection.Add(highlightedMarker);
						}
					}
					else if (Event.current.modifiers == EventModifiers.Control)
					{
						// Holding control
						// Simply add this marker to the existing selection
						// If this marker is already in the selection, remove it
						if (!selection.Contains(highlightedMarker))
						{
							selection.Add(highlightedMarker);
							if (em.startTime < emotionData[firstSelection].startTime || selection.Count == 1)
							{
								firstSelection = highlightedMarker;
							}
						}
						else
						{
							selection.Remove(highlightedMarker);
							if (highlightedMarker == firstSelection)
							{
								firstSelection = 0;
							}
						}
						selection.Sort(SortInt);
					}
					else
					{
						selection.Clear();
						selection.Add(highlightedMarker);
						firstSelection = highlightedMarker;
					}
				}
				else if (Event.current.type == EventType.ContextClick)
				{
					// Right clicked (two-finger tap on macOS)
					// Open marker context menu.
					GenericMenu markerMenu = new GenericMenu();
					markerMenu.AddItem(new GUIContent("Copy"), false, CopyMarkers);

					if (selection.Count > 1)
					{
						markerMenu.AddItem(new GUIContent("Delete Selection"), false, DeleteSelectedEmotions);
						markerMenu.AddSeparator("");
						markerMenu.AddItem(new GUIContent("Selection Settings"), false, EmotionMarkerSettingsMulti);
					}
					else
					{
						markerMenu.AddItem(new GUIContent("Delete"), false, DeleteEmotion, emotionData[highlightedMarker]);
						for (int a = 0; a < settings.emotions.Length; a++)
						{
							string emote = settings.emotions[a];
							markerMenu.AddItem(new GUIContent("Change/" + emote), false, ChangeEmotionPicked, new List<object> { emotionData[highlightedMarker], emote });
						}

						markerMenu.AddSeparator("Change/");
						if (!emotionData[highlightedMarker].isMixer)
							markerMenu.AddItem(new GUIContent("Change/Replace with New Mixer"), false, ChangeEmotionMixer, emotionData[highlightedMarker]);
						markerMenu.AddItem(new GUIContent("Change/Add\\Remove Emotions"), false, ShowProjectSettings);
						markerMenu.AddSeparator("");
						markerMenu.AddItem(new GUIContent("Marker Settings"), false, EmotionMarkerSettings, emotionData[highlightedMarker]);
						if (emotionData[highlightedMarker].isMixer)
							markerMenu.AddItem(new GUIContent("Edit Mixer"), false, OpenEmotionMixer, emotionData[highlightedMarker]);
					}
					markerMenu.ShowAsContext();

				}
				else if (Event.current.type == EventType.KeyUp && selection.Count == 0)
				{
					// Check keyboard input (individual marker)
					if (Event.current.keyCode == KeyCode.Delete)
					{
						DeleteEmotion(emotionData[highlightedMarker]);
					}
				}
			}
			else if (dragging == false && focusedWindow == this)
			{
				if (Event.current.type == EventType.MouseUp && !(Event.current.modifiers == EventModifiers.Control || Event.current.modifiers == EventModifiers.Shift))
				{
					// Released left mouse button with no modifiers - clear selection
					selection.Clear();
				}
				else if (Event.current.type == EventType.KeyUp && selection.Count > 0)
				{
					// Check keyboard input (selection)
					if (Event.current.keyCode == KeyCode.Delete)
					{
						DeleteSelectedEmotions();
					}
				}
			}
			#endregion

			#region Emotion Drawing
			// Loop through and draw markers
			foreach (EmotionMarker marker in unorderedEmotionData)
			{
				// Get rects for re-use
				Rect markerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.startTime * (fileLength * pixelsPerSecond)), previewRect.y - 30, (marker.endTime - marker.startTime) * (fileLength * pixelsPerSecond) - 2, 31);
				Rect startMarkerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.startTime * (fileLength * pixelsPerSecond)) - 6, previewRect.y - 30, 25, 31);
				Rect endMarkerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.endTime * (fileLength * pixelsPerSecond)) - 20, previewRect.y - 30, 25, 31);

				#region Get Bar Colors
				Color barColor = Color.gray;
				lastColor = Color.gray;
				nextColor = Color.gray;

				// Calculate bar colour (and prev/next bar colours for blends)
				if (useColors)
				{
					for (int em = 0; em < settings.emotions.Length; em++)
					{
						// Set barColor when matched
						if (settings.emotions[em] == marker.emotion)
						{
							barColor = settings.emotionColors[em];
						}

						// Get previous marker colour
						if (emotionData.IndexOf(marker) - 1 >= 0)
						{
							if (settings.emotions[em] == emotionData[emotionData.IndexOf(marker) - 1].emotion)
							{
								lastColor = settings.emotionColors[em];
							}
						}

						// Get next marker colour
						if (emotionData.IndexOf(marker) + 1 < emotionData.Count)
						{
							if (settings.emotions[em] == emotionData[emotionData.IndexOf(marker) + 1].emotion)
							{
								nextColor = settings.emotionColors[em];
							}
						}
					}

					if (marker.isMixer)
					{
						// Special case for Emotion Mixers as they store their own colour (mix of the emotions contained within)
						barColor = marker.mixer.displayColor;
					}

					// Main bar hover states
					if (highlightedMarker == emotionData.IndexOf(marker) && highlightComponent == 1)
					{
						barColor = Color.white;
					}
					else if ((currentMarker == emotionData.IndexOf(marker) && currentComponent == 1) || selection.Contains(emotionData.IndexOf(marker)))
					{
						barColor = new Color(0.4f, 0.6f, 1f);
					}
					else
					{
						barColor = Color.Lerp(Color.gray, barColor, marker.intensity);
					}

					if (marker.invalid)
					{
						// Marker is invalid - fade out slightly
						barColor.a = 0.5f;
					}

					// Blend in hover states
					if (emotionData.IndexOf(marker) - 1 >= 0 && marker.blendFromMarker)
					{
						int prevMarker = emotionData.IndexOf(marker) - 1;


						if (highlightedMarker == prevMarker && highlightComponent == 1)
						{
							lastColor = Color.white;
						}
						else if ((currentMarker == prevMarker && currentComponent == 1) || selection.Contains(prevMarker))
						{
							lastColor = new Color(0.4f, 0.6f, 1f);
						}
						else
						{
							lastColor = Color.Lerp(Color.gray, lastColor, emotionData[prevMarker].intensity);
						}

						if (emotionData[prevMarker].invalid)
						{
							// Marker is invalid - fade out slightly
							lastColor.a = 0.5f;
						}
					}
					else
					{
						lastColor = Darken(barColor, 0.5f);
					}

					// Blend out hover states
					if (emotionData.IndexOf(marker) + 1 < emotionData.Count && marker.blendToMarker)
					{
						int nextMarker = emotionData.IndexOf(marker) + 1;


						if (highlightedMarker == nextMarker && highlightComponent == 1)
						{
							nextColor = Color.white;
						}
						else if ((currentMarker == nextMarker && currentComponent == 1) || selection.Contains(nextMarker))
						{
							nextColor = new Color(0.4f, 0.6f, 1f);
						}
						else
						{
							nextColor = Color.Lerp(Color.gray, nextColor, emotionData[nextMarker].intensity);
						}

						if (emotionData[nextMarker].invalid)
						{
							// Marker is invalid - fade out slightly
							nextColor.a = 0.5f;
						}
					}
					else
					{
						nextColor = Darken(barColor, 0.5f);
					}

				}
				else
				{
					// Custom bar colours disabled in settings. Use default colour.
					barColor = HexToColor(defaultColor);
					lastColor = Darken(HexToColor(defaultColor), 0.5f);
					nextColor = Darken(HexToColor(defaultColor), 0.5f);
				}
				#endregion

				// Drawing
				// Main Bar
				GUI.color = barColor;
				GUI.DrawTexture(markerRect, emotion_area);

				// Blends
				GUI.color = lastColor;
				GUI.DrawTexture(new Rect(markerRect.x, markerRect.y, marker.blendInTime * (fileLength * pixelsPerSecond), markerRect.height), marker.blendFromMarker ? emotion_blend_in : emotion_blend_out);
				GUI.color = nextColor;
				GUI.DrawTexture(new Rect(markerRect.x + markerRect.width, markerRect.y, marker.blendOutTime * (fileLength * pixelsPerSecond), markerRect.height), emotion_blend_out);
				GUI.color = Color.white;

				// Blend Handles
				if (!marker.blendFromMarker && (currentComponent == 3 && currentMarker == emotionData.IndexOf(marker) || highlightComponent == 3 && highlightedMarker == emotionData.IndexOf(marker)))
					GUI.DrawTexture(new Rect(startMarkerRect.x + (marker.blendInTime * (fileLength * pixelsPerSecond)), startMarkerRect.y, startMarkerRect.width, startMarkerRect.height), emotion_blend_start);
				if (!marker.blendToMarker && (currentComponent == 4 && currentMarker == emotionData.IndexOf(marker) || highlightComponent == 4 && highlightedMarker == emotionData.IndexOf(marker)))
					GUI.DrawTexture(new Rect(endMarkerRect.x + (marker.blendOutTime * (fileLength * pixelsPerSecond)), endMarkerRect.y, endMarkerRect.width, endMarkerRect.height), emotion_blend_end);

				if (marker.invalid)
				{
					GUI.DrawTexture(new Rect(markerRect.x + 10, markerRect.y + 8, 14, 14), emotion_error);
				}
			}

			// Draw labels & end handles in seperate pass to avoid clipping
			foreach (EmotionMarker marker in unorderedEmotionData)
			{
				Rect startMarkerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.startTime * (fileLength * pixelsPerSecond)) - 6, previewRect.y - 30, 25, 31);
				Rect endMarkerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.endTime * (fileLength * pixelsPerSecond)) - 20, previewRect.y - 30, 25, 31);
				Rect markerRect = new Rect(((-(viewportStart * pixelsPerSecond)) + (marker.startTime * (fileLength * pixelsPerSecond))) + (marker.blendInTime / 2) * (fileLength * pixelsPerSecond) + 6, previewRect.y - 30, ((marker.endTime - marker.startTime) * (fileLength * pixelsPerSecond)) - ((marker.blendInTime / 2) * (fileLength * pixelsPerSecond)) + ((marker.blendOutTime / 2) * (fileLength * pixelsPerSecond)) - 14, 31);

				// Calculate bar colour
				Color barColor = Color.gray;
				if (useColors)
				{
					if (marker.isMixer)
					{
						barColor = marker.mixer.displayColor;
					}
					else
					{
						for (int em = 0; em < settings.emotions.Length; em++)
						{
							if (settings.emotions[em] == marker.emotion)
							{
								barColor = settings.emotionColors[em];
							}
						}
					}
				}
				else
				{
					barColor = HexToColor(defaultColor);
				}

				// Bar luminance used to change label colour for readability
				float lum = (0.299f * barColor.r + 0.587f * barColor.g + 0.114f * barColor.b);

				// Start Handle
				if (currentMarker == emotionData.IndexOf(marker) && (currentComponent == 0 || currentComponent == 1))
				{
					GUI.DrawTexture(startMarkerRect, emotion_start_selected);
					GUI.DrawTexture(new Rect((-(viewportStart * pixelsPerSecond)) + previewRect.x + marker.startTime * (fileLength * pixelsPerSecond), previewRect.y, 1, previewRect.height - 15), marker_line);
				}
				else if (highlightedMarker == emotionData.IndexOf(marker) && (highlightComponent == 0 || highlightComponent == 1))
				{
					GUI.DrawTexture(startMarkerRect, emotion_start_hover);
					GUI.DrawTexture(new Rect((-(viewportStart * pixelsPerSecond)) + previewRect.x + marker.startTime * (fileLength * pixelsPerSecond), previewRect.y, 1, previewRect.height - 15), marker_line);
				}
				else
				{
					if (!marker.blendFromMarker)
						GUI.DrawTexture(startMarkerRect, emotion_start);
				}

				// End Handle
				if (currentMarker == emotionData.IndexOf(marker) && (currentComponent == 1 || currentComponent == 2))
				{
					GUI.DrawTexture(endMarkerRect, emotion_end_selected);
					GUI.DrawTexture(new Rect((-(viewportStart * pixelsPerSecond)) + previewRect.x + marker.endTime * (fileLength * pixelsPerSecond), previewRect.y, 1, previewRect.height - 15), marker_line);
				}
				else if (highlightedMarker == emotionData.IndexOf(marker) && (highlightComponent == 1 || highlightComponent == 2))
				{
					GUI.DrawTexture(endMarkerRect, emotion_end_hover);
					GUI.DrawTexture(new Rect((-(viewportStart * pixelsPerSecond)) + previewRect.x + marker.endTime * (fileLength * pixelsPerSecond), previewRect.y, 1, previewRect.height - 15), marker_line);
				}
				else
				{
					if (!marker.blendToMarker)
						GUI.DrawTexture(endMarkerRect, emotion_end);
				}

				// Labels
				string markerLabel = marker.isMixer ? "Mixed Emotion" : marker.emotion;
				if (GUI.skin.label.CalcSize(new GUIContent(markerLabel + " - " + (marker.intensity * 100f).ToString() + "%")).x > markerRect.width)
				{
					if (highlightedMarker == emotionData.IndexOf(marker) && (tip == null || tip != null && tipType != MessageType.Error))
					{
						tipType = MessageType.None;
						tip = new GUIContent(markerLabel + " - " + (marker.intensity * 100f).ToString() + "%");
					}

					if (lum > 0.5f || highlightedMarker == emotionData.IndexOf(marker) && highlightComponent == 1)
					{
						GUI.contentColor = Color.black;
						GUI.Box(new Rect(markerRect.x, markerRect.y + 2, markerRect.width, markerRect.height - 9), "...", centeredStyle);
						GUI.contentColor = Color.white;
					}
					else
					{
						GUI.Box(new Rect(markerRect.x, markerRect.y + 2, markerRect.width, markerRect.height - 9), "...", centeredStyle);
					}
				}
				else
				{
					if (lum > 0.5f || highlightedMarker == emotionData.IndexOf(marker) && highlightComponent == 1)
					{
						GUI.contentColor = Color.black;
						GUI.Box(new Rect(markerRect.x, markerRect.y + 2, markerRect.width, markerRect.height - 9), markerLabel + " - " + (marker.intensity * 100f).ToString() + "%", centeredStyle);
						GUI.contentColor = Color.white;
					}
					else
					{
						GUI.Box(new Rect(markerRect.x, markerRect.y + 2, markerRect.width, markerRect.height - 9), markerLabel + " - " + (marker.intensity * 100f).ToString() + "%", centeredStyle);
					}
				}
			}
			#endregion

		}
		else if (markerTab == 2)
		{
			// Gestures
			#region Gesture Interaction
			if (currentModal == null)
			{
				highlightedMarker = -1;
			}

			if (dragging == false)
			{
				for (int m = 0; m < gestureData.Count; m++)
				{
					GestureMarker marker = gestureData[m];
					Rect markerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.time * (fileLength * pixelsPerSecond)) - 12.5f, previewRect.y - 25, 25, 26);

					// Test for marker at mouse position
					if (mouseX > markerRect.x + 5 && mouseX < markerRect.x + markerRect.width - 5 && mouseY > markerRect.y && mouseY < markerRect.y + markerRect.height - 4 && currentMarker == -1 && selectionRect.width < 2 && selectionRect.width > -2)
					{
						highlightedMarker = m;
						break;
					}
				}
			}

			if (dragging == false && highlightedMarker > -1 && focusedWindow == this)
			{
				GestureMarker cm = gestureData[highlightedMarker];

				if (Event.current.type == EventType.MouseDrag)
				{
					currentMarker = highlightedMarker;
					startOffset = cm.time - ((Event.current.mousePosition.x + (viewportStart * pixelsPerSecond)) / (fileLength * pixelsPerSecond));

					if (selection.Count > 0)
					{
						if (!selection.Contains(highlightedMarker))
						{
							selection.Clear();
						}
						else
						{
							selectionOffsets = new float[selection.Count];
							for (int marker = 0; marker < selectionOffsets.Length; marker++)
							{
								selectionOffsets[marker] = gestureData[currentMarker].time - gestureData[selection[marker]].time;
							}
						}
					}

					dragging = true;
				}
				else if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
				{
					if (Event.current.modifiers == EventModifiers.Shift)
					{
						if (selection.Count > 0)
						{
							List<GestureMarker> tempData = new List<GestureMarker>(gestureData);
							tempData.Sort(LipSync.SortTime);

							int tempIndex = tempData.IndexOf(cm);
							int tempStart = tempData.IndexOf(gestureData[firstSelection]);
							selection.Clear();

							if (tempStart > tempIndex)
							{
								for (int m = tempIndex; m <= tempStart; m++)
								{
									int realIndex = gestureData.IndexOf(tempData[m]);

									selection.Add(realIndex);
								}
							}
							else if (tempStart < tempIndex)
							{
								for (int m = tempStart; m <= tempIndex; m++)
								{
									int realIndex = gestureData.IndexOf(tempData[m]);

									selection.Add(realIndex);
								}
							}
						}
						else
						{
							firstSelection = highlightedMarker;
							selection.Add(highlightedMarker);
						}
					}
					else if (Event.current.modifiers == EventModifiers.Control)
					{
						if (!selection.Contains(highlightedMarker))
						{
							selection.Add(highlightedMarker);
							if (cm.time < gestureData[firstSelection].time || selection.Count == 1)
							{
								firstSelection = highlightedMarker;
							}
						}
						else
						{
							selection.Remove(highlightedMarker);
							if (highlightedMarker == firstSelection)
							{
								firstSelection = 0;
							}
						}
						selection.Sort(SortInt);
					}
					else
					{
						selection.Clear();
						selection.Add(highlightedMarker);
						firstSelection = highlightedMarker;
					}
				}
				else if (Event.current.type == EventType.ContextClick)
				{
					GenericMenu markerMenu = new GenericMenu();
					markerMenu.AddItem(new GUIContent("Copy"), false, CopyMarkers);

					if (selection.Count > 1)
					{
						markerMenu.AddItem(new GUIContent("Delete Selection"), false, DeleteSelectedGestures);
					}
					else
					{
						markerMenu.AddItem(new GUIContent("Delete"), false, DeleteGesture, cm);

						for (int a = 0; a < settings.gestures.Count; a++)
						{
							string gesture = settings.gestures[a];
							markerMenu.AddItem(new GUIContent("Change/" + gesture), false, ChangeGesturePicked, new List<object> { highlightedMarker, gesture });
						}
						markerMenu.AddSeparator("Change/");
						markerMenu.AddItem(new GUIContent("Change/Add New Gesture"), false, ShowProjectSettings);
					}

					markerMenu.ShowAsContext();
				}
				else if (Event.current.type == EventType.KeyUp && selection.Count == 0)
				{
					// Check keyboard input (individual marker)
					if (Event.current.keyCode == KeyCode.Delete)
					{
						DeleteGesture(gestureData[highlightedMarker]);
					}
				}
			}
			else if (dragging == false && focusedWindow == this)
			{
				if (Event.current.type == EventType.MouseUp && !(Event.current.modifiers == EventModifiers.Control || Event.current.modifiers == EventModifiers.Shift))
				{
					// Release mouse button while not dragging or holding any modifiers - clear selection
					selection.Clear();
				}
				else if (Event.current.type == EventType.KeyUp && selection.Count > 0)
				{
					// Check keyboard input (on selection)
					if (Event.current.keyCode == KeyCode.Delete)
					{
						DeleteSelectedGestures();
					}
				}
			}
			#endregion

			#region Gesture Drawing
			for (int m = 0; m < gestureData.Count; m++)
			{
				GestureMarker marker = gestureData[m];

				Rect markerRect = new Rect((-(viewportStart * pixelsPerSecond)) + (marker.time * (fileLength * pixelsPerSecond)) - 12.5f, previewRect.y - 25, 25, 26);

				if (currentMarker == m)
				{
					GUI.Box(markerRect, gesture_selected, GUIStyle.none);
					GUI.DrawTexture(new Rect(markerRect.x + 12, previewRect.y, 1, previewRect.height - 15), marker_line);
					tip = new GUIContent(marker.gesture);
				}
				else if (highlightedMarker == m)
				{
					GUI.Box(markerRect, gesture_hover, GUIStyle.none);
					GUI.DrawTexture(new Rect(markerRect.x + 12, previewRect.y, 1, previewRect.height - 15), marker_line);
					tip = new GUIContent(marker.gesture);
				}
				else if (selection.Contains(m))
				{
					GUI.Box(markerRect, gesture_selected, GUIStyle.none);
					GUI.DrawTexture(new Rect(markerRect.x + 12, previewRect.y, 1, previewRect.height - 15), marker_line);
				}
				else
				{
					GUI.Box(markerRect, gesture_normal, GUIStyle.none);
				}
			}
			#endregion

		}

		// Rect Selection
		if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && new Rect(0, previewRect.y - 35, position.width, 36).Contains(Event.current.mousePosition) && highlightedMarker == -1)
		{
			// Left mouse button pressed - begin selection
			selection.Clear();
			selectionRect = new Rect(Event.current.mousePosition, Vector2.zero);
		}
		else if ((Event.current.type == EventType.MouseUp && Event.current.button == 0 || currentMarker > -1) && selection.Count == 0)
		{

			// If selection rect is inverted (width < 0), flip along X
			if (selectionRect.width < 0)
			{
				selectionRect = new Rect(selectionRect.x + selectionRect.width, selectionRect.y, -selectionRect.width, selectionRect.height);
			}

			// If no modifiers, clear selection
			// Shift and Control are treated the same for rect selecting for the sake of simplicity
			if (Event.current.modifiers != EventModifiers.Shift && Event.current.modifiers != EventModifiers.Control)
			{
				selection.Clear();
			}

			if (markerTab == 0)
			{
				// Phonemes
				for (int i = 0; i < phonemeData.Count; i++)
				{
					float screenPos = (-(viewportStart * pixelsPerSecond)) + (phonemeData[i].time * (fileLength * pixelsPerSecond));
					if (screenPos > selectionRect.x && screenPos < selectionRect.x + selectionRect.width)
					{
						if (selection.Contains(i))
						{
							selection.Remove(i);
						}
						else
						{
							selection.Add(i);
						}

					}
				}
			}
			else if (markerTab == 1)
			{
				// Emotions
				for (int i = 0; i < emotionData.Count; i++)
				{
					float startPos = (-(viewportStart * pixelsPerSecond)) + (emotionData[i].startTime * (fileLength * pixelsPerSecond));
					float endPos = (-(viewportStart * pixelsPerSecond)) + (emotionData[i].endTime * (fileLength * pixelsPerSecond));
					if ((startPos > selectionRect.x && startPos < selectionRect.x + selectionRect.width) || (endPos > selectionRect.x && endPos < selectionRect.x + selectionRect.width))
					{
						if (selection.Contains(i))
						{
							selection.Remove(i);
						}
						else
						{
							selection.Add(i);
						}

					}
				}
			}
			else if (markerTab == 2)
			{
				// Gestures
				for (int i = 0; i < gestureData.Count; i++)
				{
					float screenPos = (-(viewportStart * pixelsPerSecond)) + (gestureData[i].time * (fileLength * pixelsPerSecond));
					if (screenPos > selectionRect.x && screenPos < selectionRect.x + selectionRect.width)
					{
						if (selection.Contains(i))
						{
							selection.Remove(i);
						}
						else
						{
							selection.Add(i);
						}

					}
				}
			}

			// Reset rect
			selectionRect = new Rect(0, 0, 0, 0);
		}

		// Update and Draw Selection Rect
		if (selectionRect != new Rect(0, 0, 0, 0))
		{
			selectionRect = Rect.MinMaxRect(selectionRect.position.x, selectionRect.y, Event.current.mousePosition.x, Mathf.Clamp(Event.current.mousePosition.y, previewRect.y - 35, previewRect.y - 3));
			EditorGUI.DrawRect(selectionRect, new Color(0.2f, 0.4f, 1f, 0.5f));
		}

		// Draw tooltip
		if (tip != null && dragging == false)
		{
			Rect tooltipRect = new Rect();
			float tooltipWidth = Mathf.Clamp(((GUIStyle)"flow node 0").CalcSize(tip).x + 20, 40, 450);
			if (Event.current.mousePosition.x + tooltipWidth + 10 > this.position.width)
			{
				tooltipRect = new Rect(Event.current.mousePosition.x - (tooltipWidth + 10), Event.current.mousePosition.y - 10, tooltipWidth, 30);
			}
			else
			{
				tooltipRect = new Rect(Event.current.mousePosition.x + 10, Event.current.mousePosition.y - 10, tooltipWidth, 30);
			}

			if (tipType == MessageType.None)
			{
				GUI.Box(tooltipRect, tip, (GUIStyle)"flow node 0");
			}
			else if (tipType == MessageType.Info)
			{
				GUI.Box(tooltipRect, tip, (GUIStyle)"flow node 1");
			}
			else if (tipType == MessageType.Warning)
			{
				GUI.Box(tooltipRect, tip, (GUIStyle)"flow node 5");
			}
			else if (tipType == MessageType.Error)
			{
				GUI.Box(tooltipRect, tip, (GUIStyle)"flow node 6");
			}
		}

		// Update marker dragging
		if (markerTab == 0)
		{
			// Phonemes
			if (dragging == true && Event.current.type == EventType.MouseDrag && currentMarker > -1)
			{
				if (selection.Count > 0)
				{
					selection.Sort(SortInt);
					float firstMarkerOffset = phonemeData[currentMarker].time - phonemeData[selection[0]].time;
					float lastMarkerOffset = phonemeData[selection[selection.Count - 1]].time - phonemeData[currentMarker].time;
					float currentMarkerNewTime = Mathf.Clamp01(((Event.current.mousePosition.x / (fileLength * pixelsPerSecond)) + (viewportStart / fileLength)) + startOffset);

					if (currentMarkerNewTime - firstMarkerOffset >= 0 && currentMarkerNewTime + lastMarkerOffset <= 1)
					{
						for (int marker = 0; marker < selection.Count; marker++)
						{
							phonemeData[selection[marker]].time = currentMarkerNewTime - selectionOffsets[marker];
						}
					}
				}
				else
				{
					phonemeData[currentMarker].time = Mathf.Clamp01(((Event.current.mousePosition.x / (fileLength * pixelsPerSecond)) + (viewportStart / fileLength)) + startOffset);
				}

				changed = true;
				previewOutOfDate = true;
			}
		}
		else if (markerTab == 1)
		{
			// Emotions
			if (currentMarker > -1)
			{
				if (currentComponent == 0 || currentComponent == 2)
					EditorGUIUtility.AddCursorRect(new Rect(0, 0, this.position.width, this.position.height), MouseCursor.SlideArrow);
			}

			if (dragging == true && Event.current.type == EventType.MouseDrag && currentMarker > -1)
			{
				if (currentComponent == 0)
				{
					// Start handle
					if (selection.Count > 1)
					{
						selection.Sort(SortInt);
						float currentHandleNewTime = ((Event.current.mousePosition.x / (fileLength * pixelsPerSecond)) + (viewportStart / fileLength)) + startOffset;

						for (int marker = 0; marker < selection.Count; marker++)
						{
							float tempChange = currentHandleNewTime - selectionOffsets[marker];

							if (tempChange > emotionData[selection[marker]].endTime - 0.008f)
							{
							}
							else
							{
								emotionData[selection[marker]].startTime = tempChange;
							}

							changed = true;
							previewOutOfDate = true;

							emotionData[selection[marker]].startTime = Mathf.Clamp01(emotionData[selection[marker]].startTime);
							emotionData[selection[marker]].blendOutTime = Mathf.Clamp(emotionData[selection[marker]].blendOutTime, -(emotionData[selection[marker]].endTime - emotionData[selection[marker]].startTime) + emotionData[selection[marker]].blendInTime, 0);
							emotionData[selection[marker]].blendInTime = Mathf.Clamp(emotionData[selection[marker]].blendInTime, 0, (emotionData[selection[marker]].endTime - emotionData[selection[marker]].startTime));

							if (emotionData[selection[marker]].startTime <= lowSnapPoint && emotionData[selection[marker]].startTime >= lowSnapPoint - (snappingDistance * 2))
							{
								emotionData[selection[marker]].startTime = (lowSnapPoint - snappingDistance);
							}
						}
					}
					else
					{
						float tempChange = ((Event.current.mousePosition.x / (fileLength * pixelsPerSecond)) + (viewportStart / fileLength)) + startOffset;
						if (tempChange > emotionData[currentMarker].endTime - 0.008f)
						{
						}
						else
						{
							emotionData[currentMarker].startTime = tempChange;
						}
						changed = true;
						previewOutOfDate = true;

						emotionData[currentMarker].startTime = Mathf.Clamp01(emotionData[currentMarker].startTime);
						emotionData[currentMarker].blendOutTime = Mathf.Clamp(emotionData[currentMarker].blendOutTime, -(emotionData[currentMarker].endTime - emotionData[currentMarker].startTime) + emotionData[currentMarker].blendInTime, 0);
						emotionData[currentMarker].blendInTime = Mathf.Clamp(emotionData[currentMarker].blendInTime, 0, (emotionData[currentMarker].endTime - emotionData[currentMarker].startTime));

						if (emotionData[currentMarker].startTime <= lowSnapPoint && emotionData[currentMarker].startTime >= lowSnapPoint - (snappingDistance * 2))
						{
							emotionData[currentMarker].startTime = (lowSnapPoint - snappingDistance);
						}
					}
				}
				else if (currentComponent == 1)
				{
					// Main bar
					if (selection.Count > 1)
					{
						selection.Sort(SortInt);
						float currentMarkerNewTime = ((Event.current.mousePosition.x + (viewportStart * pixelsPerSecond)) / (fileLength * pixelsPerSecond)) + startOffset;

						for (int marker = 0; marker < selection.Count; marker++)
						{
							float length = emotionData[selection[marker]].endTime - emotionData[selection[marker]].startTime;

							emotionData[selection[marker]].startTime = currentMarkerNewTime - selectionOffsets[marker];
							emotionData[selection[marker]].endTime = emotionData[selection[marker]].startTime + length;

							if (emotionData[selection[0]].startTime <= lowSnapPoint && emotionData[selection[0]].startTime >= lowSnapPoint - (snappingDistance * 2))
							{
								emotionData[selection[marker]].startTime = (lowSnapPoint - snappingDistance) + sequentialStartOffsets[marker];
								emotionData[selection[marker]].endTime = ((lowSnapPoint - snappingDistance) + sequentialStartOffsets[marker]) + length;
							}
							else if (emotionData[selection[selection.Count - 1]].endTime >= highSnapPoint && emotionData[selection[selection.Count - 1]].endTime <= highSnapPoint + (snappingDistance * 2))
							{
								emotionData[selection[marker]].endTime = (highSnapPoint + snappingDistance) - sequentialEndOffsets[marker];
								emotionData[selection[marker]].startTime = ((highSnapPoint + snappingDistance) - sequentialEndOffsets[marker]) - length;
							}
						}
					}
					else
					{
						float length = emotionData[currentMarker].endTime - emotionData[currentMarker].startTime;
						emotionData[currentMarker].startTime = ((Event.current.mousePosition.x + (viewportStart * pixelsPerSecond)) / (fileLength * pixelsPerSecond)) + startOffset;
						emotionData[currentMarker].endTime = emotionData[currentMarker].startTime + length;

						if (emotionData[currentMarker].startTime <= lowSnapPoint && emotionData[currentMarker].startTime >= lowSnapPoint - (snappingDistance * 2))
						{
							emotionData[currentMarker].startTime = (lowSnapPoint - snappingDistance);
							emotionData[currentMarker].endTime = (lowSnapPoint - snappingDistance) + length;
						}
						else if (emotionData[currentMarker].endTime >= highSnapPoint && emotionData[currentMarker].endTime <= highSnapPoint + (snappingDistance * 2))
						{
							emotionData[currentMarker].endTime = (highSnapPoint + snappingDistance);
							emotionData[currentMarker].startTime = (highSnapPoint + snappingDistance) - length;
						}
					}

					changed = true;
					previewOutOfDate = true;
				}
				else if (currentComponent == 2)
				{
					// End handle
					if (selection.Count > 1)
					{
						selection.Sort(SortInt);

						float currentHandleNewTime = ((Event.current.mousePosition.x / (fileLength * pixelsPerSecond)) + (viewportStart / fileLength)) + endOffset;

						for (int marker = 0; marker < selection.Count; marker++)
						{
							float tempChange = currentHandleNewTime - selectionOffsets[marker];

							if (tempChange >= emotionData[selection[marker]].startTime + 0.008f)
							{
								emotionData[selection[marker]].endTime = tempChange;
							}

							changed = true;
							previewOutOfDate = true;

							emotionData[selection[marker]].endTime = Mathf.Clamp01(emotionData[selection[marker]].endTime);
							emotionData[selection[marker]].blendInTime = Mathf.Clamp(emotionData[selection[marker]].blendInTime, 0, (emotionData[selection[marker]].endTime - emotionData[selection[marker]].startTime) + emotionData[selection[marker]].blendOutTime);
							emotionData[selection[marker]].blendOutTime = Mathf.Clamp(emotionData[selection[marker]].blendOutTime, -(emotionData[selection[marker]].endTime - emotionData[selection[marker]].startTime), 0);

							if (emotionData[selection[marker]].endTime >= highSnapPoint && emotionData[selection[marker]].endTime <= highSnapPoint + (snappingDistance * 2))
							{
								emotionData[selection[marker]].endTime = (highSnapPoint + snappingDistance);
							}
						}
					}
					else
					{
						float tempChange = ((Event.current.mousePosition.x / (fileLength * pixelsPerSecond)) + (viewportStart / fileLength)) + endOffset;

						if (tempChange >= emotionData[currentMarker].startTime + 0.008f)
						{
							emotionData[currentMarker].endTime = tempChange;
						}

						changed = true;
						previewOutOfDate = true;

						emotionData[currentMarker].endTime = Mathf.Clamp01(emotionData[currentMarker].endTime);
						emotionData[currentMarker].blendInTime = Mathf.Clamp(emotionData[currentMarker].blendInTime, 0, (emotionData[currentMarker].endTime - emotionData[currentMarker].startTime) + emotionData[currentMarker].blendOutTime);
						emotionData[currentMarker].blendOutTime = Mathf.Clamp(emotionData[currentMarker].blendOutTime, -(emotionData[currentMarker].endTime - emotionData[currentMarker].startTime), 0);

						if (emotionData[currentMarker].endTime >= highSnapPoint && emotionData[currentMarker].endTime <= highSnapPoint + (snappingDistance * 2))
						{
							emotionData[currentMarker].endTime = (highSnapPoint + snappingDistance);
						}
					}
				}
				else if (currentComponent == 3)
				{
					// Blend-in Handle
					emotionData[currentMarker].customBlendIn = true;
					changed = true;
					previewOutOfDate = true;

					float tempChange = ((Event.current.mousePosition.x / (fileLength * pixelsPerSecond)) + (viewportStart / fileLength)) + startOffset;
					emotionData[currentMarker].blendInTime = Mathf.Clamp(tempChange, 0, (emotionData[currentMarker].endTime - emotionData[currentMarker].startTime) + emotionData[currentMarker].blendOutTime);

					if (selection.Count > 1)
					{
						for (int m = 0; m < selection.Count; m++)
						{
							emotionData[selection[m]].customBlendIn = true;
							emotionData[selection[m]].blendInTime = Mathf.Clamp(emotionData[currentMarker].blendInTime, 0, (emotionData[selection[m]].endTime - emotionData[selection[m]].startTime) + emotionData[selection[m]].blendOutTime);
						}
					}
				}
				else if (currentComponent == 4)
				{
					// Blend-out Handle
					changed = true;
					previewOutOfDate = true;
					emotionData[currentMarker].customBlendOut = true;

					float tempChange = ((Event.current.mousePosition.x / (fileLength * pixelsPerSecond)) + (viewportStart / fileLength)) + endOffset;
					emotionData[currentMarker].blendOutTime = Mathf.Clamp(tempChange, -(emotionData[currentMarker].endTime - emotionData[currentMarker].startTime) + emotionData[currentMarker].blendInTime, 0);

					if (selection.Count > 1)
					{
						for (int m = 0; m < selection.Count; m++)
						{
							emotionData[selection[m]].customBlendOut = true;
							emotionData[selection[m]].blendOutTime = Mathf.Clamp(emotionData[currentMarker].blendOutTime, -(emotionData[selection[m]].endTime - emotionData[selection[m]].startTime) + emotionData[selection[m]].blendInTime, 0);
						}
					}
				}
				FixEmotionBlends();
			}
		}
		else if (markerTab == 2)
		{
			// Gestures
			if (dragging == true && Event.current.type == EventType.MouseDrag && currentMarker > -1)
			{
				if (selection.Count > 0)
				{
					selection.Sort(SortInt);
					float firstMarkerOffset = gestureData[currentMarker].time - gestureData[selection[0]].time;
					float lastMarkerOffset = gestureData[selection[selection.Count - 1]].time - gestureData[currentMarker].time;
					float currentMarkerNewTime = Mathf.Clamp01(((Event.current.mousePosition.x / (fileLength * pixelsPerSecond)) + (viewportStart / fileLength)) + startOffset);

					if (currentMarkerNewTime - firstMarkerOffset >= 0 && currentMarkerNewTime + lastMarkerOffset <= 1)
					{
						for (int marker = 0; marker < selection.Count; marker++)
						{
							gestureData[selection[marker]].time = currentMarkerNewTime - selectionOffsets[marker];
						}
					}
				}
				else
				{
					gestureData[currentMarker].time = Mathf.Clamp01(((Event.current.mousePosition.x / (fileLength * pixelsPerSecond)) + (viewportStart / fileLength)) + startOffset);
				}

				changed = true;
				previewOutOfDate = true;
			}
		}

		if (Event.current.type == EventType.MouseUp && dragging == true)
		{
			// Finished dragging
			dragging = false;

			// Sort markers now that dragging has finished
			if (markerTab == 0)
			{
				// Phonemes
				phonemeData.Sort(LipSync.SortTime);

				// Check sustained markers are still valid 
				for (int i = 0; i < phonemeData.Count; i++)
				{
					PhonemeMarker m = phonemeData[i];

					if (i + 1 < phonemeData.Count)
					{
						if (m.sustain && phonemeData[i + 1].phonemeNumber != m.phonemeNumber)
						{
							m.sustain = false;
							if (i - 1 >= 0)
							{
								if (phonemeData[i - 1].phonemeNumber == m.phonemeNumber)
								{
									phonemeData[i - 1].sustain = true;
								}
							}
						}
					}
					else if (i - 1 >= 0)
					{
						if (m.sustain && phonemeData[i - 1].phonemeNumber == m.phonemeNumber)
						{
							m.sustain = false;
							phonemeData[i - 1].sustain = true;
						}
						else
						{
							m.sustain = false;
						}
					}
					else
					{
						m.sustain = false;
					}
				}
			}
			else if (markerTab == 1)
			{
				// Emotions
				emotionData.Sort(EmotionSort);
			}
			else
			{
				// Gestures
				gestureData.Sort(LipSync.SortTime);
			}

			currentMarker = -1;
			currentComponent = -1;
		}

		//Controls
		Rect bottomToolbarRect = EditorGUILayout.BeginHorizontal();
		GUI.Box(bottomToolbarRect, "", EditorStyles.toolbar);
		timeSpan = TimeSpan.FromSeconds(seekPosition * fileLength);
		Char pad = '0';
		string minutes = timeSpan.Minutes.ToString().PadLeft(2, pad);
		string seconds = timeSpan.Seconds.ToString().PadLeft(2, pad);
		string milliseconds = timeSpan.Milliseconds.ToString().PadLeft(3, pad);

		string currentTime = minutes + ":" + seconds + ":" + milliseconds;

		timeSpan = TimeSpan.FromSeconds(fileLength);
		minutes = timeSpan.Minutes.ToString().PadLeft(2, pad);
		seconds = timeSpan.Seconds.ToString().PadLeft(2, pad);
		milliseconds = timeSpan.Milliseconds.ToString().PadLeft(3, pad);

		string totalTime = minutes + ":" + seconds + ":" + milliseconds;
		GUILayout.FlexibleSpace();
		if (GUILayout.Button(new GUIContent(currentTime + " / " + totalTime, "Change Clip Length"), EditorStyles.toolbarTextField))
		{
			ClipSettings();
		}
		GUILayout.FlexibleSpace();
		if (GUILayout.Button(isPlaying && !isPaused ? pauseIcon : playIcon, EditorStyles.toolbarButton, GUILayout.Width(50)))
		{
			PlayPause();
		}
		if (GUILayout.Button(stopIcon, EditorStyles.toolbarButton, GUILayout.Width(50)))
		{
			Stop();
		}
		looping = GUILayout.Toggle(looping, loopIcon, EditorStyles.toolbarButton, GUILayout.Width(50));

		GUILayout.FlexibleSpace();

		switch (markerTab)
		{
			case 0:
				if (GUILayout.Button("Add Phoneme", EditorStyles.toolbarButton, GUILayout.Width(160)))
				{
					GenericMenu phonemeMenu = new GenericMenu();

					for (int a = 0; a < settings.phonemeSet.phonemes.Length; a++)
					{
						phonemeMenu.AddItem(new GUIContent(settings.phonemeSet.phonemes[a].name), false, PhonemePicked, new object[] { a, seekPosition });
					}
					phonemeMenu.ShowAsContext();
				}

				GUILayout.Space(40);
				GUILayout.Box("Filters:", EditorStyles.label);
				filterMask = EditorGUILayout.MaskField(filterMask, phonemeSetNames, EditorStyles.toolbarPopup, GUILayout.MaxWidth(100));
				break;
			case 1:
				if (GUILayout.Button("Add Emotion", EditorStyles.toolbarButton, GUILayout.Width(160)))
				{
					GenericMenu emotionMenu = new GenericMenu();

					for (int a = 0; a < settings.emotions.Length; a++)
					{
						string emote = settings.emotions[a];
						emotionMenu.AddItem(new GUIContent(emote), false, EmotionPicked, new object[] { emote, seekPosition });
					}
					emotionMenu.AddSeparator("");
					emotionMenu.AddItem(new GUIContent("Add New Mixer"), false, AddEmotionMixer, seekPosition);
					emotionMenu.AddItem(new GUIContent("Add\\Remove Emotions"), false, ShowProjectSettings);

					emotionMenu.ShowAsContext();
				}

				break;
			case 2:
				if (GUILayout.Button("Add Gesture", EditorStyles.toolbarButton, GUILayout.Width(160)))
				{
					GenericMenu gestureMenu = new GenericMenu();

					for (int a = 0; a < settings.gestures.Count; a++)
					{
						string gesture = settings.gestures[a];
						gestureMenu.AddItem(new GUIContent(gesture), false, GesturePicked, new object[] { gesture, seekPosition });
					}
					gestureMenu.AddSeparator("");
					gestureMenu.AddItem(new GUIContent("Add\\Remove Gestures"), false, ShowProjectSettings);

					gestureMenu.ShowAsContext();
				}

				break;
		}

		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		GUILayout.Space(1);
	}
	#endregion

	#region Window Management
	[MenuItem("Window/Rogo Digital/LipSync Pro/Open Clip Editor %&a", false, 11)]
	public static LipSyncClipSetup ShowWindow ()
	{
		return ShowWindow("", false, "", "", 0, 0);
	}

	public static LipSyncClipSetup ShowWindow (string loadPath, bool newWindow)
	{
		return ShowWindow(loadPath, newWindow, "", "", 0, 0);
	}

	public static LipSyncClipSetup ShowWindow (string loadPath, bool newWindow, string oldFileName, string oldLastLoad, int oldMarkerTab, float oldSeekPosition)
	{
		LipSyncClipSetup window;

		UnityEngine.Object[] current = Selection.GetFiltered(typeof(AudioClip), SelectionMode.Assets);

		if (newWindow)
		{
			window = ScriptableObject.CreateInstance<LipSyncClipSetup>();
			window.Show();
		}
		else
		{
			window = EditorWindow.GetWindow<LipSyncClipSetup>();
		}

		if (current.Length > 0)
		{
			window.clip = (AudioClip)current[0];
			window.fileLength = window.clip.length;
#if !(UNITY_5_3_6 || UNITY_5_4_OR_NEWER)
			window.waveform = null;
#endif
		}
		else if (loadPath == "")
		{
			current = Selection.GetFiltered(typeof(LipSyncData), SelectionMode.Assets);
			if (current.Length > 0)
			{
				loadPath = AssetDatabase.GetAssetPath(current[0]);
			}
		}

		if (EditorPrefs.GetBool("LipSync_ShowExtensionsOnLoad", true))
		{
			EditorWindow.GetWindow<RDExtensionWindow>("Extensions", false, typeof(LipSyncClipSetup));
			RDExtensionWindow.ShowWindow("LipSync");
		}

		window.Focus();

		if (window.changed)
		{
			int choice = EditorUtility.DisplayDialogComplex("Save Changes", "You have made changes to the current file, do you want to save them before closing?", "Yes", "No", "Cancel");
			if (choice != 2)
			{
				if (choice == 0)
				{
					window.OnSaveClick();
				}

				window.changed = false;
				window.fileName = "Untitled";
				window.oldClip = window.clip;
				window.phonemeData = new List<PhonemeMarker>();
				window.emotionData = new List<EmotionMarker>();
				window.unorderedEmotionData = new List<EmotionMarker>();
				window.gestureData = new List<GestureMarker>();

				window.seekPosition = 0;
				AudioUtility.StopAllClips();
				window.currentMarker = -1;

				if (loadPath != "")
				{
					window.LoadFile(loadPath);
					window.previewOutOfDate = true;
				}
			}
			else
			{
				window.clip = window.oldClip;
			}
		}
		else
		{
			window.oldClip = window.clip;
			window.fileName = "Untitled";
			window.phonemeData = new List<PhonemeMarker>();
			window.emotionData = new List<EmotionMarker>();
			window.unorderedEmotionData = new List<EmotionMarker>();
			window.gestureData = new List<GestureMarker>();

			window.seekPosition = 0;
			AudioUtility.StopAllClips();
			window.currentMarker = -1;

			if (loadPath != "")
			{
				window.LoadFile(loadPath);
				window.previewOutOfDate = true;
			}
		}


		if (EditorGUIUtility.isProSkin)
		{
			window.windowIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/icon.png");
		}
		else
		{
			window.windowIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/icon.png");
		}

		window.titleContent = new GUIContent("LipSync", window.windowIcon);
		window.minSize = new Vector2(700, 200);
		if (newWindow)
		{
			window.changed = true;
			window.lastLoad = oldLastLoad;
			window.fileName = oldFileName;
			window.markerTab = oldMarkerTab;
			window.seekPosition = oldSeekPosition;
			window.oldSeekPosition = oldSeekPosition;
			if (window.clip != null)
				AudioUtility.SetClipSamplePosition(window.clip, (int)(window.seekPosition * AudioUtility.GetSampleCount(window.clip)));
			AssetDatabase.DeleteAsset("Assets/LIPSYNC_AUTOSAVE.asset");
		}

		if (EditorPrefs.GetBool("LipSync_SetViewportOnLoad", true))
		{
			if (window.clip != null)
			{
				window.viewportEnd = window.fileLength;
			}
			window.viewportStart = 0;
		}

		return window;
	}
	#endregion

	#region Input/Output
	void LoadFile (string path)
	{
		if (changed)
		{
			int choice = EditorUtility.DisplayDialogComplex("Save Changes", "You have made changes to the current file, do you want to save them before closing?", "Yes", "No", "Cancel");

			if (choice == 1)
			{
				OnSaveClick();
			}
			else if (choice == 2)
			{
				return;
			}
		}
		LipSyncData file = (LipSyncData)AssetDatabase.LoadAssetAtPath(path, typeof(LipSyncData));

		if (file.clip != null)
			clip = file.clip;
		oldClip = clip;
#if !(UNITY_5_3_6 || UNITY_5_4_OR_NEWER)
		if (clip != null) waveform = AudioUtility.GetWaveForm(clip, 0, (int)(fileLength * (position.width / (viewportEnd - viewportStart))), (position.height - waveformHeight) - 18);
#endif
		fileName = file.name + ".Asset";

		fileLength = file.length;
		if (file.transcript != null)
			transcript = file.transcript;

		if (setViewportOnLoad)
		{
			if (clip != null)
				fileLength = clip.length;
			viewportEnd = fileLength;
			viewportStart = 0;
		}

		phonemeData = new List<PhonemeMarker>();
		foreach (PhonemeMarker marker in file.phonemeData)
		{
			PhonemeMarker newMarker = new PhonemeMarker(marker.phonemeNumber, marker.time, marker.intensity, marker.sustain);
			newMarker.phoneme = marker.phoneme;

			// Randomness
			newMarker.useRandomness = marker.useRandomness;
			newMarker.intensityRandomness = marker.intensityRandomness;
			newMarker.blendableRandomness = marker.blendableRandomness;
			newMarker.bonePositionRandomness = marker.bonePositionRandomness;
			newMarker.boneRotationRandomness = marker.boneRotationRandomness;

			phonemeData.Add(newMarker);
		}

		emotionData = new List<EmotionMarker>();
		unorderedEmotionData = new List<EmotionMarker>();
		foreach (EmotionMarker marker in file.emotionData)
		{
			EmotionMarker newMarker = null;
			if (marker.isMixer)
			{
				EmotionMixer mixer = new EmotionMixer();
				mixer.emotions = new List<EmotionMixer.EmotionComponent>(marker.mixer.emotions);
				mixer.displayColor = marker.mixer.displayColor;
				mixer.mixingMode = marker.mixer.mixingMode;
				newMarker = new EmotionMarker(mixer, marker.startTime, marker.endTime, marker.blendInTime, marker.blendOutTime, marker.blendToMarker, marker.blendFromMarker, marker.customBlendIn, marker.customBlendOut);
			}
			else
			{
				newMarker = new EmotionMarker(marker.emotion, marker.startTime, marker.endTime, marker.blendInTime, marker.blendOutTime, marker.blendToMarker, marker.blendFromMarker, marker.customBlendIn, marker.customBlendOut);
			}

			newMarker.intensity = marker.intensity;

			// Variation
			newMarker.continuousVariation = marker.continuousVariation;
			newMarker.variationFrequency = marker.variationFrequency;
			newMarker.intensityVariation = marker.intensityVariation;
			newMarker.blendableVariation = marker.blendableVariation;
			newMarker.bonePositionVariation = marker.bonePositionVariation;
			newMarker.boneRotationVariation = marker.boneRotationVariation;

			emotionData.Add(newMarker);
			unorderedEmotionData.Add(newMarker);
		}

		gestureData = new List<GestureMarker>();
		if (file.gestureData != null)
		{
			foreach (GestureMarker marker in file.gestureData)
			{
				gestureData.Add(new GestureMarker(marker.gesture, marker.time));
			}
		}

		currentMarker = -1;
		previewOutOfDate = true;
		changed = false;

		if (file.version < version)
		{
			UpdateFile(file.version, false, false);
		}

		FixEmotionBlends();
		lastLoad = path;
	}

	private void LoadXML (TextAsset xmlFile, AudioClip linkedClip, bool suppressInput)
	{
		clip = linkedClip;
		LoadXML(xmlFile, suppressInput);
	}

	private void LoadXML (TextAsset xmlFile, bool suppressInput)
	{
		XmlDocument document = new XmlDocument();
		document.LoadXml(xmlFile.text);

		// Clear/define marker lists, to overwrite any previous file
		phonemeData = new List<PhonemeMarker>();
		emotionData = new List<EmotionMarker>();
		unorderedEmotionData = new List<EmotionMarker>();
		gestureData = new List<GestureMarker>();

		float fileVersion = 0;

		if (LipSync.ReadXML(document, "LipSyncData", "version") == null)
		{
			// Update data
			fileLength = clip.length;
		}
		else
		{
			fileVersion = float.Parse(LipSync.ReadXML(document, "LipSyncData", "version"));
			fileLength = float.Parse(LipSync.ReadXML(document, "LipSyncData", "length"));
		}

		if (LipSync.ReadXML(document, "LipSyncData", "transcript") != null)
		{
			transcript = LipSync.ReadXML(document, "LipSyncData", "transcript");
		}

		//Phonemes
		XmlNode phonemesNode = document.SelectSingleNode("//LipSyncData//phonemes");
		if (phonemesNode != null)
		{
			XmlNodeList phonemeNodes = phonemesNode.ChildNodes;

			for (int p = 0; p < phonemeNodes.Count; p++)
			{
				XmlNode node = phonemeNodes[p];

				if (node.LocalName == "marker")
				{
					string phonemeName = "";
					float time = float.Parse(node.Attributes["time"].Value) / fileLength;
					float intensity = 1;
					bool sustain = false;
					bool useModifier = false;
					float intensityMod = 0.1f;
					float blendableMod = 0.3f;
					float bonePosMod = 0.3f;
					float boneRotMod = 0.3f;

					if (node.Attributes["phonemeName"] != null)
					{
						phonemeName = node.Attributes["phonemeName"].Value;
					}
					else
					{
						phonemeName = node.Attributes["phoneme"].Value;
					}
					if (node.Attributes["intensity"] != null)
						intensity = float.Parse(node.Attributes["intensity"].Value);
					if (node.Attributes["sustain"] != null)
						sustain = bool.Parse(node.Attributes["sustain"].Value);
					if (node.Attributes["useRandomness"] != null)
						useModifier = bool.Parse(node.Attributes["useRandomness"].Value);
					if (node.Attributes["intensityRandomness"] != null)
						intensityMod = float.Parse(node.Attributes["intensityRandomness"].Value);
					if (node.Attributes["blendableRandomness"] != null)
						blendableMod = float.Parse(node.Attributes["blendableRandomness"].Value);
					if (node.Attributes["bonePositionRandomness"] != null)
						bonePosMod = float.Parse(node.Attributes["bonePositionRandomness"].Value);
					if (node.Attributes["boneRotationRandomness"] != null)
						boneRotMod = float.Parse(node.Attributes["boneRotationRandomness"].Value);

					bool found = false;
					int phoneme;
					for (phoneme = 0; phoneme < settings.phonemeSet.phonemes.Length; phoneme++)
					{
						if (settings.phonemeSet.phonemes[phoneme].name == phonemeName)
						{
							found = true;
							break;
						}
					}

					if (found)
					{
						PhonemeMarker phonemeMarker = new PhonemeMarker(phoneme, time, intensity, sustain);
						phonemeMarker.useRandomness = useModifier;
						phonemeMarker.intensityRandomness = intensityMod;
						phonemeMarker.blendableRandomness = blendableMod;
						phonemeMarker.bonePositionRandomness = bonePosMod;
						phonemeMarker.boneRotationRandomness = boneRotMod;
						phonemeData.Add(phonemeMarker);
					}
					else
					{
						Debug.LogWarning("XML Parser: Phoneme '" + phonemeName + "' does not exist in the current set adn has been skipped.");
					}

				}
			}
		}

		//Emotions
		XmlNode emotionsNode = document.SelectSingleNode("//LipSyncData//emotions");
		if (emotionsNode != null)
		{
			XmlNodeList emotionNodes = emotionsNode.ChildNodes;

			for (int p = 0; p < emotionNodes.Count; p++)
			{
				XmlNode node = emotionNodes[p];

				if (node.LocalName == "marker")
				{
					string emotion = node.Attributes["emotion"].Value;
					float startTime = float.Parse(node.Attributes["start"].Value) / fileLength;
					float endTime = float.Parse(node.Attributes["end"].Value) / fileLength;
					float blendInTime = float.Parse(node.Attributes["blendIn"].Value);
					float blendOutTime = float.Parse(node.Attributes["blendOut"].Value);
					bool blendTo = bool.Parse(node.Attributes["blendToMarker"].Value);
					bool blendFrom = bool.Parse(node.Attributes["blendFromMarker"].Value);
					bool customBlendIn = false;
					bool customBlendOut = false;
					float intensity = 1;
					bool isMixer = false;
					EmotionMixer mixer = null;
					bool useModifier = false;
					float modFreq = 0.5f;
					float intensityMod = 0.35f;
					float blendableMod = 0.1f;
					float bonePosMod = 0.1f;
					float boneRotMod = 0.1f;

					if (node.Attributes["customBlendIn"] != null)
						customBlendIn = bool.Parse(node.Attributes["customBlendIn"].Value);
					if (node.Attributes["customBlendOut"] != null)
						customBlendOut = bool.Parse(node.Attributes["customBlendOut"].Value);
					if (node.Attributes["intensity"] != null)
						intensity = float.Parse(node.Attributes["intensity"].Value);
					if (node.Attributes["continuousVariation"] != null)
						useModifier = bool.Parse(node.Attributes["continuousVariation"].Value);
					if (node.Attributes["variationFrequency"] != null)
						modFreq = float.Parse(node.Attributes["variationFrequency"].Value);
					if (node.Attributes["intensityVariation"] != null)
						intensityMod = float.Parse(node.Attributes["intensityVariation"].Value);
					if (node.Attributes["blendableVariation"] != null)
						blendableMod = float.Parse(node.Attributes["blendableVariation"].Value);
					if (node.Attributes["bonePositionVariation"] != null)
						bonePosMod = float.Parse(node.Attributes["bonePositionVariation"].Value);
					if (node.Attributes["boneRotationVariation"] != null)
						boneRotMod = float.Parse(node.Attributes["boneRotationVariation"].Value);

					XmlNode mixerNode = node.SelectSingleNode("mixer");
					isMixer = mixerNode != null;

					if (isMixer)
					{
						mixer = new EmotionMixer();
						if (mixerNode.Attributes["mixingMode"] != null)
						{
							mixer.mixingMode = (EmotionMixer.MixingMode)Enum.Parse(typeof(EmotionMixer.MixingMode), mixerNode.Attributes["mixingMode"].Value);
						}

						for (int e = 0; e < mixerNode.ChildNodes.Count; e++)
						{
							XmlNode eNode = mixerNode.ChildNodes[e];
							mixer.emotions.Add(new EmotionMixer.EmotionComponent(eNode.InnerText, float.Parse(eNode.Attributes["weight"].Value)));
						}

					}

					EmotionMarker newMarker = null;
					if (isMixer)
					{
						newMarker = new EmotionMarker(mixer, startTime, endTime, blendInTime, blendOutTime, blendTo, blendFrom, customBlendIn, customBlendOut);

						newMarker.mixer.displayColor = Color.black;
						for (int i = 0; i < newMarker.mixer.emotions.Count; i++)
						{
							for (int c = 0; c < settings.emotions.Length; c++)
							{
								if (settings.emotions[c] == newMarker.mixer.emotions[i].emotion)
								{
									newMarker.mixer.displayColor += newMarker.mixer.emotions[i].weight * settings.emotionColors[c];
								}
							}
						}
					}
					else
					{
						newMarker = new EmotionMarker(emotion, startTime, endTime, blendInTime, blendOutTime, blendTo, blendFrom, customBlendIn, customBlendOut);
					}

					newMarker.intensity = intensity;
					newMarker.continuousVariation = useModifier;
					newMarker.variationFrequency = modFreq;
					newMarker.intensityVariation = intensityMod;
					newMarker.blendableVariation = blendableMod;
					newMarker.bonePositionVariation = bonePosMod;
					newMarker.boneRotationVariation = boneRotMod;

					emotionData.Add(newMarker);
					unorderedEmotionData.Add(newMarker);
				}
			}
		}

		//Gestures
		XmlNode gesturesNode = document.SelectSingleNode("//LipSyncData//gestures");
		if (gesturesNode != null)
		{
			XmlNodeList gestureNodes = gesturesNode.ChildNodes;

			for (int p = 0; p < gestureNodes.Count; p++)
			{
				XmlNode node = gestureNodes[p];

				if (node.LocalName == "marker")
				{
					string gesture = node.Attributes["gesture"].Value;
					float time = float.Parse(node.Attributes["time"].Value) / fileLength;

					gestureData.Add(new GestureMarker(gesture, time));
				}
			}
		}

		phonemeData.Sort(LipSync.SortTime);
		gestureData.Sort(LipSync.SortTime);
		FixEmotionBlends();

		if (fileVersion < version)
		{
			UpdateFile(fileVersion, true, suppressInput);
		}

		changed = true;
		previewOutOfDate = true;
	}

	public void FixEmotionBlends ()
	{
		unorderedEmotionData.Sort(EmotionSortSize);

		foreach (EmotionMarker eMarker in emotionData)
		{
			eMarker.blendFromMarker = false;
			eMarker.blendToMarker = false;
			if (!eMarker.customBlendIn)
				eMarker.blendInTime = 0;
			if (!eMarker.customBlendOut)
				eMarker.blendOutTime = 0;
			eMarker.invalid = false;
		}

		foreach (EmotionMarker eMarker in emotionData)
		{
			foreach (EmotionMarker tMarker in emotionData)
			{
				if (eMarker != tMarker)
				{
					if (eMarker.startTime > tMarker.startTime && eMarker.startTime < tMarker.endTime)
					{
						if (eMarker.customBlendIn)
						{
							eMarker.customBlendIn = false;
							FixEmotionBlends();
							return;
						}
						eMarker.blendFromMarker = true;

						if (eMarker.endTime > tMarker.startTime && eMarker.endTime < tMarker.endTime)
						{
							eMarker.invalid = true;
						}
						else
						{
							eMarker.blendInTime = tMarker.endTime - eMarker.startTime;
						}
					}

					if (eMarker.endTime > tMarker.startTime && eMarker.endTime < tMarker.endTime)
					{
						if (eMarker.customBlendOut)
						{
							eMarker.customBlendOut = false;
							FixEmotionBlends();
							return;
						}
						eMarker.blendToMarker = true;

						if (eMarker.startTime > tMarker.startTime && eMarker.startTime < tMarker.endTime)
						{
							eMarker.invalid = true;
						}
						else
						{
							eMarker.blendOutTime = tMarker.startTime - eMarker.endTime;
						}
					}
				}
			}
		}
	}

	void UpdateFile (float version, bool isXML, bool suppressInput)
	{
		bool wasChanged = false;
		if (version < 1.0f)
		{
			// Pre 1.0 - Update Emotion Markers
			wasChanged = true;
			emotionData.Sort(EmotionSort);
			for (int e = 0; e < emotionData.Count; e++)
			{
				if (emotionData[e].blendFromMarker)
				{
					emotionData[e].startTime -= emotionData[e].blendInTime;
					emotionData[e - 1].endTime += emotionData[e].blendInTime;
				}
				else
				{
					emotionData[e].customBlendIn = true;
				}

				if (emotionData[e].blendToMarker)
				{
					emotionData[e + 1].startTime -= emotionData[e].blendOutTime;
					emotionData[e].endTime += emotionData[e].blendOutTime;
				}
				else
				{
					emotionData[e].customBlendOut = true;
					emotionData[e].blendOutTime = -emotionData[e].blendOutTime;
				}
			}
			FixEmotionBlends();

			previewOutOfDate = true;
			changed = true;
		}

		if (version < 1.3f && !isXML)
		{
			// 1.0 to 1.201 - Convert enum Phonemes
			wasChanged = true;

			for (int p = 0; p < phonemeData.Count; p++)
			{
				phonemeData[p].phonemeNumber = (int)phonemeData[p].phoneme;
			}

			previewOutOfDate = true;
			changed = true;
		}

		Repaint();
		if (wasChanged && !suppressInput)
			EditorUtility.DisplayDialog("Loading Old File", "This file was created in an old version of LipSync Pro. It has been automatically updated to work with the current version, but the original file has not been overwritten.", "Ok");
	}

	public static LipSyncData SaveFile (LipSyncProject settings, string path, bool isXML, string transcript, float fileLength, PhonemeMarker[] phonemeData, EmotionMarker[] emotionData, GestureMarker[] gestureData, AudioClip clip = null)
	{
		if (isXML)
		{
			XmlWriterSettings xmlWriterSettings = new XmlWriterSettings { Indent = true, IndentChars = "\t" };
			XmlWriter writer = XmlWriter.Create(path, xmlWriterSettings);

			writer.WriteStartDocument();

			//Header
			writer.WriteComment("Exported from Rogo Digital LipSync " + versionNumber + ". Exported at " + DateTime.Now.ToString());
			writer.WriteComment("Note: This format cannot directly reference the linked AudioClip like a LipSyncData asset can. It is advised that you use that format instead unless you need to process the data further outside of Unity.");

			writer.WriteStartElement("LipSyncData");
			writer.WriteElementString("version", version.ToString());
			writer.WriteElementString("transcript", transcript);
			writer.WriteElementString("length", fileLength.ToString());
			//Data
			writer.WriteStartElement("phonemes");
			foreach (PhonemeMarker marker in phonemeData)
			{
				writer.WriteStartElement("marker");

				writer.WriteAttributeString("time", (marker.time * fileLength).ToString());
				writer.WriteAttributeString("intensity", marker.intensity.ToString());
				writer.WriteAttributeString("sustain", marker.sustain.ToString());
				writer.WriteAttributeString("useRandomness", marker.useRandomness.ToString());
				writer.WriteAttributeString("intensityRandomness", marker.intensityRandomness.ToString());
				writer.WriteAttributeString("blendableRandomness", marker.blendableRandomness.ToString());
				writer.WriteAttributeString("bonePositionRandomness", marker.bonePositionRandomness.ToString());
				writer.WriteAttributeString("boneRotationRandomness", marker.boneRotationRandomness.ToString());
				writer.WriteAttributeString("phonemeName", settings.phonemeSet.phonemes[marker.phonemeNumber].name);
				writer.WriteAttributeString("phonemeNumber", marker.phonemeNumber.ToString());
				writer.WriteEndElement();
			}
			writer.WriteEndElement();

			writer.WriteStartElement("emotions");
			foreach (EmotionMarker marker in emotionData)
			{
				writer.WriteStartElement("marker");
				writer.WriteAttributeString("start", (marker.startTime * fileLength).ToString());
				writer.WriteAttributeString("end", (marker.endTime * fileLength).ToString());
				writer.WriteAttributeString("blendFromMarker", (marker.blendFromMarker ? "true" : "false"));
				writer.WriteAttributeString("blendToMarker", (marker.blendToMarker ? "true" : "false"));
				writer.WriteAttributeString("blendIn", marker.blendInTime.ToString());
				writer.WriteAttributeString("blendOut", marker.blendOutTime.ToString());
				writer.WriteAttributeString("customBlendIn", marker.customBlendIn.ToString());
				writer.WriteAttributeString("customBlendOut", marker.customBlendOut.ToString());
				writer.WriteAttributeString("intensity", marker.intensity.ToString());
				writer.WriteAttributeString("continuousVariation", marker.continuousVariation.ToString());
				writer.WriteAttributeString("variationFrequency", marker.variationFrequency.ToString());
				writer.WriteAttributeString("intensityVariation", marker.intensityVariation.ToString());
				writer.WriteAttributeString("blendableVariation", marker.blendableVariation.ToString());
				writer.WriteAttributeString("bonePositionVariation", marker.bonePositionVariation.ToString());
				writer.WriteAttributeString("boneRotationVariation", marker.boneRotationVariation.ToString());
				writer.WriteAttributeString("emotion", marker.emotion);

				if (marker.isMixer)
				{
					writer.WriteStartElement("mixer");
					writer.WriteAttributeString("mixingMode", marker.mixer.mixingMode.ToString());

					for (int i = 0; i < marker.mixer.emotions.Count; i++)
					{
						writer.WriteStartElement("emotion");
						writer.WriteAttributeString("weight", marker.mixer.emotions[i].weight.ToString());
						writer.WriteString(marker.mixer.emotions[i].emotion);
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
				}
				writer.WriteEndElement();
			}
			writer.WriteEndElement();

			writer.WriteStartElement("gestures");
			foreach (GestureMarker marker in gestureData)
			{
				writer.WriteStartElement("marker");

				writer.WriteAttributeString("time", (marker.time * fileLength).ToString());
				writer.WriteAttributeString("gesture", marker.gesture);

				writer.WriteEndElement();
			}
			writer.WriteEndElement();

			writer.WriteEndDocument();
			writer.Close();
			AssetDatabase.Refresh();
			return null;
		}
		else
		{
			LipSyncData file = ScriptableObject.CreateInstance<LipSyncData>();
			file.phonemeData = phonemeData;
			file.emotionData = emotionData;
			file.gestureData = gestureData;
			file.version = version;
			file.clip = clip;
			file.length = fileLength;
			file.transcript = transcript;

			LipSyncData outputFile = (LipSyncData)AssetDatabase.LoadAssetAtPath(path, typeof(LipSyncData));

			if (outputFile != null)
			{
				EditorUtility.CopySerialized(file, outputFile);
				AssetDatabase.SaveAssets();
			}
			else
			{
				outputFile = ScriptableObject.CreateInstance<LipSyncData>();
				EditorUtility.CopySerialized(file, outputFile);
				AssetDatabase.CreateAsset(outputFile, path);
			}

			DestroyImmediate(file);
			AssetDatabase.Refresh();
			return outputFile;
		}
	}

	// Quick Update XML files
	[MenuItem("Window/Rogo Digital/LipSync Pro/Update All XML Files")]
	public static void UpdateXMLFiles ()
	{
		string[] guids = AssetDatabase.FindAssets("t:TextAsset");
		string filesUpdated = "";
		int filesUpdatedCount = 0;

		for (int i = 0; i < guids.Length; i++)
		{
			string path = AssetDatabase.GUIDToAssetPath(guids[i]);
			if (Path.GetExtension(path).ToLowerInvariant() == ".xml")
			{
				TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
				XmlDocument document = new XmlDocument();
				document.LoadXml(textAsset.text);

				XmlNode node = document.SelectSingleNode("//LipSyncData");
				if (node != null)
				{
					string version = LipSync.ReadXML(document, "LipSyncData", "version");
					bool upToDate = false;
					if (!string.IsNullOrEmpty(version))
					{
						if (float.Parse(version) >= 1.321f)
							upToDate = true;
					}

					if (!upToDate)
					{
						string[] audioGuids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(path) + " t:AudioClip", new string[] { Path.GetDirectoryName(path) });
						AudioClip clip = null;

						if (audioGuids.Length == 1)
						{
							clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(audioGuids[0]));
						}
						else
						{
							EditorUtility.DisplayDialog("Could Not Locate AudioClip", "No matching audio file was found for the file " + Path.GetFileName(path) + ". Please press Continue and locate the audio file.", "Continue");
							string audioPath = EditorUtility.OpenFilePanel("Locate AudioClip", Path.GetDirectoryName(path), "*");
							clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets" + audioPath.Substring((Application.dataPath).Length));
						}

						LipSyncClipSetup window = ShowWindow();
						window.LoadXML(textAsset, clip, true);
						SaveFile(window.settings, path, true, window.transcript, window.fileLength, window.phonemeData.ToArray(), window.emotionData.ToArray(), window.gestureData.ToArray(), clip);
						window.changed = false;
						window.Close();

						filesUpdated += path + "\n";
						filesUpdatedCount++;
					}
				}
			}
		}

		if (filesUpdatedCount > 0)
		{
			EditorUtility.DisplayDialog("Update Complete", "Updated " + filesUpdatedCount + " XML file(s):\n" + filesUpdated, "Ok");
		}
		else
		{
			EditorUtility.DisplayDialog("Update Complete", "No changes have been made. All files are up-to-date", "Ok");
		}
	}
	#endregion

	#region AutoSync
	public void StartAutoSyncDefault ()
	{
		if (languageModelNames.Length > 0)
		{
			disabled = true;
			AutoSync.AutoSyncOptions options = new AutoSync.AutoSyncOptions(languageModelNames[defaultLanguageModel], EditorPrefs.GetBool("LipSync_SoXAvailable", false), AutoSync.AutoSyncOptionsPreset.Default);
			AutoSync.ProcessAudio(clip, OnAutoSyncDataReady, OnAutoSyncFailed, options);
		}
		else
		{
			Debug.Log("AutoSync Failed: Language Model could not be loaded. (Check Clip Editor settings)");
		}
	}

	public void StartAutoSyncHQ ()
	{
		if (languageModelNames.Length > 0)
		{
			disabled = true;
			AutoSync.AutoSyncOptions options = new AutoSync.AutoSyncOptions(languageModelNames[defaultLanguageModel], EditorPrefs.GetBool("LipSync_SoXAvailable", false), AutoSync.AutoSyncOptionsPreset.HighQuality);
			AutoSync.ProcessAudio(clip, OnAutoSyncDataReady, OnAutoSyncFailed, options);
		}
		else
		{
			Debug.Log("AutoSync Failed: Language Model could not be loaded. (Check Clip Editor settings)");
		}
	}

	void OnAutoSyncDataReady (AudioClip clip, List<PhonemeMarker> markers)
	{
		disabled = false;
		if (markers.Count > 0)
		{
			phonemeData = markers;
			changed = true;
			previewOutOfDate = true;
		}
	}

	void OnAutoSyncFailed (string error)
	{
		disabled = false;
		Debug.LogError(error);
	}

	public void OpenAutoSyncWindow (object mode)
	{
		AutoSyncWindow.CreateWindow(this, this, (int)mode);
	}
	#endregion

	#region Utility
	static int EmotionSort (EmotionMarker a, EmotionMarker b)
	{
		return a.startTime.CompareTo(b.startTime);
	}

	static int EmotionSortSize (EmotionMarker a, EmotionMarker b)
	{
		float aLength = a.endTime - a.startTime;
		float bLength = b.endTime - b.startTime;

		return bLength.CompareTo(aLength);
	}

	static int SortInt (int a, int b)
	{
		return a.CompareTo(b);
	}

	static Color HexToColor (int color)
	{
		string hex = color.ToString("X").PadLeft(6, (char)'0');

		int R = Convert.ToInt32(hex.Substring(0, 2), 16);
		int G = Convert.ToInt32(hex.Substring(2, 2), 16);
		int B = Convert.ToInt32(hex.Substring(4, 2), 16);
		return new Color(R / 255f, G / 255f, B / 255f);
	}

	static int ColorToHex (Color color)
	{
		string R = ((int)(color.r * 255)).ToString("X").PadLeft(2, (char)'0');
		string G = ((int)(color.g * 255)).ToString("X").PadLeft(2, (char)'0');
		string B = ((int)(color.b * 255)).ToString("X").PadLeft(2, (char)'0');

		string hex = R + G + B;
		return Convert.ToInt32(hex, 16);
	}

	static Color Darken (Color color, float amount)
	{
		return new Color(color.r * amount, color.g * amount, color.b * amount);
	}

	#endregion

	// *********************
	// Menu Callback Methods
	// *********************

	#region Marker Addition

	void PhonemePicked (object raw)
	{
		object[] data = (object[])raw;
		int picked = (int)data[0];
		float time = (float)data[1];
		Undo.RecordObject(this, "Add Phoneme Marker");
		PhonemeMarker newMarker = new PhonemeMarker(picked, time, defaultPhonemeIntensity, false);
		newMarker.useRandomness = defaultUseRandomness;
		newMarker.intensityRandomness = defaultIntensityRandomness;
		newMarker.blendableRandomness = defaultBlendableRandomness;
		newMarker.bonePositionRandomness = defaultBonePositionRandomness;
		newMarker.boneRotationRandomness = defaultBoneRotationRandomness;

		phonemeData.Add(newMarker);
		phonemeData.Sort(LipSync.SortTime);

		for (int i = 0; i < phonemeData.Count; i++)
		{
			PhonemeMarker m = phonemeData[i];

			if (i + 1 < phonemeData.Count)
			{
				if (m.sustain && phonemeData[i + 1].phonemeNumber != m.phonemeNumber)
				{
					m.sustain = false;
					if (i - 1 >= 0)
					{
						if (phonemeData[i - 1].phonemeNumber == m.phonemeNumber)
						{
							phonemeData[i - 1].sustain = true;
						}
					}
				}
			}
			else if (i - 1 >= 0)
			{
				if (m.sustain && phonemeData[i - 1].phonemeNumber == m.phonemeNumber)
				{
					m.sustain = false;
					phonemeData[i - 1].sustain = true;
				}
				else
				{
					m.sustain = false;
				}
			}
			else
			{
				m.sustain = false;
			}
		}

		changed = true;
		previewOutOfDate = true;
	}

	void EmotionPicked (object raw)
	{
		object[] data = (object[])raw;
		string picked = (string)data[0];
		float time = (float)data[1];

		Undo.RecordObject(this, "Add Emotion Marker");
		EmotionMarker newMarker = new EmotionMarker(picked, time, Mathf.Clamp(time + 0.1f, time + 0.003f, 1), 0.025f, -0.025f, false, false, true, true);
		newMarker.intensity = defaultEmotionIntensity;
		newMarker.continuousVariation = defaultContinuousVariation;
		newMarker.intensityVariation = defaultIntensityVariation;
		newMarker.blendableVariation = defaultBlendableVariation;
		newMarker.bonePositionVariation = defaultBonePositionVariation;
		newMarker.boneRotationVariation = defaultBoneRotationVariation;

		emotionData.Add(newMarker);
		emotionData.Sort(EmotionSort);
		unorderedEmotionData.Add(newMarker);
		FixEmotionBlends();
		changed = true;
		previewOutOfDate = true;
	}

	void GesturePicked (object raw)
	{
		object[] data = (object[])raw;
		string picked = (string)data[0];
		float time = (float)data[1];

		Undo.RecordObject(this, "Add Gesture Marker");
		GestureMarker newMarker = new GestureMarker(picked, time);
		gestureData.Add(newMarker);
		changed = true;
	}

	void AddEmotionMixer (object data)
	{
		float time = (float)data;

		Undo.RecordObject(this, "Add Emotion Marker");
		EmotionMixer mixer = new EmotionMixer();

		EmotionMarker newMarker = new EmotionMarker(mixer, time, Mathf.Clamp(time + 0.1f, time + 0.003f, 1), 0.025f, -0.025f, false, false, true, true);
		emotionData.Add(newMarker);
		emotionData.Sort(EmotionSort);
		unorderedEmotionData.Add(newMarker);
		FixEmotionBlends();
		changed = true;
		previewOutOfDate = true;

	}
	#endregion

	#region Marker Editing
	void ChangePhonemePicked (object info)
	{
		Undo.RecordObject(this, "Change Phoneme Marker");
		List<int> finalInfo = (List<int>)info;

		PhonemeMarker marker = phonemeData[finalInfo[0]];
		marker.phonemeNumber = finalInfo[1];

		for (int i = 0; i < phonemeData.Count; i++)
		{
			PhonemeMarker m = phonemeData[i];

			if (i + 1 < phonemeData.Count)
			{
				if (m.sustain && phonemeData[i + 1].phonemeNumber != m.phonemeNumber)
				{
					m.sustain = false;
					if (i - 1 >= 0)
					{
						if (phonemeData[i - 1].phonemeNumber == m.phonemeNumber)
						{
							phonemeData[i - 1].sustain = true;
						}
					}
				}
			}
			else if (i - 1 >= 0)
			{
				if (m.sustain && phonemeData[i - 1].phonemeNumber == m.phonemeNumber)
				{
					m.sustain = false;
					phonemeData[i - 1].sustain = true;
				}
				else
				{
					m.sustain = false;
				}
			}
			else
			{
				m.sustain = false;
			}
		}

		changed = true;
		previewOutOfDate = true;
	}

	void ChangeGesturePicked (object info)
	{
		Undo.RecordObject(this, "Change Gesture Marker");
		List<object> finalInfo = (List<object>)info;

		GestureMarker marker = gestureData[(int)finalInfo[0]];
		marker.gesture = (string)finalInfo[1];
		changed = true;
	}

	void ChangeEmotionPicked (object info)
	{
		Undo.RecordObject(this, "Change Emotion Marker");
		List<object> finalInfo = (List<object>)info;

		EmotionMarker marker = (EmotionMarker)finalInfo[0];
		marker.emotion = (string)finalInfo[1];
		marker.isMixer = false;

		changed = true;
		previewOutOfDate = true;
	}

	void ChangeEmotionMixer (object data)
	{
		Undo.RecordObject(this, "Change Emotion Marker");

		EmotionMarker marker = (EmotionMarker)data;
		EmotionMixer mixer = new EmotionMixer();

		marker.isMixer = true;
		marker.mixer = mixer;
		changed = true;
		previewOutOfDate = true;
	}

	void OpenEmotionMixer (object info)
	{
		EmotionMarker marker = (EmotionMarker)info;
		highlightedMarker = emotionData.IndexOf(marker);
		EmotionMixerWindow.CreateWindow(this, marker.mixer, settings);
	}

	void PhonemeMarkerSettings (object info)
	{
		PhonemeMarker marker = (PhonemeMarker)info;
		highlightedMarker = phonemeData.IndexOf(marker);
		MarkerSettingsWindow.CreateWindow(this, this, marker);
	}

	void PhonemeMarkerSettingsMulti ()
	{
		MarkerSettingsWindow.CreateWindow(this, this, phonemeData, selection);
	}

	void ToggleSustain (object marker)
	{
		PhonemeMarker m = (PhonemeMarker)marker;
		m.sustain = !m.sustain;

		changed = true;
		previewOutOfDate = true;
	}

	void EmotionMarkerSettings (object info)
	{
		EmotionMarker marker = (EmotionMarker)info;
		highlightedMarker = emotionData.IndexOf(marker);
		MarkerSettingsWindow.CreateWindow(this, this, marker);
	}

	void EmotionMarkerSettingsMulti ()
	{
		MarkerSettingsWindow.CreateWindow(this, this, emotionData, selection);
	}

	void DefaultMarkerSettings ()
	{
		DefaultMarkerSettingsWindow.CreateWindow(this, this, markerTab);
	}

	void CopyMarkers ()
	{
		copyBufferType = markerTab;
		copyBuffer.Clear();

		if (selection.Count == 0)
		{
			if (highlightedMarker == -1)
				return;

			switch (markerTab)
			{
				case 0:
					copyBuffer.Add(phonemeData[highlightedMarker].CreateCopy());
					break;
				case 1:
					copyBuffer.Add(emotionData[highlightedMarker]);
					break;
				case 2:
					copyBuffer.Add(gestureData[highlightedMarker]);
					break;
			}
		}
		else
		{
			for (int s = 0; s < selection.Count; s++)
			{
				switch (markerTab)
				{
					case 0:
						copyBuffer.Add(phonemeData[selection[s]].CreateCopy());
						break;
					case 1:
						copyBuffer.Add(emotionData[selection[s]]);
						break;
					case 2:
						copyBuffer.Add(gestureData[selection[s]]);
						break;
				}
			}
		}

		ShowNotification(new GUIContent(string.Format("Copied {0} Marker{1}", copyBuffer.Count, copyBuffer.Count == 1 ? "" : "s")));
	}

	void PasteMarkers (float startTime)
	{
		if (copyBuffer.Count < 1)
			return;
		if (copyBufferType != markerTab)
		{
			ShowNotification(new GUIContent(string.Format("Can't paste {0} into the {1} track!", markerTypes[copyBufferType], markerTypes[markerTab])));
			return;
		}

		selection.Clear();

		float selectionStartTime = 0;

		switch (copyBufferType)
		{
			case 0:
				selectionStartTime = ((PhonemeMarker)copyBuffer[0]).time;

				for (int i = 0; i < copyBuffer.Count; i++)
				{
					PhonemeMarker marker = (PhonemeMarker)copyBuffer[i];
					PhonemeMarker newMarker = marker.CreateCopy();

					newMarker.time = (marker.time - selectionStartTime) + startTime;

					phonemeData.Add(newMarker);
					phonemeData.Sort(LipSync.SortTime);
				}
				break;
			case 1:
				selectionStartTime = ((EmotionMarker)copyBuffer[0]).startTime;

				for (int i = 0; i < copyBuffer.Count; i++)
				{
					EmotionMarker marker = (EmotionMarker)copyBuffer[i];
					EmotionMarker newMarker = marker.CreateCopy();

					float duration = marker.endTime - marker.startTime;
					newMarker.startTime = (marker.startTime - selectionStartTime) + startTime;
					newMarker.endTime = newMarker.startTime + duration;

					emotionData.Add(newMarker);
					emotionData.Sort(EmotionSort);
					unorderedEmotionData.Add(newMarker);
					FixEmotionBlends();
				}
				break;
			case 2:
				selectionStartTime = ((GestureMarker)copyBuffer[0]).time;

				for (int i = 0; i < copyBuffer.Count; i++)
				{
					GestureMarker marker = (GestureMarker)copyBuffer[i];
					GestureMarker newMarker = marker.CreateCopy();

					newMarker.time = (marker.time - selectionStartTime) + startTime;

					gestureData.Add(newMarker);
					gestureData.Sort(LipSync.SortTime);
				}
				break;
		}

		changed = true;
		previewOutOfDate = true;
	}

	#endregion

	#region Marker Removal
	void DeletePhoneme (object marker)
	{
		Undo.RecordObject(this, "Delete Phoneme Marker");
		int i = phonemeData.IndexOf((PhonemeMarker)marker);

		if (i - 1 >= 0)
		{
			phonemeData[i - 1].sustain = false;
		}

		phonemeData.Remove((PhonemeMarker)marker);
		selection.Clear();
		firstSelection = 0;
		changed = true;
		previewOutOfDate = true;
	}

	void DeleteSelectedPhonemes ()
	{
		Undo.RecordObject(this, "Delete Phoneme Markers");
		selection.Sort(SortInt);

		int i = phonemeData.IndexOf(phonemeData[selection[0]]);

		if (i - 1 >= 0)
		{
			phonemeData[i - 1].sustain = false;
		}

		for (int marker = selection.Count - 1; marker >= 0; marker--)
		{
			phonemeData.Remove((phonemeData[selection[marker]]));
		}
		selection.Clear();
		firstSelection = 0;
		changed = true;
		previewOutOfDate = true;
	}

	void DeleteEmotion (object marker)
	{
		Undo.RecordObject(this, "Delete Emotion Marker");
		currentMarker = emotionData.IndexOf((EmotionMarker)marker);
		changed = true;
		previewOutOfDate = true;
		selection.Clear();
		firstSelection = 0;

		if (currentMarker - 1 > -1)
		{
			if (emotionData[currentMarker - 1].endTime == emotionData[currentMarker].startTime)
			{
				previousMarker = emotionData[currentMarker - 1];
				previousMarker.blendOutTime = 0;
				previousMarker.blendToMarker = false;
			}
		}
		if (currentMarker + 1 < emotionData.Count)
		{
			if (emotionData[currentMarker + 1].startTime == emotionData[currentMarker].endTime)
			{
				nextMarker = emotionData[currentMarker + 1];
				nextMarker.blendInTime = 0;
				nextMarker.blendFromMarker = false;
			}
		}
		currentMarker = -1;
		emotionData.Remove((EmotionMarker)marker);
		unorderedEmotionData.Remove((EmotionMarker)marker);
		FixEmotionBlends();
	}

	void DeleteSelectedEmotions ()
	{
		Undo.RecordObject(this, "Delete Emotion Markers");
		selection.Sort(SortInt);

		for (int marker = selection.Count - 1; marker >= 0; marker--)
		{
			currentMarker = selection[marker];

			if (currentMarker - 1 > -1)
			{
				if (emotionData[currentMarker - 1].endTime == emotionData[currentMarker].startTime)
				{
					previousMarker = emotionData[currentMarker - 1];
					previousMarker.blendOutTime = 0;
					previousMarker.blendToMarker = false;
				}
			}
			if (currentMarker + 1 < emotionData.Count)
			{
				if (emotionData[currentMarker + 1].startTime == emotionData[currentMarker].endTime)
				{
					nextMarker = emotionData[currentMarker + 1];
					nextMarker.blendInTime = 0;
					nextMarker.blendFromMarker = false;
				}
			}

			unorderedEmotionData.Remove(emotionData[selection[marker]]);
			emotionData.RemoveAt(selection[marker]);
		}

		currentMarker = -1;
		FixEmotionBlends();

		selection.Clear();
		firstSelection = 0;
		changed = true;
		previewOutOfDate = true;
	}

	void DeleteGesture (object marker)
	{
		Undo.RecordObject(this, "Delete Gesture Marker");
		gestureData.Remove((GestureMarker)marker);
		changed = true;
		selection.Clear();
		firstSelection = 0;
	}

	void DeleteSelectedGestures ()
	{
		Undo.RecordObject(this, "Delete Gesture Markers");
		selection.Sort(SortInt);
		for (int marker = selection.Count - 1; marker >= 0; marker--)
		{
			gestureData.Remove((gestureData[selection[marker]]));
		}
		selection.Clear();
		firstSelection = 0;
		changed = true;
	}

	void RemoveMissingEmotions ()
	{
		foreach (EmotionMarker marker in emotionData)
		{
			bool verified = false;
			foreach (string emotion in settings.emotions)
			{
				if (marker.emotion == emotion)
					verified = true;
			}

			if (!verified)
			{
				DeleteEmotion(marker);
			}
		}

		changed = true;
		previewOutOfDate = true;
	}

	void RemoveMissingGestures ()
	{
		foreach (GestureMarker marker in gestureData)
		{
			bool verified = false;
			foreach (string gesture in settings.gestures)
			{
				if (marker.gesture == gesture)
					verified = true;
			}

			if (!verified)
			{
				DeleteGesture(marker);
			}
		}
	}
	#endregion

	#region File Methods
	void OnNewClick ()
	{
		if (changed)
		{
			int choice = EditorUtility.DisplayDialogComplex("Save Changes", "You have made changes to the current file, do you want to save them before closing?", "Yes", "No", "Cancel");

			if (choice == 0)
			{
				OnSaveClick();
			}
			else if (choice == 2)
			{
				return;
			}
		}

		lastLoad = "";
		fileName = "Untitled";
		AudioUtility.StopAllClips();
		seekPosition = 0;
		oldSeekPosition = 0;
		clip = null;
		phonemeData = new List<PhonemeMarker>();
		emotionData = new List<EmotionMarker>();
		unorderedEmotionData = new List<EmotionMarker>();
		oldClip = null;
		changed = false;
		fileLength = 10;
		transcript = "";
		previewOutOfDate = true;
#if !(UNITY_5_3_6 || UNITY_5_4_OR_NEWER)
		waveform = null;
		DestroyImmediate(waveform);
#endif
		fileLength = 10;

	}

	void OnLoadClick ()
	{
		string loadPath = EditorUtility.OpenFilePanel("Load LipSync Data File", "Assets", "asset");

		if (loadPath != "")
		{
			loadPath = "Assets" + loadPath.Substring(Application.dataPath.Length);
			LoadFile(loadPath);
		}
	}

	void OnSaveClick ()
	{
		if (lastLoad != "")
		{
			string savePath = lastLoad;
			LipSyncData outputFile = SaveFile(settings, savePath, false, transcript, fileLength, phonemeData.ToArray(), emotionData.ToArray(), gestureData.ToArray(), clip);
			fileName = outputFile.name + ".Asset";
			changed = false;
		}
		else
		{
			OnSaveAsClick();
		}
	}

	void OnSaveAsClick ()
	{
		string defaultName = "New Asset";

		if (clip != null)
		{
			defaultName = clip.name;
		}

		string savePath = EditorUtility.SaveFilePanel("Save LipSync Data File", "Assets" + lastLoad, defaultName + ".asset", "asset");
		if (savePath != "")
		{
			savePath = "Assets" + savePath.Substring(Application.dataPath.Length);
			LipSyncData outputFile = SaveFile(settings, savePath, false, transcript, fileLength, phonemeData.ToArray(), emotionData.ToArray(), gestureData.ToArray(), clip);
			fileName = outputFile.name + ".Asset";
			changed = false;
			lastLoad = savePath;
		}
	}

	void OnUnityExport ()
	{
		string savePath = EditorUtility.SaveFilePanel("Export LipSync Data and audio", "Assets" + lastLoad, clip.name + ".unitypackage", "unitypackage");
		if (savePath != "")
		{
			if (!savePath.Contains("Assets/"))
			{
				if (EditorUtility.DisplayDialog("Invalid Save Path", "Cannot export outside the assets folder. Do you want to pick a different folder?", "yes", "no"))
				{
					OnUnityExport();
				}
				return;
			}

			savePath = "Assets" + savePath.Substring(Application.dataPath.Length);
			string folderPath = savePath.Remove(savePath.Length - Path.GetFileName(savePath).Length) + Path.GetFileNameWithoutExtension(savePath);
			AssetDatabase.CreateFolder(savePath.Remove(savePath.Length - (Path.GetFileName(savePath).Length + 1)), Path.GetFileNameWithoutExtension(savePath));

			string originalName = fileName;

			if (clip != null)
			{
				AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(clip), folderPath + "/" + Path.GetFileName(AssetDatabase.GetAssetPath(clip)));
				AssetDatabase.ImportAsset(folderPath + "/" + Path.GetFileName(AssetDatabase.GetAssetPath(clip)));
			}

			AudioClip newClip = AssetDatabase.LoadAssetAtPath<AudioClip>(folderPath + "/" + Path.GetFileName(AssetDatabase.GetAssetPath(clip)));
			AudioClip originalClip = clip;
			if (newClip != null)
			{
				clip = newClip;
			}
			else
			{
				Debug.Log("LipSync: AudioClip copy at " + folderPath + "/" + Path.GetFileName(AssetDatabase.GetAssetPath(clip)) + " could not be reloaded for compression. Proceding without AudioClip.");
			}

			SaveFile(settings, folderPath + "/" + Path.ChangeExtension(Path.GetFileName(savePath), ".asset"), false, transcript, fileLength, phonemeData.ToArray(), emotionData.ToArray(), gestureData.ToArray(), clip);

			LipSyncData file = AssetDatabase.LoadAssetAtPath<LipSyncData>(folderPath + "/" + Path.ChangeExtension(Path.GetFileName(savePath), ".asset"));
			if (file != null)
			{
				AssetDatabase.ExportPackage(folderPath, savePath, ExportPackageOptions.Recurse);
				AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
				AssetDatabase.DeleteAsset(folderPath);

				fileName = originalName;
				lastLoad = "";
			}
			else
			{
				Debug.LogError("LipSync: File could not be reloaded for compression. Aborting Export.");
			}

			clip = originalClip;
		}
	}

	void OnXMLExport ()
	{
		string savePath = EditorUtility.SaveFilePanel("Export LipSync Data to XML", "Assets" + lastLoad, Path.GetFileNameWithoutExtension(fileName) + ".xml", "xml");
		if (savePath != "")
		{
			savePath = "Assets" + savePath.Substring(Application.dataPath.Length);
			SaveFile(settings, savePath, true, transcript, fileLength, phonemeData.ToArray(), emotionData.ToArray(), gestureData.ToArray(), clip);
		}
	}

	void OnXMLImport ()
	{
		string xmlPath = EditorUtility.OpenFilePanel("Import LipSync Data from XML", "Assets" + lastLoad, "xml");
		bool loadAudio = EditorUtility.DisplayDialog("Load Audio?", "Do you want to load an audioclip for this animation?", "Yes", "No");
		string audioPath = "";
		if (loadAudio)
		{
			audioPath = EditorUtility.OpenFilePanel("Load AudioClip", "Assets" + lastLoad, "wav;*.mp3;*.ogg");
		}

		if (xmlPath != "")
		{
			xmlPath = "Assets" + xmlPath.Substring(Application.dataPath.Length);
			TextAsset xmlFile = AssetDatabase.LoadAssetAtPath<TextAsset>(xmlPath);

			if (loadAudio)
			{
				audioPath = "Assets" + audioPath.Substring(Application.dataPath.Length);
				AudioClip linkedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath);

				LoadXML(xmlFile, linkedClip, false);
			}
			else
			{
				LoadXML(xmlFile, false);
			}
		}
	}

	void ShowProjectSettings ()
	{
		LipSyncProjectSettings.ShowWindow();
	}
	#endregion

	#region Edit Methods
	void SelectAll ()
	{
		if (markerTab == 0)
		{
			if (phonemeData.Count > 0)
			{
				selection.Clear();
				firstSelection = 0;
				for (int marker = 0; marker < phonemeData.Count; marker++)
				{
					selection.Add(marker);
				}
			}
		}
		else if (markerTab == 1)
		{
			if (emotionData.Count > 0)
			{
				selection.Clear();
				firstSelection = 0;
				for (int marker = 0; marker < emotionData.Count; marker++)
				{
					selection.Add(marker);
				}
			}
		}
		else if (markerTab == 2)
		{
			if (gestureData.Count > 0)
			{
				selection.Clear();
				firstSelection = 0;
				for (int marker = 0; marker < gestureData.Count; marker++)
				{
					selection.Add(marker);
				}
			}
		}
	}

	void SelectNone ()
	{
		selection.Clear();
		firstSelection = 0;
	}

	void InvertSelection ()
	{
		if (markerTab == 0)
		{
			if (phonemeData.Count > 0)
			{
				List<int> tempSelection = new List<int>();
				for (int marker = 0; marker < phonemeData.Count; marker++)
				{
					if (!selection.Contains(marker))
					{
						if (tempSelection.Count == 0)
						{
							firstSelection = marker;
						}
						tempSelection.Add(marker);
					}
				}
				selection = tempSelection;
			}
		}
		else if (markerTab == 1)
		{
			if (emotionData.Count > 0)
			{
				List<int> tempSelection = new List<int>();
				for (int marker = 0; marker < emotionData.Count; marker++)
				{
					if (!selection.Contains(marker))
					{
						if (tempSelection.Count == 0)
						{
							firstSelection = marker;
						}
						tempSelection.Add(marker);
					}
				}
				selection = tempSelection;
			}
		}
		else if (markerTab == 2)
		{
			if (gestureData.Count > 0)
			{
				List<int> tempSelection = new List<int>();
				for (int marker = 0; marker < gestureData.Count; marker++)
				{
					if (!selection.Contains(marker))
					{
						if (tempSelection.Count == 0)
						{
							firstSelection = marker;
						}
						tempSelection.Add(marker);
					}
				}
				selection = tempSelection;
			}
		}
	}

	void SetIntensitiesVolume ()
	{
		SetIntensityWindow.CreateWindow(this, this);
	}

	void ResetIntensities ()
	{
		if (markerTab == 0)
		{
			for (int m = 0; m < phonemeData.Count; m++)
			{
				phonemeData[m].intensity = 1;
			}
		}
		else if (markerTab == 1)
		{
			for (int m = 0; m < emotionData.Count; m++)
			{
				emotionData[m].intensity = 1;
			}
		}

		changed = true;
		previewOutOfDate = true;
	}

	void ClipSettings ()
	{
		ClipSettingsWindow.CreateWindow(this, this);
	}
	#endregion

	#region Realtime Preview
	void TargetChosen (object data)
	{
		if (data == null)
		{
			visualPreview = false;
		}
		else
		{
			LipSync target = (LipSync)data;
			visualPreview = true;
			previewTarget = target;
			target.onSettingsChanged += () =>
			{
				previewOutOfDate = true;
			};
			previewOutOfDate = true;
			SceneView.RepaintAll();
		}
	}

	void UpdatePreview (float time)
	{
		if (previewTarget != null)
		{
			if (previewTarget.blendSystem != null)
			{
				if (previewTarget.blendSystem.isReady)
				{
					if (previewOutOfDate)
					{
						previewTarget.PreviewAtTime(0);
						previewTarget.TempLoad(phonemeData, emotionData, clip, fileLength);
						previewTarget.ProcessData();
						previewOutOfDate = false;
					}

					previewTarget.PreviewAtTime(time);
					EditorUtility.SetDirty(previewTarget.blendSystem);
				}
			}
		}
	}
	#endregion

	#region Playback Methods
	void Stop ()
	{
		isPaused = true;
		isPlaying = false;
		if (clip)
			AudioUtility.StopClip(clip);
		seekPosition = 0;
		oldSeekPosition = seekPosition;
		float vpDiff = viewportEnd - viewportStart;
		viewportStart = 0;
		viewportEnd = vpDiff;
	}

	void PlayPause ()
	{
		if (isPlaying && !isPaused)
		{
			AudioUtility.PauseClip(clip);
			isPaused = true;
		}
		else
		{
			AudioUtility.PlayClip(clip);

			isPaused = false;
			isPlaying = true;
		}
	}
	#endregion

	#region Generic Methods
	void OpenURL (object url)
	{
		Application.OpenURL((string)url);
	}
	#endregion

	// ********************

	public bool MinMaxScrollbar (Rect position, Rect viewportRect, ref float minValue, ref float maxValue, float minLimit, float maxLimit, float minThumbSize)
	{
		float thumbWidth = (maxValue - minValue) / (maxLimit - minLimit);
		Rect thumbRect = new Rect((position.x + 32) + ((position.width - 64) * (minValue / maxLimit)), position.y, (position.width - 64) * thumbWidth, position.height);
		Rect thumbLeftRect = new Rect(thumbRect.x - 15, thumbRect.y, 15, thumbRect.height);
		Rect thumbRightRect = new Rect(thumbRect.x + thumbRect.width, thumbRect.y, 15, thumbRect.height);

		// Draw Dummy Scrollbar
		GUI.Box(new Rect(position.x + 17, position.y, position.width - 34, position.height), "", (GUIStyle)"horizontalScrollbar");

		if (GUI.Button(new Rect(position.x, position.y, 17, position.height), "", (GUIStyle)"horizontalScrollbarLeftButton"))
		{
			float size = maxValue - minValue;
			minValue -= 0.2f;
			maxValue = minValue + size;

			if (minValue < minLimit)
			{
				minValue = minLimit;
				maxValue = minValue + size;
			}
		}

		if (GUI.Button(new Rect((position.x + position.width) - 17, position.y, 17, position.height), "", (GUIStyle)"horizontalScrollbarRightButton"))
		{
			float size = maxValue - minValue;
			minValue += 0.2f;
			maxValue = minValue + size;

			if (maxValue > maxLimit)
			{
				maxValue = maxLimit;
				minValue = maxValue - size;
			}
		}

		GUI.Box(new Rect(thumbRect.x - 15, thumbRect.y, thumbRect.width + 30, thumbRect.height), "", (GUIStyle)"HorizontalMinMaxScrollbarThumb");

		// Logic
		if (Event.current.type == EventType.MouseDown && draggingScrollbar == 0)
		{
			if (thumbRect.Contains(Event.current.mousePosition))
			{
				draggingScrollbar = 1;
				scrollbarStartOffset = Event.current.mousePosition.x - thumbRect.x + 32;
				Event.current.Use();
			}
			else if (thumbLeftRect.Contains(Event.current.mousePosition))
			{
				draggingScrollbar = 2;
				scrollbarStartOffset = Event.current.mousePosition.x - thumbLeftRect.x + 17;
				Event.current.Use();
			}
			else if (thumbRightRect.Contains(Event.current.mousePosition))
			{
				draggingScrollbar = 3;
				scrollbarStartOffset = Event.current.mousePosition.x - thumbRightRect.x + 32;
				Event.current.Use();
			}
		}

		if (Event.current.type == EventType.MouseDrag && draggingScrollbar == 1)
		{
			float size = maxValue - minValue;
			minValue = ((Event.current.mousePosition.x - scrollbarStartOffset) / (position.width - 64)) * maxLimit;
			maxValue = minValue + size;

			if (minValue < minLimit)
			{
				minValue = minLimit;
				maxValue = minValue + size;
			}
			else if (maxValue > maxLimit)
			{
				maxValue = maxLimit;
				minValue = maxValue - size;
			}

			Event.current.Use();
		}
		else if (Event.current.type == EventType.MouseDrag && draggingScrollbar == 2)
		{
			minValue = ((Event.current.mousePosition.x - scrollbarStartOffset) / (position.width - 64)) * maxLimit;

			if (minValue < minLimit)
			{
				minValue = minLimit;
			}
			minValue = Mathf.Clamp(minValue, 0, maxValue - minThumbSize);

			Event.current.Use();
		}
		else if (Event.current.type == EventType.MouseDrag && draggingScrollbar == 3)
		{
			maxValue = ((Event.current.mousePosition.x - scrollbarStartOffset) / (position.width - 64)) * maxLimit;

			if (maxValue > maxLimit)
			{
				maxValue = maxLimit;
			}
			maxValue = Mathf.Clamp(maxValue, minValue + minThumbSize, fileLength);

			Event.current.Use();
		}

		if (Event.current.type == EventType.ScrollWheel && viewportRect.Contains(Event.current.mousePosition))
		{
			//float viewportSeconds = (maxValue - minValue);
			//float pixelsPerSecond = viewportRect.width / viewportSeconds;

			//float pointerTime = minValue + (Event.current.mousePosition.x / pixelsPerSecond);
			//float viewCentre = minValue + ((maxValue - minValue) / 2);
			//float diff = pointerTime - viewCentre;

			minValue -= Event.current.delta.y / 8f;
			maxValue += Event.current.delta.y / 8f;

			minValue = Mathf.Clamp(minValue, 0, maxValue - minThumbSize);
			maxValue = Mathf.Clamp(maxValue, minValue + minThumbSize, fileLength);

			Event.current.Use();
		}

		if (Event.current.type == EventType.MouseUp && draggingScrollbar > 0)
		{
			draggingScrollbar = 0;
			Event.current.Use();
			return true;
		}

		return false;
	}

	// Customisation Delegates
	public delegate void LipSyncClipEditorMenuDelegate (LipSyncClipSetup instance);
	public delegate void LipSyncClipEditorMenuItemDelegate (LipSyncClipSetup instance, GenericMenu menu);
}