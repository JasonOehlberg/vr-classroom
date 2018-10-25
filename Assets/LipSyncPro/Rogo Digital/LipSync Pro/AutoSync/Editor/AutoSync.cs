using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;

namespace RogoDigital.Lipsync
{
	public class AutoSync
	{

		private static List<PhonemeMarker>[] tempData;
		private static AudioClip tempClip;
		private static AutoSyncOptions tempOptions;
		private static AutoSyncDataReadyDelegate tempDelegate;
		private static AutoSyncFailedDelegate tempFailDelegate;
		private static string[] tempPaths;

		private static int multiFileIndex = 0;
		private static float multiFileLength = 0;
		private static float multiFileOffset = 0;

		/// <summary>
		/// Confirms a valid SoX installation
		/// </summary>
		/// <returns></returns>
		public static bool CheckSoX ()
		{
			// Get path from settings
			string soXPath = EditorPrefs.GetString("LipSync_SoXPath");
			bool gotOutput = false;

			if (string.IsNullOrEmpty(soXPath))
				return false;

			// Attempt to start application
			System.Diagnostics.Process process = new System.Diagnostics.Process();
			process.StartInfo.FileName = soXPath;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.RedirectStandardOutput = true;

			// Only verify if process output a string containing "SoX" on start
			process.OutputDataReceived += (object e, System.Diagnostics.DataReceivedEventArgs outLine) =>
			{
				if (!string.IsNullOrEmpty(outLine.Data))
				{
					if (outLine.Data.Contains("SoX"))
						gotOutput = true;
				}
			};

			// Fail if anything goes wrong
			try
			{
				process.Start();
			}
			catch
			{
				return false;
			}

			process.BeginOutputReadLine();
			process.WaitForExit();

			return gotOutput;
		}

