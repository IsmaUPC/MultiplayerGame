using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.IO;

public class Serialization : MonoBehaviour
{
    static MemoryStream readStream;
    static MemoryStream writeStream;
    BinaryWriter writer;
    BinaryReader reader;
    byte[] bytes;

    public byte[] GetReaderStreamBytes()
    {
        return readStream.ToArray();
    }

    public (byte, char) DeserializeHeader(byte[] data)
    {
        InitializeReader(data);

        byte id = reader.ReadByte();
        char type = reader.ReadChar();

        return (id, type);
    }

    public byte[] SerializeConnection(byte id, string name)
    {
        InitializeWriter();

        writer.Write(id);
        writer.Write('C');
        writer.Write(name);

        return writeStream.ToArray();
    }

    public byte[] SerializeConnection(byte id, int port)
    {
        InitializeWriter();

        writer.Write(id);
        writer.Write('C');
        writer.Write(port);

        return writeStream.ToArray();
    }
    public byte[] SerializeSpawnPlayerInfo(byte myId, int[] index, int portId)
    {
        InitializeWriter();

        writer.Write(myId);
        writer.Write('S');
        writer.Write((byte)0); // 0 = PLAYER TYPE
        foreach (var i in index)
        {
            writer.Write(i);
        }

        writer.Write(portId);

        return writeStream.ToArray();
    }

    public (byte, int[], int) DeserializeSpawnPlayerInfo(byte[] data, int cosmeticLength)
    {
        InitializeReader(data, 2);
        byte objType = reader.ReadByte();
        int[] newlist = new int[cosmeticLength];
        for (int i = 0; i < cosmeticLength; i++)
        {
            newlist[i] = reader.ReadInt32();
        }
        return (objType, newlist, reader.ReadInt32());
    }

    public byte[] SerializeDeniedConnection()
    {
        InitializeWriter();

        writer.Write(0);
        writer.Write('F');

        return writeStream.ToArray();
    }

    public string DeserializeUsername(byte[] data)
    {
        InitializeReader(data, 2);

        return reader.ReadString();
    }

    public byte[] SerializeKeepConnect(byte id)
    {
        InitializeWriter();

        writer.Write(id);
        writer.Write('K');

        return writeStream.ToArray();
    }

    public byte[] SerializeChatMessage(int color, string username, byte[] actualMessage)
    {
        InitializeWriter();

        writer.Write(((byte)0));
        writer.Write('M');
        writer.Write(color.ToString() + username + AuxiliarDeserializeMessage(actualMessage));

        return writeStream.ToArray();
    }

    public byte[] SerializeReadyToPlay(bool ready)
    {
        InitializeWriter();

        writer.Write(((byte)0));
        writer.Write('R');
        writer.Write(ready);

        return writeStream.ToArray();
    }

    public bool DeserializeReadyToPlay(byte[] data)
    {
        InitializeReader(data, 2);

        return reader.ReadBoolean();
    }

    public string AuxiliarDeserializeMessage(byte[] m)
    {
        MemoryStream ms = new MemoryStream(m);

        BinaryReader br = new BinaryReader(ms);
        ms.Seek(2, SeekOrigin.Begin);

        string aux = br.ReadString();
        return aux;
    }

    public byte[] SerializeChatMessage(byte id, string message)
    {
        InitializeWriter();

        writer.Write(id);
        writer.Write('M');
        writer.Write(message);

        return writeStream.ToArray();
    }

    public byte[] SerializeDisconnection(byte id)
    {
        InitializeWriter();

        writer.Write(id);
        writer.Write('D');

        return writeStream.ToArray();
    }

    public string DeserializeChatMessage(byte[] data)
    {
        InitializeReader(data, 2);
        string a = reader.ReadString();
        return a;
    }

    public int DeserializeConnectionPort(byte[] data)
    {
        InitializeReader(data, 2);
        return reader.ReadInt32();
    }


    public byte[] SerializeDirection(byte id, byte type, Vector2 dir)
    {
        InitializeWriter();
        writer.Write(id);
        writer.Write('U');
        writer.Write(type);
        writer.Write((double)dir.x);
        writer.Write((double)dir.y);

        return writeStream.ToArray();
    }

    public (byte, Vector2) DeserializeDirection(byte[] data)
    {
        InitializeReader(data, 2);
        byte type = reader.ReadByte();
        float x = (float)reader.ReadDouble();
        float y = (float)reader.ReadDouble();

        return (type, new Vector2(x, y));
    }

    public byte[] SerializeTransform(byte id, int netId, Vector3 trans, int state)
    {
        InitializeWriter();
        writer.Write(id);
        writer.Write('U');

        writer.Write(netId);
        writer.Write((double)trans.x);
        writer.Write((double)trans.y);
        writer.Write((double)trans.z);
        writer.Write(state);

        return writeStream.ToArray();
    }

    // TODO Transform
    public (byte, Vector3, int) DeserializeTransform(byte[] data)
    {
        InitializeReader(data, 2);

        byte netId = reader.ReadByte();

        float x = (float)reader.ReadDouble();
        float pitch = (float)reader.ReadDouble();
        float z = (float)reader.ReadDouble();
        int state = reader.ReadInt32();

        return (netId, new Vector3(x, pitch, z), state);
    }

    private void InitializeReader(byte[] data, int pos = 0)
    {
        readStream = new MemoryStream(data);

        reader = new BinaryReader(readStream);
        readStream.Seek(pos, SeekOrigin.Begin);
    }

    private void InitializeWriter()
    {
        bytes = new byte[0];
        writeStream = new MemoryStream();
        writer = new BinaryWriter(writeStream);
    }


}
