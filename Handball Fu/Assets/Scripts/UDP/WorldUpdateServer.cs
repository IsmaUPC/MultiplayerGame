using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldUpdateServer : MonoBehaviour
{
    // Server world object
    public class WorldObject
    {
        // This two together identify the world object
        public byte netId;

        // Gameobject reference
        public GameObject obj;

        // Creator id
        public byte clientCreator;

        // Time since last update to clients
        public float deltaLastTime;

        // Player type
        public byte type;
    }

    // All world objects to be updated in clients
    public List<WorldObject> worldObjects;

    // Interpolation time - How often to deliver new positions
    private float interpolationTime;

    private UDPServer server;

    public GameObject playerPrefab;
    public GameObject projectilePrefab;

    private bool[] usedIDs;

    // Start is called before the first frame update
    void Start()
    {
        // 50 ms
        interpolationTime = 0.050F;

        worldObjects = new List<WorldObject>();

        usedIDs = new bool[256];

    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < worldObjects.Count; ++i)
        {
            worldObjects[i].deltaLastTime += Time.deltaTime;
            switch (worldObjects[i].type)
            {
                case 0:
                    worldObjects[i].obj.GetComponent<PlayerController>().UpdateMove();
                    break;
                default:
                    break;
            }

            // If it's been more time than the interpolation time designed
            if (worldObjects[i].deltaLastTime > interpolationTime)
            {
                server.BroadcastInterpolation(worldObjects[i].netId, GetDataUpdateTransform(worldObjects[i].obj.transform), (int)worldObjects[i].obj.GetComponent<PlayerController>().state);
                worldObjects[i].deltaLastTime = 0.0f;
            }
        }
    }

    private Vector3 GetDataUpdateTransform(Transform trans)
    {
        Vector3 posPitch;
        posPitch.x = trans.position.x;
        posPitch.y = trans.rotation.eulerAngles.y;
        posPitch.z = trans.position.z;

        return posPitch;
    }

    // Create world objects with determined netIDs
    public byte CreateWorldObject(byte type, byte clientCreator, Transform tform = null, byte portIDCreator = 0)
    {
        byte retID = 0;
        WorldObject wo = new WorldObject();
        wo.clientCreator = clientCreator;
        switch (type)
        {
            // Case 0 used for player game objects
            case 0:
                retID = portIDCreator;
                wo.obj = Instantiate(playerPrefab, tform);
                break;

            // Case 1 used for projectile game objects
            case 1:
                retID = AssignNetId(10, 60);
                wo.obj = Instantiate(projectilePrefab, tform);
                break;
            default:
                break;
        }
        wo.netId = retID;
        wo.deltaLastTime = 0.0F;
        worldObjects.Add(wo);

        Debug.Log("Network object with ID " + retID.ToString() + " created");

        return retID;
    }

    /*
     * Net IDs assigned as follows:
     * 0 is undefined
     * 1 to 9 are for player game objects
     * 10 to 29 are for projectile game objects
     */
    private byte AssignNetId(byte minB, byte maxB)
    {
        for (byte i = minB; i <= maxB; ++i)
        {
            if (!usedIDs[i])
            {
                usedIDs[i] = true;
                return i;
            }
        }
        return 0;
    }

    // TODO: Net id clear when objects are destroyed [all of them at once or a single one]
    public void DestroyAllObjects()
    {
        for (int i = worldObjects.Count - 1; i >= 0; --i)
        {
            if (worldObjects[i].obj != null)
            {
                Destroy(worldObjects[i].obj);
            }
            worldObjects.RemoveAt(i);
        }
        worldObjects.Clear();
    }

    public void DestroyWorldObject(byte netID)
    {
        for (int i = 0; i < worldObjects.Count; ++i)
        {
            if (worldObjects[i].netId == netID)
            {
                if (worldObjects[i].obj != null)
                {
                    Destroy(worldObjects[i].obj);
                }
                worldObjects.RemoveAt(i);
                Debug.Log("Network object with ID " + netID.ToString() + " destroyed");
                break;
            }
        }
    }

    public void UpdateWorldObject(byte netID, Vector3 deltaPosition, Vector3 eulerAngles, byte state)
    {
        for (int i = 0; i < worldObjects.Count; ++i)
        {
            if (worldObjects[i].netId == netID)
            {
                CharacterController auxPlayerController = worldObjects[i].obj.GetComponent<CharacterController>();
                auxPlayerController.Move(deltaPosition);
                worldObjects[i].obj.transform.rotation = Quaternion.Euler(eulerAngles);
                //worldObjects[i].state = state;
            }
        }
    }

    public void AssignUDPServerReference(UDPServer udp)
    {
        server = udp;
    }
}
