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
        initialPort = 9050;
        openPorts = 6;


        // Server end point
        ipep = new IPEndPoint(IPAddress.Parse("192.168.1.88"), initialPort);
        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // Bind server end point and listen
        serverSocket.Bind(ipep);

        try
        {
            serverSocket.Listen(openPorts);
        }
        catch (SocketException e)
        {
            Debug.LogException(e);
            OnServerClose();
        }

        threadSrvConnect = new Thread(ThreadSrvConnect);
        threadSrvConnect.Start();

        threadListeng = new Thread(ThreadListeng);
        threadListeng.Start();
    }

    private void Update()
    {
        if (clientSockets.Count < openPorts && !threadSrvConnect.IsAlive)
        {
            threadSrvConnect = new Thread(ThreadSrvConnect);
            threadSrvConnect.Start();
        }
    }

    void ThreadSrvConnect()
    {
        Socket clientSocket;

        Debug.Log("Waiting for client...");

        // Accept client and save end point
        if (clientSockets.Count < openPorts)
        {
            clientSocket = serverSocket.Accept();
            clientSockets.Add(clientSocket);
            //Debug.Log("Connected with " + ((IPEndPoint)clientSocket).Address.ToString() + " at port " + clientep.Port.ToString());

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
            if (rr.Count != 0 || rw.Count != 0 || re.Count != 0)
                Socket.Select(rr, rw, re, 0);

            for (int i = 0; i < rr.Count; ++i)
            {
                int recv;
                data = new byte[1024];
                string tmpMessage;

                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint remote = (EndPoint)sender;

                recv = ((Socket)rr[i]).ReceiveFrom(data, ref remote);
                tmpMessage = Encoding.ASCII.GetString(data, 0, recv);
                Debug.Log("From client" + i + ": " + tmpMessage);

                data = new byte[1024];
                tmpMessage = "I can see you WOO";
                data = Encoding.ASCII.GetBytes(tmpMessage);
                ((Socket)rr[i]).Send(data, data.Length, SocketFlags.None);
            }
        }
    }

    public void OnServerClose()
    {
        if (threadSrvConnect.IsAlive) threadSrvConnect.Abort();
        if (threadListeng.IsAlive) threadListeng.Abort();

        // Close all sockets
        for (int i = clientSockets.Count - 1; i >= 0; --i)
        {
            // Debug.Log("Disconnected from " + ((IPEndPoint)clientSockets[i]).Address.ToString() + "\nClosing server...");
            ((Socket)clientSockets[i]).Close();

        }

        Debug.Log("Server closed, thread ended");
        serverSocket.Close();
    }
    private void OnDestroy()
    {
        OnServerClose();
    }
}
