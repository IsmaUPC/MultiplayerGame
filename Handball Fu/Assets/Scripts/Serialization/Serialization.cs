using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.IO;
using System.Text;

public class Serialization : MonoBehaviour
{
    int integer = 1;
    static MemoryStream stream;
    BinaryWriter writer;
    BinaryReader reader;
    byte[] bytes;

    public void Serialize(int DataInt)
    {
        InitializeWriter();
        writer.Write(DataInt);
        bytes = stream.ToArray();
    }

    public void Deserialize()
    {
        //InitializeReader();
        Debug.Log(reader.ReadInt32());
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

        bytes = stream.ToArray();

        return bytes;
    }
    public byte[] DeserializeTransform(byte[] data)
    {

        InitializeReader(data);
        int id= reader.ReadInt32();
        char type= reader.ReadChar();
        int netId= reader.ReadInt32();

        float x = (float)reader.ReadDouble();
        float z = (float)reader.ReadDouble();

        float rx = (float)reader.ReadDouble();
        float ry = (float)reader.ReadDouble();
        float rz = (float)reader.ReadDouble();

        return bytes;
    }



    private void InitializeReader(byte[] data)
    {
        stream = new MemoryStream();
        stream.Write(data, 0, data.Length);

        BinaryReader reader = new BinaryReader(stream);
        stream.Seek(0, SeekOrigin.Begin);
    }
    private void InitializeWriter()
    {
        bytes = new byte[0];
        stream = new MemoryStream();
        writer = new BinaryWriter(stream);
    }


}
