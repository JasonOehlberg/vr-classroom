using UnityEngine;
using UnityEditor;
using RogoDigital.Lipsync;

public class LipSyncPresetUtility {
#pragma warning disable 618
	[MenuItem("Window/Rogo Digital/LipSync Pro/Convert LipSync Preset")]
	public static void ConvertPreset () {
		Object[] selection = Selection.GetFiltered(typeof(BlendshapePreset), SelectionMode.Assets);

		foreach (Object preset in selection) {
			BlendshapePreset oldPreset = (BlendshapePreset)preset;
			LipSyncPreset newPreset = ScriptableObject.CreateInstance<LipSyncPreset>();

			string path = AssetDatabase.GetAssetPath(oldPreset);
			path = System.IO.Path.GetDirectoryName(path);

			newPreset.CreateFromShapes(oldPreset.phonemeShapes.ToArray(), oldPreset.emotionShapes.ToArray(), null);

			AssetDatabase.CreateAsset(newPreset, path + "/" + oldPreset.name + "_converted.Asset");
			AssetDatabase.Refresh();
		}
	}

	[MenuItem("Window/Rogo Digital/LipSync Pro/Convert LipSync Preset", true)]
	public static bool ValidateConvertPreset () {
		Object[] selection = Selection.GetFiltered(typeof(BlendshapePreset), SelectionMode.Assets);

		if (selection.Length > 0) {
			return true;
		}
		return false;
	}
#pragma warning restore 618
}
