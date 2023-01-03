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
        public GameObject parent;
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
    private Queue<KeyValuePair<byte, byte>> notifyWorldObjQueue;

    private float interpolationTime;

    private UDPClient client;

    private GameObject ownPlayerRef;

    private PlayerSpawner ps;

    private object worldObjQueueLock = new object();
    private object updateWorldObjQueueLock = new object();
    private object notifyWorldObjQueueLock = new object();

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
        lock (notifyWorldObjQueueLock)
        {
            notifyWorldObjQueue = new Queue<KeyValuePair<byte, byte>>();
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
        lock (notifyWorldObjQueueLock)
        {
            while (notifyWorldObjQueue.Count > 0)
                ProcessNotification(notifyWorldObjQueue.Dequeue());
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

    public void AddNotify(byte notifyType, byte netID)
    {
        KeyValuePair<byte, byte> up = new KeyValuePair<byte, byte>(notifyType, netID);
        lock (notifyWorldObjQueueLock)
        {
            notifyWorldObjQueue.Enqueue(up);
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

    private void ProcessNotification(KeyValuePair<byte, byte> notify)
    {
        switch (notify.Key)
        {
            case 0:
                GetObjectWithNetID(notify.Value).GetComponent<Rigidbody>().useGravity = true;
                break;
            default:
                break;
        }
    }
    public void UpdateFutureTransform(TransformUpdate up)
    {
        // Is valid solution?
        if (float.IsInfinity(up.tform.y) || up.tform.x > 5000 || up.tform.z > 5000  || !float.IsFinite(up.tform.x) ||!float.IsFinite(up.tform.z))
            return;

        for (int i = 0; i < worldObjects.Count; ++i)
        {
            if (worldObjects[i].netId == up.netID)
            {
                worldObjects[i].pastTransform.position = worldObjects[i].futurePosition;
                worldObjects[i].pastTransform.rotation = worldObjects[i].futureRotation;

                worldObjects[i].futurePosition = new Vector3(up.tform.x, worldObjects[i].obj.transform.position.y, up.tform.z);
                worldObjects[i].futureRotation = Quaternion.Euler(0, up.tform.y, 0);

                worldObjects[i].deltaLastTime = 0.0f;
                worldObjects[i].atTargetTransform = false;

                if (worldObjects[i].netId < 9) // Minus 9 mean type = PLAYER
                {
                    int isStill = (worldObjects[i].futurePosition == worldObjects[i].pastTransform.position) ? 0 : 1;
                    worldObjects[i].obj.GetComponent<PlayerController>().UpdateAnimation(up.state, isStill);
                }
                if(worldObjects[i].netId > 9 && worldObjects[i].netId <= 60)
                {
                    if (worldObjects[i].obj.GetComponent<Rigidbody>().useGravity == true)
                        worldObjects[i].futurePosition.y = 0;
                }

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
    public void DestroyWorldObjectByGameObject(GameObject obj)
    {
        for (int i = 0; i < worldObjects.Count; ++i)
        {
            if (worldObjects[i].obj == obj)
            {
                if (worldObjects[i].obj != null)
                {
                    Destroy(worldObjects[i].obj);
                }
                worldObjects.RemoveAt(i);
                break;
            }
        }
    }

    // Save a reference to call events from server
    public void AssignUDPClientReference(UDPClient udp)
    {
        client = udp;
    }

    public void AddWorldObjectsPendingSpawn(byte netID, byte type, int[] cosmeticsIdxs, int portID, byte idParent = 0)
    {
        WorldObjInfo wops = new WorldObjInfo();
        wops.type = type;
        wops.isMyObject = client.GetPortIdx() == portID;
        wops.netID = (netID == 0) ? (byte)portID : netID;
        wops.portID = portID;
        wops.idxs = cosmeticsIdxs;
        if (netID > 9)
            wops.parent = GetObjectWithNetID(idParent);

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
                break;

            // Case 1 used for projectile game objects
            case 1:
                wo.obj = woi.parent.GetComponent<PlayerController>().SpawnProjectile();
                break;
            default:
                break;
        }
        wo.futurePosition = wo.obj.transform.position;
        wo.futureRotation = wo.obj.transform.rotation;
        if (woi.tform == null)
            wo.pastTransform = wo.obj.transform;

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

    private GameObject GetObjectWithNetID(byte NetId)
    {
        for (int i = 0; i < worldObjects.Count; ++i)
        {
            if (worldObjects[i].netId == NetId)
                return worldObjects[i].obj;
        }
        return null;
    }
}
