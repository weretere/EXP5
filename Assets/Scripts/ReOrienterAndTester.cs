using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine.VR;
using System.IO;
using System;
using UnityEngine.UI;

public class ReOrienterAndTester : MonoBehaviour {

	//this -- used to move the person to the start of a trial
	private GameObject _humanMover; //this, used to reset the subject and allow human movement
	private GameObject _maze; //this, used to turn the features of the maze on and off
	private GameObject _voronoi; //this, used to turn the features of the vornoi ground plane on and off
	private GameObject _textMessage;
	private GameObject _cylinder;

	private TrialManager myTrials;
	private NetworkControlCenter networkControlCenter;
	private int _trial = 0;

	public Text _stringMessage;

	void Awake() {
		lastButtonPress = Time.fixedTime + 2; //ensure no button can be pressed for 10 seconds
	}

	// Use this for initialization
	void Start () {

		_humanMover = GameObject.Find ("TestObject");
		_maze = GameObject.Find ("Maze");
		_voronoi = GameObject.Find ("Voronoi");
		_textMessage = GameObject.Find ("Canvas");
		_cylinder = GameObject.Find ("CylinderGoal");
		_voronoi.SetActive (true);
		_textMessage.SetActive (false);
		_cylinder.SetActive (false);

		myTrials = new TrialManager ();


		//setup portion
		_maze.SetActive (false);

		//Networking for controls
		networkControlCenter = new NetworkControlCenter();
		networkControlCenter.Start (this.gameObject, myTrials, true);
	}

	//public const int TRAINING = 0;
	public const int PRETRIAL = 1;
	public const int INTRIAL = 2;
	public const int POSTTRIAL = 3;
	public const int PRETEST = 0;
	private int state = PRETEST;
	private float lastButtonPress = 0;
	private Vector3 startLocation = new Vector3(0f,0f,0f);
	private bool directionLogged = false;

	// Update is called once per frame
	void Update () {
		networkControlCenter.Update (myTrials.GetOrderIndex());

		if (state == PRETEST) {
			if ((Input.GetMouseButton (0) || AccelerometerInput4.advanceState) && Time.fixedTime > lastButtonPress + 1) {
				lastButtonPress = Time.fixedTime;
				if (ResetPerson ()) {
					state = PRETRIAL;
					AccelerometerInput4.advanceState = false;
				}
			}
		}
		else if (state == PRETRIAL) {
			if ((Input.GetMouseButton (0) || AccelerometerInput4.advanceState) && Time.fixedTime > lastButtonPress + 1) {
				if (Mathf.Abs (_humanMover.transform.position.x - myTrials.GetTrial().startObject.transform.position.x) < 1.2) {
					if (Mathf.Abs (_humanMover.transform.position.z - myTrials.GetTrial().startObject.transform.position.z) < 1.2) {
						lastButtonPress = Time.fixedTime;
						state = INTRIAL;
						AccelerometerInput4.advanceState = false;
						StartTrial ();
					}
				}
			}
		} else if (state == INTRIAL) {
			if ((Input.GetMouseButton (0) || AccelerometerInput4.advanceState) && Time.fixedTime > lastButtonPress + 1) {
				lastButtonPress = Time.fixedTime;
				state = POSTTRIAL;
				AccelerometerInput4.advanceState = false;
				UserEndTrial ();
				_trial++;
				myTrials.MoveToNextTrial ();
			}
			if (Time.fixedTime > lastButtonPress + 3) {
				_textMessage.SetActive (false);
			}
			if (!directionLogged && Vector3.Distance (_humanMover.transform.position, startLocation) > .5) {
				directionLogged = true;
				LogData ("DirectionDecided");
			}
		} else if (state == POSTTRIAL) {
			if ((Input.GetMouseButton (0) || AccelerometerInput4.advanceState) && Time.fixedTime > lastButtonPress + 1) {
				lastButtonPress = Time.fixedTime;
				if (ResetPerson ()) {
					state = PRETRIAL;
					AccelerometerInput4.advanceState = false;

					networkControlCenter.SendStateUpdateUDP ();
				}
			}
		}
	}

