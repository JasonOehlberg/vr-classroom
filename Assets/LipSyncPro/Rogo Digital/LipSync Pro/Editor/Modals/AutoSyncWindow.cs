using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using RogoDigital;
using RogoDigital.Lipsync;
using System.Collections.Generic;
using System.IO;

public class AutoSyncWindow : ModalWindow {
	private LipSyncClipSetup setup;

	// Single Mode
	private AudioClip clip;

	// Multiple Mode
	private List<AudioClip> clips;
	private int currentClip = 0;
	private bool xmlMode = false;

	private string[] languageModelNames = new string[0];

	private int languageModel;
	private bool attemptAudioConversion = false;
	private bool allphone_ciEnabled;
	private bool backtraceEnabled;
	private int beamExponent;
	private int pbeamExponent;
	private float lwValue;
	private bool doCleanup;
	private float cleanupAggression;

	private Vector2 scroll;
	private int tab = 0;
	private bool ready;

	private bool soXDefined = false;
	private bool advancedOptions = false;
	private AnimBool advancedBool;
	private AnimBool cleanupBool;

	void OnEnable () {
		languageModelNames = AutoSyncLanguageModel.FindModels();
		languageModel = EditorPrefs.GetInt("LipSync_DefaultLanguageModel", 0);
		attemptAudioConversion = EditorPrefs.GetBool("LipSync_SoXAvailable", false);
		allphone_ciEnabled = EditorPrefs.GetBool("LipSync_Allphone_ciEnabled", true);
		backtraceEnabled = EditorPrefs.GetBool("LipSync_BacktraceEnabled", false);
		beamExponent = EditorPrefs.GetInt("LipSync_BeamExponent", -20);
		pbeamExponent = EditorPrefs.GetInt("LipSync_PbeamExponent", -20);
		lwValue = EditorPrefs.GetFloat("LipSync_LwValue", 2.5f);
		doCleanup = EditorPrefs.GetBool("LipSync_DoCleanup", false);
		cleanupAggression = EditorPrefs.GetFloat("LipSync_CleanupAggression", 0.003f);

		soXDefined = attemptAudioConversion;

		cleanupBool = new AnimBool(doCleanup);
		cleanupBool.valueChanged.AddListener(Repaint);
		advancedBool = new AnimBool(advancedOptions);
		advancedBool.valueChanged.AddListener(Repaint);

		clips = new List<AudioClip>();
	}

