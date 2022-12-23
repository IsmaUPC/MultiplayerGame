using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System;
using System.Security.Cryptography;

public class UDPClient : MonoBehaviour
{
    enum EVENT_TYPE
    {
        EVENT_NULL,
        EVENT_CONNECTION,       // A client wants to connect
        EVENT_DISCONNETION,     // A client wants to disconnect
        EVENT_DENIEDCONNECT,    // No more client free spaces
        EVENT_KEEPCONNECT,      // A client is still connected
        EVENT_MESSAGE,          // A client sent a message
        EVENT_UPDATE,           // A client sent an updated "transform"
        EVENT_SPAWN_PLAYER,     // A client sent own spawn
        EVENT_READY_TO_PLAY,    // A client si ready to play                [R]
    };
    enum CONNECTION_STATE
    {
        CONNECTED,
        DISCONNECTED,
        CONNECTING,
        FAILED
    }

    // Events struct
    struct Event
    {
        public EVENT_TYPE type; // What kind of event is
        public byte[] data;     // Event data itself
        public byte id;
    }

    struct ClientData
    {
        public string name;
        public string id;
        public GameObject clientPlayer;
    }

    IPAddress host;
    IPEndPoint sep;
    EndPoint remote;
    Socket serverSocket;
    Thread threadRecieve;
    Thread threadProcess;

    ClientData[] clientsInfo;

    private object socketLock = new object();
    private object eventQueueLock = new object();
    private object stateLock = new object();
    private object clientsLock = new object();
    private object messagesLock = new object();
    private object clientWorldLock = new object();

    byte myID;

    private CONNECTION_STATE state;
    private Queue<Event> eventQueue;

    private int portIdx;

    private Queue<string> chatMessages;
    private Serialization serializer;
    private float timeOut;

    private bool isSocketAlive;

    public static Action OnStart;

    int numCosmetis = 7;

    private WorldUpdateClient clientWorld;

    private bool connected;
    // Start is called before the first frame update
    public void ClientStart()
    {
        portIdx = -1;
        myID = GetHostID();

        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        isSocketAlive = true;

        state = CONNECTION_STATE.DISCONNECTED;

        eventQueue = new Queue<Event>();
        chatMessages = new Queue<string>();

        timeOut = 5.0F;

        clientWorld = gameObject.AddComponent<WorldUpdateClient>();
        clientWorld.AssignUDPClientReference(this);

        connected = false;

        serializer = gameObject.AddComponent<Serialization>();

        threadProcess = new Thread(ThreadProcessData);
        threadRecieve = new Thread(ThreadRecieveData);
        threadProcess.Start();
        threadRecieve.Start();
    }

    void Update()
    {
        CONNECTION_STATE st;
        lock (stateLock)
        {
            st = state;
        }
        if (st == CONNECTION_STATE.CONNECTING)
        {
            timeOut -= Time.deltaTime;
            if (timeOut < 0.0F)
            {
                lock (stateLock)
                {
                    state = CONNECTION_STATE.FAILED;
                    connected = false;
                }
            }
        }
    }

    public bool ConnectToIp(string ip, string username)
    {
        if (connected) return false;
        bool ret = false;

        IPAddress currentIP;
        try
        {
            currentIP = IPAddress.Parse(ip);
        }
        catch
        {
            Debug.Log("IP to connect has not a correct format!");
            return ret;
        }

        state = CONNECTION_STATE.CONNECTING;

        sep = new IPEndPoint(currentIP, 7400);

        byte[] data;
        data = serializer.SerializeConnection(myID, username);
        serverSocket.SendTo(data, data.Length, SocketFlags.None, sep);

        return true;

    }