		/// <summary>
		/// Begin processing an audioclip. Phoneme data will be passed along with the input AudioClip to the AutoSyncDataReadyDelegate callback. 
		/// </summary>
		/// <param name="clip">AudioClip to be processed.</param>
		/// <param name="languageModel">Name of a language model present in the project.</param>
		/// <param name="dataReadyCallback">Method that will receive the results of the process.</param>
		/// <param name="progressPrefix">Prefix shown on the progress bar.</param>
		/// <param name="enableConversion">If true, audio files will be temporarily converted if possible to maximise compatibility.</param>
		public static void ProcessAudio (AudioClip clip, AutoSyncDataReadyDelegate dataReadyCallback, AutoSyncFailedDelegate failedCallback, string progressPrefix, AutoSyncOptions options)
		{
			if (clip == null)
				return;
			EditorUtility.DisplayCancelableProgressBar(progressPrefix + " - Analysing Audio File", "Please wait, analysing file " + progressPrefix, 0.1f);

			bool converted = false;
			string audioPath = AssetDatabase.GetAssetPath(clip).Substring("/Assets".Length);

			if (audioPath != null)
			{
				// Get absolute path
				audioPath = Application.dataPath + "/" + audioPath;

				// Check Path
				if (audioPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || Path.GetFileNameWithoutExtension(audioPath).IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
				{
					EditorUtility.ClearProgressBar();
					failedCallback.Invoke("AutoSync failed. Audio path or filename contained invalid characters.");
					return;
				}

				bool failed = false;
				// Convert to acceptable format
				if (options.useAudioConversion)
				{
					if (CheckSoX())
					{
						EditorUtility.DisplayProgressBar(progressPrefix + " - Converting Audio File", "Please wait, converting file " + progressPrefix, 0.2f);
						converted = true;

						string newAudioPath = Application.dataPath + "/" + Path.GetFileNameWithoutExtension(audioPath) + "_temp_converted.wav";
						string soXPath = EditorPrefs.GetString("LipSync_SoXPath");

						// Convert to compatible .wav file
						string soXArgs = "\"" + audioPath + "\" -c 1 -b 16 -e s -r 16k \"" + newAudioPath + "\"";
						audioPath = newAudioPath;

						System.Diagnostics.Process process = new System.Diagnostics.Process();
						process.StartInfo.FileName = soXPath;
						process.StartInfo.Arguments = soXArgs;
						process.StartInfo.UseShellExecute = false;
						process.StartInfo.CreateNoWindow = true;
						process.StartInfo.RedirectStandardError = true;

						process.ErrorDataReceived += (object e, System.Diagnostics.DataReceivedEventArgs outLine) =>
						{
							if (!string.IsNullOrEmpty(outLine.Data))
							{
								if (outLine.Data.Contains("FAIL"))
								{
									failed = true;
									converted = false;
									process.Close();
									failedCallback.Invoke("AutoSync: SoX Conversion Failed: " + outLine.Data);
								}
							}
						};

						process.Start();
						process.BeginErrorReadLine();
						process.WaitForExit(5000);
					}
				}

				if (!File.Exists(audioPath) || failed)
				{
					EditorUtility.ClearProgressBar();
					return;
				}

				// Split into multiple clips if necessary
				if (clip.length > 30 && options.useAudioConversion)
				{

					multiFileLength = clip.length;
					multiFileOffset = 0;
					tempData = new List<PhonemeMarker>[Mathf.CeilToInt(clip.length / 30)];
					tempPaths = new string[Mathf.CeilToInt(clip.length / 30)];

					// Create paths
					for (int l = 0; l < Mathf.CeilToInt(clip.length / 30); l++)
					{
						tempPaths[l] = Application.dataPath + "/" + Path.GetFileNameWithoutExtension(audioPath) + "_chunk_" + (l + 1) + ".wav";
					}

					string soXPath = EditorPrefs.GetString("LipSync_SoXPath");
					string soXArgs = "\"" + audioPath + "\" \"" + Application.dataPath + "/" + Path.GetFileNameWithoutExtension(audioPath) + "_chunk_%1n.wav\" trim 0 30 : newfile : restart";

					System.Diagnostics.Process process = new System.Diagnostics.Process();
					process.StartInfo.FileName = soXPath;
					process.StartInfo.Arguments = soXArgs;
					process.StartInfo.UseShellExecute = false;
					process.StartInfo.CreateNoWindow = true;
					process.StartInfo.RedirectStandardError = true;

					process.ErrorDataReceived += (object e, System.Diagnostics.DataReceivedEventArgs outLine) =>
					{
						if (!string.IsNullOrEmpty(outLine.Data))
						{
							if (outLine.Data.Contains("FAIL"))
							{
								failedCallback.Invoke("AutoSync: SoX Audio Splitting Failed: " + outLine.Data);
								failed = true;
								converted = false;
								process.Close();
							}
						}
					};

					process.Start();
					process.BeginErrorReadLine();
					process.WaitForExit(5000);

					if (!File.Exists(audioPath) || failed)
					{
						EditorUtility.ClearProgressBar();
						return;
					}

					// Fix paths
					for (int l = 0; l < tempPaths.Length; l++)
					{
						tempPaths[l] = "Assets" + tempPaths[l].Substring(Application.dataPath.Length);
					}

					// Delete overlong temporary converted file and prevent autosync from attempting it
					tempDelegate = dataReadyCallback;
					tempFailDelegate = failedCallback;
					tempClip = clip;
					tempOptions = options;

					multiFileIndex = 0;

					if (File.Exists(audioPath))
					{
						File.Delete(audioPath);
						AssetDatabase.Refresh();
					}

					ProcessAudio(AssetDatabase.LoadAssetAtPath<AudioClip>(tempPaths[0]), MultiClipCallback, failedCallback, options);
					return;
				}

				EditorUtility.DisplayProgressBar(progressPrefix + " - Preparing AutoSync", "Please wait, preparing AutoSync.", 0.3f);

				// Load Language Model
				AutoSyncLanguageModel model = AutoSyncLanguageModel.Load(options.languageModel);
				if (model == null)
				{
					EditorUtility.ClearProgressBar();
					if (converted)
					{
						if (File.Exists(audioPath))
						{
							File.Delete(audioPath);
							AssetDatabase.Refresh();
						}
					}
					failedCallback.Invoke("AutoSync Failed: Language Model was not loaded.");
					return;
				}
				string basePath = model.GetBasePath();

				List<string> args = new List<string>();
				args.Add("-infile");
				args.Add(audioPath);
				args.Add("-hmm");
				args.Add(basePath + model.hmmDir);
				args.Add("-allphone");
				args.Add(basePath + model.allphoneFile);
				if (options.allphone_ciEnabled)
				{ args.Add("-allphone_ci"); args.Add("yes"); }
				if (options.backtraceEnabled)
				{ args.Add("-backtrace"); args.Add("yes"); }
				args.Add("-time");
				args.Add("yes");
				args.Add("-beam");
				args.Add("1e" + options.beamExponent);
				args.Add("-pbeam");
				args.Add("1e" + options.pbeamExponent);
				args.Add("-lw");
				args.Add(options.lwValue.ToString());

				EditorUtility.DisplayProgressBar(progressPrefix + " - Recognising Phonemes", "Please wait, recognising phonemes.", 0.5f);
				SphinxWrapper.Recognize(args.ToArray());

				ContinuationManager.Add(() => SphinxWrapper.isFinished, () =>
				{
					if (SphinxWrapper.error != null)
					{
						EditorUtility.ClearProgressBar();
						failedCallback.Invoke("AutoSync Failed.");
						EditorUtility.DisplayDialog("AutoSync Failed",
							"AutoSync failed. Check the console for more information.", "OK");
						return;
					}

					EditorUtility.DisplayProgressBar(progressPrefix + " - Generating Data", "Please wait, generating LipSync data.", 0.85f);

					List<PhonemeMarker> data = ParseOutput(
							SphinxWrapper.result.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries),
							model,
							clip
						);

					if (options.useAudioConversion)
						data = CleanupOutput(data, options.cleanupAggression);

					dataReadyCallback.Invoke(
						clip,
						data
					);

					if (converted)
					{
						if (File.Exists(audioPath))
						{
							File.Delete(audioPath);
							AssetDatabase.Refresh();
						}
					}
				});
			}
		}

