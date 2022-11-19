using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.IO;
using System.Text;
using System.Linq;

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
        writer.Write(color.ToString()+username);
        byte[] aux = new byte[actualMessage.Length - 2];
        Array.Copy(actualMessage, 2, aux, 0, aux.Length);

        IEnumerable<byte> ret = writeStream.ToArray().Concat(aux);
        return ret.ToArray();
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

    public byte[] SerializePlayerCreation(int id, char type, ref Transform t)
    {
        byte[] bytes = new byte[2];
        bytes[0] = 4;


        return bytes;
    }

    public byte[] SerializeTransform(int id, char type, int netId, Transform t)
    {

        InitializeWriter();
        double x = t.position.x;
        double z = t.position.z;

        double rx = t.eulerAngles.x;
        double ry = t.eulerAngles.y;
        double rz = t.eulerAngles.z;

        writer.Write(id);
        writer.Write(type);
        writer.Write(netId);

        writer.Write(x);
        writer.Write(z);

        writer.Write(rx);
        writer.Write(ry);
        writer.Write(rz);

        return writeStream.ToArray();
    }

    // TODO
    public void DeserializeTransform(byte[] data)
    {
        InitializeReader(data, 2);

        float x = (float)reader.ReadDouble();
        float z = (float)reader.ReadDouble();

        float rx = (float)reader.ReadDouble();
        float ry = (float)reader.ReadDouble();
        float rz = (float)reader.ReadDouble();

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