	void OnGUI () {
		GUILayout.Space(10);
		tab = GUILayout.Toolbar(tab, new string[] { "AutoSync Settings", "Batch Process" });
		GUILayout.Space(10);

		if (tab == 0) {
			scroll = GUILayout.BeginScrollView(scroll);
			if (languageModelNames.Length > 0) {
				languageModel = EditorGUILayout.Popup("Language Model", languageModel, languageModelNames, GUILayout.MaxWidth(400));
				if (clip == null) {
					ready = false;
				} else {
					ready = true;
				}
			} else {
				EditorGUILayout.HelpBox("No language models found. You can download language models from the extensions window or the LipSync website.", MessageType.Error);
				ready = false;
			}
			GUILayout.Space(5);
			EditorGUI.BeginDisabledGroup(!soXDefined);
			attemptAudioConversion = EditorGUILayout.Toggle(new GUIContent("Enable Audio Conversion", "Improves compatibility with a wider range of Audio Formats by creating a temporary copy of your file and converting it to the correct format."), attemptAudioConversion);
			EditorGUI.EndDisabledGroup();
			if (!soXDefined) {
				GUILayout.Space(5);
				EditorGUILayout.HelpBox("SoX Audio Converter is not defined. See \"Using SoX with AutoSync.pdf\" for more info.", MessageType.Warning);
			}
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Default Preset")) {
				if (languageModelNames == null)
					return;
				if (languageModelNames.Length == 0)
					return;

				AutoSync.AutoSyncOptions preset = new AutoSync.AutoSyncOptions(languageModelNames[languageModel], attemptAudioConversion, AutoSync.AutoSyncOptionsPreset.Default);

				doCleanup = preset.doCleanup;
				cleanupAggression = preset.cleanupAggression;
				allphone_ciEnabled = preset.allphone_ciEnabled;
				backtraceEnabled = preset.backtraceEnabled;
				beamExponent = preset.beamExponent;
				pbeamExponent = preset.pbeamExponent;
				lwValue = preset.lwValue;

				cleanupBool.target = doCleanup;
			}
			if (GUILayout.Button("High Quality Preset")) {
				if (languageModelNames == null)
					return;
				if (languageModelNames.Length == 0)
					return;

				AutoSync.AutoSyncOptions preset = new AutoSync.AutoSyncOptions(languageModelNames[languageModel], attemptAudioConversion, AutoSync.AutoSyncOptionsPreset.HighQuality);

				doCleanup = preset.doCleanup;
				cleanupAggression = preset.cleanupAggression;
				allphone_ciEnabled = preset.allphone_ciEnabled;
				backtraceEnabled = preset.backtraceEnabled;
				beamExponent = preset.beamExponent;
				pbeamExponent = preset.pbeamExponent;
				lwValue = preset.lwValue;

				cleanupBool.target = doCleanup;
			}
			GUILayout.EndHorizontal();
			doCleanup = EditorGUILayout.Toggle(new GUIContent("Enable Auto Cleanup", "Will attempt to improve quality by removing markers close to other markers"), doCleanup);
			cleanupBool.target = doCleanup;
			if (EditorGUILayout.BeginFadeGroup(cleanupBool.faded)) {
				cleanupAggression = EditorGUILayout.Slider(new GUIContent("Cleanup Aggression", "How aggressive auto cleanup is. VERY low values recommended."), cleanupAggression, 0, 1);
				if (cleanupAggression > 0.01f) {
					if (cleanupAggression > 0.2f) {
						if (cleanupAggression > 0.95f) {
							EditorGUILayout.HelpBox("This will literally remove every marker.", MessageType.Error);
						} else {
							EditorGUILayout.HelpBox("Honestly, LOW values are best.", MessageType.Warning);
						}
					} else {
						EditorGUILayout.HelpBox("Cleanup Aggression should be a very low value to avoid removing wanted markers.", MessageType.Info);
					}
				}
			}
			EditorGUILayout.EndFadeGroup();
			GUILayout.Space(10);
			advancedOptions = EditorGUILayout.Toggle(new GUIContent("Show Advanced Options", "Warning: These options can cause more problems than they solve if set wrong!"), advancedOptions);
			advancedBool.target = advancedOptions;
			if (EditorGUILayout.BeginFadeGroup(advancedBool.faded)) {
				GUILayout.Label("Advanced", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox("Leave these options alone unless you know what you're doing! Incorrect settings here can seriously reduce AutoSync quality.", MessageType.Warning);
				allphone_ciEnabled = EditorGUILayout.Toggle("Enable Allphone_ci", allphone_ciEnabled);
				backtraceEnabled = EditorGUILayout.Toggle("Enable Backtrace", backtraceEnabled);
				beamExponent = EditorGUILayout.IntField("Beam Exponent", beamExponent);
				pbeamExponent = EditorGUILayout.IntField("Pbeam Exponent", pbeamExponent);
				lwValue = EditorGUILayout.FloatField("Lw Value", lwValue);
			}
			EditorGUILayout.EndFadeGroup();
			GUILayout.EndScrollView();
			GUILayout.Space(10);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			EditorGUI.BeginDisabledGroup(!ready);
			if (GUILayout.Button("Start Single Process", GUILayout.Height(25))) {
				AutoSync.AutoSyncOptions options = new AutoSync.AutoSyncOptions(languageModelNames[languageModel], attemptAudioConversion, allphone_ciEnabled, backtraceEnabled, beamExponent, pbeamExponent, lwValue, doCleanup, cleanupAggression);
				AutoSync.ProcessAudio(clip, FinishedProcessingSingle, OnAutoSyncFailed, options);
			}
			EditorGUI.EndDisabledGroup();

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(20);
		} else {
			if (languageModelNames.Length > 0) {
				ready = true;
			} else {
				EditorGUILayout.HelpBox("No language models found. You can download language models from the extensions window or the LipSync website.", MessageType.Warning);
				ready = false;
			}
			GUILayout.Space(5);
			GUILayout.Box("Select AudioClips", EditorStyles.boldLabel);
			GUILayout.Space(10);
			scroll = GUILayout.BeginScrollView(scroll);
			for (int a = 0; a < clips.Count; a++) {
				GUILayout.BeginHorizontal();
				GUILayout.Space(5);
				clips[a] = (AudioClip)EditorGUILayout.ObjectField(clips[a], typeof(AudioClip), false);
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Remove", GUILayout.MaxWidth(200))) {
					clips.RemoveAt(a);
					break;
				}
				GUILayout.Space(5);
				GUILayout.EndHorizontal();
			}
			GUILayout.Space(5);
			GUILayout.EndScrollView();
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Add AudioClip")) {
				clips.Add(null);
			}
			if (GUILayout.Button("Add Selected")) {
				foreach (AudioClip c in Selection.GetFiltered(typeof(AudioClip), SelectionMode.Assets)) {
					if (!clips.Contains(c))
						clips.Add(c);
				}
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(10);
			EditorGUILayout.HelpBox("Settings from the AutoSync Settings tab will be used. Make sure they are correct.", MessageType.Info);
			xmlMode = EditorGUILayout.Toggle("Export as XML", xmlMode);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			EditorGUI.BeginDisabledGroup(!ready);
			if (GUILayout.Button("Start Batch Process", GUILayout.Height(25))) {
				if (clips.Count > 0) {
					currentClip = 1;
					AutoSync.AutoSyncOptions options = new AutoSync.AutoSyncOptions(languageModelNames[languageModel], attemptAudioConversion, allphone_ciEnabled, backtraceEnabled, beamExponent, pbeamExponent, lwValue, doCleanup, cleanupAggression);
					AutoSync.ProcessAudio(clips[0], FinishedProcessingMulti, OnAutoSyncFailed, "1/" + clips.Count.ToString(), options);
				} else {
					ShowNotification(new GUIContent("No clips added for batch processing!"));
				}
			}
			EditorGUI.EndDisabledGroup();
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(20);
		}
	}

	void FinishedProcessingMulti (AudioClip finishedClip, List<PhonemeMarker> markers) {
		//Get Settings File
		string[] guids = AssetDatabase.FindAssets("ProjectSettings t:LipSyncProject");
		string spath = "";

		if (guids.Length > 0) {
			spath = AssetDatabase.GUIDToAssetPath(guids[0]);

			if (guids.Length > 1) Debug.LogWarning("LipSync: Multiple LipSyncProject files found. Only one will be used.");
		}

		LipSyncProject settings = (LipSyncProject)AssetDatabase.LoadAssetAtPath(spath, typeof(LipSyncProject));

		// Create File
		string path = AssetDatabase.GetAssetPath(finishedClip);
		path = Path.ChangeExtension(path, xmlMode ? "xml" : "asset");

		try {
			LipSyncClipSetup.SaveFile(settings, path, xmlMode, "", finishedClip.length, markers.ToArray(), new EmotionMarker[0],
				new GestureMarker[0], finishedClip);
		} catch {
			Debug.Log(settings);
			Debug.Log(path);
			Debug.Log(xmlMode);
			Debug.Log(finishedClip);
			Debug.Log(markers);
		}

		if (currentClip < clips.Count) {
			AutoSync.AutoSyncOptions options = new AutoSync.AutoSyncOptions(languageModelNames[languageModel], attemptAudioConversion, allphone_ciEnabled, backtraceEnabled, beamExponent, pbeamExponent, lwValue, doCleanup, cleanupAggression);
			AutoSync.ProcessAudio(clips[currentClip], FinishedProcessingMulti, OnAutoSyncFailed, (currentClip + 1).ToString() + "/" + clips.Count.ToString(), options);
			currentClip++;
		} else {
			AssetDatabase.Refresh();
			EditorUtility.ClearProgressBar();
			setup.ShowNotification(new GUIContent("Batch AutoSync Complete."));
			Close();
		}
	}

	void FinishedProcessingSingle (AudioClip clip, List<PhonemeMarker> markers) {
		if (markers.Count > 0) {
			setup.phonemeData = markers;
			setup.changed = true;
			setup.previewOutOfDate = true;
			setup.disabled = false;
			Close();
		}
	}

	void OnAutoSyncFailed (string error) {
		setup.disabled = false;
		Debug.LogError(error);
		Close();
	}

	public static void CreateWindow (ModalParent parent, LipSyncClipSetup setup, int mode) {
		AutoSyncWindow window = CreateInstance<AutoSyncWindow>();

		window.position = new Rect(parent.center.x - 250, parent.center.y - 150, 500, 300);
		window.minSize = new Vector2(500, 300);
		window.titleContent = new GUIContent("AutoSync");

		window.setup = setup;

		window.tab = mode;
		window.clip = setup.clip;
		window.Show(parent);
	}
}