		/// <summary>
		/// Begin processing an audioclip. Phoneme data will be passed along with the input AudioClip to the AutoSyncDataReadyDelegate callback. 
		/// </summary>
		/// <param name="clip">AudioClip to be processed.</param>
		/// <param name="languageModel">Name of a language model present in the project.</param>
		/// <param name="callback">Method that will receive the results of the process.</param>
		/// <param name="enableConversion">If true, audio files will be temporarily converted if possible to maximise compatibility.</param>
		public static void ProcessAudio (AudioClip clip, AutoSyncDataReadyDelegate callback, AutoSyncFailedDelegate failedCallback, AutoSyncOptions options)
		{
			ProcessAudio(clip, callback, failedCallback, "", options);
		}

		// Delegate for assembling multiple files together
		private static void MultiClipCallback (AudioClip clip, List<PhonemeMarker> data)
		{
			// Adjust data to fit within subclip
			for (int i = 0; i < data.Count; i++)
			{
				float newTime = multiFileOffset + (data[i].time * (clip.length / multiFileLength));
				data[i].time = newTime;
			}

			tempData[multiFileIndex] = data;

			multiFileIndex++;
			multiFileOffset += (clip.length / multiFileLength);

			if (multiFileIndex < tempData.Length)
			{
				tempOptions.useAudioConversion = false;
				ProcessAudio(AssetDatabase.LoadAssetAtPath<AudioClip>(tempPaths[multiFileIndex]), MultiClipCallback, tempFailDelegate, tempOptions);
			}
			else
			{
				// Delete temp files
				foreach (string path in tempPaths)
				{
					if (File.Exists(path))
					{
						File.Delete(path);
					}
				}

				AssetDatabase.Refresh();

				// Final assembly
				List<PhonemeMarker> finalMarkers = new List<PhonemeMarker>();

				foreach (List<PhonemeMarker> markers in tempData)
				{
					finalMarkers.AddRange(markers);
				}

				tempDelegate.Invoke(tempClip, finalMarkers);
			}
		}

