using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class TCPServer : MonoBehaviour
{
    int recv;
    byte[] data;
    bool exit;
    string tmpMessage;

    IPEndPoint ipep, clientep;
    EndPoint remote;
    Socket serverSocket, clientSocket;

    Thread threadSrvConnect;


    // Start is called before the first frame update
    void Start()
    {
        data = new byte[1024];
        exit = false;

        // Server end point
        ipep = new IPEndPoint(IPAddress.Any, 9050);

        // TCP socket
        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        threadSrvConnect = new Thread(ThreadSrvConnect);

        threadSrvConnect.Start();
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKey(KeyCode.V) && !exit)
        {

            exit = true;

        }
    }

    void ThreadSrvConnect()
    {
        // Bind server end point and listen
        serverSocket.Bind(ipep);

        try
        {
            serverSocket.Listen(1);
        }
        catch (SocketException e)
        {
            Debug.LogException(e);

            exit = true;

        }

        Debug.Log("Waiting for client...");

        // Accept client and save end point
        clientSocket = serverSocket.Accept();
        clientep = (IPEndPoint)clientSocket.RemoteEndPoint;

        Debug.Log("Connected with " + clientep.Address.ToString() + " at port " + clientep.Port.ToString());

        // Send message to client
        tmpMessage = "Welcome to TCP server";
        data = Encoding.ASCII.GetBytes(tmpMessage);
        clientSocket.Send(data, data.Length, SocketFlags.None);

        while (true)
        {
            data = new byte[1024];

            // Recieve client message
            recv = clientSocket.Receive(data);
            if (recv == 0 || exit)
                break;

            Debug.Log(Encoding.ASCII.GetString(data, 0, recv));

            clientSocket.Send(data, recv, SocketFlags.None);
        }
        Debug.Log("Disconnected from " + clientep.Address + "\nClosing server...");

        clientSocket.Close();
        serverSocket.Close();
        Debug.Log("Server closed, thread ended");
    }
}
