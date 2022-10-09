using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class TCPClient : MonoBehaviour
{
    int recv;
    byte[] data;
    bool exit;
    string tmpMessage;

    IPEndPoint ipep;
    Socket server;

    Thread threadClientConnect;
    // Start is called before the first frame update
    void Start()
    {
        data = new byte[1024];
        exit = false;

        // Server end point
        ipep = new IPEndPoint(IPAddress.Parse("192.168.1.135"), 9050);

        // TCP server socket
        server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        threadClientConnect = new Thread(ThreadClientConnect);
        threadClientConnect.Start();
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKey(KeyCode.C) && !exit)
        {
            exit = true;

        }
    }

    void ThreadClientConnect()
    {
        try
        {
            // Connect to server
            server.Connect(ipep);
        }
        catch (SocketException e)
        {
            Debug.LogException(e);
        }

        recv = server.Receive(data);
        tmpMessage = Encoding.ASCII.GetString(data, 0, recv);
        Debug.Log(tmpMessage);

        while (true)
        {
            server.Send(Encoding.ASCII.GetBytes("Hii"));

            data = new byte[1024];
            if (recv == 0 || exit) break;

            recv = server.Receive(data);

            tmpMessage = Encoding.ASCII.GetString(data, 0, recv);
            Debug.Log(tmpMessage);
        }

        Debug.Log("Disconnecting from server...");
        server.Shutdown(SocketShutdown.Both);
        server.Close();
    }
}
