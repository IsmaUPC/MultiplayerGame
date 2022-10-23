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
        EVENT_CONNECTION,       // A client wants to connect
        EVENT_DISCONNETION,     // A client wants to disconnect
        EVENT_DENIEDCONNECT,    // No more client free spaces
        EVENT_KEEPCONNECT,      // A client is still connected
        EVENT_MESSAGE,          // A client sent a message
        EVENT_NAMES,            // Send client usernames
        EVENT_UPDATE,           // A client sent an updated "transform"
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
        public string data;     // Event data itself
    }

    IPAddress host;
    IPEndPoint sep;
    EndPoint remote;
    Socket serverSocket;
    Thread threadRecieve;
    Thread threadProcess;

    private object socketLock = new object();
    private object eventQueueLock = new object();
    private object stateLock = new object();

    string myID;

    private CONNECTION_STATE state;
    private Queue<Event> eventQueue;

    private float timeOut;

    // Start is called before the first frame update
    public void ClientStart()
    {
        myID = GetHostID();

        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        state = CONNECTION_STATE.DISCONNECTED;

        eventQueue = new Queue<Event>();

        timeOut = 5.0F;

        threadProcess = new Thread(ThreadProcessData);
        threadRecieve = new Thread(ThreadRecieveData);

    }

    void Update()
    {
        CONNECTION_STATE st;
        lock(stateLock)
        {
            st = state;
        }
        if (st == CONNECTION_STATE.CONNECTING)
        {
            timeOut -= Time.deltaTime;
            if(timeOut < 0.0F)
            {
                lock(stateLock)
                {
                    state = CONNECTION_STATE.FAILED;
                }
            }
        }
    }

    public bool ConnectToIp(string ip)
    {
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

        sep = new IPEndPoint(currentIP, 9050);
        EndPoint ep = (EndPoint)sep;

        string tmp = myID+"C";
        byte[] data = new byte[1024];
        data = Encoding.ASCII.GetBytes(tmp);
        Debug.Log(tmp + " " + data.Length.ToString() + " " + sep.Address.ToString()) ;
        serverSocket.SendTo(data, data.Length, SocketFlags.None, ep);

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
                        e = new Event();
                        e.type = EVENT_TYPE.EVENT_CONNECTION;
                        e.data = tmpMessage;
                        lock (eventQueueLock)
                        {
                            eventQueue.Enqueue(e);
                        }
                        break;
                    case 'D':
                        e = new Event();
                        e.type = EVENT_TYPE.EVENT_DISCONNETION;
                        e.data = tmpMessage;
                        lock (eventQueueLock)
                        {
                            eventQueue.Enqueue(e);
                        }
                        break;
                    case 'F':
                        e = new Event();
                        e.type = EVENT_TYPE.EVENT_DENIEDCONNECT;
                        e.data = tmpMessage;
                        lock (eventQueueLock)
                        {
                            eventQueue.Enqueue(e);
                        }
                        break;
                    case 'K':
                        e = new Event();
                        e.type = EVENT_TYPE.EVENT_KEEPCONNECT;
                        e.data = tmpMessage;
                        lock (eventQueueLock)
                        {
                            eventQueue.Enqueue(e);
                        }
                        break;
                    case 'M':
                        e = new Event();
                        e.type = EVENT_TYPE.EVENT_MESSAGE;
                        e.data = tmpMessage;
                        lock (eventQueueLock)
                        {
                            eventQueue.Enqueue(e);
                        }
                        break;
                    case 'U':
                        e = new Event();
                        e.type = EVENT_TYPE.EVENT_UPDATE;
                        e.data = tmpMessage;
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

    private void ThreadProcessData()
    {
        while(true)
        {
            Queue<Event> events;
            lock (eventQueueLock)
            {
                events = eventQueue;
                eventQueue.Clear();
            }
            while (events.Count > 0)
            {
                Event e = events.Dequeue();
                switch (e.type)
                {
                    case EVENT_TYPE.EVENT_CONNECTION:

                        if(e.data.Substring(0,3) == "000")
                        {
                            IPAddress ip = sep.Address;
                            sep = new IPEndPoint(ip, int.Parse(e.data.Substring(4, 4)));
                            Debug.Log("New endpoint connection:" + sep.ToString());
                            lock(stateLock)
                            {
                                state = CONNECTION_STATE.CONNECTED;
                            }
                        }

                        break;
                    case EVENT_TYPE.EVENT_DISCONNETION:
                        lock (stateLock)
                        {
                            state = CONNECTION_STATE.CONNECTED;
                        }
                        break;
                    case EVENT_TYPE.EVENT_KEEPCONNECT:

                        if (e.data.Substring(0, 3) == "000")
                        {
                            byte[] data = new byte[4];
                            string tmp = myID + "K";
                            data = Encoding.ASCII.GetBytes(tmp);
                            lock (socketLock)
                            {
                                serverSocket.SendTo(data, SocketFlags.None, sep);
                            }
                        }

                        break;
                    case EVENT_TYPE.EVENT_NAMES:
                        break;

                    case EVENT_TYPE.EVENT_DENIEDCONNECT:
                        lock (stateLock)
                        {
                            state = CONNECTION_STATE.FAILED;
                        }
                        break;

                    case EVENT_TYPE.EVENT_MESSAGE:
                        // TODO print message

                        break;
                    default:
                        break;
                }
            }
        }
    }

    public void ShutdownClient()
    {
        if (threadProcess.IsAlive) threadProcess.Abort();
        if (threadRecieve.IsAlive) threadRecieve.Abort();
        serverSocket.Close();
        Debug.Log("Shuttingdown udp client");
    }

    public int GetCurrentState()
    {
        int ret;
        lock(stateLock)
        {
            ret = ((int)state);
        }
        return ret;
    }

    private string GetHostID()
    {
        IPHostEntry entry = Dns.GetHostEntry(Dns.GetHostName());
        for (int i = 0; i < entry.AddressList.Length; ++i)
        {
            if (entry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
            {
                host = entry.AddressList[i];
                int j = host.ToString().LastIndexOf(".");
                string tmp = host.ToString().Substring(j + 1);
                if (tmp.Length == 1) tmp = "00" + tmp;
                if (tmp.Length == 2) tmp = "0" + tmp;
                return tmp;
            }
        }
        return "";
    }
}


