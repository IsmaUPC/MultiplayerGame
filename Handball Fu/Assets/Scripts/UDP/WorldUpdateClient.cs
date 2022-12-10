using System.Collections;
using System.Collections.Generic;
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
        public Transform futureTransform;

        // Creator id
        public bool isMyObject;

        // Time since last update to clients
        public float deltaLastTime;

        public bool atTargetTransform;
    }

    // World objects
    private List<WorldObject> worldObjects = new List<WorldObject>();

    private float interpolationTime;

    private UDPClient client;

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
        // Iterate world objects and get interpolated transform
        for (int i = 0; i < worldObjects.Count; ++i)
        {
            worldObjects[i].deltaLastTime += Time.deltaTime;
            if (worldObjects[i].deltaLastTime < interpolationTime && !worldObjects[i].atTargetTransform)
            {
                worldObjects[i].obj.transform.position = Vector3.Lerp(worldObjects[i].pastTransform.position, worldObjects[i].futureTransform.position, worldObjects[i].deltaLastTime / interpolationTime);
                worldObjects[i].obj.transform.rotation = Quaternion.Lerp(worldObjects[i].pastTransform.rotation, worldObjects[i].futureTransform.rotation, worldObjects[i].deltaLastTime / interpolationTime);
            }
            if (!worldObjects[i].atTargetTransform && (Vector3.Distance(worldObjects[i].obj.transform.position, worldObjects[i].futureTransform.position) < 0.01F || worldObjects[i].deltaLastTime >= interpolationTime))
            {
                worldObjects[i].atTargetTransform = true;
                worldObjects[i].obj.transform.position = worldObjects[i].futureTransform.position;
                worldObjects[i].obj.transform.rotation = worldObjects[i].futureTransform.rotation;
            }
        }
    }

    public void UpdateFutureTransform(byte netID, Transform tform)
    {
        for (int i = 0; i < worldObjects.Count; ++i)
        {
            if (worldObjects[i].netId == netID)
            {
                worldObjects[i].pastTransform = worldObjects[i].futureTransform;
                worldObjects[i].futureTransform = tform;
                worldObjects[i].deltaLastTime = 0.0F;
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

    public void CreateWorldObject(byte netID, byte type, bool isMyObject, Transform tform)
    {
        WorldObject wo = new WorldObject();

        wo.netId = netID;
        wo.isMyObject = isMyObject;
        // TODO NET: If structure, depending on type, create specific prefab with specific cosmetics and the specific given transform

        worldObjects.Add(wo);
    }
}
