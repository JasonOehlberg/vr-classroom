using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using RogoDigital.Lipsync;

[CustomEditor(typeof(AutoSyncLanguageModel))]
public class AutoSyncLanguageModelEditor : Editor
{

	private SerializedProperty language;
	private SerializedProperty phonemeSet;
	private SerializedProperty hmmDir;
	private SerializedProperty dictFile;
	private SerializedProperty allphoneFile;
	private SerializedProperty lmFile;

	private ReorderableList phonemeMapper;

	void OnEnable ()
	{
		phonemeSet = serializedObject.FindProperty("recommendedPhonemeSet");
		language = serializedObject.FindProperty("language");
		hmmDir = serializedObject.FindProperty("hmmDir");
		dictFile = serializedObject.FindProperty("dictFile");
		allphoneFile = serializedObject.FindProperty("allphoneFile");
		lmFile = serializedObject.FindProperty("lmFile");

		phonemeMapper = new ReorderableList(serializedObject, serializedObject.FindProperty("phonemeMapper"));
		phonemeMapper.drawHeaderCallback = (Rect rect) =>
		{
			EditorGUI.LabelField(rect, "Phoneme Mapper");
		};

		phonemeMapper.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
		{
			SerializedProperty element = phonemeMapper.serializedProperty.GetArrayElementAtIndex(index);
			rect.y += 1;
			rect.height -= 4;
			EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width * 0.15f, rect.height), "Label");
			EditorGUI.PropertyField(new Rect(rect.x + rect.width * 0.15f, rect.y, rect.width * 0.3f, rect.height), element.FindPropertyRelative("label"), GUIContent.none);
			EditorGUI.LabelField(new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.2f, rect.height), "Phoneme");
			EditorGUI.PropertyField(new Rect(rect.x + rect.width * 0.7f, rect.y, rect.width * 0.3f, rect.height), element.FindPropertyRelative("phonemeName"), GUIContent.none);
		};
	}

	public override void OnInspectorGUI ()
	{
		serializedObject.Update();

		EditorGUILayout.PropertyField(language);
		EditorGUILayout.PropertyField(phonemeSet);
		GUILayout.Space(20);
		GUILayout.Label("Paths", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField(hmmDir);
		EditorGUILayout.PropertyField(dictFile);
		EditorGUILayout.PropertyField(allphoneFile);
		EditorGUILayout.PropertyField(lmFile);
		GUILayout.Space(20);
		EditorGUILayout.HelpBox("Leave the Phoneme Mapper below empty to use the hard-coded default Phoneme Mapper.", MessageType.Info);
		phonemeMapper.DoLayoutList();
		serializedObject.ApplyModifiedProperties();
	}
}
