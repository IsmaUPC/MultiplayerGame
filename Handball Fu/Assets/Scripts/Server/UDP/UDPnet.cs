using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UDPnet : MonoBehaviour
{
    Socket newSocket;
    IPEndPoint ipep;
    IPEndPoint sender;

    Thread threadConnect;

    // Start is called before the first frame update
    void Start()
    {
        // Socket creation
        newSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        
        ipep = new IPEndPoint(IPAddress.Any, 9999);
        threadConnect = new Thread(ThreadNetConnect);

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ThreadNetConnect()
    {
        newSocket.Bind(ipep);
        Debug.Log("Waiting for a client...");
    }
}
