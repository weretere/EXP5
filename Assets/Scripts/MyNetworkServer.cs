using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.VR;
using System.IO;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class MyNetworkServer : MonoBehaviour {
	//Networking information for ppt system
	const short MESSAGE_DATA = 880;
	const short MESSAGE_INFO = 881;
	const string SERVER_ADDRESS = "192.168.11.11";
	const string TRACKER_ADDRESS = "192.168.11.33";
	const int SERVER_PORT = 5000;

	public int _connectionID;
	public static Vector3 _pos = new Vector3 ();
	public static Quaternion _quat = new Quaternion ();

	//HUD text
	public string message = "";
	public string message2 = "";
	public Text messageText;

	public int _messageCount = 0;

	public bool disConnected = false;

	public static bool advanceState = false;

    public static bool UDP = true;

	NetworkClient myClient;

	GameObject feather;
	GameObject featherDestination;
	GameObject HUD;

	public static MyNetworkServer Singleton;

	// Use this for initialization
	void Start () {
		//messageText = GetComponentInChildren<Text> ();
		feather = GameObject.Find ("The Lead Feather");
		featherDestination = GameObject.Find ("FeatherDestination");
		HUD = GameObject.Find ("HUD");
		HUD.SetActive (false);
		if (Application.platform == RuntimePlatform.Android) {
			SetupClient2 ();
			SetupClient ();
		}
		message = "Discovered Android";

		Singleton = this;
	}


	void Update () 
	{
		//if secondary client to send user state info is setup send it
		if (myClient2 != null) {
			myClient2.Send (MESSAGE_DATA, new TDMessage (this.transform.localPosition, Camera.main.transform.eulerAngles, advanceState));
			advanceState = false;
		}
		//if (Application.platform == RuntimePlatform.Android) {
		resettingFSM ();
		//}
	}

    // Create a client and connect to the server port
	Thread recieveThread;
	UdpClient client;
    public void SetupClient()
	{
		//Set up UDP connection thread
		if (UDP) {
			recieveThread = new Thread (new ThreadStart (RecieveData));
			recieveThread.IsBackground = true;
			recieveThread.Start ();
		}

		//set up TCP connection to set up communication
		disConnected = false;
		myClient = new NetworkClient ();
		myClient.RegisterHandler (MESSAGE_DATA, DataReceptionHandler);
		myClient.RegisterHandler (MsgType.Connect, OnConnected);     
		myClient.Connect (SERVER_ADDRESS, SERVER_PORT);
	}

	private void RecieveData()
	{
		client = new UdpClient (8051);
		while (true){
			try {
				IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 8051);
				byte[] data = client.Receive(ref anyIP);
				Vector3 vect = Vector3.zero;
				vect.x = BitConverter.ToSingle(data, 0*sizeof(float));
				vect.y = BitConverter.ToSingle(data, 1*sizeof(float));
				vect.z = BitConverter.ToSingle(data, 2*sizeof(float));
				_pos = vect;
			}
			catch (Exception err) {
				print (err.ToString ());
			}
			Thread.Sleep (1);
		}
	}

	// client function
	public void OnConnected(NetworkMessage netMsg)
	{
		myClient.Send(MESSAGE_INFO, new VRPNInfo("PPT0", TRACKER_ADDRESS));
		_connectionID = netMsg.conn.connectionId;
		message = "Connected";
		disConnected = false;
	}

	public void OnDisconnected(NetworkMessage netMsg)
	{
		_connectionID = -1;
		message = "Disconnected";
		disConnected = true;
	}

	public void DataReceptionHandler(NetworkMessage _vrpnData)
	{
		VRPNMessage vrpnData = _vrpnData.ReadMessage<VRPNMessage>();
		_pos = vrpnData._pos;
		_quat = vrpnData._quat;
	}
		
	#region Resetting

	private Vector3 intendedCenter = new Vector3 (-0.0f, 0, 0.0f); //adjust as neccesary
	private float prevXAngle = 0f;
	private Vector3 prevPos = new Vector3();
	private float virtualAngleTurned = 0f; //each reset
	private float cumulativeAngleTurned = 0f; //total

	private bool resettingEnabled = true;
	private bool resetNeeded = false;
	private bool hasNotReturnedToBounds = false;

	public void DisableResetting()
	{
		resettingEnabled = false;
		resetNeeded = false;
		hasNotReturnedToBounds = false;
		HUD.SetActive (false);
	}

	public bool EnableResetting()
	{
		if (ReturnedToBounds() || resettingEnabled) {
			resettingEnabled = true;
			return true;
		}
		return false;
	}

	public void resettingFSM()
	{
		//Gather pertinent data
		Vector3 deltaTranslationByFrame = _pos - prevPos;
		float realWorldRotation = Camera.main.transform.localEulerAngles.y;
		float deltaRotationByFrame = realWorldRotation - prevXAngle;
		//if crossed threshold from + to - (1 to 359)
		if (deltaRotationByFrame > 90) {
			deltaRotationByFrame = deltaRotationByFrame - 360;
		}
		//if crossed threshold from - to + (359 to 1)
		else if (deltaRotationByFrame < -90) {
			deltaRotationByFrame = deltaRotationByFrame + 360;
		}

		//check to see if a reset is needed (only check if no reset has
		//	been triggered yet, and the subject has returned to inner bounds
		if (Application.platform == RuntimePlatform.Android) {
			#region AndroidResetting
			//perform reset by manipulating gain (to do this we will rotate the object in the opposite direction)
			#region ResettingPhase
			/*else*/ if (resetNeeded) {
					HUD.SetActive (true);
					//Calculate the total rotation neccesary
					float calc1 = Mathf.Rad2Deg * Mathf.Atan2 (intendedCenter.x - _pos.x, intendedCenter.z - _pos.z);
					float rotationRemainingToCenter = calc1 - realWorldRotation;
					//fix rotation variables
					if (rotationRemainingToCenter < -360) {
						rotationRemainingToCenter += 360;
					}
					if (rotationRemainingToCenter < -180) {
						rotationRemainingToCenter = 360 + rotationRemainingToCenter;
					}
					float rotationRemaningToCenterP = 0;
					float rotationRemaningToCenterN = 0;
					//determine left and right angles to rotate
					if (rotationRemainingToCenter < 0) {
						rotationRemaningToCenterN = rotationRemainingToCenter;
						rotationRemaningToCenterP = 360 + rotationRemainingToCenter;
					} else {
						rotationRemaningToCenterP = rotationRemainingToCenter;
						rotationRemaningToCenterN = rotationRemainingToCenter - 360;
					}

					//determine gain based on direction subject has rotated already
					//tuned so that at 360 virtual angle turned the person is pointing back to the center
					float gain = 0;
					if (virtualAngleTurned > 0) {
						gain = (360f - virtualAngleTurned) / rotationRemaningToCenterP - 1;
					} else {
						gain = -(360f + virtualAngleTurned) / rotationRemaningToCenterN - 1;
					}
					if (gain < .1f)
						gain = .1f;
					//inject rotation
					float injectedRotation = (deltaRotationByFrame) * gain;
					virtualAngleTurned += deltaRotationByFrame; //baseline turn
					virtualAngleTurned += injectedRotation; //amount we make them turn as well
					cumulativeAngleTurned -= injectedRotation; //to keep the person moving in the correct direction

					//add the injected rotation to the parent object
					Vector3 tmp = transform.eulerAngles;
					tmp.y += injectedRotation;
					//if (Application.platform == RuntimePlatform.Android) 
					transform.eulerAngles = tmp;
					//if a full turn has occured then stop resetting
					if (Mathf.Abs (virtualAngleTurned) > 359.9f || advanceState) {
						resetNeeded = false;
						advanceState = true;
					}
					if (ReturnedToBounds ()) {
						resetNeeded = false;
						HUD.SetActive (false);
					}
					message = "Please turn around";
			} 
			#endregion
			//Subject needs to walk forward two steps to prevent further triggers
			#region ForwardWalkingPhase
			else if (hasNotReturnedToBounds) {
					if (ReturnedToBounds () || advanceState) {
						HUD.SetActive (false);
						hasNotReturnedToBounds = false;
						advanceState = true;
					}
					message = "Please walk forward";
					feather.SetActive (false);
					transform.Translate (deltaTranslationByFrame);
			}
			#endregion
			//Subject is free to do whatever they'd like
			#region General Operating
			else {
				message = "Please go to the destination";
				transform.Translate (deltaTranslationByFrame);
				Vector3 tmp = transform.position;
				tmp.y = _pos.y;
				transform.position = tmp;
				if (OutOfBounds() && resettingEnabled) {
					resetNeeded = true; //tell the system we need a reset
					hasNotReturnedToBounds = true; //set up the variable indicating a reset has not completed
					virtualAngleTurned = 0f; //reset the virtual angle turned (must reach 360 to complete a reset
					feather.SetActive (true); //activate the reset indicator
					//place the feather at a location directly in front of the user's gaze and rotate it so it can be seen
					Vector3 featherPosition = new Vector3 (featherDestination.transform.position.x, featherDestination.transform.position.y, featherDestination.transform.position.z);
					feather.transform.position = featherPosition;
					Vector3 featherEuler = new Vector3 (90, featherDestination.transform.eulerAngles.y, 0);
					feather.transform.eulerAngles = featherEuler;
					advanceState = true; //indicate to the computer a state change is occuring
				}
			}
			#endregion
			#endregion
		} else {
			#region ComputerResetting
			if (advanceState && resetNeeded == false && hasNotReturnedToBounds == false) {
				resetNeeded = true;
				hasNotReturnedToBounds = true;
				virtualAngleTurned = 0f;
				feather.SetActive (true);
				Vector3 featherPosition = new Vector3 (featherDestination.transform.position.x, featherDestination.transform.position.y, featherDestination.transform.position.z);
				feather.transform.position = featherPosition;
				Vector3 featherEuler = new Vector3 (90, featherDestination.transform.eulerAngles.y, 0);
				feather.transform.eulerAngles = featherEuler;
				advanceState = false;
			}
			//perform reset by manipulating gain (to do this we will rotate the object in the opposite direction)
			else if (resetNeeded) {
				HUD.SetActive (true);
				if (advanceState || Input.GetMouseButton (0)) {
					resetNeeded = false;
					advanceState = false;
				}
				message = "Please turn around";
			} 
			//Subject needs to walk forward two steps to prevent further triggers
			else if (hasNotReturnedToBounds) {
				if (advanceState) {
					HUD.SetActive (false);
					hasNotReturnedToBounds = false;
					advanceState = false;
				}
				message = "Please walk forward";
				feather.SetActive (false);
			}
			//General Operating
			else {
				message = "Please go to the destination";
				//transform.position.y = _pos.y;
			}
			#endregion
		}
		//update position incrementally using sin and cos
		prevPos = _pos;
		prevXAngle = Camera.main.transform.localEulerAngles.y;
	}

    public static float MAX_Z = 2.0f;//+2.0f;
    public static float MIN_Z = -2.0f;//-1.7f;
    public static float MAX_X = 2.0f;//+1.2f;
    public static float MIN_X = -2.0f;//-1.8f;

	public bool OutOfBounds() {
		if (_pos.z > MAX_Z)
			return true;
		if (_pos.z < MIN_Z)
			return true;
		if (_pos.x > MAX_X)
			return true;
		if (_pos.x < MIN_X)
			return true;
		return false;
	}

	public bool ReturnedToBounds() {
		if (_pos.z > MAX_Z - 0.2)
			return false;
		if (_pos.z < MIN_Z + 0.2)
			return false;
		if (_pos.x > MAX_X - 0.2)
			return false;
		if (_pos.x < MIN_X + 0.2)
			return false;
		return true;
	}

	#endregion

	#region NetworkingCode
	//Declare a client node
	NetworkClient myClient2;
	const int SERVER_PORT2 = 5002;

	// Create a client and connect to the server port
	public void SetupClient2()
	{
		myClient2 = new NetworkClient(); //Instantiate the client
		myClient2.Connect(SERVER_ADDRESS, SERVER_PORT2); //Attempt to connect, this will send a connect request which is good if the OnConnected fires
	}
	#endregion

	#region DataCollection
	void FixedUpdate() //was previously FixedUpdate()
	{
		string path = Application.persistentDataPath + "/CW4Test_Data_" + MainMenu.SubjectID +".txt";

		// This text is always added, making the file longer over time if it is not deleted
		string appendText = "\n" + DateTime.Now.ToString() + "\t" + 
			Time.time + "\t" + 

			Input.GetMouseButtonDown(0) + "\t" +

			Input.gyro.userAcceleration.x + "\t" + 
			Input.gyro.userAcceleration.y + "\t" + 
			Input.gyro.userAcceleration.z + "\t" + 

			gameObject.transform.position.x + "\t" + 
			gameObject.transform.position.y + "\t" + 
			gameObject.transform.position.z + "\t" +

			UnityEngine.XR.InputTracking.GetLocalRotation (UnityEngine.XR.XRNode.Head).eulerAngles.x + "\t" +
			UnityEngine.XR.InputTracking.GetLocalRotation (UnityEngine.XR.XRNode.Head).eulerAngles.y + "\t" +
			UnityEngine.XR.InputTracking.GetLocalRotation (UnityEngine.XR.XRNode.Head).eulerAngles.z;

		File.AppendAllText(path, appendText);
	}

	void OnGUI()
	{
		messageText.text = message;
	}

	void OnApplicationQuit()
	{
		if (recieveThread != null)
		{
			recieveThread.Abort();
		}
		if (client != null)
			client.Close();
	}
	#endregion
}
