using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;


public class UDPClient : MonoBehaviour
{
    byte[] data = new byte[1024];
    string input, stringData;
    string tmpMessage;
    int recv;
    bool exit;

    IPEndPoint ipep,sender;
    EndPoint remote;
    Socket servSock;
    Thread threadConnect;


    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("I'am a client");

        // Set IP adress o
        // f server
        ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9050);

        // Create Socket whit IPv4 
        servSock = new Socket(AddressFamily.InterNetwork,
                       SocketType.Dgram, ProtocolType.Udp);

        // The client sends a message to ask that the server iss listening
        tmpMessage = "Hello, are you there?";
        data = Encoding.ASCII.GetBytes(tmpMessage);
        threadConnect = new Thread(ThreadNetConnect);
        threadConnect.Start();

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("c"))
        {
            Debug.Log("Press c");

            exit = true;
        }
    }
    void ThreadNetConnect()
    {
        Debug.Log("Thread start");

        // Client send data to server with data length and flags to ip end point of server
        servSock.SendTo(data, data.Length, SocketFlags.None, ipep);

        // IP endpoint variable with an ip 0.0.0.0 which will later
        // be filled in by the ReciveFrom function.
        sender = new IPEndPoint(IPAddress.Any, 0);
        remote = (EndPoint)sender;

        // Clear data
        data = new byte[1024];
        // Obtain server information and fill the variable named Remote
        recv = servSock.ReceiveFrom(data, ref remote);

        // Print message from server
        Debug.Log("Message received from {0}:"+ remote.ToString());
        Debug.Log(Encoding.ASCII.GetString(data, 0, recv));

        while (true)
        {
            //Debug.Log("Client Online");

            // Wait for input client
            input = "test input";
            if (exit)
                break;

            //Send input client to server
            servSock.SendTo(Encoding.ASCII.GetBytes(input), remote);
        }
        Debug.Log("Stopping client");
        servSock.Close();
    }

}