    private void ThreadRecieveData()
    {
        while (true)
        {
            ArrayList rr = new ArrayList();
            ArrayList rw = new ArrayList();
            ArrayList re = new ArrayList();

            lock (socketLock)
            {
                // Copy array to evaluate data input
                rr.Add(serverSocket);
                rw.Add(serverSocket);
                re.Add(serverSocket);
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
                if (data.Length < 2)
                {
                    continue;
                }
                (byte id, char type) header = serializer.DeserializeHeader(data);

                Event e = new Event();
                e.data = serializer.GetReaderStreamBytes();
                e.id = header.id;

                // Check what event type it is and save it to process
                switch (header.type)
                {
                    case 'C':
                        e.type = EVENT_TYPE.EVENT_CONNECTION;
                        break;
                    case 'D':
                        e.type = EVENT_TYPE.EVENT_DISCONNETION;
                        break;
                    case 'F':
                        e.type = EVENT_TYPE.EVENT_DENIEDCONNECT;
                        break;
                    case 'K':
                        e.type = EVENT_TYPE.EVENT_KEEPCONNECT;
                        break;
                    case 'M':
                        e.type = EVENT_TYPE.EVENT_MESSAGE;
                        break;
                    case 'U':
                        e.type = EVENT_TYPE.EVENT_UPDATE;
                        break;
                    case 'S':
                        e.type = EVENT_TYPE.EVENT_SPAWN_PLAYER;
                        break;
                    case 'R':
                        e.type = EVENT_TYPE.EVENT_READY_TO_PLAY;
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

    private void ThreadProcessData()
    {
        while (true)
        {
            Queue<Event> events;
            lock (eventQueueLock)
            {
                events = new Queue<Event>(eventQueue);
                eventQueue.Clear();
            }
            while (events.Count > 0)
            {
                Event e = events.Dequeue();
                switch (e.type)
                {
                    case EVENT_TYPE.EVENT_CONNECTION:

                        if (!connected && e.id == 0)
                        {
                            IPAddress ip = sep.Address;
                            int port = serializer.DeserializeConnectionPort(e.data);
                            sep = new IPEndPoint(ip, port);
                            portIdx = sep.Port - 7401;
                            Debug.Log("New endpoint connection:" + sep.ToString());
                            lock (stateLock)
                            {
                                state = CONNECTION_STATE.CONNECTED;
                                connected = true;
                            }
                        }

                        break;
                    case EVENT_TYPE.EVENT_DISCONNETION:
                        lock (stateLock)
                        {
                            state = CONNECTION_STATE.DISCONNECTED;
                            connected = false;
                        }
                        break;
                    case EVENT_TYPE.EVENT_KEEPCONNECT:

                        if (e.id == 0)
                        {
                            byte[] data;
                            data = serializer.SerializeKeepConnect(myID);
                            lock (socketLock)
                            {
                                serverSocket.SendTo(data, SocketFlags.None, sep);
                            }
                        }

                        break;

                    case EVENT_TYPE.EVENT_DENIEDCONNECT:
                        lock (stateLock)
                        {
                            state = CONNECTION_STATE.FAILED;
                            connected = false;
                        }
                        break;

                    case EVENT_TYPE.EVENT_MESSAGE:
                        lock (messagesLock)
                        {
                            chatMessages.Enqueue(serializer.DeserializeChatMessage(e.data));

                        }
                        break;
                    case EVENT_TYPE.EVENT_UPDATE:
                        if (e.id == 0 && e.data.Length >= 31)
                        {
                            (byte netId, Vector3 posPitch, int state) transform = serializer.DeserializeTransform(e.data);
                            lock (clientWorldLock)
                            {
                                clientWorld.AddUpdateFutureTransform(transform.netId, transform.posPitch, transform.state);
                            }
                        }

                        break;
                    case EVENT_TYPE.EVENT_SPAWN_PLAYER:
                        //Deserialize Info index cosmetics and spawnpoint player
                        (byte objType, int[] indexs, int portId) info = serializer.DeserializeSpawnPlayerInfo(e.data, numCosmetis);
                        lock (clientWorldLock)
                        {
                            clientWorld.AddWorldObjectsPendingSpawn((byte)info.portId, info.objType, info.indexs, info.portId, null);
                        }
                        break;
                    case EVENT_TYPE.EVENT_READY_TO_PLAY:
                        // Call all functions suscribe to OnStart
                        if (OnStart != null)
                        {
                            OnStart.Invoke();
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }

    public int GetPortIdx()
    {
        return portIdx;
    }

    public string GetLastMessage()
    {
        if (chatMessages.Count > 0)
        {
            return chatMessages.Dequeue();
        }
        return "";
    }

    public void SendMessageToServer(string message)
    {
        byte[] data;
        data = serializer.SerializeChatMessage(myID, message);
        lock (socketLock)
        {
            serverSocket.SendTo(data, SocketFlags.None, sep);
        }
    }

    public void SendReadyToPlay(bool ready)
    {
        byte[] data;
        data = serializer.SerializeReadyToPlay(ready);
        lock (socketLock)
        {
            serverSocket.SendTo(data, SocketFlags.None, sep);
        }
    }
    public void SendDirToServer(byte netID, byte type, Vector2 dir)
    {
        // TODO move to world script
        byte[] data;
        data = serializer.SerializeDirection(myID, netID,type, dir);
        lock (socketLock)
        {
            serverSocket.SendTo(data, SocketFlags.None, sep);
        }
    }

    public void SendInfoSpawnToServer(int[] indexs, int portId)
    {
        byte[] data;
        data = serializer.SerializeSpawnPlayerInfo(myID, indexs, portId);
        lock (socketLock)
        {
            serverSocket.SendTo(data, SocketFlags.None, sep);
        }
    }

    public void DisconnectFromServer()
    {
        byte[] data;
        data = serializer.SerializeDisconnection(myID);
        if (isSocketAlive)
        {
            lock (socketLock)
            {
                serverSocket.SendTo(data, SocketFlags.None, sep);
            }
            serverSocket.Close();
            isSocketAlive = false;
        }
    }

    public void ShutdownClient()
    {
        if (threadProcess.IsAlive) threadProcess.Abort();
        if (threadRecieve.IsAlive) threadRecieve.Abort();
        DisconnectFromServer();
        Debug.Log("Shuttingdown udp client");
    }

    public int GetCurrentState()
    {
        int ret;
        lock (stateLock)
        {
            ret = ((int)state);
        }
        return ret;
    }

    private byte GetHostID()
    {
        IPHostEntry entry = Dns.GetHostEntry(Dns.GetHostName());
        for (int i = 0; i < entry.AddressList.Length; ++i)
        {
            if (entry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
            {
                host = entry.AddressList[i];
                int j = host.ToString().LastIndexOf(".");
                string tmp = host.ToString().Substring(j + 1);
                return byte.Parse(tmp);
            }
        }
        return 0;
    }

    private void OnDestroy()
    {
        ShutdownClient();
    }
}


