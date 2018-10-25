using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System;
using System.IO;

namespace RogoDigital.Lipsync {
    public class ClipSettingsWindow : ModalWindow {
        private LipSyncClipSetup setup;

        private float start;
        private float end;
        private float length;
        private string transcript;
        private Vector2 scroll;

        private bool adjustMarkers = true;
        private bool willTrim = false;
        private bool soXAvailable = false;

        private int durationMode = 0;
        private AnimBool adjustMarkersAnimBool;

        void OnGUI () {
            GUILayout.Space(20);
            scroll = GUILayout.BeginScrollView(scroll);

            EditorGUI.BeginDisabledGroup(setup.clip && !soXAvailable);
            LipSyncEditorExtensions.BeginPaddedHorizontal();
            durationMode = GUILayout.Toolbar(durationMode, new string[] { "Duration", "Start + End Times" });
            LipSyncEditorExtensions.EndPaddedHorizontal();
            GUILayout.Space(10);
            if (durationMode == 0) {
                willTrim = length != setup.fileLength;
                TimeSpan time = TimeSpan.FromSeconds(length);

                int minutes = time.Minutes;
                int seconds = time.Seconds;
                int milliseconds = time.Milliseconds;

                GUILayout.BeginHorizontal(GUILayout.MaxWidth(280));
                EditorGUI.BeginChangeCheck();
                GUILayout.Label("Duration");
                minutes = EditorGUILayout.IntField(minutes);
                GUILayout.Label("m", EditorStyles.miniLabel);
                seconds = EditorGUILayout.IntField(seconds);
                GUILayout.Label("s", EditorStyles.miniLabel);
                milliseconds = EditorGUILayout.IntField(milliseconds);
                GUILayout.Label("ms", EditorStyles.miniLabel);
                if (EditorGUI.EndChangeCheck()) {
                    float nl = (minutes * 60) + seconds + (milliseconds / 1000f);
                    if (setup.clip) nl = Mathf.Clamp(nl, 0, setup.clip.length);
                    length = nl;
                }
                GUILayout.EndHorizontal();
            } else {
                willTrim = start > 0 || end < setup.fileLength;
                TimeSpan startTime = TimeSpan.FromSeconds(start);
                TimeSpan endTime = TimeSpan.FromSeconds(end);

                int startMinutes = startTime.Minutes;
                int startSeconds = startTime.Seconds;
                int startMilliseconds = startTime.Milliseconds;
                int endMinutes = endTime.Minutes;
                int endSeconds = endTime.Seconds;
                int endMilliseconds = endTime.Milliseconds;

                GUILayout.BeginHorizontal(GUILayout.MaxWidth(280));
                EditorGUI.BeginChangeCheck();
                GUILayout.Label("Start Time");
                startMinutes = EditorGUILayout.IntField(startMinutes);
                GUILayout.Label("m", EditorStyles.miniLabel);
                startSeconds = EditorGUILayout.IntField(startSeconds);
                GUILayout.Label("s", EditorStyles.miniLabel);
                startMilliseconds = EditorGUILayout.IntField(startMilliseconds);
                GUILayout.Label("ms", EditorStyles.miniLabel);
                if (EditorGUI.EndChangeCheck()) {
                    float ns = (startMinutes * 60) + startSeconds + (startMilliseconds / 1000f);
                    ns = Mathf.Clamp(ns, 0, end);
                    start = ns;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(GUILayout.MaxWidth(280));
                EditorGUI.BeginChangeCheck();
                GUILayout.Label("End Time");
                endMinutes = EditorGUILayout.IntField(endMinutes);
                GUILayout.Label("m", EditorStyles.miniLabel);
                endSeconds = EditorGUILayout.IntField(endSeconds);
                GUILayout.Label("s", EditorStyles.miniLabel);
                endMilliseconds = EditorGUILayout.IntField(endMilliseconds);
                GUILayout.Label("ms", EditorStyles.miniLabel);
                if (EditorGUI.EndChangeCheck()) {
                    float ne = (endMinutes * 60) + endSeconds + (endMilliseconds / 1000f);
                    if (setup.clip) ne = Mathf.Clamp(ne, start, setup.clip.length);
                    end = ne;
                }
                GUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();
            adjustMarkersAnimBool.target = willTrim;
            if (EditorGUILayout.BeginFadeGroup(adjustMarkersAnimBool.faded)) {
                adjustMarkers = EditorGUILayout.Toggle("Keep Marker Times", adjustMarkers);
            }
            EditorGUILayout.EndFadeGroup();

            if (setup.clip && !soXAvailable) EditorGUILayout.HelpBox("Cannot Change duration as SoX is not available to trim the audio. Follow the included guide to set up SoX.", MessageType.Warning);

            GUILayout.Space(10);
            GUILayout.Label("Transcript");
            transcript = GUILayout.TextArea(transcript, GUILayout.MinHeight(90));

            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(willTrim && setup.clip ? "Trim & Save" : "Save", GUILayout.MinWidth(100), GUILayout.Height(20))) {
                setup.transcript = transcript;
                if (willTrim) {
                    if (durationMode == 0) {
                        if (setup.clip) {
                            TrimClip(0, length);
                        } else {
                            if (adjustMarkers) AdjustMarkers(0, length);
                        }

                        setup.fileLength = length;
                    } else {
                        if (setup.clip) {
                            TrimClip(start, end - start);
                        } else {
                            if (adjustMarkers) AdjustMarkers(start, end - start);
                        }

                        setup.fileLength = end - start;
                    }
                }

                setup.changed = true;
                setup.previewOutOfDate = true;
                Close();
            }
            GUILayout.Space(10);
            if (GUILayout.Button("Cancel", GUILayout.MinWidth(100), GUILayout.Height(20))) {
                Close();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        void TrimClip (double newStartTime, double newLength) {
            if (soXAvailable) {
                // Paths
                string originalPathRelative = AssetDatabase.GetAssetPath(setup.clip);
                string originalPathAbsolute = Application.dataPath + "/" + originalPathRelative.Substring("/Assets".Length);

                string newPathRelative = Path.GetDirectoryName(originalPathRelative) + "/" + Path.GetFileNameWithoutExtension(originalPathRelative) + "_Trimmed_" + newLength + Path.GetExtension(originalPathRelative);
                string newPathAbsolute = Application.dataPath + "/" + newPathRelative.Substring("/Assets".Length);

                string soXPath = EditorPrefs.GetString("LipSync_SoXPath");
                string soXArgs = "\"" + originalPathAbsolute + "\" \"" + newPathAbsolute + "\" trim " + newStartTime + " " + newLength;

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = soXPath;
                process.StartInfo.Arguments = soXArgs;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardError = true;

                process.ErrorDataReceived += (object e, System.Diagnostics.DataReceivedEventArgs outLine) => {
                    if (!string.IsNullOrEmpty(outLine.Data)) {
                        if (outLine.Data.Contains("FAIL")) {
                            Debug.LogError("SoX Audio Trimming Failed: " + outLine.Data);
                            process.Close();
                        }
                    }
                };

                process.Start();
                process.BeginErrorReadLine();
                process.WaitForExit(5000);

                AssetDatabase.Refresh();
                AudioClip newClip = AssetDatabase.LoadAssetAtPath<AudioClip>(newPathRelative);

                if (adjustMarkers) AdjustMarkers(newStartTime, newLength);

                setup.clip = newClip;
                length = setup.clip.length;
                setup.FixEmotionBlends();
            }
        }

        void AdjustMarkers (double newStartTime, double newLength) {
            // Times
            float newStartNormalised = 1 - ((setup.fileLength - (float)newStartTime) / setup.fileLength);
            float newEndNormalised = ((float)newStartTime + (float)newLength) / setup.fileLength;

            // Adjust Marker timings (go backwards so indices don't change)
            float multiplier = 1 / (newEndNormalised - newStartNormalised);
            for (int p = setup.phonemeData.Count - 1; p >= 0; p--) {
                if (setup.phonemeData[p].time < newStartNormalised || setup.phonemeData[p].time > newEndNormalised) {
                    setup.phonemeData.RemoveAt(p);
                } else {
                    setup.phonemeData[p].time -= newStartNormalised;
                    setup.phonemeData[p].time *= multiplier;
                }
            }

            for (int g = setup.gestureData.Count - 1; g >= 0; g--) {
                if (setup.gestureData[g].time < newStartNormalised || setup.gestureData[g].time > newEndNormalised) {
                    setup.gestureData.RemoveAt(g);
                } else {
                    setup.gestureData[g].time -= newStartNormalised;
                    setup.gestureData[g].time *= multiplier;
                }
            }

            for (int e = setup.emotionData.Count - 1; e >= 0; e--) {
                if (setup.emotionData[e].endTime < newStartNormalised || setup.emotionData[e].startTime > newEndNormalised) {
                    EmotionMarker em = setup.emotionData[e];
                    setup.emotionData.Remove(em);
                    setup.unorderedEmotionData.Remove(em);
                } else {
                    setup.emotionData[e].startTime -= newStartNormalised;
                    setup.emotionData[e].startTime *= multiplier;
                    setup.emotionData[e].startTime = Mathf.Clamp01(setup.emotionData[e].startTime);

                    setup.emotionData[e].endTime -= newStartNormalised;
                    setup.emotionData[e].endTime *= multiplier;
                    setup.emotionData[e].endTime = Mathf.Clamp01(setup.emotionData[e].endTime);
                }
            }
        }

        public static ClipSettingsWindow CreateWindow (ModalParent parent, LipSyncClipSetup setup) {
            ClipSettingsWindow window = CreateInstance<ClipSettingsWindow>();

            window.length = setup.fileLength;
            window.transcript = setup.transcript;
            window.end = window.length;

            window.position = new Rect(parent.center.x - 250, parent.center.y - 100, 500, 200);
            window.minSize = new Vector2(500, 200);
            window.titleContent = new GUIContent("Clip Settings");

            window.adjustMarkersAnimBool = new AnimBool(window.willTrim, window.Repaint);

            window.soXAvailable = AutoSync.CheckSoX();
            window.setup = setup;
            window.Show(parent);
            return window;
        }
    }
}