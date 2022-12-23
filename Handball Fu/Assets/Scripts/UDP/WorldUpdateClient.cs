using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WorldUpdateClient : MonoBehaviour
{
    // Client world object
    class WorldObject
    {
        // This two together identify the world object
        public byte netId;

        // Gameobject reference
        public GameObject obj;

        // Past transform
        public Transform pastTransform;

        // Future transform
        public Vector3 futurePosition;
        public Quaternion futureRotation;

        // Creator id
        public bool isMyObject;

        // Time since last update to clients
        public float deltaLastTime;

        public bool atTargetTransform;
    }

    public struct WorldObjInfo
    {
        public byte netID;
        public byte type;
        public bool isMyObject;
        public Transform tform;
        public int[] idxs;
        public int portID;
    }
    public struct TransformUpdate
    {
        public byte netID;
        public Vector3 tform;
        public int state;
    }

    // World objects
    private List<WorldObject> worldObjects = new List<WorldObject>();

    private Queue<WorldObjInfo> worldObjQueue;
    private Queue<TransformUpdate> updateWorldObjQueue;

    private float interpolationTime;

    private UDPClient client;

    private GameObject ownPlayerRef;

    private PlayerSpawner ps;

    private object worldObjQueueLock = new object();
    private object updateWorldObjQueueLock = new object();

    // Start is called before the first frame update
    void Start()
    {
        // 50 ms
        interpolationTime = 0.05f;

        worldObjects = new List<WorldObject>();

        lock (worldObjQueueLock)
        {
            worldObjQueue = new Queue<WorldObjInfo>();
        }
        lock (updateWorldObjQueueLock)
        {
            updateWorldObjQueue = new Queue<TransformUpdate>();
        }

        ps = FindObjectOfType<PlayerSpawner>();

    }

    // Update is called once per frame
    void Update()
    {
        lock (worldObjQueueLock)
        {
            while (worldObjQueue.Count > 0)
                CreateWorldObject(worldObjQueue.Dequeue());
        }

        lock (updateWorldObjQueueLock)
        {
            while (updateWorldObjQueue.Count > 0)
                UpdateFutureTransform(updateWorldObjQueue.Dequeue());
        }

        Interpolation();
    }

    private void Interpolation()
    {
        // Iterate world objects and get interpolated transform
        for (int i = 0; i < worldObjects.Count; ++i)
        {
            worldObjects[i].deltaLastTime += Time.deltaTime;
            if (worldObjects[i].deltaLastTime < interpolationTime && !worldObjects[i].atTargetTransform)
            {
                worldObjects[i].obj.transform.position = Vector3.Lerp(worldObjects[i].pastTransform.position, worldObjects[i].futurePosition, worldObjects[i].deltaLastTime / interpolationTime);
                worldObjects[i].obj.transform.rotation = Quaternion.Lerp(worldObjects[i].pastTransform.rotation, worldObjects[i].futureRotation, worldObjects[i].deltaLastTime / interpolationTime);
            }
            if (!worldObjects[i].atTargetTransform && (Vector3.Distance(worldObjects[i].obj.transform.position, worldObjects[i].futurePosition) < 0.01F || worldObjects[i].deltaLastTime >= interpolationTime))
            {
                worldObjects[i].atTargetTransform = true;
                worldObjects[i].obj.transform.position = worldObjects[i].futurePosition;
                worldObjects[i].obj.transform.rotation = worldObjects[i].futureRotation;
            }
        }
    }

    public void AddUpdateFutureTransform(byte netID, Vector3 tform, int state)
    {
        TransformUpdate up = new TransformUpdate();
        up.netID = netID;
        up.tform = tform;
        up.state = state;
        lock (updateWorldObjQueueLock)
        {
            updateWorldObjQueue.Enqueue(up);
        }
    }
    public void UpdateFutureTransform(TransformUpdate up)
    {
        for (int i = 0; i < worldObjects.Count; ++i)
        {
            if (worldObjects[i].netId == up.netID)
            {
                worldObjects[i].pastTransform.position = worldObjects[i].futurePosition;
                worldObjects[i].pastTransform.rotation = worldObjects[i].futureRotation;
                if (float.IsFinite(up.tform.x) && float.IsFinite(up.tform.z))
                {
                    worldObjects[i].futurePosition = new Vector3(up.tform.x, worldObjects[i].obj.transform.position.y, up.tform.z);
                }

                Debug.Log("up.tform.y value is: " + up.tform.y.ToString());
                worldObjects[i].futureRotation = Quaternion.Euler(0, up.tform.y, 0);

                worldObjects[i].deltaLastTime = 0.0f;
                worldObjects[i].atTargetTransform = false;
                break;
            }
        }
    }

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

    // Save a reference to call events from server
    public void AssignUDPClientReference(UDPClient udp)
    {
        client = udp;
    }

    public void AddWorldObjectsPendingSpawn(byte netID, byte type, int[] cosmeticsIdxs, int portID, Transform tform = null)
    {
        WorldObjInfo wops = new WorldObjInfo();
        wops.type = type;
        wops.isMyObject = client.GetPortIdx() == portID;
        wops.netID = (netID == 0) ? (byte)portID : netID;
        wops.tform = tform;
        wops.portID = portID;
        wops.idxs = cosmeticsIdxs;

        lock (worldObjQueueLock)
        {
            worldObjQueue.Enqueue(wops);
        }
    }

    private bool DoesNetIDExist(byte netID)
    {
        for (int i = 0; i < worldObjects.Count; ++i)
        {
            if (worldObjects[i].netId == netID)
            {
                return true;
            }
        }
        return false;
    }

    public void CreateWorldObject(WorldObjInfo woi)
    {
        if (DoesNetIDExist(woi.netID))
        {
            Debug.Log("Network object with id " + woi.netID.ToString() + " already exists");
            return;
        }
        WorldObject wo = new WorldObject();
        wo.netId = woi.netID;
        wo.isMyObject = woi.isMyObject;

        wo.pastTransform = woi.tform;
        wo.atTargetTransform = true;
        switch (woi.type)
        {
            // Case 0 used for player game objects
            case 0:
                if (woi.isMyObject)
                {
                    wo.obj = ownPlayerRef;
                    wo.netId = (byte)woi.portID;
                }
                else
                {
                    if (ps == null) ps = FindObjectOfType<PlayerSpawner>();

                    wo.obj = ps.SpawnNetPlayer(woi.idxs, woi.portID, true);
                }
                wo.futurePosition = wo.obj.transform.position;
                wo.futureRotation = wo.obj.transform.rotation;
                if (woi.tform == null)
                    wo.pastTransform = wo.obj.transform;

                break;

            // Case 1 used for projectile game objects
            case 1:
                //wo.obj = Instantiate(projectilePrefab, tform);
                break;
            default:
                break;
        }
        wo.deltaLastTime = 0.0F;
        worldObjects.Add(wo);

        Debug.Log("Network object with ID " + woi.netID.ToString() + " created");

    }

    public void SetPlayerCreationReference(byte netID, ref GameObject obj)
    {
        for (int i = 0; i < worldObjects.Count; ++i)
        {
            if (worldObjects[i].netId == netID)
            {
                worldObjects[i].obj = obj;
                break;
            }
        }
    }

    public void SetPlayerReference(GameObject obj)
    {
        ownPlayerRef = obj;
    }
}
