using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;
using System.Linq;

public class StudentBehavior : MonoBehaviour {

    public List<GameObject> students;
    KeywordRecognizer keywordRecognizer;
    Dictionary<string, System.Action> keywords = new Dictionary<string, System.Action>();
    string called = "";
    Transform target;
    float speed;
    
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
                    Animator animator = student.GetComponent<Animator>();
                    if (student.name.Equals(called))
                    {
                        if(animator.GetBool("IsAttendance") == false)
                        {
                            animator.SetBool("HasAttention", true);
                        }
                        else
                        {
                            animator.SetTrigger("IsCalled");
                            animator.SetBool("IsAttendance", false);
                        }
                    }
                }
            });
        }

        keywords.Add("attendance", () =>
       {
           foreach(GameObject student in students)
           {
               student.GetComponent<Animator>().SetBool("IsAttendance", true);
           }
       });

        keywords.Add("work", () =>
        {
            foreach(GameObject student in students)
            {
                Animator anim = student.GetComponent<Animator>();
                    anim.SetBool("HasAttention", false);
            }
        });

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
        /*foreach(GameObject student in students)
        {
            Animator anim = student.GetComponent<Animator>();
            if(anim.GetBool("HasAttention") || anim.GetBool("IsAttendance"))
            {

            }
        }*/
		
    }
}
