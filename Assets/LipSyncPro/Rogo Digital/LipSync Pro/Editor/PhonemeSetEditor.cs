using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using RogoDigital.Lipsync;

[CustomEditor(typeof(PhonemeSet))]
public class PhonemeSetEditor : Editor {

	private ReorderableList phonemeList;

	void OnEnable () {
		phonemeList = new ReorderableList(serializedObject, serializedObject.FindProperty("phonemes").FindPropertyRelative("phonemeNames"), true, true, true, true);
		phonemeList.drawHeaderCallback += (Rect rect) => {
			GUI.Label(rect, "Phonemes");
		};
		phonemeList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) => {
			serializedObject.Update();
			rect = new Rect(rect.x, rect.y + 1, rect.width, rect.height - 4);
			SerializedProperty element = phonemeList.serializedProperty.GetArrayElementAtIndex(index);
			GUI.Label(new Rect(rect.x, rect.y, rect.width * 0.2f, rect.height), index.ToString() + " (" + Mathf.Pow(2, index) + ")");
			element.stringValue = GUI.TextField(new Rect(rect.x + (rect.width * 0.2f), rect.y, rect.width * 0.8f, rect.height), element.stringValue);
			serializedObject.ApplyModifiedProperties();
		};
	}

	public override void OnInspectorGUI () {
		serializedObject.Update();

		phonemeList.DoLayoutList();
		GUILayout.Space(10);
		EditorGUILayout.PropertyField(serializedObject.FindProperty("scriptingName"));
		EditorGUILayout.PropertyField(serializedObject.FindProperty("guideImages"), true);

		serializedObject.ApplyModifiedProperties();
	}
}
