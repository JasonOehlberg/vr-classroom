using UnityEngine;
using System.Collections.Generic;

namespace RogoDigital.Lipsync {
	[System.Obsolete("BlendshapePresets have been deprecated in favour of the new LipSyncPreset class in LipSync Pro 1.0.")]
	public class BlendshapePreset : ScriptableObject {
		[SerializeField]
		public List<PhonemeShape> phonemeShapes;
		[SerializeField]
		public List<EmotionShape> emotionShapes;
	}
}