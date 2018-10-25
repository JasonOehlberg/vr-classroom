using UnityEngine;
using System.Collections.Generic;

namespace RogoDigital.Lipsync {
	/// <summary>
	/// Stores a collection of phonemes to be used on a project-wide basis.
	/// </summary>
	[System.Serializable, CreateAssetMenu(fileName = "New Phoneme Set", menuName = "LipSync Pro/Phoneme Set")]
	public class PhonemeSet : ScriptableObject {
		[SerializeField]
		public string scriptingName;
		[SerializeField]
		public PhonemeCollection phonemes = new PhonemeCollection();
		[SerializeField]
		public Texture2D[] guideImages;

		[System.Serializable]
		public class PhonemeCollection {
			public List<string> phonemeNames;

			public int Length { get { return phonemeNames.Count; } }

			public Phoneme this[int index] {
				get {
					return new Phoneme(phonemeNames[index], index, Mathf.RoundToInt(Mathf.Pow(2, index)));
				}
			}

			public PhonemeCollection () {
				phonemeNames = new List<string>();
			}
		}

		public struct Phoneme {
			/// <summary>
			/// The name of the phoneme.
			/// </summary>
			public string name { get; private set; }

			/// <summary>
			/// Sequential base-10 index of the phoneme
			/// </summary>
			public int number { get; private set; }

			/// <summary>
			/// Sequential power of 2 identifier for this phoneme (for use in bitmasks)
			/// </summary>
			public int flag { get; private set; }

			public Phoneme (string name, int number, int flag) : this() {
				this.name = name;
				this.number = number;
				this.flag = flag;
			}
		}
	}
}