		public static List<PhonemeMarker> CleanupOutput (List<PhonemeMarker> data, float aggressiveness)
		{
			List<PhonemeMarker> output = new List<PhonemeMarker>(data);
			List<bool> markedForDeletion = new List<bool>();
			output.Sort(LipSync.SortTime);

			for (int m = 0; m < data.Count; m++)
			{
				if (m > 0)
				{
					if (data[m].time - data[m - 1].time < aggressiveness && !markedForDeletion[m - 1])
					{
						markedForDeletion.Add(true);
					}
					else
					{
						markedForDeletion.Add(false);
					}
				}
				else
				{
					markedForDeletion.Add(false);
				}
			}

			for (int m = 0; m < markedForDeletion.Count; m++)
			{
				if (markedForDeletion[m])
				{
					output.Remove(data[m]);
				}
			}

			return output;
		}

		private static List<PhonemeMarker> ParseOutput (string[] lines, AutoSyncLanguageModel lm, AudioClip clip)
		{
			List<PhonemeMarker> results = new List<PhonemeMarker>();

			Dictionary<string, string> phonemeMapper = new Dictionary<string, string>();

			// Get Settings File
			string[] guids = AssetDatabase.FindAssets("ProjectSettings t:LipSyncProject");
			string path = "";

			if (guids.Length > 0)
			{
				path = AssetDatabase.GUIDToAssetPath(guids[0]);

				if (guids.Length > 1)
					Debug.LogWarning("LipSync: Multiple LipSyncProject files found. Only one will be used.");
			}

			LipSyncProject settings = (LipSyncProject)AssetDatabase.LoadAssetAtPath(path, typeof(LipSyncProject));

			if (settings == null)
			{
				LipSyncProject newSettings = ScriptableObject.CreateInstance<LipSyncProject>();
				newSettings.emotions = new string[] { "default" };
				newSettings.emotionColors = new Color[] { new Color(1f, 0.7f, 0.1f) };

				AssetDatabase.CreateAsset(settings, "Assets/Rogo Digital/LipSync Pro/ProjectSettings.asset");
				AssetDatabase.Refresh();

				settings = newSettings;
			}

			if (lm.phonemeMapper.Length == 0)
			{
				// Default Phoneme Mapper
				phonemeMapper = new Dictionary<string, string>() {
					// Vowels
					{"IY"          , "E"},
					{"IH"          , "AI"},
					{"EH"          , "E"},
					{"AE"          , "AI"},
					{"AH"          , "U"},
					{"UW"          , "O"},
					{"UH"          , "U"},
					{"AA"          , "AI"},
					{"AO"          , "AI"},
					{"EY"          , "AI"},
					{"AY"          , "AI"},
					{"OY"          , "O"},
					{"AW"          , "AI"},
					{"OW"          , "O"},
					{"ER"          , "U"},

					// Consonants
					{"JH"          , "CDGKNRSThYZ"},
					{"L"           , "L"},
					{"R"           , "CDGKNRSThYZ"},
					{"Y"           , "CDGKNRSThYZ"},
					{"W"           , "WQ"},
					{"M"           , "MBP"},
					{"N"           , "CDGKNRSThYZ"},
					{"NG"          , "CDGKNRSThYZ"},
					{"CH"          , "CDGKNRSThYZ"},
					{"J"           , "CDGKNRSThYZ"},
					{"DH"          , "CDGKNRSThYZ"},
					{"B"           , "MBP"},
					{"D"           , "CDGKNRSThYZ"},
					{"G"           , "CDGKNRSThYZ"},
					{"P"           , "MBP"},
					{"T"           , "CDGKNRSThYZ"},
					{"K"           , "CDGKNRSThYZ"},
					{"Z"           , "CDGKNRSThYZ"},
					{"ZH"          , "CDGKNRSThYZ"},
					{"V"           , "FV"},
					{"F"           , "FV"},
					{"TH"          , "CDGKNRSThYZ"},
					{"S"           , "CDGKNRSThYZ"},
					{"SH"          , "CDGKNRSThYZ"},
					{"HH"          , "CDGKNRSThYZ"},
				};
			}
			else
			{
				// LM Phoneme Mapper
				foreach (AutoSyncLanguageModel.PhonemeMapping mapping in lm.phonemeMapper)
				{
					phonemeMapper.Add(mapping.label, mapping.phonemeName);
				}
			}

			foreach (string line in lines)
			{
				if (string.IsNullOrEmpty(line))
					break;
				string[] tokens = line.Split(' ');

				try
				{
					if (tokens[0] != "SIL")
					{
						string phonemeName = phonemeMapper[tokens[0]];
						float startTime = float.Parse(tokens[1]) / clip.length;

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
							results.Add(new PhonemeMarker(phoneme, startTime));
						}
						else
						{
							Debug.LogWarning("Phoneme mapper returned '" + phonemeName + "' but this phoneme does not exist in the current set. Skipping this entry.");
						}
					}
				}
				catch (ArgumentOutOfRangeException)
				{
					Debug.LogWarning("Phoneme Label missing from return data. Skipping this entry.");
				}
				catch (KeyNotFoundException)
				{
					Debug.LogWarning("Phoneme Label '" + tokens[0] + "' not found in phoneme mapper. Skipping this entry.");
				}
			}

