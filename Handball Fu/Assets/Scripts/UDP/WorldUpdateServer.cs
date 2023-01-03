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

    public class WorldObjInfo
    {
        public WorldObjInfo() { }
        public WorldObjInfo(byte t, byte c, int id, GameObject p)
        {
            type = t;
            clientCreator = c;
            portID = id;
            parent = p;
        }

        // Player type
        public byte type;
        // Creator id
        public byte clientCreator;
        // Trasnform //TODO: Change to posPitch
        public Transform trans;

        // This two together identify the world object
        public int portID;
        // Cosmetics Indexs
        public int[] cosmeticsIdxs;

        // Gameobject reference
        public GameObject parent;
    }

    // All world objects to be updated in clients
    public List<WorldObject> worldObjects;

    // All world objects to be swpawned in server,
    // this list is necesary because Instantiate function only work on main thread
    public List<WorldObjInfo> worldObjectsPendingSpawn;
    public Queue<KeyValuePair<int, Vector2>> updateDirection;
    public Queue<KeyValuePair<int, int>> updateState;

    // Interpolation time - How often to deliver new positions
    private float interpolationTime;

    private UDPServer server;

    public GameObject playerPrefab;
    public GameObject projectilePrefab;
    public PlayerSpawner ps;

    private bool[] usedIDs;

    private object worldObjectsPendingSpawnLock = new object();
    private object updateDirectionLock = new object();
    private object updateStateLock = new object();

    // Start is called before the first frame update
    void Start()
    {
        // 50 ms
        interpolationTime = 0.050F;

        worldObjects = new List<WorldObject>();
        lock (worldObjectsPendingSpawnLock)
        {
            worldObjectsPendingSpawn = new List<WorldObjInfo>();
        }
        lock (updateDirectionLock)
        {
            updateDirection = new Queue<KeyValuePair<int, Vector2>>();
        }
        lock(updateStateLock)
        {
            updateState = new Queue<KeyValuePair<int, int>>();
        }

        usedIDs = new bool[256];

        ps = FindObjectOfType<PlayerSpawner>();
    }

    // Update is called once per frame
    void Update()
    {
        lock (worldObjectsPendingSpawnLock)
        {
            for (int i = 0; i < worldObjectsPendingSpawn.Count; ++i)
            {
                CreateWorldObject(worldObjectsPendingSpawn[i]);
            }
            worldObjectsPendingSpawn.Clear();
        }

        lock (updateDirectionLock)
        {
            while (updateDirection.Count > 0)
            {
                KeyValuePair<int, Vector2> aux = updateDirection.Dequeue();
                worldObjects[aux.Key].obj.GetComponent<PlayerController>().Move(aux.Value);
            }
        }

        lock(updateStateLock)
        {
            while (updateState.Count > 0)
            {
                KeyValuePair<int, int> aux = updateState.Dequeue();
                switch (aux.Value)
                {
                    case 1:
                        worldObjects[aux.Key].obj.GetComponent<PlayerController>().ActiveDash();
                        break;
                    case 2:
                        worldObjects[aux.Key].obj.GetComponent<PlayerController>().Cut();
                        break;
                    default:
                        break;
                }                
            }
        }

        for (int i = 0; i < worldObjects.Count; ++i)
        {
            worldObjects[i].deltaLastTime += Time.deltaTime;
            switch (worldObjects[i].type)
            {
                case 0:
                    worldObjects[i].obj.GetComponent<PlayerController>().UpdateMove();
                    break;
                case 1:
                    worldObjects[i].obj.GetComponent<Projectile>().UpdateTransform();
                    break;
                default:
                    break;
            }

            // If it's been more time than the interpolation time designed
            if (worldObjects[i].deltaLastTime > interpolationTime)
            {
                int state = (worldObjects[i].type == 0)? (int)worldObjects[i].obj.GetComponent<PlayerController>().state : 0;
                server.BroadcastInterpolation(worldObjects[i].netId, GetDataUpdateTransform(worldObjects[i].obj.transform), state);
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

    public void AddWorldObjectsPendingSpawn(byte type, byte clientCreator, int portID, int[] cosmeticsIdxs = null)
    {
        WorldObjInfo wops = new WorldObjInfo();
        wops.type = type;
        wops.clientCreator = clientCreator;
        wops.portID = portID;
        wops.cosmeticsIdxs = cosmeticsIdxs;

        lock (worldObjectsPendingSpawnLock)
        {
            worldObjectsPendingSpawn.Add(wops);
        }
    }

    // Create world objects with determined netIDs
    public byte CreateWorldObject(WorldObjInfo wops)
    {
        byte retID = 0;
        WorldObject wo = new WorldObject();
        wo.clientCreator = wops.clientCreator;
        switch (wops.type)
        {
            // Case 0 used for player game objects
            case 0:
                wo.type = 0;
                retID = (byte)wops.portID;
                if (ps == null) ps = FindObjectOfType<PlayerSpawner>();
                wo.obj = ps.SpawnNetPlayer(wops.cosmeticsIdxs, wops.portID, true);
                break;

            // Case 1 used for projectile game objects
            case 1:
                retID = AssignNetId(10, 60);
                wo.type = 1;
                wo.obj = wops.parent.GetComponent<PlayerController>().SpawnProjectile();
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

    public void DestroyAllObjects()
    {
        for (int i = worldObjects.Count - 1; i >= 0; --i)
        {
            if (worldObjects[i].obj != null)
            {
                Destroy(worldObjects[i].obj);
                usedIDs[worldObjects[i].netId] = false;
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
                    usedIDs[worldObjects[i].netId] = false;
                }
                worldObjects.RemoveAt(i);
                Debug.Log("Network object with ID " + netID.ToString() + " destroyed");
                break;
            }
        }
    }
    public void DestroyWorldObjectByGameObject(GameObject obj)
    {
        for (int i = 0; i < worldObjects.Count; ++i)
        {
            if (worldObjects[i].obj == obj)
            {
                if (worldObjects[i].obj != null)
                {
                    Destroy(worldObjects[i].obj);
                    usedIDs[worldObjects[i].netId] = false;
                }
                worldObjects.RemoveAt(i);
                break;
            }
        }
    }

    public void UpdateWorldObject(int index, int state, Vector2 dir)
    {
        switch (state)
        {
            case 0:
                lock (updateDirectionLock)
                {
                    updateDirection.Enqueue(new KeyValuePair<int, Vector2>(index, dir));
                }
                break;
            case 1:
            case 2:
                lock (updateStateLock)
                {
                    updateState.Enqueue(new KeyValuePair<int, int>(index, state));
                }
                break;
            default:
                break;
        }
    }

    public void AssignUDPServerReference(UDPServer udp)
    {
        server = udp;
    }

    public void AddSpawnPunch(GameObject obRef)
    {
        for (int i = 0; i < worldObjects.Count; ++i)
        {
            if(worldObjects[i].obj == obRef)
            {
                byte netId = CreateWorldObject(new WorldObjInfo(1, worldObjects[i].clientCreator, worldObjects[i].netId, obRef));
                byte[] data;
                lock (server.serializerLock)
                {
                    data = server.serializer.SerializeSpawnObjectInfo(worldObjects[i].clientCreator, 1, netId, null, worldObjects[i].netId);
                }
                server.AddNotifyEnqueueEvent(data);
                break;
            }
        }
    }
    public void ActiveGravityPunch(GameObject obRef)
    {
        for (int i = 0; i < worldObjects.Count; ++i)
        {
            if (worldObjects[i].obj == obRef)
            {
                byte[] data;
                lock (server.serializerLock)
                {
                    data = server.serializer.SerializeNotify(worldObjects[i].clientCreator, 0, worldObjects[i].netId);
                }
                server.AddNotifyEnqueueEvent(data);
                break;
            }
        }
    }
}
