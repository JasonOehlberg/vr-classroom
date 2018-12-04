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

        // Adds each Student GameObject in the students List
        foreach (GameObject student in GameObject.FindGameObjectsWithTag("Student"))
        {
            students.Add(student);
            // Finds the Name TextMesh object (child of the Student GameObject) and sets to temp variable
            var textObject = student.transform.Find("Name").gameObject;
            // sets the TextMesh object to a position just above the Student object
            textObject.transform.position = new Vector3(student.transform.position.x, (student.transform.position.y) + 1.35F, student.transform.position.z);
            // sets a temp variable to the TextMesh component of the TextMesh object
            var textMesh = textObject.GetComponent<TextMesh>();
            // sets the default values of the TextMesh component
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

                            // sets a temp variable the child "Name" TextMesh component of the Student object
                            var nameColor = student.transform.Find("Name").gameObject.GetComponent<TextMesh>();
                            // Changes the color if called on and not in the Attendance scenerio
                            nameColor.color = Color.blue;
                        }
                        else
                        {
                            // else attendance is being called
                            // Animation Raise Hand is called
                            animator.SetTrigger("IsCalled");
                            // IsAttendance for that student is set to false
                            animator.SetBool("IsAttendance", false);
                            // sets a temp variable the child "Name" TextMesh component of the Student object
                            var nameColor = student.transform.Find("Name").gameObject.GetComponent<TextMesh>();
                            // sets color back to default if called during Attendance scenerio
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
               // sets a temp variable the child "Name" TextMesh component of the Student object
               var nameColor = student.transform.Find("Name").gameObject.GetComponent<TextMesh>();
               // sets the TextMesh color if the Attendance scenerio is called
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
                    // sets a temp variable the child "Name" TextMesh component of the Student object
                    var nameColor = student.transform.Find("Name").gameObject.GetComponent<TextMesh>();
                    // changes the color back to the default if not in Attendance scenerio
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
        // Keeps the "Name" TextMesh child of student facing the camera at any position
        foreach( GameObject student in students)
        {
            student.transform.Find("Name").gameObject.transform.rotation = Camera.main.transform.rotation;
        }
        

    }
}
