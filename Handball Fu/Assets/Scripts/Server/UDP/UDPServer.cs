using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using JetBrains.Annotations;

/*
 * This script component has and controls the server behaviour
 * It is not deleted between scenes, so it keeps its data
 */

public class UDPServer : MonoBehaviour
{
    // Accepting 5 clients a part of this
    Socket serverSocket;
    ArrayList remoteSockets = new ArrayList();
    IPEndPoint serverEndPoint;
    IPEndPoint[] clientEndPointList = new IPEndPoint[5];
    EndPoint[] remotes = new EndPoint[5];

    // Start is called before the first frame update
    void Start()
    {
        // Fill sockets list to get data
        for (int i = 0; i < 5; ++i)
        {
            remoteSockets.Add(new Socket(AddressFamily.InterNetwork,
                        SocketType.Dgram, ProtocolType.Udp));
            ((Socket)remoteSockets[i]).Bind(new IPEndPoint(IPAddress.Any, 9050+i));
            Debug.Log("Socket added!");
        }
    }

    private void FixedUpdate()
    {
    }

    // Update is called once per frame
    void Update()
    {
        ArrayList rr = new ArrayList(remoteSockets);
        Socket.Select(rr, null, null, 1000);
        for (int i = 0; i < rr.Count; ++i)
        {
            int recv;
            byte[] data = new byte[1024];
            string tmpMessage;

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint remote = (EndPoint)sender;

            recv = ((Socket)rr[i]).ReceiveFrom(data, ref remote);
            tmpMessage = Encoding.ASCII.GetString(data, 0, recv);

            Debug.Log(tmpMessage);
        }
    }

    void OnServerClose()
    {
        serverSocket.Close();
    }

    //void ThreatSrvConnect()
    //{
    //    // Fill variables data and remote with client through the socket binded 
    //    recv = serverSocket.ReceiveFrom(data, ref remote);

    //    // Print debug message with client information
    //    Debug.Log("Message received from {0}: " + remote.ToString());
    //    Debug.Log(Encoding.ASCII.GetString(data, 0, recv));

    //    string welcome = "Welcome to my test server";
    //    data = Encoding.ASCII.GetBytes(welcome);
    //    // Send message to client 
    //    serverSocket.SendTo(data, data.Length, SocketFlags.None, remote);
    //    // Messaging server/client loop

    //    while (true)
    //    {
    //        // Debug.Log("Server Online");


    //        // Clear the content of the data variable
    //        data = new byte[1024];
    //        recv = serverSocket.ReceiveFrom(data, ref remote);

    //        Debug.Log("Recived from client " + Encoding.ASCII.GetString(data, 0, recv));

    //        data = new byte[1024];
    //        tmpMessage = "I can see you WOO";
    //        data = Encoding.ASCII.GetBytes(tmpMessage);

    //        serverSocket.SendTo(data, recv, SocketFlags.None, remote);
    //    }
    //    Debug.Log("Stopping server");
    //    newsock.Close();
    //}


}

