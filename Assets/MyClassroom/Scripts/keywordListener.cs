using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;
using RogoDigital.Lipsync;
using UnityEditor;
using System.Linq;


public class keywordListener : MonoBehaviour {

    // Array of keywords to use in th KeywordRecognizer
    public string[] keywords;
    // How sure the recognizer has to be that the word is true
    public ConfidenceLevel confidence = ConfidenceLevel.Medium;
    // The Scriptable data created by LipSync Pro
    // Contains the audio file, reference to a gesture and facial animation
    public LipSyncData data;
    // The word that the Recognizer will try to recognize
   
    protected string word = "";

    // The KeywordRecognizer to be used on the gameObject
    KeywordRecognizer recognizer;
   // Dictionary<string, System.Action> keywords; 
    Animator anim;
    /*private void createKeywords()
    {
        keywords = new Dictionary<string, System.Action>();
        keywords.Add("hello", () =>
        {
            PlayClip("Assets/HelloAlex.asset");
        });
        keywords.Add("help", () => 
        {
            PlayClip("Assets/HelloAlex.asset");
        });
       
    }*/
    

	// Use this for initialization
	void Start () {
        // If there are no keywords specified the resource will not start
        anim = GetComponent<Animator>();
        //createKeywords();
        keywords = new string[] { gameObject.name, "hello", "help"};



        if (keywords != null)
        {
            // An Instance of the Recognizer is created and the array
            // of keywords and the confidence level on the keywords
            // are passed in as the arguments for the constructor
            recognizer = new KeywordRecognizer(keywords, confidence);
            // When a phrase is recognized the Recognizer will add this function call
            recognizer.OnPhraseRecognized += Recognizer_OnPhraseRecognized;
            // Start method runs the Listener for the KeywordRecognizer
            recognizer.Start();
            Debug.Log(recognizer.IsRunning);
        }

	}

    private void Recognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        // the word it recognized is then passe to the string variable 'word'
        word = args.text;
        Debug.Log(word);
    }

    // Method takes ta pathstring as an argument and plays the clip
    private void PlayClip(string path)
    {
        Debug.Log("I am in PlayClip");
        // The LipSyncData at the specified path is passed to the data variable
        data = (LipSyncData)AssetDatabase.LoadAssetAtPath(path, typeof(LipSyncData));
        // The LipSyncData is then played on the gameObject's LipSync component
        gameObject.GetComponent<LipSync>().Play(data);
        Debug.Log(gameObject.name);
        // word is set back to an empty string
        // This avoids the function to be called everytime the frame Update method is called 
        word = "";
        Debug.Log(word);
    }
	
	// Update is called once per frame
	void Update () {
        // On Update checks whether the string is set to one of the keywords
        // If it is true the PlayClip method is called and a path to the LipSyncData
        // is passed to it
        if (!gameObject.GetComponent<LipSync>().IsPlaying) // *******Need an && statement for it not to play if gesture animation is active
        {
            if (word.Equals("help"))
            {
                PlayClip("Assets/HelpYou.asset");
            }
            else if (word.Equals("hello"))
            {
                PlayClip("Assets/HelloAlex.asset");
            }
            else if (word.Equals(gameObject.name))
            {
                anim.SetBool("isAcknowledged", true);
            }

            anim.SetBool("isAcknowledged", false);
        }
        
    }

    // Built in function for a MonoBehavoir
    // If the Recognizer is running and it does not contain any keywords
    // it frees up the resource
    private void OnApplicationQuit()
    {
        if(recognizer != null && recognizer.IsRunning)
        {
            recognizer.OnPhraseRecognized -= Recognizer_OnPhraseRecognized;
            recognizer.Stop();
        }
    }


}
