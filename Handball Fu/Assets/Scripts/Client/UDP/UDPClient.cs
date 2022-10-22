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

    enum CONNECTION_STATE
    {
        CONNECTED,
        DISCONNECTED,
        CONNECTING,
        FAILED
    }

    IPAddress host;
    IPEndPoint sep;
    EndPoint remote;
    Socket servSock;
    Thread threadConnect;

    int myID = 0;

    private CONNECTION_STATE state;

    // Start is called before the first frame update
    void Start()
    {
        myID = GetHostID();

        servSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        state = CONNECTION_STATE.DISCONNECTED;
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

        string tmp = myID.ToString()+"C";
        byte[] data = new byte[4];
        data = Encoding.ASCII.GetBytes(tmp);
        servSock.SendTo(data, data.Length, SocketFlags.None, sep);

        return true;

    }

    public int GetCurrentState()
    {
        return ((int)state);
    }

    private int GetHostID()
    {
        IPHostEntry entry = Dns.GetHostEntry(Dns.GetHostName());
        for (int i = 0; i < entry.AddressList.Length; ++i)
        {
            if (entry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
            {
                host = entry.AddressList[i];
                int j = host.ToString().LastIndexOf(".");
                return int.Parse(host.ToString().Substring(j + 1));
            }
        }
        return 0;
    }
}


