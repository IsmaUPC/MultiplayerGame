using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System;

public class UDPClient : MonoBehaviour
{
    IPEndPoint sep;
    EndPoint remote;
    Socket servSock;
    Thread threadConnect;

    int myID = 0;

    // Start is called before the first frame update
    void Start()
    {
        myID = GetHostID();

        servSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    }

    private bool ConnectToIp(string ip)
    {
        bool ret = false;

        sep = new IPEndPoint(IPAddress.Parse(ip), 9050);

        return ret;

    }

    private int GetHostID()
    {
        IPAddress host;
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


