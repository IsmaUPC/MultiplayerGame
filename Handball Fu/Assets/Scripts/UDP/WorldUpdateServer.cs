using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldUpdateServer : MonoBehaviour
{
    // Server world object
    struct WorldObject
    {
        // This two together identify the world object
        byte objectType;
        byte objectId;

        // Gameobject reference
        GameObject obj;

        // Creator id
        string id;
    }

    List<WorldObject> objects;

    // Start is called before the first frame update
    void Start()
    {
        objects = new List<WorldObject>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //public void CreateWorldObject(byte type, )
}
