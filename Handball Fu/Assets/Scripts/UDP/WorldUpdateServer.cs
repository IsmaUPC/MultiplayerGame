using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEngine;

public class WorldUpdateServer : MonoBehaviour
{
    // Server world object
    class WorldObject
    {
        // This two together identify the world object
        public byte netId;

        // Gameobject reference
        public GameObject obj;

        // Creator id
        public byte clientCreator;

        // Time since last update to clients
        public float deltaLastTime;
    }

    // All world objects to be updated in clients
    private List<WorldObject> worldObjects;

    // Interpolation time - How often to deliver new positions
    private float interpolationTime;

    private UDPServer server;

    // Start is called before the first frame update
    void Start()
    {
        // 50 ms
        interpolationTime = 0.050F;


        worldObjects = new List<WorldObject>();

    }

    // Update is called once per frame
    void Update()
    {
        for(int i = 0; i < worldObjects.Count; ++i)
        {
            worldObjects[i].deltaLastTime += Time.deltaTime;

            // If it's been more time than the interpolation time designed
            if (worldObjects[i].deltaLastTime > interpolationTime)
            {
                server.BroadcastInterpolation(worldObjects[i].netId, worldObjects[i].obj.transform);
                worldObjects[i].deltaLastTime = 0.0F;
            }
        }
    }


    // TODO: Is called to create an object in world, then returns the netID to be sent to the client ho has created it
    // maybe, clients can wait for server confirmation and the server sends a "property bool" which tells if said client 
    // has the ownership
    public byte CreateWorldObject(byte type)
    {
        byte retID = 0;
        WorldObject wo = new WorldObject();
        //wo.obj = Instantiate();
        // TODO: Depending on type assing a net id
        // TODO: Create GameObject depending on cosmetics

        wo.deltaLastTime = 0.0F;
        worldObjects.Add(wo);

        Debug.Log("Network object with ID " + retID.ToString() + " created");

        return retID;
    }

    // TODO NET: Add function to delete all objects

    public void DestroyWorldObject(byte netID)
    {
        for (int i = 0; i < worldObjects.Count; ++i)
        {
            if (worldObjects[i].netId == netID)
            {
                Destroy(worldObjects[i].obj);
                worldObjects.RemoveAt(i);
                Debug.Log("Network object with ID " + netID.ToString() + " destroyed");
                break;
            }
        }
    }

    public void UpdateWorldObject(byte netID, Transform tform, Vector3 velocity)
    {
        for(int i = 0; i < worldObjects.Count; ++i)
        {
            if (worldObjects[i].netId == netID)
            {
                // TODO NET: change transform and velocity
                

                // Line below used to send interpolation position in next iteration
                worldObjects[i].deltaLastTime = interpolationTime;
                Debug.Log("Network object with ID " + netID.ToString() + " updated");
                break;
            }
        }
    }

    public void AssignUDPServerReference(UDPServer udp)
    {
        server = udp;
    }
}
