using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine.VR;
using System.IO;
using System;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Threading;
using System.Net.Sockets;
using System.Net;

public class ExperimentalState : MessageBase
{
	public Vector3 _pos;
	public Vector3 _euAngle;
	public int _trialNumber;

	public ExperimentalState()
	{
		_pos = new Vector3 ();
		_euAngle = new Vector3 ();
		_trialNumber = 0;
	}

	public ExperimentalState(Vector3 pos, Vector3 euAngle, int trialNumber)
	{
		_pos = pos;
		_euAngle = euAngle;
		_trialNumber = trialNumber;
	}
}

class NetworkControlCenter
{
	int framesSinceContact = 0;
	private bool isClient = false;

	private GameObject parent;
	private TrialManager trialManager;

	// receiving Thread
	public Thread receiveThread;

	// udpclient object
	public UdpClient clientR;
	public UdpClient clientS;

	// remoteendpoint object
	IPEndPoint remoteEndPoint;

	private void SetupClientUDP()
	{
		receiveThread = new Thread (new ThreadStart (RecieveDataComputer));
		receiveThread.IsBackground = true;
		receiveThread.Start ();
		clientR = new UdpClient (8054);

		clientS = new UdpClient (8053);
		remoteEndPoint = new IPEndPoint (IPAddress.Parse ("192.168.11.2"), 8053);
	}

	public void RecieveDataComputer()
	{
		while (true) {
			try{
				IPEndPoint anyIP = new IPEndPoint (IPAddress.Any, 8053);
				byte[] data = clientR.Receive (ref anyIP);
				int trialNumber = BitConverter.ToInt32(data,0);
				Debug.Log("Recieve:");
				Debug.Log(trialNumber);
				trialManager.SetOrderIndex (trialNumber);
			}
			catch (Exception err){
				Debug.Log (err.ToString ());
			}
		}
  	}

	public void SendClientUpdateUDP()
	{
		//Debug.Log ("Sent Data to Server");
		byte[] data = new byte[sizeof(int)];
		Buffer.BlockCopy (BitConverter.GetBytes (trialManager.GetOrderIndex ()), 0, data, 0, sizeof(int));
		Debug.Log("Send:");	
		Debug.Log(trialManager.GetOrderIndex ());
		for (int i = 0; i < 10; i++)
			clientS.Send (data, data.Length, remoteEndPoint);
	}
		

	//server code

	private void StartServerUDP()
	{
		receiveThread = new Thread (new ThreadStart (RecieveDataPhone));
		receiveThread.IsBackground = true;
		receiveThread.Start ();
		clientR = new UdpClient (8053);

		clientS = new UdpClient (8054);
		remoteEndPoint = new IPEndPoint (IPAddress.Parse ("192.168.11.11"), 8054);
	}

	public void RecieveDataPhone()
	{
		while (true) {
			try{
				IPEndPoint anyIP = new IPEndPoint (IPAddress.Any, 8054);
				byte[] data = clientR.Receive (ref anyIP);
				int trialNumber = BitConverter.ToInt32(data,0);
				Debug.Log("Recieve:");
				Debug.Log(trialNumber);
				trialManager.SetOrderIndex (trialNumber);
				ReOrienterAndTester parentTest = parent.GetComponent<ReOrienterAndTester> ();
				parentTest.UpdateTrial ("");
				framesSinceContact = 0;
			}
			catch (Exception err){
				Debug.Log (err.ToString ());
			}
		}
	}

	public void SendStateUpdateUDP()
	{
		byte[] data = new byte[sizeof(int)];
		Buffer.BlockCopy (BitConverter.GetBytes (trialManager.GetOrderIndex ()), 0, data, 0, sizeof(int));
		Debug.Log("Send:");
		Debug.Log(trialManager.GetOrderIndex());
		for (int i = 0; i < 10; i++)
			clientS.Send (data, data.Length, remoteEndPoint);
  	}

	#region UnitySpecific

	void OnApplicationQuit()
	{
		if (receiveThread != null)
		{
			receiveThread.Abort();
		}
	}

	public void Start(GameObject newParent, TrialManager newTM, bool server)
	{
		parent = newParent;
		trialManager = newTM;
		if (!server) {
			SetupClientUDP();
		} else {
			StartServerUDP ();
		}
	}

