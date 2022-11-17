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
    BinaryReader reader;
    byte[] bytes;

    public void Serialize(int DataInt)
    {
        bytes= new byte[0];
        stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);


        //TODO:
        writer.Write(DataInt);
        bytes = stream.ToArray();
    }
    public void Deserialize( )
    {

        stream = new MemoryStream();
        stream.Write(bytes, 0, bytes.Length);

        BinaryReader reader = new BinaryReader(stream);
        stream.Seek(0, SeekOrigin.Begin);

        Debug.Log(reader.ReadInt32());

         
    }

}
