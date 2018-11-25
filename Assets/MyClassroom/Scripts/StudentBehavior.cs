using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;
using System.Linq;

public class StudentBehavior : MonoBehaviour {

    // A List Collection to hold all the student GameObjects
    public List<GameObject> students;

    // KeywordRecognizer Class
    KeywordRecognizer keywordRecognizer;
    // Dictionary will hold the word to recognize as the key and a callback function as the value
    Dictionary<string, System.Action> keywords = new Dictionary<string, System.Action>();
    // Holds a string to recognize the student.name called
    string called = "";

    // ---- target will be the user - may need to implimented in another script
    Transform target;
    float speed;
    
    // Use this for initialization
    void Start () {
        // Instanciates teh stunendts List Collection
        students = new List<GameObject>();

        // Adds each Student GameObject in the stuedents List
        foreach (GameObject student in GameObject.FindGameObjectsWithTag("Student"))
        {
            students.Add(student);
            var textObject = student.transform.Find("Name").gameObject;
            textObject.transform.position = new Vector3(student.transform.position.x, (student.transform.position.y) + 1.35F, student.transform.position.z);
            var textMesh = textObject.GetComponent<TextMesh>();
            textMesh.text = student.name;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.1F;
            textMesh.color = Color.red;
            textMesh.anchor = TextAnchor.MiddleCenter;
        }

        // adds a keywords Dictionary entry for each studnent based on their name
        foreach (GameObject s in students)
        {
            keywords.Add(s.name, () => {
                foreach (GameObject student in students)
                {
                    // either calls the IsCalled trigger or HasAttention bool
                    Animator animator = student.GetComponent<Animator>();
                    // if the string called is set to the this student.name string
                    if (student.name.Equals(called))
                    {
                        // if attendance is not being called
                        if(animator.GetBool("IsAttendance") == false)
                        {
                            // it runs the Idle animation for attention
                            animator.SetBool("HasAttention", true);
                            var nameColor = student.transform.Find("Name").gameObject.GetComponent<TextMesh>();
                            nameColor.color = Color.blue;
                        }
                        else
                        {
                            // else attendance is being called
                            // Animation Raise Hand is called
                            animator.SetTrigger("IsCalled");
                            // IsAttendance for that student is set to false
                            animator.SetBool("IsAttendance", false);
                            var nameColor = student.transform.Find("Name").gameObject.GetComponent<TextMesh>();
                            nameColor.color = Color.red;
                        }
                    }
                }
            });
        }

        // add keywords Dictioary entry for attendance
        keywords.Add("attendance", () =>
       {
           foreach(GameObject student in students)
           {
               // sets the Animator IsAttendance bool variable to true -- playing the Sitting Idle animation
               student.GetComponent<Animator>().SetBool("IsAttendance", true);
               var nameColor = student.transform.Find("Name").gameObject.GetComponent<TextMesh>();
               nameColor.color = Color.green;
           }
       });

        // add keywords Dictionary entry for work
        keywords.Add("work", () =>
        {
            foreach(GameObject student in students)
            {
                // sets the HasAttention bool value to false -- stops looping the Idle animation
                Animator anim = student.GetComponent<Animator>();
                anim.SetBool("HasAttention", false);
                if (anim.GetBool("IsAttendance") == false)
                {
                    var nameColor = student.transform.Find("Name").gameObject.GetComponent<TextMesh>();
                    nameColor.color = Color.red;
                }
                
            }
        });

        // sets the keywords keys to an array for the KeywordRecognizer to recognize
        keywordRecognizer = new KeywordRecognizer(keywords.Keys.ToArray(),ConfidenceLevel.Medium);
        // Adds a method to listen for each keyword
        keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;
        // starts the recognizer
        keywordRecognizer.Start();

	}


    private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        // variable to hold the callback function Action
        System.Action keywordAction;
        if (keywords.TryGetValue(args.text, out keywordAction))
        {
            // if the args.text matches the key in the Dictionary it invokes the callback function Action
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

    private void LateUpdate()
    {
        //this.position = target.position;
        //position.y += 2;
        foreach( GameObject student in students)
        {
            student.transform.Find("Name").gameObject.transform.rotation = Camera.main.transform.rotation;
        }
        

    }
}
