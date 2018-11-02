using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;
using System.Linq;

public class StudentBehavior : MonoBehaviour {

    public List<GameObject> students;
    KeywordRecognizer keywordRecognizer;
    Dictionary<string, System.Action> keywords = new Dictionary<string, System.Action>();
    Animator animator;
    string called = "";
    
    // Use this for initialization
    void Start () {
        students = new List<GameObject>();

        foreach (GameObject student in GameObject.FindGameObjectsWithTag("Student"))
        {
            students.Add(student);
        }
        foreach (GameObject s in students)
        {
            keywords.Add(s.name, () => {
                foreach (GameObject student in students)
                {
                    if (student.name.Equals(called))
                    {
                        Debug.Log("Im here" + student.name);
                        animator = student.GetComponent<Animator>();
                        animator.SetBool("IsCalled", true);
                    }
                   
                }
                
            });
        }
        keywordRecognizer = new KeywordRecognizer(keywords.Keys.ToArray());
        keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;
        keywordRecognizer.Start();

	}

    private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        System.Action keywordAction;
        if (keywords.TryGetValue(args.text, out keywordAction))
        {
            Debug.Log(args.text);
            called = args.text;
            keywordAction.Invoke();
        }
    }


    // Update is called once per frame
    void Update ()
    {
		
    }
}
