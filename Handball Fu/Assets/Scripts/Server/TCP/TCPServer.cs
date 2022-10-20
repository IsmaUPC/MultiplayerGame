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
    Socket serverSocket;
    private ArrayList clientSockets = new ArrayList();


    Thread threadSrvConnect, threadListeng;



    // Total sockets *One socket is for initial connection*
    // After initial connection, the server send an unused port to the client
    private int openPorts;
    private int initialPort;


    // Start is called before the first frame update
    void Start()
    {
        data = new byte[1024];
        exit = false;

        initialPort = 9050;
        openPorts = 6;


        // Server end point
        ipep = new IPEndPoint(IPAddress.Any, 9050);

        //// TCP sockets
        //for (int i = 0; i < openPorts; ++i)
        //{
        //    // Create different sockets
        //    clientSockets.Add(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));

        //    // Bind each socket with a different port
        //    ((Socket)clientSockets[i]).Bind(new IPEndPoint(IPAddress.Any, initialPort + i));
        //}

        threadSrvConnect = new Thread(ThreadSrvConnect);
        threadSrvConnect.Start();
        threadListeng.Start();
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
        Socket clientSocket;
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
        if (clientSockets.Count < openPorts)
        {
            clientSocket = serverSocket.Accept();
            //clientep = (IPEndPoint)clientSocket.RemoteEndPoint;
            clientSockets[clientSockets.Count] = clientSocket;
            Debug.Log("Connected with " + clientep.Address.ToString() + " at port " + clientep.Port.ToString());

            // Send message to client
            tmpMessage = "Welcome to TCP server";
            data = Encoding.ASCII.GetBytes(tmpMessage);
            clientSocket.Send(data, data.Length, SocketFlags.None);
        }

    }
    private void ThreadListeng()
    {
        while (true)
        {
            // Copy array to evaluate data input
            ArrayList rr = new ArrayList(clientSockets);
            ArrayList rw = new ArrayList(clientSockets);
            ArrayList re = new ArrayList(clientSockets);
            // Delete array sockets that hasn't send any data
            Socket.Select(rr, rw, re, 0);
            for (int i = 0; i < rr.Count; ++i)
            {
                int recv;
                byte[] data = new byte[1024];
                string tmpMessage;

                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint remote = (EndPoint)sender;

                recv = ((Socket)rr[i]).ReceiveFrom(data, ref remote);
                tmpMessage = Encoding.ASCII.GetString(data, 0, recv);

                //((IPEndPoint)((Socket)rr[i]).LocalEndPoint).Port; Depending on port do something...

                //eventQueue.Enqueue(new Event { }); Enqueue event to process it
            }

            data = new byte[1024];

            // Recieve client message
            //recv = clientSockets.Receive(data);
            //if (recv == 0 || exit)
            //    break;

            //Debug.Log(Encoding.ASCII.GetString(data, 0, recv));

            //clientSocket.Send(data, recv, SocketFlags.None);
        }
        Debug.Log("Disconnected from " + clientep.Address + "\nClosing server...");


    }

    public void OnServerClose()
    {
        if (threadSrvConnect.IsAlive) threadSrvConnect.Abort();
        if (threadListeng.IsAlive) threadListeng.Abort();

        // Close all sockets
        for (int i = clientSockets.Count - 1; i >= 0; --i)
        {
            ((Socket)clientSockets[i]).Close();
        }

        Debug.Log("Server closed, thread ended");
        serverSocket.Close();


    }
}