	public void Update(int currentTrial)
	{
		//return;
		if (isClient) {
			framesSinceContact++;
			if (framesSinceContact > 60) {
				framesSinceContact = 0;
				SendStateUpdateUDP ();
			}
		}
	}

	#endregion

	#region OldNetworking
	const short STATE_DATA = 890;
	const short CONNECTION_STATUS = 893;
	const string SERVER_ADDRESS = "192.168.11.255";
	const int SERVER_PORT = 5001;
	NetworkClient myClient;
	int trialNumber = 0;

	private void SetupClient()
	{
		//This code runs on windows
		isClient = true;
		myClient = new NetworkClient ();
		myClient.RegisterHandler (STATE_DATA, StateUpdate);
		myClient.RegisterHandler (MsgType.Connect, OnClientConnected);
		myClient.RegisterHandler (CONNECTION_STATUS, OnKeepInTouch);
		myClient.Connect (SERVER_ADDRESS, SERVER_PORT);
	}

	public void OnStateRecieved(NetworkMessage ClientStateDataMessage)
	{
		//Run on server, recieves what new state should be from client
		ExperimentalState ClientData = ClientStateDataMessage.ReadMessage<ExperimentalState> ();
		trialManager.SetOrderIndex (ClientData._trialNumber);
		ReOrienterAndTester parentTest = parent.GetComponent<ReOrienterAndTester> ();
		parentTest.UpdateTrial ("");
		framesSinceContact = 0;
	}

	public void SendStateUpdate()
	{
		NetworkServer.SendToAll (STATE_DATA, new ExperimentalState (parent.transform.position, parent.transform.eulerAngles, trialManager.GetOrderIndex ()));
	}

	public void OnServerConnected(NetworkMessage netMsg)
	{
		//Send out current state
		//hasClient = true;
		NetworkServer.SetClientReady(netMsg.conn);
		this.parent.gameObject.transform.transform.Translate (0, trialNumber*5, 0);
		SendStateUpdate ();
	}

	private void RestartServer()
	{
		//NetworkServer.Reset ();
		NetworkServer.Listen(SERVER_PORT);
		NetworkServer.DisconnectAll ();
		NetworkServer.UnregisterHandler (MsgType.Connect);
		NetworkServer.UnregisterHandler (STATE_DATA);
		NetworkServer.RegisterHandler (MsgType.Connect, OnServerConnected);
		NetworkServer.RegisterHandler (STATE_DATA, OnStateRecieved);
	}

	private void StartServer()
	{
		//This code runs on android
		NetworkServer.Listen(SERVER_PORT);
		NetworkServer.RegisterHandler (MsgType.Connect, OnServerConnected);
		NetworkServer.RegisterHandler (STATE_DATA, OnStateRecieved);
		Debug.Log ("Hello");
	}

	public void StateUpdate(NetworkMessage ServerStateDataMessage)
	{
		//Run on client, recives stat update
		Debug.Log("Recieved Data from Server");
		ExperimentalState ServerData = ServerStateDataMessage.ReadMessage<ExperimentalState> ();
		trialManager.SetOrderIndex (ServerData._trialNumber);

	}

	public void OnClientConnected(NetworkMessage netMsg)
	{
		//do nothing
		trialManager.SetOrderIndex(5);
		Debug.Log ("Client Connected");
	}

	//both somehow

	public void OnKeepInTouch(NetworkMessage connectionPresent)
	{
		framesSinceContact = 0;
	}

	private void RestartClient()
	{
		//This code runs on windows
		myClient.Disconnect();
		myClient.UnregisterHandler (STATE_DATA);
		myClient.UnregisterHandler (MsgType.Connect);
		myClient.UnregisterHandler (CONNECTION_STATUS);
		isClient = true;
		myClient = new NetworkClient ();
		myClient.RegisterHandler (STATE_DATA, StateUpdate);
		myClient.RegisterHandler (MsgType.Connect, OnClientConnected);
		myClient.RegisterHandler (CONNECTION_STATUS, OnKeepInTouch);
		myClient.Connect (SERVER_ADDRESS, SERVER_PORT);
	}

	public void SendClientUpdate()
	{
		Debug.Log ("Sent Data to Server");
		//myClient.Send(STATE_DATA, new ExperimentalState (parent.transform.position, parent.transform.eulerAngles, trialManager.GetOrderIndex ()));
	}
	#endregion
}

