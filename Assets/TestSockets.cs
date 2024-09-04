using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Net;

using Orbital.Networking;
using Orbital.Networking.Sockets;
using Orbital.Networking.DataProcessors;
using System.Text;

/// <summary>
/// NOTE: code here is ported from my console test app to work the same way in Unity3D to its 1 to 1 with other runtimes
/// </summary>
public class TestSockets : MonoBehaviour
{
	private string output, input;
	private int consoleStage;
	private bool isQuitting;

	#region Console Fields
	bool useTCP;
	bool isServer;
	IPAddress serverAddress;
	IPAddress localAddress;
	TCPSocketServer tcpSocketServer;
	TCPSocketClient tcpSocketClient;
	RUDPSocket rudpSocket;
	MessageDataProcessor messageProcessor;
	#endregion

	private void Start()
	{
		messageProcessor = new MessageDataProcessor();
		messageProcessor.MessageRecievedCallback += MessageProcessor_MessageRecievedCallback;

		UpdateConsoleStage();
	}

	private void OnGUI()
	{
		const int width = 512;
		const int height = 32;
		int x = (Screen.width / 2) - (width / 2);
		int y = (Screen.height / 2) - (height / 2);

		GUI.Label(new Rect(x, y, width, height), output);
		
		y += height + 8;
		string origInput = input;
		input = GUI.TextField(new Rect(x, y, width, height), input);

		y += height + 8;
		if (GUI.Button(new Rect(x, y, width, height), "Submit") && !isQuitting)
		{
			UpdateConsoleStage();
			input = string.Empty;
		}
	}

	private void UpdateConsoleStage()
	{
		while (true)
		{
			consoleStage++;
			Debug.Log("Console Stage: " + consoleStage.ToString());
			switch (consoleStage)
			{
				// =============================
				// use TCP?
				// =============================
				case 1:
					ConsoleWrite("Use TCP? (y/n)");
					break;

				case 2:
					if (string.IsNullOrEmpty(input) || input != "y" && input != "n")
					{
						ConsoleWrite("Invalid argument");
						Exit();
						return;
					}
					useTCP = input == "y";
					continue;

				// =============================
				// is server?
				// =============================
				case 3:
					ConsoleWrite("Is Server? (y/n)");
					break;

				case 4:
					if (string.IsNullOrEmpty(input) || input != "y" && input != "n")
					{
						ConsoleWrite("Invalid argument");
						Exit();
						return;
					}
					isServer = input == "y";
					continue;

				// =============================
				// get local address
				// =============================
				case 5:
					ConsoleWrite("Enter your IP Address...");
					break;

				case 6:
					if (string.IsNullOrEmpty(input))
					{
						ConsoleWrite("Invalid ip");
						Exit();
						return;
					}

					if (!IPAddress.TryParse(input, out localAddress))
					{
						ConsoleWrite("Invalid local address");
						Exit();
						return;
					}
					continue;

				// =============================
				// get server address
				// =============================
				case 7:
					if (isServer)
					{
						consoleStage++;// we're server so skip
						continue;
					}
					else
					{
						ConsoleWrite("Enter server IP Address...");
					}
					break;

				case 8:
					if (string.IsNullOrEmpty(input))
					{
						ConsoleWrite("Invalid ip");
						Exit();
						return;
					}

					if (!IPAddress.TryParse(input, out serverAddress))
					{
						ConsoleWrite("Invalid server address");
						Exit();
						return;
					}
					continue;

				// =============================
				// start sockets
				// =============================
				case 9:
					// connect
					if (useTCP)
					{
						if (isServer)
						{
							tcpSocketServer = new TCPSocketServer(localAddress, 8080, timeout:60);
							tcpSocketServer.ListenDisconnectedErrorCallback += TCPSocket_ListenDisconnectedErrorCallback;
							tcpSocketServer.ConnectedCallback += TCPSocket_ConnectedCallback;
							tcpSocketServer.Listen(16);
						}
						else
						{
							tcpSocketClient = new TCPSocketClient(serverAddress, localAddress, 8080, timeout:60);
							tcpSocketClient.ConnectedCallback += TCPSocket_ConnectedCallback;
							tcpSocketClient.Connect();
						}
					}
					else
					{
						rudpSocket = new RUDPSocket(IPAddress.Any, localAddress, 8080, 1024);
						rudpSocket.ListenDisconnectedErrorCallback += RUDPSocket_ListenDisconnectedErrorCallback;
						rudpSocket.ConnectedCallback += RUDPSocket_ConnectedCallback;
						rudpSocket.Listen(16);
						if (!isServer) rudpSocket.Connect(serverAddress);
					}

					// messaging
					ConsoleWrite("Type Messages after you have a connection (or 'q' to quit)...");
					break;

				// =============================
				// send messages
				// =============================
				default:
					if (input == "q")
					{
						ExitNow();
						return;
					}

					if (useTCP)
					{
						if (isServer)
						{
							foreach (var connection in tcpSocketServer.connections)
							{
								SendMessage(connection, input);
							}
						}
						else
						{
							foreach (var connection in tcpSocketClient.connections)
							{
								SendMessage(connection, input);
							}
						}
					}
					else
					{
						foreach (var connection in rudpSocket.connections)
						{
							SendMessage(connection, input);
						}
					}
					break;
			}

			break;
		}
	}