    void OnApplicationQuit()
    {
        if (networkControlCenter != null && networkControlCenter.receiveThread != null)
        {
            networkControlCenter.receiveThread.Abort();
            if (networkControlCenter.clientR != null)
                networkControlCenter.clientR.Close();
            if (networkControlCenter.clientS != null)
                networkControlCenter.clientS.Close();
        }
	}

	public void UpdateTrial(string message)
	{
		state = POSTTRIAL;
		//Alert user to resuming testing
		_textMessage.SetActive (true);
		_voronoi.SetActive (true);
		_maze.SetActive (false);
		_stringMessage.text = "Resuming Testing" + message;
		_trial = myTrials.GetOrderIndex () + 1;
	}



	bool ResetPerson() {
		if (!MyNetworkServer.Singleton.EnableResetting ())
			return false;
		_stringMessage.text = "Please go to " + myTrials.GetTrial().endObject.name;
		//Place this at corresponding waypoint
		transform.position = myTrials.GetTrial().startPosition;
		//Place test object at (0, eyeHeight, 0)
		float eyeHeight = _humanMover.transform.localPosition.y;
		_humanMover.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
		//Turn maze on
		_maze.SetActive(true);
		//Turn voronoi off
		_voronoi.SetActive(false);
		_cylinder.SetActive (false);
		_textMessage.SetActive (false);
		//Record time, time, start, end, position, orientation
		LogData("PreTrialOrientation");
		return true;
	}

	void StartTrial() {
		//Turn maze off
		_maze.SetActive(false);
		//Turn voronoi on
		_voronoi.SetActive(true);
		_textMessage.SetActive (true);
		//Give user instruction
		directionLogged = false;
		startLocation = _humanMover.transform.position;

		//Record time, time, start, end, position, orientation
		LogData("BeginTrial");
	}

	void UserEndTrial() {
		//Record time, time, start, end, position, orientation
		_textMessage.SetActive(true);
		_stringMessage.text = "Good Job";
		_cylinder.SetActive (true);
		//_cylinder.transform.position = new Vector3 (Camera.main.transform.position.x - MyNetworkServer._pos.z, -0.99f, Camera.main.transform.position.z + MyNetworkServer._pos.x);
		_cylinder.transform.position = new Vector3 (Camera.main.transform.position.x, -0.99f, Camera.main.transform.position.z);
		_cylinder.transform.Translate (-MyNetworkServer._pos.x, 0, -MyNetworkServer._pos.z, MyNetworkServer.Singleton.transform);
		MyNetworkServer.Singleton.DisableResetting ();

		// If we just completed the last test, end
		if (myTrials.GetOrderIndex() >= myTrials.GetMaxOrderIndex()) {
			_stringMessage.text = "Testing Complete";
		}

		LogData("EndTrial");
	}

	void LogData(string action) {
		string path = Application.persistentDataPath + "/Summary_Data_Test_Phase" + MainMenu.SubjectID +".txt";

		string appendText = "\n" + DateTime.Now.ToString() + "\t" + 
			Time.time + "\t" + 

			_trial + "\t" + action + "\t" +

			myTrials.GetTrial().startObject.name + "\t" + myTrials.GetTrial().endObject.name + "\t" +

			_humanMover.transform.position.x + "\t" + 
			_humanMover.transform.position.y + "\t" + 
			_humanMover.transform.position.z + "\t" +

			UnityEngine.XR.InputTracking.GetLocalRotation (UnityEngine.XR.XRNode.Head).eulerAngles.x + "\t" +
			UnityEngine.XR.InputTracking.GetLocalRotation (UnityEngine.XR.XRNode.Head).eulerAngles.y + "\t" +
			UnityEngine.XR.InputTracking.GetLocalRotation (UnityEngine.XR.XRNode.Head).eulerAngles.z;


		File.AppendAllText(path, appendText);
		Debug.Log(appendText);
	}
}
