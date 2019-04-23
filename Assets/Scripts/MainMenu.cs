using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using VRStandardAssets.Utils;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour {

	[SerializeField] private VRInput myVRInput;

    public Canvas menuTemplate;

    private Canvas curMenu;
	private GameObject canvasCamera;
	private Button[] buttons;
	private Text prompt;
	private Phases myPhases;

	private SceneHandler mySceneHandler;

	public enum LocomotionType { Resetting_4x4, Resetting_3x3, Resetting_2x2, WiP, Resetting_1x1};
	public LocomotionType locoType = LocomotionType.Resetting_4x4;
	public string subject_id;
	public static string SubjectID = "test";

	void Awake() {
		myVRInput = this.gameObject.AddComponent<VRInput>();
	}

	// Use this for initialization
	void Start () {
		SubjectID = subject_id;
        Selection();
	}

	// Selection screen
	void Selection()
	{
		curMenu = Instantiate (menuTemplate);
		canvasCamera = GameObject.Find ("Camera");
		buttons = curMenu.GetComponentsInChildren<Button> ();
		prompt = GameObject.Find ("Prompt").GetComponent<Text>();
		myPhases = new Phases();

		myVRInput.OnClick += ChooseMode;
	}

    // Interprets swipes into actions
	void ChooseMode() {
        bool invalidSelection = true;
		string mode = "";

        switch(locoType)
        {
		case LocomotionType.Resetting_4x4:
			invalidSelection = false;
			mode =  string.Concat("4 ", SubjectID);
			myPhases.SetLearning (Phases.PhaseTypes.Resetting4);
			myPhases.SetTesting (Phases.PhaseTypes.Resetting4);
			break;
		case LocomotionType.Resetting_3x3:
			invalidSelection = false;
			mode =  string.Concat("3 ", SubjectID);
			myPhases.SetLearning (Phases.PhaseTypes.Resetting3);
			myPhases.SetTesting (Phases.PhaseTypes.Resetting4);
			break;
		case LocomotionType.Resetting_2x2:
			invalidSelection = false;
			mode =  string.Concat("2 ", SubjectID);
			myPhases.SetLearning (Phases.PhaseTypes.Resetting2);
			myPhases.SetTesting (Phases.PhaseTypes.Resetting4);
			break;
		case LocomotionType.WiP:
			invalidSelection = false;
			mode =  string.Concat("1 ", SubjectID);
			myPhases.SetLearning (Phases.PhaseTypes.CW4);
			myPhases.SetTesting (Phases.PhaseTypes.Resetting4);
			break;
		default:
			break;
        }

		if (!invalidSelection) {
			myVRInput.OnClick -= ChooseMode;
			StartCoroutine(DisplayMode (mode));
        }
	}

	IEnumerator DisplayMode(string s) {
		prompt.text = s;
		foreach( var b in buttons) {
			Destroy(b.gameObject);
		}
		yield return new WaitForSecondsRealtime (5);
		Experiment();
	}

	void Experiment() {
		//Destroy useless elements just in case
		Destroy (curMenu.gameObject);

		mySceneHandler = this.gameObject.AddComponent<SceneHandler> ();

		mySceneHandler.PlayScenes (myPhases.getPhases());
	}

}
	