	private void SendMessage(INetworkDataSender sender, string message)
	{
		for (int i = 0; i != 5; ++i)// burst send 5 packets all using a new buffer
		{
			var data = Encoding.ASCII.GetBytes(message);
			MessageDataProcessor.PrefixMessageData(ref data);
			if (data.Length >= sizeof(int) + 2)// message-prefix + the two ascii chars we will change
			{
				data[sizeof(int)] = (byte)('a' + i);// a, b, c, d, e
				data[sizeof(int) + 1] = (byte)'_';
			}
			sender.Send(data);
		}
	}

	private void OnDestroy()
	{
		if (tcpSocketServer != null) tcpSocketServer.Dispose();
		if (tcpSocketClient != null) tcpSocketClient.Dispose();
		if (rudpSocket != null) rudpSocket.Dispose();
	}

	private void Exit()
	{
		isQuitting = true;
		StartCoroutine(DelayExit());
	}

	private IEnumerator DelayExit()
	{
		yield return new WaitForSeconds(3);
		ExitNow();
	}

	private void ExitNow()
	{
		Application.Quit();
		#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
		#endif
	}

	private void ConsoleWrite(string message)
	{
		output = message;
		Debug.Log(message);
	}

	// =======================
	// Message Processor
	// =======================
	private void MessageProcessor_MessageRecievedCallback(byte[] data, int size)
	{
		string message = Encoding.ASCII.GetString(data, 0, size);
		ConsoleWrite(string.Format("Message: '{0}'", message));
	}

	// =======================
	// TCP
	// =======================
	private void TCPSocket_ListenDisconnectedErrorCallback(TCPSocketServer sender, string message)
	{
		ConsoleWrite("ERROR: " + message);
	}

	private void TCPSocket_ConnectedCallback(TCPSocket socket, TCPSocketConnection connection, bool success, string message)
	{
		ConsoleWrite("Connected: " + connection.address.ToString());
		connection.DataRecievedCallback += TCPConnection_DataRecievedCallback;
		connection.DisconnectedCallback += TCPConnection_DisconnectedCallback;
	}

	private void TCPConnection_DataRecievedCallback(TCPSocketConnection connection, byte[] data, int size)
	{
		ConsoleWrite(string.Format("Data From:({0}) Size:{1}", connection.address, size));
		messageProcessor.Process(data, 0, size);
	}

	private void TCPConnection_DisconnectedCallback(TCPSocketConnection connection, string message)
	{
		ConsoleWrite(string.Format("Diconnected: {0} '{1}'", connection.address, message));
	}

	// =======================
	// RUDP
	// =======================
	private void RUDPSocket_ListenDisconnectedErrorCallback(RUDPSocket sender, string message)
	{
		ConsoleWrite("ERROR: " + message);
	}

	private void RUDPSocket_ConnectedCallback(RUDPSocket sender, RUDPSocketConnection connection, bool success, string message)
	{
		ConsoleWrite("Connected: " + connection.address.ToString());
		connection.DataRecievedCallback += RUDPConnection_DataRecievedCallback;
		connection.DisconnectedCallback += RUDPConnection_DisconnectedCallback;
	}

	private void RUDPConnection_DataRecievedCallback(RUDPSocketConnection connection, byte[] data, int offset, int size)
	{
		ConsoleWrite(string.Format("Data From:({0}) Size:{1}", connection.address, size));
		messageProcessor.Process(data, offset, size);
	}

	private void RUDPConnection_DisconnectedCallback(RUDPSocketConnection connection, string message)
	{
		ConsoleWrite(string.Format("Diconnected: {0} '{1}'", connection.address, message));
	}
}