			EditorUtility.ClearProgressBar();
			return results;
		}

		public delegate void AutoSyncDataReadyDelegate (AudioClip clip, List<PhonemeMarker> markers);
		public delegate void AutoSyncFailedDelegate (string error);
		[Obsolete("Use AutoSyncDataReadyDelegate instead.")]
		public delegate void AutoSyncDataReady (AudioClip clip, List<PhonemeMarker> markers);

		public enum AutoSyncOptionsPreset
		{
			Default,
			HighQuality,
		}

		public struct AutoSyncOptions
		{
			public string languageModel;
			public bool useAudioConversion;
			public bool allphone_ciEnabled;
			public bool backtraceEnabled;
			public int beamExponent;
			public int pbeamExponent;
			public float lwValue;
			public bool doCleanup;
			public float cleanupAggression;

			public AutoSyncOptions (string languageModel, bool useAudioConversion, bool allphone_ciEnabled, bool backtraceEnabled, int beamExponent, int pbeamExponent, float lwValue, bool doCleanup, float cleanupAggression)
			{
				this.languageModel = languageModel;
				this.useAudioConversion = useAudioConversion;
				this.allphone_ciEnabled = allphone_ciEnabled;
				this.backtraceEnabled = backtraceEnabled;
				this.beamExponent = beamExponent;
				this.pbeamExponent = pbeamExponent;
				this.lwValue = lwValue;
				this.doCleanup = doCleanup;
				this.cleanupAggression = cleanupAggression;
			}

			public AutoSyncOptions (string languageModel, bool useAudioConversion, AutoSyncOptionsPreset preset)
			{
				this.languageModel = languageModel;
				this.useAudioConversion = useAudioConversion;

				if (preset == AutoSyncOptionsPreset.HighQuality)
				{
					this.allphone_ciEnabled = false;
					this.backtraceEnabled = true;
					this.beamExponent = -40;
					this.pbeamExponent = -40;
					this.lwValue = 15f;
					this.doCleanup = true;
					this.cleanupAggression = 0.003f;
				}
				else
				{
					this.allphone_ciEnabled = EditorPrefs.GetBool("LipSync_Allphone_ciEnabled", true);
					this.backtraceEnabled = EditorPrefs.GetBool("LipSync_BacktraceEnabled", false);
					this.beamExponent = EditorPrefs.GetInt("LipSync_BeamExponent", -20);
					this.pbeamExponent = EditorPrefs.GetInt("LipSync_PbeamExponent", -20);
					this.lwValue = EditorPrefs.GetFloat("LipSync_LwValue", 2.5f);
					this.doCleanup = EditorPrefs.GetBool("LipSync_DoCleanup", false);
					this.cleanupAggression = EditorPrefs.GetFloat("LipSync_CleanupAggression", 0);
				}
			}
		}
	}
}