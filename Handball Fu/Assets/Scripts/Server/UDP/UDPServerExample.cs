/*
C# Network Programming 
by Richard Blum

Publisher: Sybex 
ISBN: 0782141765
*/
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine.InputSystem;

public class UDPServerExample
{
    public static void Main()
    {
        int recv;
        byte[] data = new byte[1024];
        IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 9050);

        // Creation of a new IPV4 format socket, with Datagram type and UDP protocol.
        Socket newsock = new Socket(AddressFamily.InterNetwork,
                        SocketType.Dgram, ProtocolType.Udp);

        // Set bind with server IP and selected port
        newsock.Bind(ipep);
        Console.WriteLine("Waiting for a client...");

        // Fill IPEndPoint with empty IP and empty port
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        EndPoint remote = (EndPoint)(sender);

        // Fill variables data and remote with client through the socket binded 
        recv = newsock.ReceiveFrom(data, ref remote);


        // Print debug message with client information
        Console.WriteLine("Message received from {0}:", remote.ToString());
        Console.WriteLine(Encoding.ASCII.GetString(data, 0, recv));

        string welcome = "Welcome to my test server";
        data = Encoding.ASCII.GetBytes(welcome);
        // Send message to client 
        newsock.SendTo(data, data.Length, SocketFlags.None, remote);

        // Messaging server/client loop
        while (true)
        {
            // Clear the content of the data variable
            data = new byte[1024];
            recv = newsock.ReceiveFrom(data, ref remote);

            Console.WriteLine(Encoding.ASCII.GetString(data, 0, recv));
            newsock.SendTo(data, recv, SocketFlags.None, remote);
        }
    }
}