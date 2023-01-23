using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine.SceneManagement;

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
        EVENT_NULL,
        EVENT_CONNECTION,           // A client wants to connect                [C]
        EVENT_DISCONNETION,         // A client wants to disconnect             [D]
        EVENT_DENIEDCONNECT,        // No more client free spaces               [F]
        EVENT_KEEPCONNECT,          // A client is still connected              [K]
        EVENT_MESSAGE,              // A client sent a message                  [M]
        EVENT_UPDATE,               // A client sent an updated "transform"     [U]
        EVENT_SPAWN_PLAYER,         // A client sent own spawn                  [S]
        EVENT_NOTIFY_ALL_CLIENTS,   // A SERVER sent spawn fist                 [N]
        EVENT_READY_TO_PLAY,        // A client si ready to play                [R]
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
    private List<Event> playerData = new List<Event>();

    // Counts
    private int playerConnec = 0;
    public int GetPlayerConnec() { return playerConnec; }

    // Accepting 6 clients a part of this
    private ArrayList clientSockets = new ArrayList();

    // This will save basic data of clients, as self-given username, an ip end point and id
    // id is the last 3 ip digits
    struct ClientData
    {
        public string name;
        public bool reaching;
        public bool ready;
        public IPEndPoint ipep;
        public byte id;
        public float lastContact;
        public int port;
        public double RTT;

        public int victories;
    }

    IPEndPoint ipepRTT;
    bool rttBool = false;
    double[] rtt = new double[9];
    double maxRTT = 0;
    float lastRTTUpdate;
    double minTimeInterpolation = 0.02f;

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
    private object serverWorldLock = new object();
    public object serializerLock = new object();
    private object RTTLock = new object();

    // Host address
    IPAddress host;

    [HideInInspector] public Serialization serializer;
    private bool ready = false;
    private bool inGame = false;
    private bool breakReady = false;

    private WorldUpdateServer serverWorld;
    int numCosmetis = 7;
    int currentLevel = 0;
    public int maxVictories = 10;
    bool win = false;
    private LevelLoader levelLoader;
    public void SetLevelLoader(LevelLoader level) { levelLoader = level; }

    // Start is called before the first frame update
    void Start()
    {
        //startTime = Time.realtimeSinceStartupAsDouble;
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

        serverWorld = gameObject.GetComponent<WorldUpdateServer>();
        serverWorld.AssignUDPServerReference(this);

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
        if (rttBool)
            RTTCalculate();

        lastRTTUpdate += Time.deltaTime;
        if (lastRTTUpdate > 0.150f)
        {
            maxRTT = (maxRTT > minTimeInterpolation) ? maxRTT : minTimeInterpolation;
            lock (RTTLock) serverWorld.interpolationTime = (float)maxRTT*0.5f;
            lastRTTUpdate = 0;
            RTTInit();
        }
        // This is necesary do in main thread 
        // When all players are ready countdown is begin
        if (ready)
        {
            ready = false;
            if (!inGame)
                CouldDownGameBegin();
            else
            {
                Event ev;
                ev.ipep = new IPEndPoint(IPAddress.Any, 0);
                ev.type = EVENT_TYPE.EVENT_READY_TO_PLAY;
                ev.senderId = 0;

                lock (serializerLock)
                {
                    currentLevel = levelLoader.GetFirstLevelOfList();
                    if (win)
                    {
                        currentLevel = 2; // CUSTOM AVATAR SCENE
                        ResetVictory();
                    }
                    ev.data = serializer.SerializeReadyToPlay(true, currentLevel);
                    EnqueueEvent(ev);
                }
                NextScene();
            }            
        }
        // If the countdown was begin but a player press Cancel or disconnect break countdown
        if (breakReady)
        {
            breakReady = false;
            CancelInvoke();
            StopAllCoroutines();
        }

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

    private void CouldDownGameBegin()
    {
        Event ev;
        ev.ipep = new IPEndPoint(IPAddress.Any, 0);
        ev.type = EVENT_TYPE.EVENT_MESSAGE;
        ev.senderId = 0;

        // Countdown
        lock (serializerLock)
        {
            ev.data = serializer.SerializeChatMessage(0, "3");
            StartCoroutine(EnqueueEventCoroutine(ev, 1));
            ev.data = serializer.SerializeChatMessage(0, "2");
            StartCoroutine(EnqueueEventCoroutine(ev, 2));
            ev.data = serializer.SerializeChatMessage(0, "1");
            StartCoroutine(EnqueueEventCoroutine(ev, 3));
            ev.data = serializer.SerializeChatMessage(0, "GAME START!");
            StartCoroutine(EnqueueEventCoroutine(ev, 3.5f));
            Invoke("NextScene", 3.5f);

            // Game begin
            ev.type = EVENT_TYPE.EVENT_READY_TO_PLAY;
            ev.data = serializer.SerializeReadyToPlay(true, SceneManager.GetActiveScene().buildIndex + 1);
            StartCoroutine(EnqueueEventCoroutine(ev, 4));
        }
    }

    private void NextScene()
    {
        ResetClientReady();
        serverWorld.DestroyAllObjects();

        if (!inGame)
        {
            inGame = true;
            levelLoader.OnNextLevel(SceneManager.GetActiveScene().buildIndex + 1);
        }
        else
        {
            if(win)
            {
                win = false;
                inGame = false;
            }
            levelLoader.OnNextLevel(currentLevel);
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

                (byte id, char type) header;
                lock (serializerLock)
                {
                    header = serializer.DeserializeHeader(data);
                }

                Event e = new Event();
                e.ipep = remote as IPEndPoint;
                lock (serializerLock)
                {
                    e.data = serializer.GetReaderStreamBytes();
                }
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
                    case 'S': // Spawn player event
                        e.type = EVENT_TYPE.EVENT_SPAWN_PLAYER;
                        break;
                    case 'R': // Player are ready to begin game
                        e.type = EVENT_TYPE.EVENT_READY_TO_PLAY;
                        break;
                    case 'T': // Calculate RTT Time
                        ipepRTT = e.ipep;
                        rttBool = true;
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
                        playerConnec--;
                        SetClientReady(clients, e, false);
                        breakReady = true;

                        bool disconnected = false;
                        int playerIdx = -1;
                        string name = "";

                        // The port is now available, clear old data and save client index
                        for (int i = 0; i < prts.Length; ++i)
                        {
                            if (prts[i].remoteIP.Equals(e.ipep.Address))
                            {
                                if (playerData.Count != 0)
                                    playerData.RemoveAt(i);
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
                            lock (serializerLock)
                            {
                                ev.data = serializer.SerializeChatMessage(0, name + " has disconnected!");
                            }
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
                        //if (Time.realtimeSinceStartupAsDouble - startTime > 5.0D) RTTInit();
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
                                EnqueueEvent(e);
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
                                    lock (serializerLock)
                                    {
                                        clientsData[i].name = serializer.DeserializeUsername(e.data);
                                    }
                                    clientsData[i].port = p;
                                    Debug.Log("User " + clients[i].name + " connected at port: " + clients[i].port);
                                }
                                playerConnec++;
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
                            EnqueueEvent(ev);
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
                                EnqueueEvent(ev);

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
                        {
                            (byte netId, byte type, int state, Vector2 dir) direction;
                            lock (serializerLock)
                            {
                                direction = serializer.DeserializeDirection(e.data);
                            }
                            byte netid = direction.netId;
                            lock (serverWorldLock)
                            {
                                switch (direction.type)
                                {
                                    case 0:
                                        for (int i = 0; i < serverWorld.worldObjects.Count; i++)
                                        {
                                            if (serverWorld.worldObjects[i].netId == netid)
                                            {
                                                serverWorld.UpdateWorldObject(i, direction.state, direction.dir);
                                            }
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                            break;
                        }
                    case EVENT_TYPE.EVENT_SPAWN_PLAYER:
                        {
                            // Store player data for replicate it on other clients
                            playerData.Add(e);
                            lock (serverWorldLock)
                            {
                                byte[] data = e.data;
                                (byte objType, int[] indexs, byte idParent, int portId) info;
                                lock (serializerLock)
                                {
                                    info = serializer.DeserializeSpawnObjectInfo(data, numCosmetis);
                                }
                                serverWorld.AddWorldObjectsPendingSpawn(info.objType, e.senderId, info.portId, info.indexs);
                            }
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

                                        // Add so many EVENT_SPAWN_PLAYER events so many players connected before you
                                        for (int j = 0; j < playerData.Count; j++)
                                        {
                                            Event ev = playerData[j];
                                            ev.ipep = clients[i].ipep;
                                            EnqueueEvent(ev);
                                        }

                                    }
                                    // Notify others clinets that you have entered the game
                                    else
                                    {
                                        Event ev;
                                        ev.data = e.data;
                                        ev.ipep = clients[i].ipep;
                                        ev.type = EVENT_TYPE.EVENT_SPAWN_PLAYER;
                                        ev.senderId = e.senderId;
                                        EnqueueEvent(ev);
                                    }
                                }
                            }
                        }
                        break;
                    case EVENT_TYPE.EVENT_READY_TO_PLAY:
                        {
                            // playerReady = if client click "Start" playerReady = true
                            // playerReady = if client click "Cancel" playerReady = false
                            (bool playerReady, int level) readyToPlay;
                            lock (serializerLock)
                            {
                                readyToPlay = serializer.DeserializeReadyToPlay(e.data);
                            }
                            SetClientReady(clients, e, readyToPlay.playerReady);
                            lock (clientsLock)
                            {
                                clients = clientsData;
                            }

                            // Get how many players are ready
                            int playerReadys = 0;
                            for (int i = 0; i < clients.Length; i++)
                            {
                                if (clients[i].ipep != null && clients[i].ready)
                                    playerReadys++;
                            }

                            // TODO: If there are less minimum players required(2) cancel countdown
                            if (playerReadys < 1 && !readyToPlay.playerReady)
                                breakReady = true;

                            if (!inGame)
                                SendMessageReadyToPlay(e, playerReadys);
                            else if (playerReadys == playerConnec)
                                ready = true;
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
                        byte[] data;
                        lock (serializerLock)
                        {
                            data = serializer.SerializeKeepConnect(0);
                        }
                        lock (socketsLock)
                        {
                            ((Socket)clientSockets[i + 1]).SendTo(data, clients[i].ipep);
                        }
                    }
                }
            }
        }
    }

    private void SendMessageReadyToPlay(Event e, int playerReadys)
    {
        String data = playerReadys.ToString() + " / " + playerConnec.ToString() + " players are ready. ";
        if (playerConnec == 1 && !breakReady)
            data += "<br><color=red>MINIMUM 2 PLAYERS ARE REQUIRED</color=red>";

        Event ev;
        lock (serializerLock)
        {
            ev.data = serializer.SerializeChatMessage(0, data);
        }
        ev.ipep = new IPEndPoint(IPAddress.Any, 0);
        ev.type = EVENT_TYPE.EVENT_MESSAGE;
        ev.senderId = e.senderId;
        EnqueueEvent(ev);

        // If all players are ready game begin
        /*else*/
        if (playerReadys == playerConnec)
        {
            // Begin game event
            lock (serializerLock)
            {
                ev.data = serializer.SerializeChatMessage(0, "All players are ready, the game begin in 3 seconds!");
            }
            EnqueueEvent(ev);
            ready = true;
        }
    }

    // Update client struct (bool ready)
    private void SetClientReady(ClientData[] clients, Event e, bool ret)
    {
        for (int i = 0; i < clients.Length; i++)
        {
            if (clients[i].ipep.Equals(e.ipep))
            {
                lock (clientsLock)
                {
                    clientsData[i].ready = ret;
                }
                break;
            }
        }
    }
    private void ResetClientReady()
    {
        lock (clientsLock)
        {
            for (int i = 0; i < clientsData.Length; i++)
            {
                clientsData[i].ready = false;
            }
        }

    }

    public void BroadcastInterpolation(byte netID, Vector3 transform, int state)
    {
        ClientData[] clients;
        lock (clientsLock)
        {
            clients = clientsData;
        }
        for (int i = 0; i < clients.Length; ++i)
        {
            if (clients[i].id != 0)
            {
                byte[] data;
                lock (serializerLock)
                {
                    data = serializer.SerializeTransform(0, netID, transform, state);
                }
                lock (socketsLock)
                {
                    ((Socket)clientSockets[i + 1]).SendTo(data, clients[i].ipep);
                }
            }
        }
    }

    public void AddNotifyEnqueueEvent(byte[] data)
    {
        Event ev = new Event();
        ev.data = data;
        ev.type = EVENT_TYPE.EVENT_NOTIFY_ALL_CLIENTS;
        EnqueueEvent(ev);
    }

    private void EnqueueEvent(Event ev)
    {
        lock (sendQueueLock)
        {
            sendQueue.Enqueue(ev);
        }
    }
    private IEnumerator EnqueueEventCoroutine(Event ev, float timer)
    {
        yield return new WaitForSeconds(timer);
        lock (sendQueueLock)
        {
            sendQueue.Enqueue(ev);
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
                                byte[] data;
                                lock (serializerLock)
                                {
                                    data = serializer.SerializeConnection(0, p);
                                }

                                lock (socketsLock)
                                {
                                    ((Socket)clientSockets[0]).SendTo(data, clients[i].ipep);
                                }

                                // Send to evey client that a new user has joined!
                                Event ev;
                                ev.type = EVENT_TYPE.EVENT_MESSAGE;
                                lock (serializerLock)
                                {
                                    ev.data = serializer.SerializeChatMessage(0, clients[i].name + " has connected!");
                                }
                                ev.ipep = null;
                                ev.senderId = 0;
                                EnqueueEvent(ev);

                                break;
                            }
                        }

                        break;
                    // Send failed connection message to said client
                    case EVENT_TYPE.EVENT_DENIEDCONNECT:
                        {
                            byte[] data;
                            lock (serializerLock)
                            {
                                data = serializer.SerializeDeniedConnection();
                            }

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
                                    byte[] data;
                                    lock (serializerLock)
                                    {
                                        data = serializer.SerializeChatMessage(colorIdx, n + ";", e.data);
                                    }
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
                    case EVENT_TYPE.EVENT_NOTIFY_ALL_CLIENTS:
                        {
                            for (int i = 0; i < clients.Length; ++i)
                            {
                                if (clients[i].id != 0)
                                {
                                    int ind = clients[i].port - initialPort;
                                    lock (socketsLock)
                                    {
                                        ((Socket)clientSockets[ind]).SendTo(e.data, clients[i].ipep);
                                    }
                                }
                            }
                        }
                        break;
                    case EVENT_TYPE.EVENT_READY_TO_PLAY:
                        // Notify to all players connected
                        for (int i = 0; i < clients.Length; ++i)
                        {
                            if (clients[i].id != 0 && !clients[i].ipep.Equals(e.ipep))
                            {
                                int ind = clients[i].port - initialPort;
                                lock (socketsLock)
                                {
                                    ((Socket)clientSockets[ind]).SendTo(e.data, clients[i].ipep);
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

    public void AddVictory(int netID)
    {
        lock (clientsLock)
        {
            for (int i = 0; i < clientsData.Length; ++i)
            {
                if (clientsData[i].port - initialPort - 1 == netID)
                {
                    clientsData[i].victories++;
                    if (clientsData[i].victories >= maxVictories)
                        win = true;
                    break;
                }
            }
        }
    }
    public void ResetVictory()
    {
        lock (clientsLock)
        {
            for (int i = 0; i < clientsData.Length; ++i)
            {
                clientsData[i].victories = 0;
            }
        }
    }

    public List<KeyValuePair<string, int>> GetPlayersVictories()
    {
        List<KeyValuePair<string, int>> players = new List<KeyValuePair<string, int>>();
        lock (clientsLock)
        {
            for (int i = 0; i < clientsData.Length; ++i)
            {
                if (clientsData[i].id != 0)
                    players.Add(new KeyValuePair<string, int>(clientsData[i].name, clientsData[i].victories));
            }
        }
        return players;
    }

    private void OnDestroy()
    {
        OnServerClose();
    }
    public void RTTInit()
    {
        double initTime = Time.realtimeSinceStartupAsDouble;
        byte[] data;
        lock (clientsLock)
        {
            for (int i = 0; i < clientsData.Length; ++i)
            {
                if (clientsData[i].id != 0)
                {
                    data = new byte[1024];
                    lock (serializerLock) data = serializer.SerializeRTT(clientsData[i].id,maxRTT*0.5f);
                    lock (socketsLock) ((Socket)clientSockets[0]).SendTo(data, clientsData[i].ipep);
                    rtt[i] = initTime;
                }
            }
        }
    }
    private void RTTCalculate()
    {
        rttBool = false;
        double realtime = Time.realtimeSinceStartupAsDouble;
        ClientData[] clients;
        maxRTT = 0;
        lock (clientsLock) clients = clientsData;

        for (int f = 0; f < clients.Length; ++f)
        {
            maxRTT = (maxRTT < clients[f].RTT) ? clients[f].RTT : maxRTT;
            if (clients[f].ipep != null && clients[f].ipep.Equals(ipepRTT))
            {
                lock (RTTLock) rtt[f] = clients[f].RTT - realtime;
                break;
            }
        }

    }

}
