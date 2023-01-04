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
        if (data.Length < 2)
        {
            return (0, '\0');
        }
        InitializeReader(data);

        byte id = reader.ReadByte();
        char type = Convert.ToChar(reader.ReadByte()); // TODO: Solve error in this line. Unity says we want to decode in utf-8

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
    public byte[] SerializeRTT(byte idClient)
    {
        InitializeWriter();

        writer.Write(idClient);
        writer.Write('T');

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
    public byte[] SerializeSpawnObjectInfo(byte myId, byte type, int portId, int[] index = null, byte netIDParent = 0)
    {
        InitializeWriter();

        writer.Write(myId);
        writer.Write('S');
        writer.Write(type); // 0 = PLAYER TYPE  1 = FIST
        if(type == 0)
        {
            foreach (var i in index)
            {
                writer.Write(i);
            }
        }
        if(type == 1)
            writer.Write(netIDParent);

        writer.Write(portId);

        return writeStream.ToArray();
    }

    public (byte, int[], byte, int) DeserializeSpawnObjectInfo(byte[] data, int cosmeticLength)
    {
        InitializeReader(data, 2);
        byte objType = reader.ReadByte();
        int[] newlist = new int[cosmeticLength];
        if(objType == 0)
        {
            for (int i = 0; i < cosmeticLength; i++)
            {
                newlist[i] = reader.ReadInt32();
            }
        }
        byte idParent = 0;
        if(objType == 1)
            idParent = reader.ReadByte();

        return (objType, newlist, idParent, reader.ReadInt32());
    }
    public byte[] SerializeVictory(string names, string victories)
    {
        InitializeWriter();

        writer.Write((byte)0);
        writer.Write('W');
        writer.Write(names);
        writer.Write(victories);

        return writeStream.ToArray();
    }
    public (string, string) DeserializeVictory(byte[] data)
    {
        InitializeReader(data, 2);
        string names = reader.ReadString();
        string victories = reader.ReadString();

        return (names, victories);
    }

    public byte[] SerializeNotify(byte myId, byte type, byte portId, string names = "", string victories = "")
    {
        InitializeWriter();

        writer.Write(myId);
        writer.Write('N');
        writer.Write(type); // 0 = ACTIVE PUNCH GRAVITY || 1 = DESTROY OBJECT
        writer.Write(portId);

        return writeStream.ToArray();
    }
    public (byte, byte) DeserializeNotify(byte[] data)
    {
        InitializeReader(data, 2);
        byte notifyType = reader.ReadByte();
        byte netID = reader.ReadByte();

        return (notifyType, netID);
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


    public byte[] SerializeDirection(byte id, byte netId, byte type, int state, Vector2 dir)
    {
        InitializeWriter();
        writer.Write(id);
        writer.Write('U');
        writer.Write(netId);
        writer.Write(type);
        writer.Write(state);
        switch (state)
        {
            case 0:
                writer.Write((double)dir.x);
                writer.Write((double)dir.y);
                break;
            default:
                break;
        }

        return writeStream.ToArray();
    }

    public (byte, byte, int, Vector2) DeserializeDirection(byte[] data)
    {
        InitializeReader(data, 2);
        byte netID = reader.ReadByte();
        byte type = reader.ReadByte();
        int state = reader.ReadInt32();
        float x = 0.0f, y = 0.0f;
        switch (state)
        {
            case 0:
                x = (float)reader.ReadDouble();
                y = (float)reader.ReadDouble();
                break;
            default:
                break;
        }

        return (netID, type, state, new Vector2(x, y));
    }

    public byte[] SerializeTransform(byte id, byte netId, Vector3 trans, int state)
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
