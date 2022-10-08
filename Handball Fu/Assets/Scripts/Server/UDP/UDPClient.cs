
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class SimpleUdpClient
{
    public static void Main()
    {
        byte[] data = new byte[1024];
        string input, stringData;

        // Set IP adress of server
        IPEndPoint ipep = new IPEndPoint(
                        IPAddress.Parse("127.0.0.1"), 9050);

        // Create Socket whit IPv4 
        Socket server = new Socket(AddressFamily.InterNetwork,
                       SocketType.Dgram, ProtocolType.Udp);

        // The client sends a message to ask that the server is listening
        string welcome = "Hello, are you there?";
        data = Encoding.ASCII.GetBytes(welcome);
        
        // Client send data to server with data length and flags to ip end point of server
        server.SendTo(data, data.Length, SocketFlags.None, ipep);

        // instantiates an IP endpoint variable with an ip 0.0.0.0 which will later
        // be filled in by the ReciveFrom function.
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        EndPoint Remote = (EndPoint)sender;

        // Clear data
        data = new byte[1024];
        // Obtain server information and fill the variable named Remote
        int recv = server.ReceiveFrom(data, ref Remote);

        // Print message from server
        Console.WriteLine("Message received from {0}:", Remote.ToString());
        Console.WriteLine(Encoding.ASCII.GetString(data, 0, recv));

        while (true)
        {
            // Wait for input client
            input = Console.ReadLine();
            if (input == "exit")
                break;

            //Send input client to server
            server.SendTo(Encoding.ASCII.GetBytes(input), Remote);
            data = new byte[1024];

            // Fill Remote reference variable and data
            recv = server.ReceiveFrom(data, ref Remote);
            stringData = Encoding.ASCII.GetString(data, 0, recv);

            // Print Message of server
            Console.WriteLine(stringData);
        }
        Console.WriteLine("Stopping client");
        server.Close();
    }
}




