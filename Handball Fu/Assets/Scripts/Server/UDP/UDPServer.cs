using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


public class UDPServer : MonoBehaviour
{
    // Accepting 5 clients a part of this
    Socket serverSocket;
    IPEndPoint serverEndPoint;
    IPEndPoint[] clientEndPointList= new IPEndPoint[5];

    // Variables to delete
    int recv;
    byte[] data;
    bool exit;
    string tmpMessage;

    IPEndPoint ipep, sender;
    EndPoint remote;
    Socket newsock;

    Thread threatSrvConnect;

    // Start is called before the first frame update
    void Start()
    {
       
        data = new byte[1024];
        ipep = new IPEndPoint(IPAddress.Any, 9050);

        // Creation of a new IPV4 format socket, with Datagram type and UDP protocol.
        newsock = new Socket(AddressFamily.InterNetwork,
                        SocketType.Dgram, ProtocolType.Udp);

        // Set bind with server IP and selected port
        newsock.Bind(ipep);
        Debug.Log("Waiting for a client...");

        // Fill IPEndPoint with empty IP and empty port
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        remote = (EndPoint)(sender);

        threatSrvConnect = new Thread(ThreatSrvConnect);
        threatSrvConnect.Start();

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("v"))
        {
            Debug.Log("Press v");

            exit = true;
        }
    }

    void ThreatSrvConnect()
    {
        // Fill variables data and remote with client through the socket binded 
        recv = newsock.ReceiveFrom(data, ref remote);

        // Print debug message with client information
        Debug.Log("Message received from {0}: "+ remote.ToString());
        Debug.Log(Encoding.ASCII.GetString(data, 0, recv));

        string welcome = "Welcome to my test server";
        data = Encoding.ASCII.GetBytes(welcome);
        // Send message to client 
        newsock.SendTo(data, data.Length, SocketFlags.None, remote);
        // Messaging server/client loop

        while (true)
        {
           // Debug.Log("Server Online");

            if (exit)
                break;

            // Clear the content of the data variable
            data = new byte[1024];
            recv = newsock.ReceiveFrom(data, ref remote);

            Debug.Log("Recived from client "+Encoding.ASCII.GetString(data, 0, recv));

            data = new byte[1024];
            tmpMessage = "I can see you WOO";
            data = Encoding.ASCII.GetBytes(tmpMessage);

            newsock.SendTo(data, recv, SocketFlags.None, remote);
        }
        Debug.Log("Stopping server");
        newsock.Close();
    }


}

