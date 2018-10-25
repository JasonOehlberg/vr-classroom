using UnityEngine;

namespace RogoDigital.Lipsync {
	public class LipSyncData : ScriptableObject {
		[SerializeField]
		public AudioClip clip;
		[SerializeField]
		public PhonemeMarker[] phonemeData;
		[SerializeField]
		public EmotionMarker[] emotionData;
		[SerializeField]
		public GestureMarker[] gestureData;

		[SerializeField]
		public float version;
		[SerializeField]
		public float length;
		[SerializeField]
		public string transcript;

		public LipSyncData () {
		}

		public LipSyncData (AudioClip clip, PhonemeMarker[] pData, EmotionMarker[] eData, GestureMarker[] gData) {
			this.clip = clip;
			phonemeData = pData;
			emotionData = eData;
			gestureData = gData;
			length = clip.length;
		}

		public LipSyncData (AudioClip clip, PhonemeMarker[] pData, EmotionMarker[] eData, GestureMarker[] gData, float length) {
			this.clip = clip;
			phonemeData = pData;
			emotionData = eData;
			gestureData = gData;
			this.length = length;
		}
	}
}