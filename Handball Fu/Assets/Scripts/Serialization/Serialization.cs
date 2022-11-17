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


        //TODO:
        writer.Write(DataInt);
        bytes = stream.ToArray();
    }

    public void Deserialize()
    {

        InitializeReader();

        Debug.Log(reader.ReadInt32());


    }

    public byte[] SerializePosition(int id, char type, int netId, Transform pos)
    {

        InitializeWriter();
        float x = pos.position.x;
        float z = pos.position.z;

        float rx = pos.eulerAngles.x;
        float ry = pos.eulerAngles.y;
        float rz = pos.eulerAngles.z;

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

    private void InitializeReader()
    {
        stream = new MemoryStream();
        stream.Write(bytes, 0, bytes.Length);

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
