using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor.PackageManager;

/*
 * This script component has and controls the server behaviour
 * It is not deleted between scenes, so it keeps its data
 * 
 * Different thread might be used to:
 * - Receiving events
 * - Process events
 * - Send events and final decisions
 */

/*
 * For now, messages between servers and clients are structured like:
 * characters in positions 0, 1 and 2 are the 3 last digits of an IP (the sender)
 * character in position 3 is a letter defining the event type of this data
 * 
 * Data example: "135MHello mates!"
 * 
 * Later on, ip identifier and type can be replaced by using one "unsigned" byte for each one
 */

public class UDPServer : MonoBehaviour
{
    // Event types
    enum EVENT_TYPE
    {
        EVENT_CONNECTION,       // A client wants to connect
        EVENT_DISCONNETION,     // A client wants to disconnect
        EVENT_DENIEDCONNECT,    // No more client free spaces
        EVENT_KEEPCONNECT,      // A client is still connected
        EVENT_MESSAGE,          // A client sent a message
        EVENT_NAMES,            // Send client usernames
        EVENT_UPDATE,           // A client sent an updated "transform"
    };

    // Events struct
    struct Event
    {
        public EVENT_TYPE type; // What kind of event is
        public string data;     // Event data itself
        public IPEndPoint ipep;   // Who sent it
    }

    // Event list to process
    private Queue<Event> eventQueue;
    private Queue<Event> sendQueue;

    // Accepting 6 clients a part of this
    private ArrayList clientSockets = new ArrayList();

    // This will save basic data of clients, as self-given username, an ip end point and id
    // id is the last 3 ip digits
    struct ClientData
    {
        public string name;
        public IPEndPoint ipep;
        public string id;
        public float lastContact;
    }

    private ClientData[] clientsData = new ClientData[6];

    // Total sockets *One socket is for initial connection*
    // After initial connection, the server send an unused port to the client
    private uint openPorts;
    private int initialPort;

    struct Ports
    {
        public int port;
        public bool isUsed;
        public IPAddress remoteIP;
    }

    Ports[] ports = new Ports[6];

    // Server threads
    private Thread threadServerInBound;
    private Thread threadServerProcess;
    private Thread threadServerOutBound;

    private object clientsLock = new object();
    private object socketsLock = new object();
    private object portsLock = new object();
    private object eventQueueLock = new object();
    private object sendQueueLock = new object();

    // Host address
    IPAddress host;

    // Start is called before the first frame update
    void Start()
    {
        GetHostIP();

        // Ports available from 9050 to 9056
        initialPort = 9050;
        openPorts = 7;

        // Fill sockets list to get data
        for (int i = 0; i < openPorts; ++i)
        {
            // Create different sockets
            clientSockets.Add(new Socket(AddressFamily.InterNetwork,
                        SocketType.Dgram, ProtocolType.Udp));

            // Bind each socket with a different port
            ((Socket)clientSockets[i]).Bind(new IPEndPoint(IPAddress.Any, initialPort + i));

            if (initialPort + i > 9050)
            {
                ports[i - 1].port = initialPort + i;
                ports[i - 1].isUsed = false;
            }
        }

        // Initialize event queue
        eventQueue = new Queue<Event>();
        sendQueue = new Queue<Event>();
    }

    private void Update()
    {
        lock(clientsLock)
        {
            for(int i = 0; i < clientsData.Length; ++i)
            {
                clientsData[i].lastContact += Time.deltaTime;
            }
        }
    }

    public void OnServerClose()
    {
        // Abort all threads so none of them is active in the background
        if (threadServerInBound.IsAlive) threadServerInBound.Abort();
        if (threadServerProcess.IsAlive) threadServerProcess.Abort();
        if (threadServerOutBound.IsAlive) threadServerOutBound.Abort();

        // Close all sockets
        for (int i = clientSockets.Count - 1; i >= 0; --i)
        {
            ((Socket)clientSockets[i]).Close();
        }
    }

