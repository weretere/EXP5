using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.VR;
using System.IO;

public class UDPReceive : MonoBehaviour {

	Thread receiveThread;

	UdpClient client;

	public int port;
	public int clientSendPort;

	IPEndPoint remoteEndPoint;
	UdpClient clientSend;
	private string IP;

	public void Start()
	{
		init();
	}

	public void FixedUpdate()
	{
		BroadcastData ();
	}
		
	private void init()
	{
		//RECEIVING INITIALIZATION

		//Define the port in which you will look for the UDP data
		port = 8051;
		clientSendPort = 8050;

		//Start the thread that will receive the data from UDPSend !!!CURRENTLY DOES NOT CLOSE!!!
		receiveThread = new Thread(new ThreadStart(ReceiveData));
		receiveThread.IsBackground = true;
		receiveThread.Start();

		//BRAODCASTING INITIALIZATION

		IP="192.168.11.255"; //Broadcast to to all IPs on this router (NOT IDEAL)

		remoteEndPoint = new IPEndPoint(IPAddress.Parse(IP), clientSendPort);
		clientSend = new UdpClient();


	}

	void OnApplicationQuit()
	{
		if (receiveThread != null)
		{
			receiveThread.Abort();
			if (client != null)
				client.Close();
		}
	}
		
	private  void ReceiveData()
	{

		client = new UdpClient(port);

		while (true)
		{

			try
			{
				IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, port);
				byte[] data = client.Receive(ref anyIP);

				string received = Encoding.UTF8.GetString(data); //Can also use ASCII instead of UTF8 encoding

				print(">> " + received);

				string path = Application.persistentDataPath + "/CW4Test_Data.txt";
				File.AppendAllText(path, received);
			}

			catch (Exception err)
			{
				print(err.ToString());
			}
		}
	}

	private void BroadcastData()
	{

		try
		{			
			byte[] sendBytes = Encoding.UTF8.GetBytes("192.168.11.7;PPTo;192.168.11.33"); //Put the message into byte format and insert it into the data array

			clientSend.Send(sendBytes, sendBytes.Length, remoteEndPoint); //Send data to the client
		}

		catch (Exception err)
		{
			print(err.ToString());
		}
	}
}