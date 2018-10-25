using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;


public class UIBehavior : MonoBehaviour {

    bool isVisible;
    public Canvas canvas;
    public string[] keywords;
    KeywordRecognizer recognizer;
	void Start () {
        isVisible = false;
	}
	
	// Update is called once per frame
	void Update () {
        keywords = new string[] { gameObject.name, "attendance"};
		
	}
}
