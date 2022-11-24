using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

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
        EVENT_CONNECTION,       // A client wants to connect                [C]
        EVENT_DISCONNETION,     // A client wants to disconnect             [D]
        EVENT_DENIEDCONNECT,    // No more client free spaces               [F]
        EVENT_KEEPCONNECT,      // A client is still connected              [K]
        EVENT_MESSAGE,          // A client sent a message                  [M]
        EVENT_UPDATE,           // A client sent an updated "transform"     [U]
        EVENT_SPAWN_PLAYER,     // A client sent own spawn                  [S]
    };

    // Events struct
    struct Event
    {
        public EVENT_TYPE type; // What kind of event is
        public byte[] data;     // Event data itself
        public IPEndPoint ipep;   // Who sent it
        public byte senderId;
    }

    // Event list to process
    private Queue<Event> eventQueue;
    private Queue<Event> sendQueue;
    private List<byte[]> playerData = new List<byte[]>();

    // Accepting 6 clients a part of this
    private ArrayList clientSockets = new ArrayList();

    // This will save basic data of clients, as self-given username, an ip end point and id
    // id is the last 3 ip digits
    struct ClientData
    {
        public string name;
        public bool reaching;
        public IPEndPoint ipep;
        public byte id;
        public float lastContact;
        public int port;
    }

    private ClientData[] clientsData;

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

    private Serialization serializer;

    // Start is called before the first frame update
    void Start()
    {
        GetHostIP();

        // Ports available from 7400 to 9056
        initialPort = 7400;
        openPorts = 7;

        // Fill sockets list to get data
        for (int i = 0; i < openPorts; ++i)
        {
            // Create different sockets
            clientSockets.Add(new Socket(AddressFamily.InterNetwork,
                        SocketType.Dgram, ProtocolType.Udp));

            // Bind each socket with a different port
            ((Socket)clientSockets[i]).Bind(new IPEndPoint(IPAddress.Any, initialPort + i));

            if (initialPort + i > 7400)
            {
                ports[i - 1].remoteIP = IPAddress.Any;
                ports[i - 1].port = initialPort + i;
                ports[i - 1].isUsed = false;
            }
        }

        clientsData = new ClientData[6];

        threadServerInBound = new Thread(ThreadServerInBound);
        threadServerProcess = new Thread(ThreadServerProcess);
        threadServerOutBound = new Thread(ThreadServerOutBound);
        threadServerProcess.Start();
        threadServerInBound.Start();
        threadServerOutBound.Start();

        // Initialize event queue
        eventQueue = new Queue<Event>();
        sendQueue = new Queue<Event>();

        serializer = gameObject.AddComponent<Serialization>();
    }

    private void Update()
    {

        // Increment how much time has elapsed since last contact
        lock (clientsLock)
        {
            for (int i = 0; i < clientsData.Length; ++i)
            {
                if (clientsData[i].id != 0)
                {
                    clientsData[i].lastContact += Time.deltaTime;
                }
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
        Debug.Log("Server closed");
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
                byte[] d = new byte[1024];

                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint remote = (EndPoint)sender;

                recv = ((Socket)rr[i]).ReceiveFrom(d, ref remote);
                byte[] data = new byte[recv];
                Array.Copy(d, 0, data, 0, recv);

                (byte id, char type) header = serializer.DeserializeHeader(data);

                Event e = new Event();
                e.ipep = remote as IPEndPoint;
                e.data = serializer.GetReaderStreamBytes();
                e.senderId = header.id;

                // Check what event type it is and save it to process
                switch (header.type)
                {
                    case 'C': // Connection event only if its on port 7400
                        e.type = EVENT_TYPE.EVENT_CONNECTION;
                        break;
                    case 'D': // Desconnection event
                        e.type = EVENT_TYPE.EVENT_DISCONNETION;
                        break;
                    case 'K': // Client is still connected
                        e.type = EVENT_TYPE.EVENT_KEEPCONNECT;
                        break;
                    case 'M': // Message recieved
                        e.type = EVENT_TYPE.EVENT_MESSAGE;
                        break;
                    case 'U': // Update [STILL NOT USED]
                        e.type = EVENT_TYPE.EVENT_UPDATE;
                        break;
                    case 'S': // Spawn [STILL NOT USED]
                        e.type = EVENT_TYPE.EVENT_SPAWN_PLAYER;
                        break;
                    default:
                        break;
                }
                lock (eventQueueLock)
                {
                    eventQueue.Enqueue(e);
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
                if (eventQueue != null && eventQueue.Count > 0)
                {
                    events = new Queue<Event>(eventQueue);
                    eventQueue.Clear();
                }
                else
                {
                    events = null;
                }
            }
            ClientData[] clients;
            lock (clientsLock)
            {
                clients = clientsData;
            }

            while (events != null && events.Count > 0)
            {
                Ports[] prts;
                Event e = events.Dequeue();
                lock (portsLock)
                {
                    prts = ports;
                }
                switch (e.type)
                {
                    // Process disconnection
                    case EVENT_TYPE.EVENT_DISCONNETION:

                        bool disconnected = false;
                        int playerIdx = -1;
                        string name = "";

                        // The port is now available, clear old data and save client index
                        for (int i = 0; i < prts.Length; ++i)
                        {
                            if (prts[i].remoteIP.Equals(e.ipep.Address))
                            {
                                lock (portsLock)
                                {
                                    ports[i].isUsed = false;
                                    ports[i].remoteIP = IPAddress.Any;
                                    playerIdx = ports[i].port - (initialPort + 1);
                                    Debug.Log("Disconnected from port " + ports[i].port.ToString() + " with port id " + playerIdx.ToString());
                                }
                                disconnected = true;
                                break;
                            }
                        }

                        // Client data is removed
                        if (disconnected)
                        {
                            lock (clientsLock)
                            {
                                name = clientsData[playerIdx].name;
                                clientsData[playerIdx] = new ClientData();
                            }

                            Event ev;
                            ev.type = EVENT_TYPE.EVENT_MESSAGE;
                            ev.data = serializer.SerializeChatMessage(0, name + " has disconnected!");
                            ev.ipep = e.ipep;
                            ev.senderId = e.senderId;
                            lock (eventQueueLock)
                            {
                                eventQueue.Enqueue(ev);
                            }
                        }

                        break;
                    // Said client is still in touch
                    case EVENT_TYPE.EVENT_KEEPCONNECT:

                        for (int i = 0; i < clients.Length; ++i)
                        {
                            if (clients[i].ipep != null && clients[i].ipep.Equals(e.ipep))
                            {
                                lock (clientsLock)
                                {
                                    clientsData[i].reaching = false;
                                    clientsData[i].lastContact = 0.0F;
                                }
                                Debug.Log(clients[i].name + " is still connected!");
                                break;
                            }
                        }

                        break;

                    // Look for room for an incoming client
                    case EVENT_TYPE.EVENT_CONNECTION:
                        bool canJoin = false;
                        int p = 0;

                        // Check for a free port
                        for (int i = 0; i < prts.Length; ++i)
                        {
                            if (!prts[i].isUsed)
                            {
                                p = prts[i].port;
                                canJoin = true;
                                Debug.Log("New client connected");
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

                        // Add client data
                        for (int i = 0; canJoin && i < clients.Length; ++i)
                        {
                            if (clients[i].id == 0)
                            {
                                lock (clientsLock)
                                {
                                    clientsData[i].reaching = false;
                                    clientsData[i].lastContact = 0.0F;
                                    clientsData[i].ipep = e.ipep;
                                    clientsData[i].id = e.senderId;
                                    clientsData[i].name = serializer.DeserializeUsername(e.data);
                                    clientsData[i].port = p;
                                    Debug.Log("User " + clients[i].name + " connected at port: " + clients[i].port);
                                }
                                break;
                            }
                        }

                        // If a client cannot join, send him a timeout exception
                        if (!canJoin)
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
                    // Message
                    case EVENT_TYPE.EVENT_MESSAGE:

                        for (int i = 0; i < clients.Length; ++i)
                        {
                            if (clients[i].ipep != null && clients[i].ipep.Equals(e.ipep))
                            {
                                Event ev;
                                ev.data = e.data;
                                ev.ipep = clients[i].ipep;
                                ev.type = EVENT_TYPE.EVENT_MESSAGE;
                                ev.senderId = e.senderId;
                                lock (sendQueueLock)
                                {
                                    sendQueue.Enqueue(ev);
                                }

                                lock (clientsLock)
                                {
                                    clientsData[i].reaching = false;
                                    clientsData[i].lastContact = 0.0F;
                                }
                                break;
                            }
                        }

                        break;
                    case EVENT_TYPE.EVENT_UPDATE:
                        break;
                    case EVENT_TYPE.EVENT_SPAWN_PLAYER:
                        playerData.Add(e.data);
                        // TODO: Add event qeue
                        for (int i = 0; i < clients.Length; ++i)
                        {
                            if (clients[i].ipep != null)
                            {
                                if (clients[i].ipep.Equals(e.ipep))
                                {
                                    lock (clientsLock)
                                    {
                                        clientsData[i].lastContact = 0.0F;
                                    }


                                    for (int j = 0; j < playerData.Count - 1; j++)
                                    {
                                        Event ev;
                                        ev.data = playerData[j];
                                        ev.ipep = clients[i].ipep;
                                        ev.type = EVENT_TYPE.EVENT_SPAWN_PLAYER;
                                        ev.senderId = e.senderId;
                                        lock (sendQueueLock)
                                        {
                                            sendQueue.Enqueue(ev);
                                        }
                                    }

                                }
                                else
                                {
                                    Event ev;
                                    ev.data = e.data;
                                    ev.ipep = clients[i].ipep;
                                    ev.type = EVENT_TYPE.EVENT_SPAWN_PLAYER;
                                    ev.senderId = e.senderId;
                                    lock (sendQueueLock)
                                    {
                                        sendQueue.Enqueue(ev);
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }

                // Check if someone has not been in touch in some time
            }
            for (int i = 0; i < clients.Length; ++i)
            {
                if (clients[i].id != 0 && clients[i].lastContact > 2.5F)
                {
                    if (clients[i].reaching == false)
                    {
                        int ind = clients[i].port - initialPort;
                        lock (clientsLock)
                        {
                            clients[i].reaching = true;
                        }
                        byte[] data = serializer.SerializeKeepConnect(0);
                        lock (socketsLock)
                        {
                            ((Socket)clientSockets[i + 1]).SendTo(data, clients[i].ipep);
                        }
                    }
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
            Ports[] prts = new Ports[6];
            lock (sendQueueLock)
            {
                if (sendQueue != null && sendQueue.Count > 0)
                {
                    sendEvents = new Queue<Event>(sendQueue);
                    sendQueue.Clear();
                }
                else
                {
                    sendEvents = null;
                }
            }
            lock (portsLock)
            {
                prts = ports;
            }
            ClientData[] clients;
            lock (clientsLock)
            {
                clients = clientsData;
            }
            while (sendEvents != null && sendEvents.Count > 0)
            {
                Event e = sendEvents.Dequeue();
                switch (e.type)
                {
                    // Send the client which port has to use
                    case EVENT_TYPE.EVENT_CONNECTION:

                        lock (portsLock)
                        {
                            prts = ports;
                        }

                        for (int i = 0; i < clients.Length; ++i)
                        {
                            if (clients[i].ipep != null && clients[i].ipep.Equals(e.ipep))
                            {
                                int p = 7400;
                                for (int j = 0; j < prts.Length; ++j)
                                {
                                    if (e.ipep.Address.Equals(prts[j].remoteIP))
                                    {
                                        p = prts[j].port;
                                        break;
                                    }
                                }
                                byte[] data = serializer.SerializeConnection(0, p);

                                lock (socketsLock)
                                {
                                    ((Socket)clientSockets[0]).SendTo(data, clients[i].ipep);
                                }

                                // Send to evey client that a new user has joined!
                                Event ev;
                                ev.type = EVENT_TYPE.EVENT_MESSAGE;
                                ev.data = serializer.SerializeChatMessage(0, clients[i].name + " has connected!");
                                ev.ipep = null;
                                ev.senderId = 0;
                                lock (sendQueueLock)
                                {
                                    sendQueue.Enqueue(ev);
                                }

                                break;
                            }
                        }

                        break;

                    // Send failed connection message to said client
                    case EVENT_TYPE.EVENT_DENIEDCONNECT:
                        {
                            byte[] data = serializer.SerializeDeniedConnection();

                            lock (socketsLock)
                            {
                                ((Socket)clientSockets[0]).SendTo(data, e.ipep);
                            }
                        }
                        break;

                    // Send message to everyone! So we know data, that only server knows
                    // such as color codes, ids and other
                    case EVENT_TYPE.EVENT_MESSAGE:
                        {
                            string n = "SERVER";
                            int colorIdx = 0;
                            for (int i = 0; e.ipep != null && i < clients.Length; ++i)
                            {
                                if (clients[i].id != 0 && clients[i].ipep.Equals(e.ipep))
                                {
                                    n = clients[i].name;
                                    colorIdx = clients[i].port - initialPort;
                                    break;
                                }
                            }
                            for (int i = 0; i < clients.Length; ++i)
                            {
                                if (clients[i].id != 0)
                                {
                                    int ind = clients[i].port - initialPort;
                                    byte[] data = serializer.SerializeChatMessage(colorIdx, n + ";", e.data);
                                    lock (socketsLock)
                                    {
                                        ((Socket)clientSockets[ind]).SendTo(data, clients[i].ipep);
                                    }
                                }
                            }
                        }

                        break;
                    case EVENT_TYPE.EVENT_UPDATE:
                        break;
                    case EVENT_TYPE.EVENT_SPAWN_PLAYER:
                        {
                            // TODO: Sent info to other players
                            for (int i = 0; i < clients.Length; ++i)
                            {
                                if (clients[i].id != 0 && clients[i].ipep.Equals(e.ipep))
                                {
                                    int ind = clients[i].port - initialPort;
                                    lock (socketsLock)
                                    {
                                        ((Socket)clientSockets[ind]).SendTo(e.data, clients[i].ipep);
                                    }
                                    break;
                                }
                            }
                        }
                        break;
                    default:
                        break;
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

    private void OnDestroy()
    {
        OnServerClose();
    }
}