    // This thread is responsible to save all recieved data
    private void ThreadServerInBound()
    {

        while (true)
        {
            ArrayList rr;
            ArrayList rw;
            ArrayList re;

            lock (socketsLock)
            {
                // Copy array to evaluate data input
                rr = new ArrayList(clientSockets);
                rw = new ArrayList(clientSockets);
                re = new ArrayList(clientSockets);
            }

            // Delete array sockets that hasn't send any data
            Socket.Select(rr, rw, re, 0);

            // If we have data to check do:
            for (int i = 0; i < rr.Count; ++i)
            {
                // Get data
                int recv;
                byte[] data = new byte[1024];
                string tmpMessage;

                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint remote = (EndPoint)sender;

                recv = ((Socket)rr[i]).ReceiveFrom(data, ref remote);
                tmpMessage = Encoding.ASCII.GetString(data, 0, recv);
                Event e;

                // Check what event type it is and save it to process
                switch (tmpMessage[3])
                {
                    case 'C':
                        IPEndPoint ep = ((Socket)rr[i]).LocalEndPoint as IPEndPoint;
                        if (ep.Port != 9050)
                            break;
                        e = new Event();
                        e.type = EVENT_TYPE.EVENT_CONNECTION;
                        e.data = tmpMessage;
                        e.ipep = remote as IPEndPoint;
                        lock (eventQueueLock)
                        {
                            eventQueue.Enqueue(e);
                        }
                        break;
                    case 'D':
                        e = new Event();
                        e.type = EVENT_TYPE.EVENT_DISCONNETION;
                        e.data = tmpMessage;
                        e.ipep = remote as IPEndPoint;
                        lock (eventQueueLock)
                        {
                            eventQueue.Enqueue(e);
                        }
                        break;
                    case 'K':
                        e = new Event();
                        e.type = EVENT_TYPE.EVENT_KEEPCONNECT;
                        e.data = tmpMessage;
                        e.ipep = remote as IPEndPoint;
                        lock (eventQueueLock)
                        {
                            eventQueue.Enqueue(e);
                        }
                        break;
                    case 'M':
                        e = new Event();
                        e.type = EVENT_TYPE.EVENT_MESSAGE;
                        e.data = tmpMessage;
                        e.ipep = remote as IPEndPoint;
                        lock (eventQueueLock)
                        {
                            eventQueue.Enqueue(e);
                        }
                        break;
                    case 'U':
                        e = new Event();
                        e.type = EVENT_TYPE.EVENT_UPDATE;
                        e.data = tmpMessage;
                        e.ipep = remote as IPEndPoint;
                        lock (eventQueueLock)
                        {
                            eventQueue.Enqueue(e);
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }

    // This thread is responsible to process all data
    private void ThreadServerProcess()
    {
        while (true)
        {
            Queue<Event> events;
            lock (eventQueueLock)
            {
                events = eventQueue;
                eventQueue.Clear();
            }
            while (events.Count > 0)
            {
                ClientData[] clients;
                Ports[] prts;
                Event e = events.Dequeue();
                lock (clientsLock)
                {
                    clients = clientsData;
                }
                lock(portsLock)
                {
                    prts = ports;
                }
                switch (e.type)
                {
                    case EVENT_TYPE.EVENT_DISCONNETION:

                        bool disconnected = false;
                        for (int i = 0; i < prts.Length; ++i)
                        {
                            if (prts[i].remoteIP == e.ipep.Address)
                            {
                                lock (portsLock)
                                {
                                    ports[i].isUsed = false;
                                    ports[i].remoteIP = IPAddress.Any;
                                }
                                disconnected = true;
                                break;
                            }
                        }
                        for (int i = 0; disconnected && i < clients.Length; ++i)
                        {
                            if (e.ipep == clients[i].ipep)
                            {
                                lock (clientsLock)
                                {
                                    clientsData[i] = new ClientData();
                                }
                                break;
                            }
                        }

                        break;
                    case EVENT_TYPE.EVENT_KEEPCONNECT:

                        for (int i = 0; i < clients.Length; ++i)
                        {
                            if (clients[i].ipep == e.ipep)
                            {
                                lock (clientsLock)
                                {
                                    clientsData[i].lastContact = 0.0F;
                                }
                                break;
                            }
                        }

                        break;
                    case EVENT_TYPE.EVENT_CONNECTION:
                        bool canJoin = false;
                        for (int i = 0; i < prts.Length; ++i)
                        {
                            if (!prts[i].isUsed)
                            {
                                canJoin = true;
                                lock (portsLock)
                                {
                                    ports[i].isUsed = true;
                                    ports[i].remoteIP = e.ipep.Address;

                                }
                                lock (sendQueueLock)
                                {
                                    sendQueue.Enqueue(e);
                                }
                                break;
                            }
                        }
                        for (int i = 0;canJoin && i < clients.Length; ++i)
                        {
                            if (clients[i].id.Length == 0)
                            {
                                lock (clientsLock)
                                {
                                    clientsData[i].lastContact = 0.0F;
                                    clientsData[i].ipep = e.ipep;
                                    clientsData[i].id = e.data.Substring(0, 3);
                                    clientsData[i].name = e.data.Substring(4);
                                }
                                break;
                            }
                        }
                        if(!canJoin)
                        {
                            Event ev = new Event();
                            ev.type = EVENT_TYPE.EVENT_DENIEDCONNECT;
                            ev.data = e.data;
                            ev.ipep = e.ipep;
                            lock (sendQueueLock)
                            {
                                sendQueue.Enqueue(ev);
                            }
                        }

                        break;
                    case EVENT_TYPE.EVENT_MESSAGE:

                        for(int i = 0; i < clients.Length; ++i)
                        {
                            if (clients[i].ipep == e.ipep)
                            {
                                clients[i].lastContact = 0.0F;
                                break;
                            }
                        }

                        lock(sendQueueLock)
                        {
                            // Resend to other users
                            sendQueue.Enqueue(e);
                        }

                        break;
                    case EVENT_TYPE.EVENT_UPDATE:
                        break;
                    default:
                        break;
                }
            }
        }
    }

    // This thread is responsible to rebound and broadcast data
    private void ThreadServerOutBound()
    {
        while (true)
        {
            Queue<Event> sendEvents;
            lock (sendQueueLock)
            {
                sendEvents = sendQueue;
                sendQueue.Clear();
            }
            ClientData[] clients;
            lock (clientsLock)
            {
                clients = clientsData;
            }
            while (sendEvents.Count > 0)
            {
                Event e = sendEvents.Dequeue();
                switch (e.type)
                {
                    case EVENT_TYPE.EVENT_CONNECTION:

                        Ports[] prts;
                        lock(portsLock)
                        {
                            prts = ports;
                        }

                        for(int i = 0; i < clients.Length; ++i)
                        {
                            if (clients[i].ipep == e.ipep)
                            {
                                int p = 9050;
                                for(int j = 0; j < prts.Length; ++j)
                                {
                                    if(e.ipep.Address == prts[j].remoteIP)
                                    {
                                        p = prts[j].port;
                                        break;
                                    }
                                }
                                byte[] data = new byte[8];
                                string tmp = "000C" + p.ToString();
                                data = Encoding.ASCII.GetBytes(tmp);

                                lock(socketsLock)
                                {
                                    ((Socket)clientSockets[0]).SendTo(data, clients[i].ipep); // TODO: send all IDs and usernames to new clients
                                }

                                break;
                            }
                        }

                        break;
                    case EVENT_TYPE.EVENT_DENIEDCONNECT:
                        {
                            byte[] data = new byte[4];
                            string tmp = "000F";        // Full as F event
                            data = Encoding.ASCII.GetBytes(tmp);

                            lock (socketsLock)
                            {
                                ((Socket)clientSockets[0]).SendTo(data, e.ipep);
                            }
                        }
                        break;

                    case EVENT_TYPE.EVENT_MESSAGE:

                        for(int i = 0; i < clients.Length; ++i)
                        {
                            if (clients[i].ipep != e.ipep && clients[i].id.Length != 0)
                            {
                                int ind = clients[i].ipep.Port - initialPort;
                                byte[] data = new byte[1024];
                                data = Encoding.ASCII.GetBytes(e.data);
                                lock (socketsLock)
                                {
                                    ((Socket)clientSockets[ind]).SendTo(data, clients[i].ipep);
                                }
                            }
                        }

                        break;
                    default:
                        break;
                }
            }

            for (int i = 0; i < clients.Length; ++i)
            {
                if (clients[i].lastContact > 1.5F)
                {
                    int ind = clients[i].ipep.Port - initialPort;
                    byte[] data = new byte[4];
                    data = Encoding.ASCII.GetBytes("000K");
                    lock (socketsLock)
                    {
                        ((Socket)clientSockets[ind]).SendTo(data, clients[i].ipep);
                    }
                }
            }
        }
    }

    // Get local IP
    private void GetHostIP()
    {
        IPHostEntry entry = Dns.GetHostEntry(Dns.GetHostName());
        for (int i = 0; i < entry.AddressList.Length; ++i)
        {
            if (entry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
            {
                host = entry.AddressList[i];
                break;
            }
        }
    }

}

